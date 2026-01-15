using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace FogOfPawn.Patches
{
    /// <summary>
    /// Core trait masking patch using the RenderContext system.
    /// NOTE: Disabled [HarmonyPatch] because TraitSet.allTraits is a field in RimWorld 1.5/1.6,
    /// and fields cannot be patched directly. UI masking is handled via specific UI patches.
    /// </summary>
    // [HarmonyPatch(typeof(TraitSet), "get_allTraits")]
    public static class Patch_TraitSet_AllTraits
    {
        static void Postfix(TraitSet __instance, ref List<Trait> __result)
        {
            // Only filter during UI rendering
            if (!RenderContext.IsRendering) return;
            
            try
            {
                if (__instance == null || __result == null) return;
                
                // Get the pawn from the TraitSet
                // TraitSet has a 'pawn' field
                Pawn pawn = AccessTools.Field(typeof(TraitSet), "pawn")?.GetValue(__instance) as Pawn;
                if (pawn == null) return;
                
                var comp = pawn.GetComp<CompPawnFog>();
                if (comp == null || !comp.compInitialized) return;
                if (comp.tier == DeceptionTier.Truthful || comp.fullyRevealed) return;
                if (!FogSettingsCache.Current.fogTraits) return;
                
                // Filter to only revealed traits
                __result = __result.Where(t => comp.revealedTraits.Contains(t.def)).ToList();
            }
            catch (System.Exception ex)
            {
                if (Prefs.DevMode)
                {
                    Log.Warning($"[FogOfPawn] Exception in TraitSet.allTraits patch: {ex.Message}");
                }
            }
        }
    }
}
