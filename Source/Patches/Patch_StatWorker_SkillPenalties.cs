using HarmonyLib;
using RimWorld;
using Verse;
using UnityEngine;
using System.Collections.Generic;

namespace FogOfPawn.Patches
{
    /// <summary>
    /// Applies a flat penalty to work speed/success when a pawn is using a faked (overstated) skill.
    /// The penalty is a constant percentage regardless of how much they lied.
    /// </summary>
    [HarmonyPatch(typeof(StatWorker), "GetValue", new[] { typeof(StatRequest), typeof(bool) })]
    public static class Patch_StatWorker_SkillPenalties
    {
        static void Postfix(StatRequest req, bool applyPostProcess, StatDef ___stat, ref float __result)
        {
            if (!FogSettingsCache.Current.applySkillPenalties) return;
            if (___stat == null) return;
            
            // Filter only relevant stats (Speed, Success, or Fail chance)
            bool isSpeed = ___stat.defName.EndsWith("Speed") || ___stat.defName.Contains("Speed");
            bool isFail = ___stat.defName.Contains("Fail") || ___stat.defName == "FoodPoisonChance";
            bool isChance = ___stat.defName.Contains("Chance");
            
            // Treat generic "Chance" stats as Success (higher is better), unless identified as Fail
            bool isSuccess = isChance && !isFail;

            if (!isSpeed && !isSuccess && !isFail) return;

            if (!req.HasThing || req.Thing is not Pawn pawn) return;

            var comp = pawn.GetComp<CompPawnFog>();
            if (comp == null || !comp.compInitialized || comp.fullyRevealed) return;
            
            // Only applies if they are deceiving
            if (comp.tier == DeceptionTier.Truthful) return;

            // Check if ANY skill used by this stat is being faked (overstated)
            bool isUsingFakedSkill = false;
            
            if (___stat.skillNeedFactors != null)
            {
                foreach (var need in ___stat.skillNeedFactors)
                {
                    if (IsSkillFaked(pawn, comp, need.skill))
                    {
                        isUsingFakedSkill = true;
                        break;
                    }
                }
            }
            
            if (!isUsingFakedSkill) return;

            // Apply flat penalty (from settings)
            float penaltyFactor = FogSettingsCache.Current.skillPenaltyPct / 100f;

            if (isSpeed || isSuccess)
            {
                // Reduction: e.g. 5% penalty -> 95% speed/success
                __result *= (1.0f - penaltyFactor);
            }
            else if (isFail)
            {
                // Increase failure: e.g. 5% penalty -> 105% failure chance
                __result *= (1.0f + penaltyFactor);
            }
            
            // Debug log occasionally if verbose
            if (FogSettingsCache.Current.verboseLogging && Rand.Chance(0.01f))
            {
                FogLog.Verbose($"[SkillPenalty] Applied {penaltyFactor:P0} flat penalty to {pawn.LabelShort} for {___stat.defName}. Result: {__result}");
            }
        }

        /// <summary>
        /// Returns true if the pawn is overstating this skill (faking higher than real).
        /// </summary>
        private static bool IsSkillFaked(Pawn pawn, CompPawnFog comp, SkillDef skill)
        {
            if (skill == null) return false;

            if (comp.maskOffsets.TryGetValue(skill, out int offset))
            {
                // We only penalize Overstating (faking a higher skill).
                // If offset > 0, they are claiming to be better than they are.
                if (offset > 0)
                {
                    // If the skill is already revealed, the gig is up - no more "faking" penalties
                    if (comp.revealedSkills.Contains(skill)) return false;
                    return true;
                }
            }
            return false;
        }
    }
}
