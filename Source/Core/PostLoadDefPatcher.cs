using System.Collections.Generic;
using RimWorld;
using Verse;

namespace FogOfPawn
{
    [StaticConstructorOnStartup]
    public static class PostLoadDefPatcher
    {
        static PostLoadDefPatcher()
        {
            try
            {
                foreach (var def in DefDatabase<ThingDef>.AllDefs)
                {
                    // Target any pawn type that has skills.
                    if (def?.race != null && def.race.intelligence == Intelligence.Humanlike)
                    {
                        if (def.comps == null)
                        {
                            def.comps = new List<CompProperties>();
                        }
                        
                        // Check if CompPawnFog is already added
                        bool hasCompPawnFog = false;
                        foreach (var comp in def.comps)
                        {
                            if (comp is CompProperties_PawnFog)
                            {
                                hasCompPawnFog = true;
                                break;
                            }
                        }
                        
                        if (!hasCompPawnFog)
                        {
                            def.comps.Add(new CompProperties_PawnFog());
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Log.Error($"[FogOfPawn] Failed to patch ThingDefs: {ex}");
            }
        }
    }
} 