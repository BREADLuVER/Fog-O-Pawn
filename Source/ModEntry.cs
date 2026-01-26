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

            CheckAndCleanupOnModRemoval();

        }

        private static void DevJoiner(bool sleeper) { }

        private static void CheckAndCleanupOnModRemoval()
        {
            if (Current.Game == null) return;

            bool modStillLoaded = LoadedModManager.RunningModsListForReading.Any(m => m.PackageIdPlayerFacing == "Fog.Of.Pawn");
            if (modStillLoaded) return;

            FogLog.Reflect("ModRemoval", "Fog-O-Pawn mod detected as removed. Performing emergency cleanup...");
            PerformEmergencyCleanup();
        }

        public static void PerformEmergencyCleanup()
        {
            try
            {
                RevealAllPawnData();

                RemoveAllFogComponents();

                CleanupModItems();

                RemoveGameComponent();

                CleanupGameHistory();

                CleanupApparelPolicies();

                FogLog.Reflect("CleanupComplete", "Emergency cleanup completed successfully. Save is now safe without Fog-O-Pawn.");
            }
            catch (System.Exception e)
            {
                Log.Error($"Fog-O-Pawn cleanup failed: {e.Message}");
            }
        }

        private static void RevealAllPawnData()
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

                var comp = pawn.GetComp<CompPawnFog>();
                if (comp == null) continue;

                comp.fullyRevealed = true;

                foreach (var skillDef in comp.maskOffsets.Keys.ToList())
                {
                    if (pawn.skills != null)
                    {
                        var skill = pawn.skills.GetSkill(skillDef);
                        if (skill != null)
                        {
                            int offset = comp.maskOffsets[skillDef];
                            skill.Level = Mathf.Max(0, skill.Level + offset);
                        }
                    }
                }

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

                comp.revealedTraits.Clear();
                if (pawn.story?.traits != null)
                {
                    foreach (var trait in pawn.story.traits.allTraits)
                    {
                        comp.revealedTraits.Add(trait.def);
                    }
                }

                comp.maskOffsets.Clear();
                comp.passionOffsets.Clear();
                comp.reportedSkills.Clear();
                comp.reportedPassions.Clear();
                comp.revealedSkills.Clear();
            }
        }

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

                pawn.AllComps.RemoveAll(comp => comp is CompPawnFog);
            }
        }

        private static void CleanupModItems()
        {
            if (Current.Game.CurrentMap == null) return;

            var disguiseKitDef = DefDatabase<ThingDef>.GetNamedSilentFail("FogOfPawn_DisguiseKit");
            if (disguiseKitDef == null) return;

            var disguiseKits = Current.Game.CurrentMap.listerThings.ThingsOfDef(disguiseKitDef);
            foreach (var kit in disguiseKits.ToList())
            {
                if (kit != null && !kit.DestroyedOrNull())
                {
                    kit.Destroy();
                }
            }
        }

        private static void RemoveGameComponent()
        {
            if (Current.Game == null) return;

            var fogTracker = Current.Game.GetComponent<GameComponent_FogTracker>();
            if (fogTracker != null)
            {
                Current.Game.components.Remove(fogTracker);
            }
        }

        private static void CleanupGameHistory()
        {
            if (Current.Game == null || Current.Game.history == null || Current.Game.history.archive == null) return;

            var archive = Current.Game.history.archive;
            var fields = archive.GetType().GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            foreach (var field in fields)
            {
                if (typeof(System.Collections.IList).IsAssignableFrom(field.FieldType))
                {
                    var list = field.GetValue(archive) as System.Collections.IList;
                    if (list == null) continue;
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

        private static void CleanupApparelPolicies()
        {
            if (Current.Game == null || Current.Game.outfitDatabase == null) return;

            var disguiseKitDef = DefDatabase<ThingDef>.GetNamedSilentFail("FogOfPawn_DisguiseKit");
            if (disguiseKitDef == null) return;

            foreach (var policy in Current.Game.outfitDatabase.AllOutfits)
            {
                if (policy?.filter != null)
                {
                    policy.filter.SetAllow(disguiseKitDef, false);
                }
            }

        }
    }

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