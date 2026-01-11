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

                // The pre-1.6 method is WorkTypeIsDisabled(WorkTypeDef). RimWorld 1.6 renamed it, so we attempt
                // a signature search to stay version-agnostic.
                var target = AccessTools.Method(typeof(Pawn_StoryTracker), "WorkTypeIsDisabled", new[] { typeof(WorkTypeDef) });

                if (target == null)
                {
                    // Fallback: find ANY bool-returning instance method with WorkTypeDef parameter.
                    foreach (var mi in typeof(Pawn_StoryTracker).GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                    {
                        if (mi.ReturnType != typeof(bool)) continue;
                        var pars = mi.GetParameters();
                        if (pars.Length != 1) continue;
                        if (pars[0].ParameterType != typeof(WorkTypeDef)) continue;

                        // Heuristic: method name contains "WorkType" and "Disabled" or "IsDisabled"
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
                    FogLog.Fail("WorkTypeIsDisabled", "Could not locate suitable WorkType disabled checker – work-type masking disabled.");
                    return;
                }

                harmony.Patch(target, postfix: new HarmonyMethod(typeof(Patch_Pawn_StoryTracker_WorkTypeIsDisabled_Mask), nameof(Postfix)));
                FogLog.Reflect("WorkTypeMaskPatched", "Patched Pawn_StoryTracker.WorkTypeIsDisabled for fog masking.");
            }
            catch (Exception ex)
            {
                Log.Error("[FogOfPawn] Exception while patching WorkTypeIsDisabled: " + ex);
            }
        }

        // Postfix executes only if method exists and was successfully patched.
        // ReSharper disable once InconsistentNaming – Harmony uses parameter names.
        public static void Postfix(Pawn_StoryTracker __instance, WorkTypeDef w, ref bool __result)
        {
            if (!__result) return; // already enabled

            Pawn pawn = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();
            if (pawn == null) return;

            var comp = pawn.GetComp<CompPawnFog>();
            if (comp == null || !comp.compInitialized || comp.fullyRevealed) return;

            // Until fully revealed, pretend they can do everything (imposters especially!)
            __result = false;
        }
    }
} 