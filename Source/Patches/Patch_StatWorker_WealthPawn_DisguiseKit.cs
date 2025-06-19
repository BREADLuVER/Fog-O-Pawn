using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using FogOfPawn;

namespace FogOfPawn.Patches
{
    /// <summary>
    /// Reduces WealthPawn by 2 000 for any pawn wearing the disguise kit so raid points stay low
    /// until the scammer is revealed and the kit drops.
    /// </summary>
    [HarmonyPatch(typeof(StatWorker), "GetValue", new[] { typeof(StatRequest), typeof(bool) })]
    public static class Patch_StatWorker_WealthPawn_DisguiseKit
    {
        public static void Postfix(StatRequest req, bool applyPostProcess, StatDef ___stat, ref float __result)
        {
            if (___stat == null || ___stat.defName != "MarketValue") return;
            if (!req.HasThing || req.Thing is not Pawn pawn) return;
            if (pawn.apparel == null) return;

            var kitDef = DefDatabase<ThingDef>.GetNamedSilentFail("FogOfPawn_DisguiseKit");
            if (kitDef == null) return;

            bool wearingKit = pawn.apparel.WornApparel.Any(a => a.def == kitDef);
            if (wearingKit)
            {
                float before = __result;
                __result = Mathf.Max(0f, __result - FogSettingsCache.Current.disguiseKitWealth);
                FogLog.Verbose($"Disguise kit reduces wealth of {pawn.LabelShort}: {before:F0} → {__result:F0}");
            }
        }
    }
} 