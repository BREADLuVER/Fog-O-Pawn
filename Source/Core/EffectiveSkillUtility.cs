using RimWorld;
using UnityEngine;
using Verse;

namespace FogOfPawn
{
    /// <summary>
    /// Utility helpers for reading skills that respect the fogged profile.
    /// All internal mod code should call <see cref="GetEffectiveSkill"/> instead of accessing
    /// <c>SkillRecord.Level</c> directly when specific behaviour is required.
    /// However, a Harmony patch is also applied to <c>SkillRecord.Level</c> so that the entire
    /// game – including external mods – automatically uses the masked value whenever appropriate.
    /// </summary>
    public static class EffectiveSkillUtility
    {
        /// <summary>
        /// Returns the effective (possibly masked) skill level for the given pawn.
        /// When the pawn is fogged and the skill has not yet been revealed this will
        /// return the reported level, otherwise the real level.
        /// </summary>
        public static int GetEffectiveSkill(Pawn pawn, SkillDef def)
        {
            if (pawn?.skills == null) return 0;

            var sr = pawn.skills.GetSkill(def);
            if (sr == null) return 0;

            // Access the raw (unmasked) value directly to avoid triggering the Harmony patch again.
            int realLevel = sr.levelInt;

            // If the pawn is not fogged just return the real value.
            var comp = pawn.GetComp<CompPawnFog>();
            if (comp == null || !comp.compInitialized)
            {
                return realLevel;
            }

            // Revealed skills always show the real value.
            if (comp.revealedSkills.Contains(def))
            {
                return realLevel;
            }

            // If a reported (fake) value exists prefer that.
            if (comp.reportedSkills.TryGetValue(def, out var rep) && rep.HasValue)
            {
                return Mathf.Clamp(Mathf.RoundToInt(rep.Value), 0, 20);
            }

            // Unknown – treat as completely unskilled so work priorities etc. stay safe.
            return 0;
        }
    }
} 