using HarmonyLib;
using RimWorld;
using Verse;
using UnityEngine;

namespace FogOfPawn.Patches
{
    [HarmonyPatch(typeof(SkillRecord), "get_Level")]
    public static class Patch_SkillRecord_GetLevel_Mask
    {
        private static readonly System.Reflection.FieldInfo _pawnField = 
            AccessTools.Field(typeof(SkillRecord), "pawn");
        
        private static int _debugLogCount = 0;
        
        static void Postfix(SkillRecord __instance, ref int __result)
        {
            if (!RenderContext.IsRendering) return;
            ApplyMask(__instance, ref __result, "get_Level");
        }

        public static void ApplyMask(SkillRecord instance, ref int result, string source)
        {
            try
            {
                if (instance == null) return;
                
                Pawn pawn = _pawnField?.GetValue(instance) as Pawn;
                if (pawn == null) return;
                
                var comp = pawn.GetComp<CompPawnFog>();
                
                if (Prefs.DevMode && _debugLogCount < 200 && pawn.LabelShort == "Olive")
                {
                    _debugLogCount++;
                    string reason = "";
                    if (comp == null) reason = "No comp";
                    else if (!comp.compInitialized) reason = "Not init";
                    else if (comp.fullyRevealed) reason = "Fully revealed";
                    else if (comp.tier == DeceptionTier.Truthful) reason = "Truthful tier";
                    else if (comp.revealedSkills.Contains(instance.def)) reason = "Already revealed";
                    
                    if (reason != "")
                    {
                        Log.Message($"[FogOfPawn] Masking SKIPPED ({source}) for {pawn.LabelShort} {instance.def.defName}. Reason: {reason}");
                    }
                }

                if (comp == null || !comp.compInitialized || comp.fullyRevealed || comp.tier == DeceptionTier.Truthful) return;
                
                if (FogMaskUtility.ShouldMaskSkill(pawn, instance.def, comp))
                {
                    int originalResult = result;
                    result = FogMaskUtility.GetMaskedSkillLevel(pawn, instance.def, comp);
                    
                    if (Prefs.DevMode && _debugLogCount < 200)
                    {
                        _debugLogCount++;
                        Log.Message($"[FogOfPawn] MASK APPLIED ({source}) to {pawn.LabelShort} {instance.def.defName}: {originalResult} → {result}");
                    }
                }
            }
            catch (System.Exception ex)
            {
                if (Prefs.DevMode)
                {
                    Log.Warning($"[FogOfPawn] Exception in SkillMask ApplyMask: {ex.Message}");
                }
            }
        }
    }

    [HarmonyPatch(typeof(SkillRecord), "GetLevel")]
    public static class Patch_SkillRecord_GetLevel_Method_Mask
    {
        static void Postfix(SkillRecord __instance, ref int __result)
        {
            if (!RenderContext.IsRendering) return;
            Patch_SkillRecord_GetLevel_Mask.ApplyMask(__instance, ref __result, "GetLevel(bool)");
        }
    }
    
    public static class Patch_SkillRecord_GetPassion_Mask
    {
        private static readonly System.Reflection.FieldInfo _pawnField = 
            AccessTools.Field(typeof(SkillRecord), "pawn");
        
        static void Postfix(SkillRecord __instance, ref Passion __result)
        {
            if (!RenderContext.IsRendering) return;
            
            try
            {
                if (__instance == null) return;
                
                Pawn pawn = _pawnField?.GetValue(__instance) as Pawn;
                if (pawn == null) return;
                
                var comp = pawn.GetComp<CompPawnFog>();
                if (comp == null || !comp.compInitialized) return;
                
                if (FogMaskUtility.ShouldMaskSkill(pawn, __instance.def, comp))
                {
                    __result = FogMaskUtility.GetMaskedPassion(pawn, __instance.def, comp);
                }
            }
            catch (System.Exception ex)
            {
                if (Prefs.DevMode)
                {
                    Log.Warning($"[FogOfPawn] Exception in PassionMask patch: {ex.Message}");
                }
            }
        }
    }
}
