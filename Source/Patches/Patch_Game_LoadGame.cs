using HarmonyLib;
using RimWorld;
using Verse;
using System.Linq;
using FogOfPawn;

namespace FogOfPawn.Patches
{
    [HarmonyPatch(typeof(Game), "LoadGame")]
    public static class Patch_Game_LoadGame
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            bool modStillLoaded = LoadedModManager.RunningModsListForReading.Any(m => m.PackageIdPlayerFacing == "Fog.Of.Pawn");
            if (!modStillLoaded)
            {
                if (Current.Game != null)
                {
                    bool hasFogData = false;

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

                    if (!hasFogData && Current.Game.GetComponent<GameComponent_FogTracker>() != null)
                    {
                        hasFogData = true;
                    }

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

                    if (hasFogData)
                    {
                        FogLog.Reflect("LoadGameModRemoval", "Loading save with Fog-O-Pawn data but mod is missing. Performing cleanup...");
                        Startup.PerformEmergencyCleanup();
                    }
                }
                return;
            }

            InitializeUninitializedPawns();
        }

        private static void InitializeUninitializedPawns()
        {
            if (Current.Game == null) return;

            int initCount = 0;
            var allPawns = new System.Collections.Generic.List<Pawn>();

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

                if (comp.compInitialized) continue;

                if (pawn.skills == null || pawn.story == null)
                {
                    FogLog.Verbose($"Cannot initialize {pawn.LabelShort} - missing skills or story");
                    continue;
                }

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