using HarmonyLib;
using RimWorld;
using Verse;
using FogOfPawn;

namespace FogOfPawn.Patches
{
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
            
            if (comp.tier == DeceptionTier.Truthful) return;

            if (workDef.relevantSkills.Count == 0) return;

            float total = 0f;
            foreach (var skillDef in workDef.relevantSkills)
            {
                var skill = pawn.skills.GetSkill(skillDef);
                if (skill == null) continue;
                
                int level;
                
                if (FogMaskUtility.ShouldMaskSkill(pawn, skillDef, comp))
                {
                    level = FogMaskUtility.GetMaskedSkillLevel(pawn, skillDef, comp);
                }
                else
                {
                    level = skill.levelInt;
                }
                
                total += level;
                
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
