using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace FogOfPawn.Patches
{
    /// <summary>
    /// Filters mood thoughts during UI rendering to hide thoughts from hidden traits.
    /// The actual mood value is NOT affected - only what's shown in the breakdown.
    /// This creates the intended "tell" where mood is lower than visible thoughts would suggest.
    /// </summary>
    [HarmonyPatch]
    public static class Patch_ThoughtHandler_GetAllMoodThoughts
    {
        static System.Reflection.MethodBase TargetMethod()
        {
            // ThoughtHandler.GetAllMoodThoughts returns List<Thought>
            return AccessTools.Method(typeof(ThoughtHandler), "GetAllMoodThoughts");
        }
        
        static void Postfix(ThoughtHandler __instance, ref List<Thought> __result)
        {
            // Only filter during UI rendering
            if (!RenderContext.IsRendering) return;
            
            try
            {
                if (__instance == null || __result == null) return;
                
                // Get the pawn from ThoughtHandler
                Pawn pawn = __instance.pawn;
                if (pawn == null) return;
                
                var comp = pawn.GetComp<CompPawnFog>();
                if (comp == null || !comp.compInitialized) return;
                if (comp.tier == DeceptionTier.Truthful || comp.fullyRevealed) return;
                if (!FogSettingsCache.Current.fogTraits) return;
                
                // Filter out thoughts from hidden traits
                __result = __result.Where(t => IsThoughtVisible(t, comp)).ToList();
            }
            catch (System.Exception ex)
            {
                if (Prefs.DevMode)
                {
                    Log.Warning($"[FogOfPawn] Exception in ThoughtHandler patch: {ex.Message}");
                }
            }
        }
        
        private static bool IsThoughtVisible(Thought thought, CompPawnFog comp)
        {
            if (thought?.def == null) return true;
            
            // Check if this thought requires a trait that is hidden
            if (thought.def.requiredTraits != null && thought.def.requiredTraits.Count > 0)
            {
                foreach (var requiredTrait in thought.def.requiredTraits)
                {
                    if (!comp.revealedTraits.Contains(requiredTrait))
                    {
                        // This thought requires a hidden trait - hide it
                        return false;
                    }
                }
            }
            
            // Check the thought's source trait if it's a situational thought
            // Situational thoughts (like Pessimist, Night Owl) are linked to traits
            if (thought is Thought_Situational situational)
            {
                // Some situational thoughts have their trait in the def
                if (thought.def.requiredTraits != null)
                {
                    foreach (var trait in thought.def.requiredTraits)
                    {
                        if (!comp.revealedTraits.Contains(trait))
                        {
                            return false;
                        }
                    }
                }
            }
            
            return true;
        }
    }
    
    /// <summary>
    /// Alternative patch for getting distinct thought groups (used in some UI displays).
    /// </summary>
    [HarmonyPatch]
    public static class Patch_ThoughtHandler_GetDistinctMoodThoughtGroups
    {
        static bool Prepare()
        {
            // Only apply if this method exists (varies by RW version)
            return AccessTools.Method(typeof(ThoughtHandler), "GetDistinctMoodThoughtGroups") != null;
        }
        
        static System.Reflection.MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(ThoughtHandler), "GetDistinctMoodThoughtGroups");
        }
        
        static void Postfix(ThoughtHandler __instance, ref IEnumerable<Thought> __result)
        {
            if (!RenderContext.IsRendering) return;
            
            try
            {
                if (__instance == null || __result == null) return;
                
                Pawn pawn = __instance.pawn;
                if (pawn == null) return;
                
                var comp = pawn.GetComp<CompPawnFog>();
                if (comp == null || !comp.compInitialized) return;
                if (comp.tier == DeceptionTier.Truthful || comp.fullyRevealed) return;
                if (!FogSettingsCache.Current.fogTraits) return;
                
                __result = __result.Where(t => IsThoughtVisibleStatic(t, comp));
            }
            catch (System.Exception ex)
            {
                if (Prefs.DevMode)
                {
                    Log.Warning($"[FogOfPawn] Exception in DistinctMoodThoughtGroups patch: {ex.Message}");
                }
            }
        }
        
        private static bool IsThoughtVisibleStatic(Thought thought, CompPawnFog comp)
        {
            if (thought?.def?.requiredTraits == null) return true;
            
            foreach (var trait in thought.def.requiredTraits)
            {
                if (!comp.revealedTraits.Contains(trait))
                {
                    return false;
                }
            }
            
            return true;
        }
    }
}
