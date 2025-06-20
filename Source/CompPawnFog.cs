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

    public class CompPawnFog : ThingComp, IExposable
    {
        // Meta
        public bool compInitialized;
        public int ticksSinceJoin;
        public float truthfulness; // legacy, no longer used
        private bool disguiseKitSpawned;

        // Skills
        public Dictionary<SkillDef, float?> reportedSkills = new Dictionary<SkillDef, float?>();
        public Dictionary<SkillDef, Passion?> reportedPassions = new Dictionary<SkillDef, Passion?>();
        public HashSet<SkillDef> revealedSkills = new HashSet<SkillDef>();

        // Traits
        public HashSet<TraitDef> revealedTraits = new HashSet<TraitDef>();
        
        // Health & Genes
        public bool healthRevealed;
        public bool genesRevealed;

        public DeceptionTier tier = DeceptionTier.Truthful;
        public bool tierManuallySet;

        public bool fullyRevealed;

        // transient counters used by reveal logic (not saved)
        [System.NonSerialized]
        public System.Collections.Generic.Dictionary<string, float> tempData = new System.Collections.Generic.Dictionary<string, float>();

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
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
                defaultDesc = "Logs this pawn's deception tier and altered skills.",
                action = () =>
                {
                    if (parent is Pawn p)
                    {
                        var comp = this;
                        FogLog.Verbose($"[PROFILE] {p.LabelShort}: Tier={comp.tier}, RevealedSkills={comp.revealedSkills.Count}, ReportedSkills={string.Join(",", comp.reportedSkills.Where(kv=>kv.Value!=null).Select(kv=>kv.Key.defName))}");
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
                {"Scammer", DeceptionTier.DeceiverScammer},
                {"Sleeper", DeceptionTier.DeceiverSleeper}
            })
            {
                yield return new Command_Action
                {
                    defaultLabel = $"Dev: Set {kv.Key}",
                    action = () =>
                    {
                        if (parent is not Pawn pawn) return;

                        // Validate suitability for Scammer/Sleeper based on pawn value.
                        float pv = FogInitializer.GetPawnValue(pawn);
                        if (kv.Value == DeceptionTier.DeceiverScammer && pv > 300f)
                        {
                            Messages.Message($"{pawn.LabelShort} is too competent to be a Scammer (value {pv:F0}).", MessageTypeDefOf.RejectInput, false);
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

        public void RevealSkill(SkillDef skillDef)
        {
            // If this pawn is a Sleeper or Scammer and not yet fully revealed,
            // any attempt to expose a single skill should instead trigger a dramatic
            // full reveal for narrative impact.
            if (!fullyRevealed && (tier == DeceptionTier.DeceiverSleeper || tier == DeceptionTier.DeceiverScammer))
            {
                string reason = tier == DeceptionTier.DeceiverSleeper ? "SleeperCascade" : "ScammerCascade";
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
            if (!fullyRevealed && (tier == DeceptionTier.DeceiverSleeper || tier == DeceptionTier.DeceiverScammer))
            {
                string reason = tier == DeceptionTier.DeceiverSleeper ? "SleeperCascade" : "ScammerCascade";
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

            // Optionally regenerate reported numbers here later.
            healthRevealed = false;
            genesRevealed  = false;
            ticksSinceJoin = 0;

            FogLog.Verbose($"Dev-reset fog for {parent.LabelShort}.");
        }

        public override void CompTick()
        {
            base.CompTick();
            // Only count time for player's faction members, not prisoners or visitors
            if (parent.Faction?.IsPlayer == true)
            {
                ticksSinceJoin++;

                if (ticksSinceJoin % 2500 == 0) // ~once per in-game hour
                {
                    var settings = FogSettingsCache.Current;
                    if (settings.passiveRevealDays > 0f)
                    {
                        // The MTB calculation: mtbDays, check interval, per-tick multiplier inside helper.
                        if (Rand.MTBEventOccurs(settings.passiveRevealDays, 60000f, 2500f))
                        {
                            if (parent is Pawn pawnParent)
                                FogUtility.RevealRandomFoggedAttribute(pawnParent, preferSkill: false);
                        }
                    }

                    // Good-treatment (high mood) reveal
                    if (settings.positiveMoodRevealPct > 0 && parent is Pawn pmood)
                    {
                        var moodNeed = pmood.needs?.mood as Need_Mood;
                        if (moodNeed != null && moodNeed.CurLevel * 100f > settings.positiveMoodThresholdPct)
                        {
                            if (Rand.Chance(settings.positiveMoodRevealPct / 100f))
                            {
                                FogUtility.RevealRandomFoggedAttribute(pmood, preferSkill: false);
                            }
                        }
                    }
                }

                // 1% base daily chance for sleepers/scammers if nothing else triggered
                if ((tier == DeceptionTier.DeceiverScammer) && ticksSinceJoin % 60000 == 0 && !tierManuallySet)
                {
                    if (Rand.Chance(FogSettingsCache.Current.passiveDailyRevealPct / 100f))
                    {
                        string reason = tier == DeceptionTier.DeceiverScammer ? "ScammerPassive" : "ScammerPassive";
                        FogUtility.TriggerFullReveal((Pawn)parent, reason);
                    }
                }

                // Health-based Sleeper reveal removed – story-driven only
                /*
                if (tier == DeceptionTier.DeceiverSleeper && !fullyRevealed && parent is Pawn hpawn)
                {
                    if (!hpawn.Downed && hpawn.health?.summaryHealth?.SummaryHealthPercent < 0.20f)
                    {
                        FogUtility.TriggerFullReveal(hpawn, "SleeperWounded");
                    }
                }
                */
            }
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref compInitialized, "compInitialized", false);
            Scribe_Values.Look(ref ticksSinceJoin, "ticksSinceJoin", 0);
            Scribe_Values.Look(ref truthfulness, "truthfulness", 0f);
            Scribe_Values.Look(ref tier, "deceptionTier", DeceptionTier.Truthful);
            Scribe_Values.Look(ref tierManuallySet, "tierManual", false);
            Scribe_Values.Look(ref fullyRevealed, "fullyRevealed", false);
            
            Scribe_Collections.Look(ref reportedSkills, "reportedSkills", LookMode.Def, LookMode.Value);
            Scribe_Collections.Look(ref reportedPassions, "reportedPassions", LookMode.Def, LookMode.Value);
            Scribe_Collections.Look(ref revealedSkills, "revealedSkills", LookMode.Def);
            Scribe_Collections.Look(ref revealedTraits, "revealedTraits", LookMode.Def);

            Scribe_Values.Look(ref healthRevealed, "healthRevealed", false);
            Scribe_Values.Look(ref genesRevealed, "genesRevealed", false);
            Scribe_Values.Look(ref disguiseKitSpawned, "disguiseKitSpawned", false);
        }

        private void MaybeDropDisguiseKit()
        {
            if (disguiseKitSpawned)
            {
                FogLog.Verbose("[KIT] Already spawned – skipping.");
                return;
            }
            if (!fullyRevealed)
            {
                FogLog.Verbose("[KIT] Pawn not fully revealed yet.");
                return;
            }
            if (tier != DeceptionTier.DeceiverScammer)
            {
                FogLog.Verbose("[KIT] Pawn is not a scammer tier.");
                return;
            }
            if (!FogUtility.ShouldNotifyPlayer(parent as Pawn))
            {
                FogLog.Verbose("[KIT] Pawn does not belong to player – skipping kit drop.");
                return;
            }
            if (parent is not Pawn pawn)
            {
                FogLog.Verbose("[KIT] Parent is not a pawn.");
                return;
            }

            FogLog.Verbose($"[KIT] Attempting to spawn disguise kit for {pawn.LabelShort}.");

            // Remove the wealth-penalty hediff if it still exists.
            var penaltyDef = DefDatabase<HediffDef>.GetNamedSilentFail("Fog_DisguisePenalty");
            if (penaltyDef != null)
            {
                var penalty = pawn.health?.hediffSet?.GetFirstHediffOfDef(penaltyDef);
                if (penalty != null)
                {
                    pawn.health.RemoveHediff(penalty);
                    FogLog.Verbose("[KIT] Removed disguise penalty hediff.");
                }
            }

            var kitDef = DefDatabase<ThingDef>.GetNamedSilentFail("FogOfPawn_DisguiseKit");
            if (kitDef == null)
            {
                FogLog.Verbose("[KIT] ThingDef FogOfPawn_DisguiseKit not found.");
                return;
            }

            Thing kit = ThingMaker.MakeThing(kitDef);
            bool placedOk = GenPlace.TryPlaceThing(kit, pawn.PositionHeld, pawn.MapHeld, ThingPlaceMode.Near, out var placed);

            if (!placedOk || placed == null)
            {
                // Fallback: put it in inventory
                if (pawn.inventory != null)
                {
                    pawn.inventory.TryAddItemNotForSale(kit);
                    FogLog.Verbose("[KIT] Placed in pawn inventory as fallback.");
                }
                else
                {
                    kit.Destroy();
                    FogLog.Verbose("[KIT] Failed to spawn kit – destroyed (no inventory).");
                }
            }
            else
            {
                placed.SetForbidden(false);
                FogLog.Verbose("[KIT] Spawned at pawn position.");
            }

            disguiseKitSpawned = true;
        }
    }
}