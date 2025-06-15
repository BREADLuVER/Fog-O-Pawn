using System.Collections.Generic;
using RimWorld;
using Verse;
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
        }

        public void RevealSkill(SkillDef skillDef)
        {
            if (revealedSkills.Contains(skillDef)) return;

            revealedSkills.Add(skillDef);

            // Notify player
            var letter = LetterMaker.MakeLetter(
                "FogOfPawn.SkillRevealed.Label".Translate(parent.LabelShort, skillDef.label),
                "FogOfPawn.SkillRevealed.Text".Translate(parent.LabelShort, skillDef.label, (parent as Pawn).skills.GetSkill(skillDef).Level),
                LetterDefOf.PositiveEvent,
                parent
            );
            Find.LetterStack.ReceiveLetter(letter);
            
            Log.Message($"[FogOfPawn] Revealed skill {skillDef.defName} for {parent.LabelShort}.");
        }

        public void RevealTrait(Trait trait)
        {
            if (revealedTraits.Contains(trait.def)) return;

            revealedTraits.Add(trait.def);

            // Notify player
            var letter = LetterMaker.MakeLetter(
                "FogOfPawn.TraitRevealed.Label".Translate(parent.LabelShort, trait.Label),
                "FogOfPawn.TraitRevealed.Text".Translate(parent.LabelShort, trait.Label, trait.def.description),
                trait.Degree > 0 ? LetterDefOf.NegativeEvent : LetterDefOf.PositiveEvent,
                parent
            );
            Find.LetterStack.ReceiveLetter(letter);

            Log.Message($"[FogOfPawn] Revealed trait {trait.def.defName} for {parent.LabelShort}.");
        }

        public void RevealAll()
        {
            Log.Message($"[FogOfPawn] Dev-revealed all attributes for {parent.LabelShort}.");
            // Future: This will set all 'revealed' flags to true.
        }

        public override void CompTick()
        {
            base.CompTick();
            // Only count time for player's faction members, not prisoners or visitors
            if (parent.Faction?.IsPlayer == true)
            {
                ticksSinceJoin++;
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