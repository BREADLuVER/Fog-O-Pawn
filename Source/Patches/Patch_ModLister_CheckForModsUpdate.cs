using HarmonyLib;
using RimWorld;
using Verse;
using System.Linq;
using FogOfPawn;
using System.Reflection;

namespace FogOfPawn.Patches
{
    /// <summary>
    /// Patch to detect when the mod list changes and trigger cleanup if Fog-O-Pawn is removed.
    /// This ensures safe mod removal even when the mod is removed while the game is running.
    /// </summary>
    [HarmonyPatch]
    public static class Patch_ModLister_CheckForModsUpdate
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(ModLister), "CheckForModsUpdate");
        }

        private static bool Prepare()
        {
            return TargetMethod() != null;
        }

        private static bool wasModLoaded = true; // assume present at start

        [HarmonyPostfix]
        public static void Postfix()
        {
            // Only executes if method exists and was patched
            if (Current.Game == null) return;

            // Check if Fog-O-Pawn is currently loaded
            bool isModCurrentlyLoaded = LoadedModManager.RunningModsListForReading.Any(m => m.PackageIdPlayerFacing == "Fog.Of.Pawn");

            // If the mod was previously loaded but is no longer loaded, trigger cleanup
            if (wasModLoaded && !isModCurrentlyLoaded)
            {
                FogLog.Reflect("ModRemovalDetected", "Fog-O-Pawn mod removal detected during gameplay. Triggering emergency cleanup...");
                Startup.PerformEmergencyCleanup();
            }

            // Update our tracking
            wasModLoaded = isModCurrentlyLoaded;
        }
    }
} 