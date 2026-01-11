using HarmonyLib;
using RimWorld;
using Verse;
using System.Linq;
using FogOfPawn;

namespace FogOfPawn.Patches
{
    /// <summary>
    /// Patch to detect when a game is loaded and check if Fog-O-Pawn mod is missing.
    /// This ensures cleanup happens when loading a save that had the mod but it's now removed.
    /// </summary>
    [HarmonyPatch(typeof(Game), "LoadGame")]
    public static class Patch_Game_LoadGame
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            // Check if Fog-O-Pawn mod is missing from the current mod list
            bool modStillLoaded = LoadedModManager.RunningModsListForReading.Any(m => m.PackageIdPlayerFacing == "Fog.Of.Pawn");
            if (!modStillLoaded)
            {
                // Mod is missing - check if this save has any Fog-O-Pawn data that needs cleanup
                if (Current.Game != null)
                {
                    bool hasFogData = false;

                    // Check for CompPawnFog components
                    if (Current.Game.CurrentMap != null)
                    {
                        foreach (var pawn in Current.Game.CurrentMap.mapPawns.AllPawns)
                        {
                            if (pawn?.GetComp<CompPawnFog>() != null)
                            {
                                hasFogData = true;
                                break;
                            }
                        }
                    }

                    // Check for GameComponent_FogTracker
                    if (!hasFogData && Current.Game.GetComponent<GameComponent_FogTracker>() != null)
                    {
                        hasFogData = true;
                    }

                    // Check for disguise kits
                    if (!hasFogData && Current.Game.CurrentMap != null)
                    {
                        var disguiseKitDef = DefDatabase<ThingDef>.GetNamedSilentFail("FogOfPawn_DisguiseKit");
                        if (disguiseKitDef != null)
                        {
                            var disguiseKits = Current.Game.CurrentMap.listerThings.ThingsOfDef(disguiseKitDef);
                            if (disguiseKits.Any())
                            {
                                hasFogData = true;
                            }
                        }
                    }

                    // If we found Fog-O-Pawn data but the mod is missing, perform cleanup
                    if (hasFogData)
                    {
                        FogLog.Reflect("LoadGameModRemoval", "Loading save with Fog-O-Pawn data but mod is missing. Performing cleanup...");
                        Startup.PerformEmergencyCleanup();
                    }
                }
                return;
            }

            // Mod is still loaded - initialize any pawns that have uninitialized fog comps
            // This happens for saves created before fog system existed or during migration
            InitializeUninitializedPawns();
        }

        private static void InitializeUninitializedPawns()
        {
            if (Current.Game == null) return;

            int initCount = 0;
            var allPawns = new System.Collections.Generic.List<Pawn>();

            // Collect all pawns from current map and world
            if (Current.Game.CurrentMap != null)
            {
                allPawns.AddRange(Current.Game.CurrentMap.mapPawns.AllPawns);
            }
            allPawns.AddRange(Find.WorldPawns.AllPawnsAlive);

            foreach (var pawn in allPawns)
            {
                if (pawn == null || pawn.DestroyedOrNull()) continue;

                var comp = pawn.GetComp<CompPawnFog>();
                if (comp == null) continue;

                // Skip if already initialized
                if (comp.compInitialized) continue;

                // Skip if invalid state
                if (pawn.skills == null || pawn.story == null)
                {
                    FogLog.Verbose($"Cannot initialize {pawn.LabelShort} - missing skills or story");
                    continue;
                }

                // Initialize now that the pawn is fully loaded
                FogInitializer.InitializeFogFor(pawn);
                initCount++;
            }

            if (initCount > 0)
            {
                FogLog.Reflect("PostLoadInit", $"Initialized fog for {initCount} pawns after game load.");
            }
        }
    }
} 