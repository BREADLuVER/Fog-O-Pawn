using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using System;

namespace FogOfPawn
{
    internal static class PawnUtils
    {
        /// <summary>
        /// Returns player-owned, free colonists that are currently spawned on any map
        /// (i.e. physical presence the player can interact with).
        /// </summary>
        public static IEnumerable<Pawn> PlayerFreeColonistsSpawned()
        {
            foreach (var map in Find.Maps)
            {
                foreach (var pawn in map.mapPawns.FreeColonistsSpawned)
                {
                    if (pawn.Faction == Faction.OfPlayer)
                        yield return pawn;
                }
            }
        }
    }
} 