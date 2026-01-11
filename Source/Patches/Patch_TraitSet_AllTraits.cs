using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace FogOfPawn.Patches
{
    /// <summary>
    /// Core trait masking patch using the RenderContext system.
    /// During UI rendering, returns a filtered list of traits (hidden traits removed).
    /// During game logic, returns the full real trait list.
    /// 
    /// This single patch handles ALL trait list reads across the entire game,
    /// eliminating the need to patch individual UI components.
    /// </summary>
    [HarmonyPatch(typeof(TraitSet), "get_allTraits")]
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
