using HarmonyLib;
using RimWorld;
using Verse;
using FogOfPawn;

namespace FogOfPawn.Patches
{
    /// <summary>
    /// Patches Pawn_SkillTracker.AverageOfRelevantSkillsFor to ensure it uses MASKED skill levels
    /// when called during rendering (e.g. for the Work Tab "red box" check).
    /// The vanilla method often accesses the 'levelInt' field directly, bypassing our property patch.
    /// </summary>
    [HarmonyPatch(typeof(Pawn_SkillTracker), "AverageOfRelevantSkillsFor")]
    public static class Patch_Pawn_SkillTracker_AverageOfRelevantSkillsFor
    {
        private static int _debugLogCount = 0;
        
        public static void Postfix(Pawn_SkillTracker __instance, WorkTypeDef workDef, ref float __result)
        {
            if (!RenderContext.IsRendering) return;

            Pawn pawn = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();
            if (pawn == null) return;

            var comp = pawn.GetComp<CompPawnFog>();
            if (comp == null || !comp.compInitialized || comp.fullyRevealed) return;
            
            // Skip truthful pawns - they show real values
            if (comp.tier == DeceptionTier.Truthful) return;

            if (workDef.relevantSkills.Count == 0) return;

            // Recalculate using masked values where appropriate
            float total = 0f;
            foreach (var skillDef in workDef.relevantSkills)
            {
                var skill = pawn.skills.GetSkill(skillDef);
                if (skill == null) continue;
                
                int level;
                
                // Check if this skill should be masked
                if (FogMaskUtility.ShouldMaskSkill(pawn, skillDef, comp))
                {
                    // Use masked level for fogged skills
                    level = FogMaskUtility.GetMaskedSkillLevel(pawn, skillDef, comp);
                }
                else
                {
                    // Use real level for revealed skills (directly access levelInt to avoid recursion)
                    level = skill.levelInt;
                }
                
                total += level;
                
                // Debug logging
                if (Prefs.DevMode && _debugLogCount < 50 && comp.tier != DeceptionTier.Truthful)
                {
                    _debugLogCount++;
                    bool shouldMask = FogMaskUtility.ShouldMaskSkill(pawn, skillDef, comp);
                    FogLog.Verbose($"[WORK TAB] {pawn.LabelShort} {workDef.defName}/{skillDef.defName}: real={skill.levelInt}, used={level}, shouldMask={shouldMask}");
                }
            }

            __result = total / workDef.relevantSkills.Count;
        }
    }
}
