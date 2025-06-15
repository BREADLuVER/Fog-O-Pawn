using HarmonyLib;
using RimWorld;
using Verse;

namespace FogOfPawn.Patches
{
    [HarmonyPatch(typeof(SkillRecord), "Learn")]
    public static class Patch_SkillRecord_Learn
    {
        public static void Postfix(SkillRecord __instance, Pawn ___pawn)
        {
            var comp = ___pawn.GetComp<CompPawnFog>();
            if (comp == null || comp.revealedSkills.Contains(__instance.def))
            {
                return;
            }

            if (__instance.xpSinceLastLevel >= 300)
            {
                comp.RevealSkill(__instance.def);
            }
        }
    }
} 