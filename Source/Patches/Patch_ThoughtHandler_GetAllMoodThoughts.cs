using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace FogOfPawn.Patches
{
    [HarmonyPatch]
    public static class Patch_ThoughtHandler_GetAllMoodThoughts
    {
        static System.Reflection.MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(ThoughtHandler), "GetAllMoodThoughts");
        }
        
        static void Postfix(ThoughtHandler __instance, List<Thought> outThoughts)
        {
            if (!RenderContext.IsRendering) return;
            
            try
            {
                if (__instance == null || outThoughts == null) return;
                
                Pawn pawn = __instance.pawn;
                if (pawn == null) return;
                
                var comp = pawn.GetComp<CompPawnFog>();
                if (comp == null || !comp.compInitialized) return;
                if (comp.tier == DeceptionTier.Truthful || comp.fullyRevealed) return;
                if (!FogSettingsCache.Current.fogTraits) return;
                
                outThoughts.RemoveAll(t => !IsThoughtVisible(t, comp));
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
            
            if (thought.def.requiredTraits != null && thought.def.requiredTraits.Count > 0)
            {
                foreach (var requiredTrait in thought.def.requiredTraits)
                {
                    if (!comp.revealedTraits.Contains(requiredTrait))
                    {
                        return false;
                    }
                }
            }
            
            if (thought is Thought_Situational situational)
            {
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
    
    [HarmonyPatch]
    public static class Patch_ThoughtHandler_GetDistinctMoodThoughtGroups
    {
        static bool Prepare()
        {
            return AccessTools.Method(typeof(ThoughtHandler), "GetDistinctMoodThoughtGroups") != null;
        }
        
        static System.Reflection.MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(ThoughtHandler), "GetDistinctMoodThoughtGroups");
        }
        
        static void Postfix(ThoughtHandler __instance, List<Thought> outThoughts)
        {
            if (!RenderContext.IsRendering) return;
            
            try
            {
                if (__instance == null || outThoughts == null) return;
                
                Pawn pawn = __instance.pawn;
                if (pawn == null) return;
                
                var comp = pawn.GetComp<CompPawnFog>();
                if (comp == null || !comp.compInitialized) return;
                if (comp.tier == DeceptionTier.Truthful || comp.fullyRevealed) return;
                if (!FogSettingsCache.Current.fogTraits) return;
                
                outThoughts.RemoveAll(t => !IsThoughtVisibleStatic(t, comp));
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
