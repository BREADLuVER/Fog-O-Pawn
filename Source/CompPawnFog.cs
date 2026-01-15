using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.Grammar;
using UnityEngine;

namespace FogOfPawn
{
    public class CompProperties_PawnFog : CompProperties
    {
        public CompProperties_PawnFog()
        {
            compClass = typeof(CompPawnFog);
        }
    }

	public class CompPawnFog : ThingComp
    {
        // Meta
        public bool compInitialized;
        public int ticksSinceJoin;
        public float truthfulness; // legacy, no longer used
        private bool disguiseKitSpawned;

        // Skills - OLD format (keep for migration)
        public Dictionary<SkillDef, float?> reportedSkills = new Dictionary<SkillDef, float?>();
        public Dictionary<SkillDef, Passion?> reportedPassions = new Dictionary<SkillDef, Passion?>();
        public HashSet<SkillDef> revealedSkills = new HashSet<SkillDef>();

        // Skills - NEW format (offset-based)
        public Dictionary<SkillDef, int> maskOffsets = new Dictionary<SkillDef, int>();
        public Dictionary<SkillDef, int> passionOffsets = new Dictionary<SkillDef, int>(); // -1=remove passion, 0=no change, 1=add minor, 2=add major
        
        // Migration flag
        public bool migratedToOffsets = false;

        // Traits
        public HashSet<TraitDef> revealedTraits = new HashSet<TraitDef>();
        
        // Change detection for auto-reveal
        private HashSet<TraitDef> lastKnownTraits = new HashSet<TraitDef>();
        private int lastTraitCheckTick = -1;
        
        // Health & Genes
        public bool healthRevealed;
        public bool genesRevealed;

        public DeceptionTier tier = DeceptionTier.Truthful;
        public bool tierManuallySet;

        public bool fullyRevealed;
        // Set once the imposter has been killed, banished or otherwise removed and
        // the colony‐wide relief thought has already been given. Prevents double applying.
        public bool outcomeProcessed;
        // legacy: lastInterrogatedTick used by removed Interrogate feature (kept for save compatibility)
        public int lastInterrogatedTick;
        public bool wasPlayerColonist;

        // transient counters used by reveal logic (not saved)
        [System.NonSerialized]
        public System.Collections.Generic.Dictionary<string, float> tempData = new System.Collections.Generic.Dictionary<string, float>();

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            // Add the player-facing Accuse gizmo for ALL colonists (not just deceivers)
            // This is critical - if we only show it for deceivers, it reveals who's lying!
            if (!fullyRevealed && parent.Faction?.IsPlayer == true && compInitialized)
            {
                var accuseCmd = new Command_Action
                {
                    defaultLabel = "FogOfPawn.Accuse.Label".Translate(),
                    defaultDesc = "FogOfPawn.Accuse.Desc".Translate(),
                    icon = ContentFinder<Texture2D>.Get("Things/Item/disguise-kit", false) ?? ContentFinder<Texture2D>.Get("UI/Commands/LaunchReport", true),
                    action = HandleAccuse
                };

                // Disable if any masked skill has been revealed, as the player already has proof of deception
                if (maskOffsets.Any(kv => kv.Value != 0 && revealedSkills.Contains(kv.Key)))
                {
                    accuseCmd.Disable("FogOfPawn.Accuse.DisabledRevealed".Translate());
                }

                yield return accuseCmd;
            }

            if (!Prefs.DevMode) yield break;

            yield return new Command_Action
            {
                defaultLabel = "Dev: Reset Fog",
                defaultDesc = "Clears all discovered attributes so this pawn is fully fogged again.",
                icon = null,
                action = ResetFog
            };

            // Quick XP injection for testing the reveal threshold.
            yield return new Command_Action
            {
                defaultLabel = $"Dev: +{FogSettingsCache.Current.xpToReveal} Shooting XP",
                defaultDesc = "Adds enough XP to the Shooting skill to cross the reveal threshold once.",
                icon = null,
                action = () =>
                {
                    if (parent is Pawn pawn && pawn.skills != null)
                    {
                        var sk = pawn.skills.GetSkill(RimWorld.SkillDefOf.Shooting);
                        float factor = pawn.GetStatValue(RimWorld.StatDefOf.GlobalLearningFactor);
                        float rawXp = FogSettingsCache.Current.xpToReveal / factor + 10f;
                        sk.Learn(rawXp, direct: true);
                        FogLog.Verbose($"Dev: Added XP to {pawn.LabelShort}'s Shooting skill. Current XP since last level: {sk.xpSinceLastLevel}");
                    }
                }
            };

            yield return new Command_Action
            {
                defaultLabel = "Dev: Try Social Reveal",
                defaultDesc = "Force one social-style reveal roll (100% chance).",
                icon = null,
                action = () => {
                    if (parent is Pawn pawnParent)
                        FogUtility.RevealRandomFoggedAttribute(pawnParent, preferSkill: true);
                }
            };

            yield return new Command_Action
            {
                defaultLabel = "Dev: Try Passive Reveal",
                defaultDesc = "Force one passive-time reveal roll (100% chance).",
                icon = null,
                action = () => {
                    if (parent is Pawn pawnParent)
                        FogUtility.RevealRandomFoggedAttribute(pawnParent, preferSkill: false);
                }
            };

            yield return new Command_Action
            {
                defaultLabel = "Dev: Print Deception Profile",
                defaultDesc = "Logs this pawn's deception tier, mask state, and RenderContext status.",
                action = () =>
                {
                    if (parent is Pawn p)
                    {
                        var comp = this;
                        var maskedSkills = comp.maskOffsets.Count > 0 ? 
                            string.Join(",", comp.maskOffsets.Select(kv => $"{kv.Key.defName}({kv.Value:+0;-0;+0})")) : 
                            "none";
                        var hiddenTraits = p.story?.traits?.allTraits
                            .Where(t => !comp.revealedTraits.Contains(t.def))
                            .Select(t => t.def.defName) ?? Enumerable.Empty<string>();
                        var hiddenTraitsStr = hiddenTraits.Any() ? string.Join(",", hiddenTraits) : "none";
                        
                        FogLog.Verbose($"[PROFILE] {p.LabelShort}:");
                        FogLog.Verbose($"  Tier={comp.tier}, FullyRevealed={comp.fullyRevealed}, Initialized={comp.compInitialized}");
                        FogLog.Verbose($"  MaskOffsets count={comp.maskOffsets.Count}: {maskedSkills}");
                        FogLog.Verbose($"  RevealedSkills count={comp.revealedSkills.Count}");
                        FogLog.Verbose($"  RevealedTraits count={comp.revealedTraits.Count}");
                        FogLog.Verbose($"  HiddenTraits: {hiddenTraitsStr}");
                        FogLog.Verbose($"  RenderContext.IsRendering={RenderContext.IsRendering}");
                        FogLog.Verbose($"  FogSettings.fogSkills={FogSettingsCache.Current.fogSkills}");
                        
                        // Show actual vs masked skill levels
                        foreach (var skill in p.skills.skills.Take(5))
                        {
                            bool shouldMask = FogMaskUtility.ShouldMaskSkill(p, skill.def, comp);
                            int masked = shouldMask ? FogMaskUtility.GetMaskedSkillLevel(p, skill.def, comp) : skill.levelInt;
                            FogLog.Verbose($"    {skill.def.defName}: real={skill.levelInt}, masked={masked}, shouldMask={shouldMask}");
                        }
                    }
                }
            };
            
            yield return new Command_Action
            {
                defaultLabel = "Dev: Test Offset System",
                defaultDesc = "Tests the new offset-based masking system to verify gene compatibility.",
                action = () =>
                {
                    if (parent is Pawn p)
                    {
                        EffectiveSkillUtility.TestGeneIntegration(p);
                    }
                }
            };

            if (tier == DeceptionTier.DeceiverSleeper && !fullyRevealed)
            {
                yield return new Command_Action
                {
                    defaultLabel = "Dev: Next Sleeper Beat",
                    defaultDesc = "Immediately advance this Sleeper's storyline to the next phase.",
                    action = () =>
                    {
                        if (parent is Pawn p)
                            GameComponent_FogTracker.Get?.DevAdvanceSleeperStory(p);
                    }
                };
            }

            foreach (var kv in new System.Collections.Generic.Dictionary<string, DeceptionTier>
            {
                {"Truthful", DeceptionTier.Truthful},
                {"Slight", DeceptionTier.SlightlyDeceived},
                {"Imposter", DeceptionTier.DeceiverImposter},
                {"Sleeper", DeceptionTier.DeceiverSleeper}
            })
            {
                yield return new Command_Action
                {
                    defaultLabel = $"Dev: Set {kv.Key}",
                    action = () =>
                    {
                        if (parent is not Pawn pawn) return;

                        // Validate suitability for Imposter/Sleeper based on pawn value.
                        float pv = FogInitializer.GetPawnValue(pawn);
                        if (kv.Value == DeceptionTier.DeceiverImposter && pv > 300f)
                        {
                            Messages.Message($"{pawn.LabelShort} is too competent to be a Imposter (value {pv:F0}).", MessageTypeDefOf.RejectInput, false);
                            return;
                        }
                        if (kv.Value == DeceptionTier.DeceiverSleeper && pv < 200f)
                        {
                            Messages.Message($"{pawn.LabelShort} is too weak to be a Sleeper (value {pv:F0}).", MessageTypeDefOf.RejectInput, false);
                            return;
                        }

                        tier = kv.Value;
                        tierManuallySet = true;
                        FogInitializer.RegenerateMasksFor(pawn, this);
                        FogLog.Verbose($"[PROFILE] Manually set tier of {parent.LabelShort} to {tier}");
                    }
                };
            }
        }

        private void HandleAccuse()
        {
            if (parent is not Pawn pawn) return;

            string title = "FogOfPawn.Accuse.Dialog.Title".Translate(pawn.LabelShort);
            string text = $"FogOfPawn.Accuse.Dialog.Text.{Rand.RangeInclusive(1, 3)}".Translate(pawn.LabelShort);

            var dialog = new UI.Dialog_Accuse(
                text,
                title,
                pawn,
                () => PerformAccusation(pawn)
            );

            Find.WindowStack.Add(dialog);
        }

        private void PerformAccusation(Pawn pawn)
        {
            // Check what's actually being hidden BEFORE we reveal
            var hiddenDetails = GetHiddenSecretsDescription(pawn);
            bool hasHiddenSecrets = !string.IsNullOrEmpty(hiddenDetails);
            
            // A success is if the pawn is currently hiding ANYTHING.
            // If they are Truthful or already fully revealed, it's a false accusation.
            bool isActuallyHidingSecrets = !fullyRevealed && (tier != DeceptionTier.Truthful) && hasHiddenSecrets;
            
            // Check for the "Major" win (Imposter or Sleeper)
            bool isMalicious = (tier == DeceptionTier.DeceiverImposter || tier == DeceptionTier.DeceiverSleeper);

            if (isMalicious && hasHiddenSecrets)
            {
                // MAJOR SUCCESS logic (Imposter/Sleeper)
                FogUtility.TriggerFullRevealWithDetails(pawn, "ImposterCalledOut", hiddenDetails);

                Messages.Message("FogOfPawn.Accuse.Success.Text".Translate(pawn.LabelShort), pawn, MessageTypeDefOf.PositiveEvent);

                var thought = DefDatabase<ThoughtDef>.GetNamedSilentFail("Fog_ExposedImposter");
                if (thought != null)
                {
                    foreach (var other in pawn.MapHeld?.mapPawns?.FreeColonistsSpawned ?? Enumerable.Empty<Pawn>())
                    {
                        if (other == pawn) continue;
                        other.needs?.mood?.thoughts?.memories?.TryGainMemory(thought);
                    }
                }
            }
            else if (isActuallyHidingSecrets)
            {
                // MINOR SUCCESS logic (Slightly Deceived)
                // They were lying, but it wasn't a "threat". 
                // We reveal everything, but give a smaller colony-wide buff.
                FogUtility.TriggerFullRevealWithDetails(pawn, "MinorLiarCalledOut", hiddenDetails);
                
                Messages.Message("FogOfPawn.Accuse.MinorSuccess.Text".Translate(pawn.LabelShort), pawn, MessageTypeDefOf.PositiveEvent);
                
                // Smaller colony-wide mood buff for "Slightly Deceived"
                var thought = DefDatabase<ThoughtDef>.GetNamedSilentFail("Fog_ExposedMinorLiar");
                if (thought != null)
                {
                    foreach (var other in pawn.MapHeld?.mapPawns?.FreeColonistsSpawned ?? Enumerable.Empty<Pawn>())
                    {
                        if (other == pawn) continue;
                        other.needs?.mood?.thoughts?.memories?.TryGainMemory(thought);
                    }
                }
            }
            else
            {
                // FAILURE logic (pawn was actually truthful/revealed or had no real lies)
                // Mark as fully revealed since they've been accused
                fullyRevealed = true;
                
                var thought = DefDatabase<ThoughtDef>.GetNamedSilentFail("Fog_FalselyAccused");
                if (thought != null)
                {
                    pawn.needs?.mood?.thoughts?.memories?.TryGainMemory(thought);
                }

                Find.LetterStack.ReceiveLetter(
                    "FogOfPawn.Accuse.Failure.Label".Translate(),
                    "FogOfPawn.Accuse.Failure.Text".Translate(pawn.LabelShort),
                    LetterDefOf.NegativeEvent,
                    pawn
                );
            }
        }
        
        /// <summary>
        /// Builds a human-readable description of what secrets this pawn is currently hiding.
        /// Returns null/empty if nothing is actually being hidden.
        /// </summary>
        private string GetHiddenSecretsDescription(Pawn pawn)
        {
            var secrets = new List<string>();
            
            // Check for masked skills with actual differences
            if (FogSettingsCache.Current.fogSkills && pawn.skills != null)
            {
                foreach (var kvp in maskOffsets)
                {
                    if (revealedSkills.Contains(kvp.Key)) continue;
                    if (kvp.Value == 0) continue; // No actual difference
                    
                    var skill = pawn.skills.GetSkill(kvp.Key);
                    if (skill == null) continue;
                    
                    int realLevel = skill.levelInt;
                    int maskedLevel = Mathf.Clamp(realLevel + kvp.Value, 0, 20);
                    
                    if (realLevel != maskedLevel)
                    {
                        string direction = kvp.Value > 0 ? "FogOfPawn.Accuse.Overstated".Translate().ToString() : "FogOfPawn.Accuse.Understated".Translate().ToString();
                        secrets.Add($"• {kvp.Key.label}: {direction} ({maskedLevel} → {realLevel})");
                    }
                }
            }
            
            // Check for hidden traits
            if (FogSettingsCache.Current.fogTraits && pawn.story?.traits != null)
            {
                foreach (var trait in pawn.story.traits.allTraits)
                {
                    if (!revealedTraits.Contains(trait.def))
                    {
                        secrets.Add($"• {"FogOfPawn.Accuse.HiddenTrait".Translate()}: {trait.Label}");
                    }
                }
            }
            
            if (secrets.Count == 0) return null;
            
            return string.Join("\n", secrets);
        }

        public void RevealSkill(SkillDef skillDef)
        {
            // Truthful and fully revealed pawns don't need skill revealing
            if (tier == DeceptionTier.Truthful || fullyRevealed) return;
            
            // If this pawn is a Sleeper or Imposter and not yet fully revealed,
            // any attempt to expose a single skill should instead trigger a dramatic
            // full reveal for narrative impact.
            if (!fullyRevealed && (tier == DeceptionTier.DeceiverSleeper || tier == DeceptionTier.DeceiverImposter))
            {
                string reason = tier == DeceptionTier.DeceiverSleeper ? "SleeperCascade" : "ImposterCascade";
                FogUtility.TriggerFullReveal((Pawn)parent, reason);
                return;
            }

            if (revealedSkills.Contains(skillDef)) return;

            revealedSkills.Add(skillDef);

            var pawn = parent as Pawn;
            int real = pawn?.skills.GetSkill(skillDef).Level ?? 0;

            if (FogUtility.ShouldNotifyPlayer(pawn))
            {
                string label = "FogOfPawn.SkillRevealed.Label".Translate(pawn.LabelShort, skillDef.label);
                string text = "FogOfPawn.SkillRevealed.Text".Translate(pawn.LabelShort, skillDef.label, real.ToString());
                Find.LetterStack.ReceiveLetter(label, text, LetterDefOf.NeutralEvent, pawn);
            }

            FogLog.Verbose($"Revealed skill {skillDef.defName} for {parent.LabelShort}.");

            MaybeDropDisguiseKit();
        }

        public void RevealTrait(Trait trait)
        {
            // Truthful and fully revealed pawns don't need trait revealing
            if (tier == DeceptionTier.Truthful || fullyRevealed) return;
            
            if (!fullyRevealed && (tier == DeceptionTier.DeceiverSleeper || tier == DeceptionTier.DeceiverImposter))
            {
                string reason = tier == DeceptionTier.DeceiverSleeper ? "SleeperCascade" : "ImposterCascade";
                FogUtility.TriggerFullReveal((Pawn)parent, reason);
                return;
            }

            if (revealedTraits.Contains(trait.def)) return;

            revealedTraits.Add(trait.def);

            var pawn2 = parent as Pawn;
            if (FogUtility.ShouldNotifyPlayer(pawn2))
            {
                string labelT = "FogOfPawn.TraitRevealed.Label".Translate(pawn2.LabelShort, trait.Label);
                string textT = "FogOfPawn.TraitRevealed.Text".Translate(pawn2.LabelShort, trait.Label, trait.def.description);
                Find.LetterStack.ReceiveLetter(labelT, textT, LetterDefOf.NeutralEvent, pawn2);
            }

            FogLog.Verbose($"Revealed trait {trait.def.defName} for {parent.LabelShort}.");

            MaybeDropDisguiseKit();
        }

        public void RevealAll()
        {
            // Reveal every skill
            if (parent is Pawn pawn && pawn.skills != null)
            {
                foreach (var sk in pawn.skills.skills)
                {
                    revealedSkills.Add(sk.def);
                }
            }

            // Reveal all traits
            if (parent is Pawn pawnTraits && pawnTraits.story?.traits != null)
            {
                foreach (var trait in pawnTraits.story.traits.allTraits)
                {
                    revealedTraits.Add(trait.def);
                }
            }

            healthRevealed = true;
            genesRevealed  = true;

            FogLog.Verbose($"Dev-revealed all attributes for {parent.LabelShort}.");
        }

        public void ResetFog()
        {
            revealedSkills.Clear();
            revealedTraits.Clear();
            maskOffsets.Clear();
            passionOffsets.Clear();

            // Clear old data too (for compatibility)
            reportedSkills.Clear();
            reportedPassions.Clear();

            healthRevealed = false;
            genesRevealed  = false;
            ticksSinceJoin = 0;

            FogLog.Verbose($"Dev-reset fog for {parent.LabelShort}.");
        }

        public override void CompTick()
        {
            base.CompTick();
            
            // Run change detection every 60 ticks (~1 second) for responsiveness
            if (Find.TickManager.TicksGame % 60 == 0)
            {
                DetectAndAutoRevealChanges();
            }

            // Check prisoner interrogation logic every 2500 ticks (~1 hour)
            if (Find.TickManager.TicksGame % 2500 == 0)
            {
                CheckPrisonerInterrogation();
            }
        }

		public override void PostExposeData()
        {
            Scribe_Values.Look(ref compInitialized, "compInitialized", false);
            Scribe_Values.Look(ref ticksSinceJoin, "ticksSinceJoin", 0);
            Scribe_Values.Look(ref truthfulness, "truthfulness", 0f);
            Scribe_Values.Look(ref tier, "deceptionTier", DeceptionTier.Truthful);
            Scribe_Values.Look(ref tierManuallySet, "tierManual", false);
            Scribe_Values.Look(ref fullyRevealed, "fullyRevealed", false);
            Scribe_Values.Look(ref outcomeProcessed, "outcomeProcessed", false);
            // keep loading/saving for backward save compatibility; otherwise unused
            Scribe_Values.Look(ref lastInterrogatedTick, "lastInterrogatedTick", 0);
            Scribe_Values.Look(ref wasPlayerColonist, "wasPlayerColonist", false);
            
            // Migration flag
            Scribe_Values.Look(ref migratedToOffsets, "migratedToOffsets", false);
            
            // Fix for existing saves: Ensure SlightlyDeceived pawns have their revealedTraits populated
            if (Scribe.mode == LoadSaveMode.PostLoadInit && tier != DeceptionTier.Truthful && (revealedTraits == null || revealedTraits.Count == 0))
            {
                FogLog.Verbose($"[MIGRATION] Populating missing revealedTraits for {parent.LabelShort}");
                FogInitializer.InitializeFogFor((Pawn)parent);
            }
            
            // NEW format (offset-based)
            Scribe_Collections.Look(ref maskOffsets, "maskOffsets", LookMode.Def, LookMode.Value);
            Scribe_Collections.Look(ref passionOffsets, "passionOffsets", LookMode.Def, LookMode.Value);
            
            // OLD format (keep for migration)
            Scribe_Collections.Look(ref reportedSkills, "reportedSkills", LookMode.Def, LookMode.Value);
            Scribe_Collections.Look(ref reportedPassions, "reportedPassions", LookMode.Def, LookMode.Value);
            
            Scribe_Collections.Look(ref revealedSkills, "revealedSkills", LookMode.Def);
            Scribe_Collections.Look(ref revealedTraits, "revealedTraits", LookMode.Def);

            Scribe_Values.Look(ref healthRevealed, "healthRevealed", false);
            Scribe_Values.Look(ref genesRevealed, "genesRevealed", false);
            Scribe_Values.Look(ref disguiseKitSpawned, "disguiseKitSpawned", false);
            
            // Change tracking data
            Scribe_Collections.Look(ref lastKnownTraits, "lastKnownTraits", LookMode.Def);
            Scribe_Values.Look(ref lastTraitCheckTick, "lastTraitCheckTick", -1);
            
            // Initialize collections if null (for new saves)
            if (maskOffsets == null) maskOffsets = new Dictionary<SkillDef, int>();
            if (passionOffsets == null) passionOffsets = new Dictionary<SkillDef, int>();
            if (reportedSkills == null) reportedSkills = new Dictionary<SkillDef, float?>();
            if (reportedPassions == null) reportedPassions = new Dictionary<SkillDef, Passion?>();
            if (revealedSkills == null) revealedSkills = new HashSet<SkillDef>();
            if (revealedTraits == null) revealedTraits = new HashSet<TraitDef>();
            if (lastKnownTraits == null) lastKnownTraits = new HashSet<TraitDef>();
            
            // Migration: Convert old format to new offset-based system
			if (Scribe.mode == LoadSaveMode.PostLoadInit && !migratedToOffsets)
            {
                MigrateToOffsetSystem();
            }
        }
        
        /// <summary>
        /// Migrates old absolute skill values to new offset-based system
        /// </summary>
        private void MigrateToOffsetSystem()
        {
            if (parent is not Pawn pawn || pawn.skills == null)
            {
                migratedToOffsets = true;
                return;
            }
            
            FogLog.Verbose($"Migrating {pawn.LabelShort} from old skill system to offset-based system");
            
            // Convert old reportedSkills to maskOffsets
            foreach (var kvp in reportedSkills)
            {
                if (!kvp.Value.HasValue) continue;
                
                var skill = pawn.skills.GetSkill(kvp.Key);
                if (skill == null) continue;
                
                int currentTrainedLevel = skill.levelInt; // Use trained level to avoid recursion
                int oldReported = Mathf.RoundToInt(kvp.Value.Value);
                int offset = oldReported - currentTrainedLevel;
                
                // Only store non-zero offsets
                if (offset != 0)
            {
                    maskOffsets[kvp.Key] = offset;
                    FogLog.Verbose($"  {kvp.Key.defName}: trained={currentTrainedLevel}, old_reported={oldReported}, offset={offset}");
                }
            }
            
            // Convert old reportedPassions to passionOffsets
            foreach (var kvp in reportedPassions)
            {
                if (!kvp.Value.HasValue) continue;
                
                var skill = pawn.skills.GetSkill(kvp.Key);
                if (skill == null) continue;
                
                Passion currentPassion = skill.passion;
                Passion fakePassion = kvp.Value.Value;
                
                int offset = 0;
                if (currentPassion == Passion.None && fakePassion == Passion.Minor) offset = 1;
                else if (currentPassion == Passion.None && fakePassion == Passion.Major) offset = 2;
                else if (currentPassion == Passion.Minor && fakePassion == Passion.Major) offset = 1;
                else if (currentPassion == Passion.Minor && fakePassion == Passion.None) offset = -1;
                else if (currentPassion == Passion.Major && fakePassion == Passion.None) offset = -2;
                else if (currentPassion == Passion.Major && fakePassion == Passion.Minor) offset = -1;
                
                if (offset != 0)
                {
                    passionOffsets[kvp.Key] = offset;
                    FogLog.Verbose($"  {kvp.Key.defName} passion: real={currentPassion}, fake={fakePassion}, offset={offset}");
                }
            }
            
            // Clear old data after migration
            reportedSkills.Clear();
            reportedPassions.Clear();
            
            migratedToOffsets = true;
            FogLog.Verbose($"Migration complete for {pawn.LabelShort}");
        }

        private void MaybeDropDisguiseKit()
        {
            if (disguiseKitSpawned || tier != DeceptionTier.DeceiverImposter) return;

            disguiseKitSpawned = true;

            if (!FogSettingsCache.Current.spawnDisguiseKitOnReveal) return;

            var kitDef = DefDatabase<ThingDef>.GetNamedSilentFail("FogOfPawn_DisguiseKit");
            if (kitDef == null) return;
            
            var disguiseKit = ThingMaker.MakeThing(kitDef);
            GenPlace.TryPlaceThing(disguiseKit, parent.Position, parent.Map, ThingPlaceMode.Near);
        }

        /// <summary>
        /// Detects external modifications (traits, genes, etc.) and automatically reveals them.
        /// This ensures user modifications via Character Editor or other tools are immediately visible.
        /// </summary>
        public void DetectAndAutoRevealChanges()
        {
            var pawn = parent as Pawn;
            if (pawn?.story?.traits == null) return;
            
            // Skip if pawn is already truthful or fully revealed - no need to track changes
            if (tier == DeceptionTier.Truthful || fullyRevealed) return;

            int currentTick = Find.TickManager.TicksGame;
            
            // Initialize tracking on first run
            if (lastTraitCheckTick == -1)
            {
                lastKnownTraits.Clear();
                foreach (var trait in pawn.story.traits.allTraits)
                {
                    lastKnownTraits.Add(trait.def);
                }
                lastTraitCheckTick = currentTick;
                return;
            }

            // Check for new traits (user additions)
            var currentTraits = new HashSet<TraitDef>();
            foreach (var trait in pawn.story.traits.allTraits)
            {
                currentTraits.Add(trait.def);
                
                // If this trait wasn't there before, it's a user addition - reveal it immediately
                if (!lastKnownTraits.Contains(trait.def))
                {
                    if (!revealedTraits.Contains(trait.def))
                    {
                        revealedTraits.Add(trait.def);
                        if (Prefs.DevMode)
                        {
                            FogLog.Verbose($"[AUTO-REVEAL] User-added trait {trait.def.defName} revealed for {pawn.LabelShort}");
                }
            }
                }
            }

            // Update tracking
            lastKnownTraits = currentTraits;
            lastTraitCheckTick = currentTick;
        }

        /// <summary>
        /// Handles the "Interrogation" workflow.
        /// If a pawn is a prisoner of the colony, they are subject to constant observation/interrogation pressure.
        /// This accelerates the reveal of hidden attributes or storylines.
        /// </summary>
        private void CheckPrisonerInterrogation()
        {
            var pawn = parent as Pawn;
            if (pawn == null || !pawn.IsPrisonerOfColony) return;
            if (fullyRevealed) return;

            // Chance to reveal something per hour (tunable in settings)
            float revealChance = FogSettingsCache.Current.prisonerRevealChancePct / 100f;
            if (!Rand.Chance(revealChance)) return;

            // Attempt to reveal a random attribute
            bool revealedSomething = FogUtility.RevealRandomFoggedAttribute(pawn, preferSkill: false);
            
            if (revealedSomething)
            {
                // Check for major breaks (Imposter/Sleeper cracking)
                if (tier == DeceptionTier.DeceiverImposter || tier == DeceptionTier.DeceiverSleeper)
                {
                    // Chance for full crack on reveal (tunable in settings)
                    float crackChance = FogSettingsCache.Current.prisonerCrackChancePct / 100f;
                    if (Rand.Chance(crackChance))
                    {
                        FogUtility.TriggerFullReveal(pawn, "InterrogationCrack");
                        return;
                    }
                }

                // If it's a Sleeper, interrogation might advance their plot even without a reveal
                if (tier == DeceptionTier.DeceiverSleeper)
                {
                     GameComponent_FogTracker.Get?.DevAdvanceSleeperStory(pawn);
                }
            }
            else
            {
                 // Even if no attribute was revealed, if they are a Sleeper, we might advance the story
                 if (tier == DeceptionTier.DeceiverSleeper && Rand.Chance(0.05f))
                 {
                     GameComponent_FogTracker.Get?.DevAdvanceSleeperStory(pawn);
                 }
            }
        }
    }
}