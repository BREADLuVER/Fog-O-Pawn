using System.Collections.Generic;
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
        public float truthfulness;

        // Skills
        public Dictionary<SkillDef, float?> reportedSkills = new Dictionary<SkillDef, float?>();
        public Dictionary<SkillDef, Passion?> reportedPassions = new Dictionary<SkillDef, Passion?>();
        public HashSet<SkillDef> revealedSkills = new HashSet<SkillDef>();

        // Traits
        public HashSet<TraitDef> revealedTraits = new HashSet<TraitDef>();
        
        // Health & Genes
        public bool healthRevealed;
        public bool genesRevealed;

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (!Prefs.DevMode) yield break;

            yield return new Command_Action
            {
                defaultLabel = "Dev: Reveal Fog",
                defaultDesc = "Instantly reveals all fogged attributes for this pawn.",
                icon = null,
                action = RevealAll
            };

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
            
            Scribe_Collections.Look(ref reportedSkills, "reportedSkills", LookMode.Def, LookMode.Value);
            Scribe_Collections.Look(ref reportedPassions, "reportedPassions", LookMode.Def, LookMode.Value);
            Scribe_Collections.Look(ref revealedSkills, "revealedSkills", LookMode.Def);
            Scribe_Collections.Look(ref revealedTraits, "revealedTraits", LookMode.Def);

            Scribe_Values.Look(ref healthRevealed, "healthRevealed", false);
            Scribe_Values.Look(ref genesRevealed, "genesRevealed", false);
        }
    }
}