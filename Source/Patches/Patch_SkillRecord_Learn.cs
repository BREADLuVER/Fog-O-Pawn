using HarmonyLib;
using RimWorld;
using Verse;
using FogOfPawn;

namespace FogOfPawn.Patches
{
    [HarmonyPatch(typeof(SkillRecord), "Learn")]
    public static class Patch_SkillRecord_Learn
    {
        public static void Postfix(SkillRecord __instance, Pawn ___pawn)
        {
            var comp = ___pawn.GetComp<CompPawnFog>();
            if (comp == null || !comp.compInitialized || comp.revealedSkills.Contains(__instance.def))
            {
                return;
            }

            if (!FogSettingsCache.Current.fogSkills)
            {
                return;
            }

            int thresholdSetting = FogSettingsCache.Current.xpToReveal;
            float learnFactor = ___pawn.GetStatValue(StatDefOf.GlobalLearningFactor);
            int adjustedThreshold = (int)(thresholdSetting * learnFactor);
            if (__instance.xpSinceLastLevel >= adjustedThreshold)
            {
                comp.RevealSkill(__instance.def);
            }
        }
    }
} 