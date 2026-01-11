using Verse;
using UnityEngine;
using HarmonyLib;
using RimWorld;
using System.Linq;
using System.Collections.Generic;

namespace FogOfPawn
{
    [StaticConstructorOnStartup]
    internal static class Startup
    {
        static Startup()
        {
            var harmony = new Harmony("FogOfPawn");
            harmony.PatchAll();
            FogLog.Reflect("HarmonyPatched", "Harmony patches applied.");

            // Check if mod was removed and cleanup if needed
            CheckAndCleanupOnModRemoval();

            // DebugActionsUtility not available in release API; dev spawning via Sleeper gizmo remains.
        }

        // DevJoiner helper kept for possible future debug builds
        private static void DevJoiner(bool sleeper) { }

        /// <summary>
        /// Checks if the Fog-O-Pawn mod is missing from the current mod list and performs cleanup if needed.
        /// This ensures safe mod removal without breaking saves.
        /// </summary>
        private static void CheckAndCleanupOnModRemoval()
        {
            // Check if we're in a game and if the mod is missing
            if (Current.Game == null) return;

            bool modStillLoaded = LoadedModManager.RunningModsListForReading.Any(m => m.PackageIdPlayerFacing == "Fog.Of.Pawn");
            if (modStillLoaded) return;

            // Mod was removed - perform emergency cleanup
            FogLog.Reflect("ModRemoval", "Fog-O-Pawn mod detected as removed. Performing emergency cleanup...");
            PerformEmergencyCleanup();
        }

        /// <summary>
        /// Performs emergency cleanup when the mod is removed mid-save.
        /// Reveals all hidden data and removes all mod components to prevent crashes.
        /// </summary>
        public static void PerformEmergencyCleanup()
        {
            try
            {
                // 1. Force reveal all hidden data for all pawns
                RevealAllPawnData();

                // 2. Remove all CompPawnFog components
                RemoveAllFogComponents();

                // 3. Clean up mod-specific items
                CleanupModItems();

                // 4. Remove the game component
                RemoveGameComponent();

                // 5. Clean up game history and archives
                CleanupGameHistory();

                // 6. Clean up apparel policies and other references
                CleanupApparelPolicies();

                FogLog.Reflect("CleanupComplete", "Emergency cleanup completed successfully. Save is now safe without Fog-O-Pawn.");
            }
            catch (System.Exception e)
            {
                Log.Error($"Fog-O-Pawn cleanup failed: {e.Message}");
            }
        }

        /// <summary>
        /// Reveals all hidden skills, traits, and other data for all pawns on the map.
        /// </summary>
        private static void RevealAllPawnData()
        {
            var allPawns = new List<Pawn>();
            
            // Get all pawns from current map
            if (Current.Game.CurrentMap != null)
            {
                allPawns.AddRange(Current.Game.CurrentMap.mapPawns.AllPawns);
            }
            
            // Get all world pawns
            allPawns.AddRange(Find.WorldPawns.AllPawnsAlive);

            foreach (var pawn in allPawns)
            {
                if (pawn == null || pawn.DestroyedOrNull()) continue;

                var comp = pawn.GetComp<CompPawnFog>();
                if (comp == null) continue;

                // Force reveal everything
                comp.fullyRevealed = true;

                // Apply all masked skills back to their true values
                foreach (var skillDef in comp.maskOffsets.Keys.ToList())
                {
                    if (pawn.skills != null)
                    {
                        var skill = pawn.skills.GetSkill(skillDef);
                        if (skill != null)
                        {
                            // Apply the offset to restore true skill level
                            int offset = comp.maskOffsets[skillDef];
                            skill.Level = Mathf.Max(0, skill.Level + offset);
                        }
                    }
                }

                // Apply passion offsets
                foreach (var skillDef in comp.passionOffsets.Keys.ToList())
                {
                    if (pawn.skills != null)
                    {
                        var skill = pawn.skills.GetSkill(skillDef);
                        if (skill != null)
                        {
                            int offset = comp.passionOffsets[skillDef];
                            if (offset == -1)
                            {
                                skill.passion = Passion.None;
                            }
                            else if (offset == 1)
                            {
                                skill.passion = Passion.Minor;
                            }
                            else if (offset == 2)
                            {
                                skill.passion = Passion.Major;
                            }
                        }
                    }
                }

                // Reveal all traits
                comp.revealedTraits.Clear();
                if (pawn.story?.traits != null)
                {
                    foreach (var trait in pawn.story.traits.allTraits)
                    {
                        comp.revealedTraits.Add(trait.def);
                    }
                }

                // Clear all masking data
                comp.maskOffsets.Clear();
                comp.passionOffsets.Clear();
                comp.reportedSkills.Clear();
                comp.reportedPassions.Clear();
                comp.revealedSkills.Clear();
            }
        }

        /// <summary>
        /// Removes all CompPawnFog components from all pawns to prevent broken references.
        /// </summary>
        private static void RemoveAllFogComponents()
        {
            var allPawns = new List<Pawn>();
            
            if (Current.Game.CurrentMap != null)
            {
                allPawns.AddRange(Current.Game.CurrentMap.mapPawns.AllPawns);
            }
            
            allPawns.AddRange(Find.WorldPawns.AllPawnsAlive);

            foreach (var pawn in allPawns)
            {
                if (pawn == null || pawn.DestroyedOrNull()) continue;

                // Remove all CompPawnFog components
                pawn.AllComps.RemoveAll(comp => comp is CompPawnFog);
            }
        }

        /// <summary>
        /// Removes or converts all mod-specific items (Disguise Kits).
        /// </summary>
        private static void CleanupModItems()
        {
            if (Current.Game.CurrentMap == null) return;

            var disguiseKitDef = DefDatabase<ThingDef>.GetNamedSilentFail("FogOfPawn_DisguiseKit");
            if (disguiseKitDef == null) return;

            // Find and destroy all disguise kits
            var disguiseKits = Current.Game.CurrentMap.listerThings.ThingsOfDef(disguiseKitDef);
            foreach (var kit in disguiseKits.ToList())
            {
                if (kit != null && !kit.DestroyedOrNull())
                {
                    kit.Destroy();
                }
            }
        }

        /// <summary>
        /// Removes the GameComponent_FogTracker from the game.
        /// </summary>
        private static void RemoveGameComponent()
        {
            if (Current.Game == null) return;

            var fogTracker = Current.Game.GetComponent<GameComponent_FogTracker>();
            if (fogTracker != null)
            {
                Current.Game.components.Remove(fogTracker);
            }
        }

        /// <summary>
        /// Cleans up game history and archives to remove mod-specific letters and references.
        /// </summary>
        private static void CleanupGameHistory()
        {
            if (Current.Game == null || Current.Game.history == null || Current.Game.history.archive == null) return;

            // Use reflection to find any List<IArchivable> fields in the archive object
            var archive = Current.Game.history.archive;
            var fields = archive.GetType().GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            foreach (var field in fields)
            {
                if (typeof(System.Collections.IList).IsAssignableFrom(field.FieldType))
                {
                    var list = field.GetValue(archive) as System.Collections.IList;
                    if (list == null) continue;
                    // Remove any ChoiceLetter_DeceiverJoiner from the list
                    for (int i = list.Count - 1; i >= 0; i--)
                    {
                        var item = list[i];
                        if (item != null && item.GetType().FullName == "FogOfPawn.ChoiceLetter_DeceiverJoiner")
                        {
                            list.RemoveAt(i);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Cleans up apparel policies and other references to mod-specific items.
        /// </summary>
        private static void CleanupApparelPolicies()
        {
            if (Current.Game == null || Current.Game.outfitDatabase == null) return;

            var disguiseKitDef = DefDatabase<ThingDef>.GetNamedSilentFail("FogOfPawn_DisguiseKit");
            if (disguiseKitDef == null) return;

            // Clean up all apparel policies that reference the disguise kit
            foreach (var policy in Current.Game.outfitDatabase.AllOutfits)
            {
                if (policy?.filter != null)
                {
                    policy.filter.SetAllow(disguiseKitDef, false);
                }
            }

            // Note: Apparel policies are handled differently in RimWorld, but outfits should cover most cases
        }
    }

    // Empty Mod subclass so we show up in mod settings list (settings added later)
    public class FogOfPawnMod : Mod
    {
        public static FogOfPawnSettings Settings;

        public FogOfPawnMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<FogOfPawnSettings>();
        }

        public override string SettingsCategory() => "Fog of Pawn";

        public override void DoSettingsWindowContents(UnityEngine.Rect inRect)
        {
            Settings.DoWindowContents(inRect);
        }
    }
} 