using HarmonyLib;
using RimWorld;
using Verse;

namespace FogOfPawn.Patches
{
    [HarmonyPatch(typeof(LordJob_Ritual), "End")]
    public static class Patch_LordJob_Ritual_End
    {
        public static void Postfix(bool completedSuccessfully)
        {
            if (!completedSuccessfully) return;
            var tracker = Current.Game.GetComponent<GameComponent_FogTracker>();
            if (tracker == null) return;

            var settings = FogSettingsCache.Current;
            if (!settings.ritualExtraJoiner) return;

            if (!tracker.HasPendingJoiner() && tracker.HasSpawnedJoiner())
            {
                if (Rand.Chance(settings.ritualExtraJoinerPct / 100f))
                    tracker.ForceImmediateJoiner(true);
            }
            else if (!tracker.HasSpawnedJoiner())
            {
                tracker.ForceImmediateJoiner(true);
            }
            else if (settings.ritualExtraJoiner && tracker.CooldownPassed)
            {
                if (Rand.Chance(settings.ritualExtraJoinerPct / 100f))
                    tracker.ForceImmediateJoiner(true);
            }
        }
    }
} 