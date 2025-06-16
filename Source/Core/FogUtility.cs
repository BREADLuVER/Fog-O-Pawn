using System.Collections.Generic;
using RimWorld;
using Verse;

namespace FogOfPawn
{
    public static class FogUtility
    {
        public static bool RevealRandomFoggedAttribute(Pawn pawn, bool preferSkill = true)
        {
            if (pawn == null || pawn.Destroyed) return false;
            var comp = pawn.GetComp<CompPawnFog>();
            if (comp == null || !comp.compInitialized) return false;

            var settings = FogSettingsCache.Current;
            List<System.Action> candidates = new();

            // Skills
            if (settings.fogSkills)
            {
                foreach (var sk in pawn.skills.skills)
                {
                    if (!comp.revealedSkills.Contains(sk.def))
                    {
                        if ((preferSkill && settings.allowSocialSkillReveal) || (!preferSkill && settings.allowPassiveSkillReveal))
                        {
                            candidates.Add(() => comp.RevealSkill(sk.def));
                        }
                    }
                }
            }

            // Traits
            if (settings.fogTraits && pawn.story?.traits != null)
            {
                foreach (var tr in pawn.story.traits.allTraits)
                {
                    if (!comp.revealedTraits.Contains(tr.def))
                    {
                        if ((preferSkill && settings.allowSocialTraitReveal) || (!preferSkill && settings.allowPassiveTraitReveal))
                        {
                            var captured = tr; // closure copy
                            candidates.Add(() => comp.RevealTrait(captured));
                        }
                    }
                }
            }

            if (candidates.Count == 0) return false;

            candidates.RandomElement()();
            return true;
        }
    }
} 