using HarmonyLib;
using RimWorld;
using Verse;
using UnityEngine;

namespace FogOfPawn.Patches
{
    [HarmonyPatch(typeof(StatWorker), "GetValue", new[] { typeof(StatRequest), typeof(bool) })]
    public static class Patch_StatWorker_SocialImpact_Imposter
    {
        static void Postfix(StatRequest req, bool applyPostProcess, StatDef ___stat, ref float __result)
        {
            if (___stat == null || ___stat.defName != "SocialImpact") return;
            if (!req.HasThing || req.Thing is not Pawn pawn) return;

            var comp = pawn.GetComp<CompPawnFog>();
            if (comp == null) return;
            if (comp.tier != DeceptionTier.DeceiverImposter) return;
            if (!comp.fullyRevealed) return; 

            __result = Mathf.Clamp(__result - 0.30f, -1f, 1f);
        }
    }
} 