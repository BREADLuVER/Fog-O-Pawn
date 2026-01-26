using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace FogOfPawn.Patches
{
    [StaticConstructorOnStartup]
    public static class Patch_WidgetsWork_DrawWorkBoxFor
    {
        private static int _debugLogCount = 0;
        
        static Patch_WidgetsWork_DrawWorkBoxFor()
        {
            try
            {
                var harmony = new Harmony("FogOfPawn.WidgetsWork");
                
                Type widgetsWorkType = AccessTools.TypeByName("RimWorld.WidgetsWork")
                                    ?? AccessTools.TypeByName("WidgetsWork");
                
                if (widgetsWorkType == null)
                {
                    FogLog.Fail("WidgetsWork", "Could not find WidgetsWork type - Work Tab masking may be incomplete.");
                    return;
                }
                
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
