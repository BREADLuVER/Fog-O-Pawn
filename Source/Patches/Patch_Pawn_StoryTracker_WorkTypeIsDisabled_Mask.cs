using System;
using HarmonyLib;
using RimWorld;
using Verse;
using System.Reflection;

namespace FogOfPawn.Patches
{
    /// <summary>
    /// Makes fogged pawns appear capable of work types that they are actually incapable of
    /// until they are revealed. Uses reflection so it remains compatible across game versions
    /// even if the underlying method name changes.
    /// </summary>
    [StaticConstructorOnStartup]
    public static class Patch_Pawn_StoryTracker_WorkTypeIsDisabled_Mask
    {
        static Patch_Pawn_StoryTracker_WorkTypeIsDisabled_Mask()
        {
            try
            {
                var harmony = new Harmony("FogOfPawn.WorkTypeMask");

                // Primary target: Pawn_StoryTracker.WorkTypeIsDisabled
                var target = AccessTools.Method(typeof(Pawn_StoryTracker), "WorkTypeIsDisabled");

                if (target == null)
                {
                    // Fallback: Pawn.WorkTypeIsDisabled (often a wrapper)
                    target = AccessTools.Method(typeof(Pawn), "WorkTypeIsDisabled");
                }

                if (target == null)
                {
                    // Advanced Fallback: find ANY bool-returning instance method with WorkTypeDef parameter in StoryTracker
                    foreach (var mi in typeof(Pawn_StoryTracker).GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                    {
                        if (mi.ReturnType != typeof(bool)) continue;
                        var pars = mi.GetParameters();
                        if (pars.Length == 0) continue;
                        if (pars[0].ParameterType != typeof(WorkTypeDef)) continue;

                        string n = mi.Name;
                        if (n.Contains("WorkType") && (n.Contains("Disabled") || n.Contains("IsDisabled")))
                        {
                            target = mi;
                            break;
                        }
                    }
                }

                if (target == null)
                {
                    FogLog.Fail("WorkTypeIsDisabled", "Could not locate suitable WorkType disabled checker in Pawn or StoryTracker – work-type masking disabled.");
                    return;
                }

                // Use the correct postfix based on where we found the method
                if (target.DeclaringType == typeof(Pawn))
                {
                    harmony.Patch(target, postfix: new HarmonyMethod(typeof(Patch_Pawn_StoryTracker_WorkTypeIsDisabled_Mask), nameof(PostfixPawn)));
                }
                else
                {
                    harmony.Patch(target, postfix: new HarmonyMethod(typeof(Patch_Pawn_StoryTracker_WorkTypeIsDisabled_Mask), nameof(PostfixTracker)));
                }
                
                FogLog.Reflect("WorkTypeMaskPatched", $"Patched {target.DeclaringType.Name}.{target.Name} for fog masking.");
            }
            catch (Exception ex)
            {
                Log.Error("[FogOfPawn] Exception while patching WorkTypeIsDisabled: " + ex);
            }
        }

        public static void PostfixTracker(Pawn_StoryTracker __instance, WorkTypeDef w, ref bool __result)
        {
            if (!__result) return;
            Pawn pawn = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();
            ApplyWorkMask(pawn, ref __result);
        }

        public static void PostfixPawn(Pawn __instance, WorkTypeDef w, ref bool __result)
        {
            if (!__result) return;
            ApplyWorkMask(__instance, ref __result);
        }

        private static void ApplyWorkMask(Pawn pawn, ref bool result)
        {
            if (pawn == null) return;
            var comp = pawn.GetComp<CompPawnFog>();
            if (comp == null || !comp.compInitialized || comp.fullyRevealed) return;
            
            // Masking logic: until revealed, show as capable
            result = false;
        }
    }
} 