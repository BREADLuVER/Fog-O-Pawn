using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace FogOfPawn.Patches
{
    /// <summary>
    /// Patches WidgetsWork.DrawWorkBoxFor to ensure RenderContext is active during Work Tab rendering.
    /// This provides an additional safety layer for the skill masking system in the Work Tab,
    /// ensuring the work box colors use masked skill levels instead of real levels.
    /// </summary>
    [StaticConstructorOnStartup]
    public static class Patch_WidgetsWork_DrawWorkBoxFor
    {
        private static int _debugLogCount = 0;
        
        static Patch_WidgetsWork_DrawWorkBoxFor()
        {
            try
            {
                var harmony = new Harmony("FogOfPawn.WidgetsWork");
                
                // Find WidgetsWork type
                Type widgetsWorkType = AccessTools.TypeByName("RimWorld.WidgetsWork")
                                    ?? AccessTools.TypeByName("WidgetsWork");
                
                if (widgetsWorkType == null)
                {
                    FogLog.Fail("WidgetsWork", "Could not find WidgetsWork type - Work Tab masking may be incomplete.");
                    return;
                }
                
                // Patch DrawWorkBoxFor method(s)
                var methods = widgetsWorkType.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                int patchCount = 0;
                
                foreach (var method in methods)
                {
                    if (method.Name == "DrawWorkBoxFor" || method.Name == "DrawWorkBoxBackground")
                    {
                        try
                        {
                            harmony.Patch(method,
                                prefix: new HarmonyMethod(typeof(Patch_WidgetsWork_DrawWorkBoxFor), nameof(Prefix)),
                                finalizer: new HarmonyMethod(typeof(Patch_WidgetsWork_DrawWorkBoxFor), nameof(Finalizer)));
                            patchCount++;
                            
                            if (Prefs.DevMode)
                            {
                                Log.Message($"[FogOfPawn] Patched {widgetsWorkType.Name}.{method.Name} for RenderContext");
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Warning($"[FogOfPawn] Failed to patch {method.Name}: {ex.Message}");
                        }
                    }
                }
                
                // Also try to patch MainTabWindow_Work.DoWindowContents for complete coverage
                Type mainTabWorkType = AccessTools.TypeByName("RimWorld.MainTabWindow_Work")
                                    ?? AccessTools.TypeByName("MainTabWindow_Work");
                
                if (mainTabWorkType != null)
                {
                    var doContentsMethod = AccessTools.Method(mainTabWorkType, "DoWindowContents");
                    if (doContentsMethod != null)
                    {
                        harmony.Patch(doContentsMethod,
                            prefix: new HarmonyMethod(typeof(Patch_WidgetsWork_DrawWorkBoxFor), nameof(Prefix)),
                            finalizer: new HarmonyMethod(typeof(Patch_WidgetsWork_DrawWorkBoxFor), nameof(Finalizer)));
                        patchCount++;
                        
                        if (Prefs.DevMode)
                        {
                            Log.Message($"[FogOfPawn] Patched MainTabWindow_Work.DoWindowContents for RenderContext");
                        }
                    }
                }
                
                if (patchCount > 0)
                {
                    Log.Message($"[FogOfPawn] Work Tab patches applied ({patchCount} methods) - skill masking should work in Work Tab");
                }
                else
                {
                    Log.Warning("[FogOfPawn] No Work Tab methods found to patch - Work Tab masking may not work correctly");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[FogOfPawn] Exception patching Work Tab: {ex.Message}\n{ex.StackTrace}");
            }
        }
        
        public static void Prefix()
        {
            RenderContext.BeginRender();
            
            // Debug logging on first call
            if (Prefs.DevMode && _debugLogCount < 5)
            {
                _debugLogCount++;
                FogLog.Verbose($"[WORK TAB] Prefix called, RenderContext.IsRendering={RenderContext.IsRendering}");
            }
        }
        
        public static void Finalizer()
        {
            RenderContext.EndRender();
        }
    }
}
