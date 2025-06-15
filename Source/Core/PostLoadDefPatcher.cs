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
            foreach (var def in DefDatabase<ThingDef>.AllDefs)
            {
                // Target any pawn type that has skills.
                if (def.race != null && def.race.hediffGiverSets != null)
                {
                    if (def.comps == null)
                    {
                        def.comps = new List<CompProperties>();
                    }
                    def.comps.Add(new CompProperties_PawnFog());
                }
            }
        }
    }
} 