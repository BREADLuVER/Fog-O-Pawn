using HarmonyLib;
using RimWorld;
using Verse;
using UnityEngine;
using System.Collections.Generic;

namespace FogOfPawn.Patches
{
    [HarmonyPatch(typeof(StatWorker), "GetValue", new[] { typeof(StatRequest), typeof(bool) })]
    public static class Patch_StatWorker_SkillPenalties
    {
        static void Postfix(StatRequest req, bool applyPostProcess, StatDef ___stat, ref float __result)
        {
            if (!FogSettingsCache.Current.applySkillPenalties) return;
            if (___stat == null) return;
            
            bool isSpeed = ___stat.defName.EndsWith("Speed") || ___stat.defName.Contains("Speed");
            bool isFail = ___stat.defName.Contains("Fail") || ___stat.defName == "FoodPoisonChance";
            bool isChance = ___stat.defName.Contains("Chance");
            
            bool isSuccess = isChance && !isFail;

            if (!isSpeed && !isSuccess && !isFail) return;

            if (!req.HasThing || req.Thing is not Pawn pawn) return;

            var comp = pawn.GetComp<CompPawnFog>();
            if (comp == null || !comp.compInitialized || comp.fullyRevealed) return;
            
            if (comp.tier == DeceptionTier.Truthful) return;

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

            float penaltyFactor = FogSettingsCache.Current.skillPenaltyPct / 100f;

            if (isSpeed || isSuccess)
            {
                __result *= (1.0f - penaltyFactor);
            }
            else if (isFail)
            {
                __result *= (1.0f + penaltyFactor);
            }
            
            if (FogSettingsCache.Current.verboseLogging && Rand.Chance(0.01f))
            {
                FogLog.Verbose($"[SkillPenalty] Applied {penaltyFactor:P0} flat penalty to {pawn.LabelShort} for {___stat.defName}. Result: {__result}");
            }
        }

        private static bool IsSkillFaked(Pawn pawn, CompPawnFog comp, SkillDef skill)
        {
            if (skill == null) return false;

            if (comp.maskOffsets.TryGetValue(skill, out int offset))
            {
                if (offset > 0)
                {
                    if (comp.revealedSkills.Contains(skill)) return false;
                    return true;
                }
            }
            return false;
        }
    }
}
