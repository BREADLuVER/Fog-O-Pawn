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
                        Log.Message($"[FogOfPawn] Dev: Added XP to {pawn.LabelShort}'s Shooting skill. Current XP since last level: {sk.xpSinceLastLevel}");
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
            if (revealedSkills.Contains(skillDef)) return;

            revealedSkills.Add(skillDef);

            var pawn = parent as Pawn;
            int real = pawn?.skills.GetSkill(skillDef).Level ?? 0;

            var rules = new List<Rule>(GrammarUtility.RulesForPawn("PAWN", pawn));
            rules.Add(new Rule_String("SKILL_label", skillDef.label));
            rules.Add(new Rule_String("REAL", real.ToString()));

            GrammarRequest req = new GrammarRequest();
            req.Includes.Add(DefDatabase<RulePackDef>.GetNamed("FogOfPawn_RevealSkill"));
            req.Rules.AddRange(rules);

            string text = GrammarResolver.Resolve("root", req, null, false);

            Messages.Message(text, pawn, MessageTypeDefOf.PositiveEvent);

            FogLog.Verbose($"Revealed skill {skillDef.defName} for {parent.LabelShort}.");
        }

        public void RevealTrait(Trait trait)
        {
            if (revealedTraits.Contains(trait.def)) return;

            revealedTraits.Add(trait.def);

            var pawn2 = parent as Pawn;
            var rulesT = new List<Rule>(GrammarUtility.RulesForPawn("PAWN", pawn2));
            rulesT.Add(new Rule_String("TRAIT_label", trait.Label));
            rulesT.Add(new Rule_String("TRAIT_desc", trait.def.description));

            GrammarRequest reqT = new GrammarRequest();
            reqT.Includes.Add(DefDatabase<RulePackDef>.GetNamed("FogOfPawn_RevealTrait"));
            reqT.Rules.AddRange(rulesT);

            string textT = GrammarResolver.Resolve("root", reqT, null, false);

            Messages.Message(textT, pawn2, trait.Degree > 0 ? MessageTypeDefOf.NeutralEvent : MessageTypeDefOf.PositiveEvent);

            FogLog.Verbose($"Revealed trait {trait.def.defName} for {parent.LabelShort}.");
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

            Log.Message($"[FogOfPawn] Dev-revealed all attributes for {parent.LabelShort}.");
        }

        public void ResetFog()
        {
            revealedSkills.Clear();
            revealedTraits.Clear();

            // Optionally regenerate reported numbers here later.
            healthRevealed = false;
            genesRevealed  = false;
            ticksSinceJoin = 0;

            Log.Message($"[FogOfPawn] Dev-reset fog for {parent.LabelShort}.");
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
                }
            }
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref compInitialized, "compInitialized", false);
            Scribe_Values.Look(ref ticksSinceJoin, "ticksSinceJoin", 0);
            Scribe_Values.Look(ref truthfulness, "truthfulness", 0f);
            Scribe_Values.Look(ref tier, "deceptionTier", DeceptionTier.Truthful);
            Scribe_Values.Look(ref tierManuallySet, "tierManual", false);
            
            Scribe_Collections.Look(ref reportedSkills, "reportedSkills", LookMode.Def, LookMode.Value);
            Scribe_Collections.Look(ref reportedPassions, "reportedPassions", LookMode.Def, LookMode.Value);
            Scribe_Collections.Look(ref revealedSkills, "revealedSkills", LookMode.Def);
            Scribe_Collections.Look(ref revealedTraits, "revealedTraits", LookMode.Def);

            Scribe_Values.Look(ref healthRevealed, "healthRevealed", false);
            Scribe_Values.Look(ref genesRevealed, "genesRevealed", false);
        }
    }
}