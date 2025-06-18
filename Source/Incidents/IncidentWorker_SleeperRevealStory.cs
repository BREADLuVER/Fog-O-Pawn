using RimWorld;
using Verse;
using System.Linq;

namespace FogOfPawn
{
    public class IncidentWorker_SleeperRevealStory : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            if (map == null || !map.IsPlayerHome) return false;

            // Need at least one sleeper colonist not yet revealed and not already in a story.
            return FindSleeperCandidate(map) != null;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            Pawn pawn = FindSleeperCandidate(map);
            if (pawn == null) return false;

            // Kick off story in the fog tracker
            var comp = Current.Game.GetComponent<GameComponent_FogTracker>();
            comp.StartSleeperStory(pawn);
            return true;
        }

        private Pawn FindSleeperCandidate(Map map)
        {
            foreach (var p in map.mapPawns.FreeColonistsSpawned)
            {
                var fog = p.GetComp<CompPawnFog>();
                if (fog != null && fog.tier == DeceptionTier.DeceiverSleeper && !fog.fullyRevealed)
                {
                    // ensure not already in tracker
                    var tracker = Current.Game.GetComponent<GameComponent_FogTracker>();
                    if (!tracker.IsSleeperStoryActive(p))
                        return p;
                }
            }
            return null;
        }
    }
} 