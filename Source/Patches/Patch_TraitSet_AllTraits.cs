using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace FogOfPawn.Patches
{
    public static class Patch_TraitSet_AllTraits
    {
        static void Postfix(TraitSet __instance, ref List<Trait> __result)
        {
            if (!RenderContext.IsRendering) return;
            
            try
            {
                if (__instance == null || __result == null) return;
                
                Pawn pawn = AccessTools.Field(typeof(TraitSet), "pawn")?.GetValue(__instance) as Pawn;
                if (pawn == null) return;
                
                var comp = pawn.GetComp<CompPawnFog>();
                if (comp == null || !comp.compInitialized) return;
                if (comp.tier == DeceptionTier.Truthful || comp.fullyRevealed) return;
                if (!FogSettingsCache.Current.fogTraits) return;
                
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
