using HarmonyLib;
using RimWorld;
using Verse;
using UnityEngine;

namespace FogOfPawn.Patches
{
    /// <summary>
    /// Applies a -0.30 offset to SocialImpact once a scammer has been revealed to reflect damaged reputation.
    /// </summary>
    [HarmonyPatch(typeof(StatWorker), "GetValue", new[] { typeof(StatRequest), typeof(bool) })]
    public static class Patch_StatWorker_SocialImpact_Scammer
    {
        static void Postfix(StatRequest req, bool applyPostProcess, StatDef ___stat, ref float __result)
        {
            if (___stat == null || ___stat.defName != "SocialImpact") return;
            if (!req.HasThing || req.Thing is not Pawn pawn) return;

            var comp = pawn.GetComp<CompPawnFog>();
            if (comp == null) return;
            if (comp.tier != DeceptionTier.DeceiverScammer) return;
            if (!comp.fullyRevealed) return; // penalty only after reveal

            // Apply offset once – SocialImpact is additive.
            __result = Mathf.Clamp(__result - 0.30f, -1f, 1f);
        }
    }
} 