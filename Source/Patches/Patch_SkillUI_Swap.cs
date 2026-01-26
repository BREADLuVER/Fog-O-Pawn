using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using FogOfPawn;
using System.Reflection;

namespace FogOfPawn.Patches
{
    [HarmonyPatch]
    public static class Patch_SkillUI_Swap
    {
        [HarmonyPatch(typeof(SkillUI), "DrawSkill", new[] { typeof(SkillRecord), typeof(Rect), typeof(SkillUI.SkillDrawMode), typeof(string) })]
        [HarmonyPrefix]
        public static void Prefix_DrawSkill(ref SkillRecord skill)
        {
            if (skill == null) return;
            skill = FogMaskUtility.CreateFakeSkillRecord(skill);
        }

        [HarmonyPatch(typeof(SkillUI), "GetSkillDescription", new[] { typeof(SkillRecord) })]
        [HarmonyPrefix]
        public static void Prefix_GetSkillDescription(ref SkillRecord sk)
        {
            if (sk == null) return;
            sk = FogMaskUtility.CreateFakeSkillRecord(sk);
        }
    }
}
