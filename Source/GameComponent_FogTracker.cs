using Verse;
using RimWorld;
using System.Collections.Generic;
using System.Linq;

namespace FogOfPawn
{
    public class GameComponent_FogTracker : GameComponent
    {
        private HashSet<int> baselineColonistIds = new HashSet<int>();

        private static IEnumerable<Pawn> PlayerOwnedPawns() => PawnUtils.PlayerFreeColonistsSpawned();

        public GameComponent_FogTracker(Game game) : base()
        {
        }

        public override void FinalizeInit()
        {
            base.FinalizeInit();
            // Record all current free colonists as baseline (starting pawns)
            foreach (var pawn in PlayerOwnedPawns())
            {
                baselineColonistIds.Add(pawn.thingIDNumber);
            }
            Log.Message($"[FogOfPawn] FogTracker initialised. Baseline colonists recorded: {baselineColonistIds.Count}");
        }

        public override void GameComponentTick()
        {
            // Tick every 250 ticks (~4s) to find new pawns on map
            if (Find.TickManager.TicksGame % 250 != 0) return;

            foreach (var pawn in PlayerOwnedPawns())
            {
                // Only consider spawned pawns (on a map)
                if (!pawn.Spawned) continue;

                // Skip baseline colonists
                if (baselineColonistIds.Contains(pawn.thingIDNumber)) continue;

                var comp = pawn.TryGetComp<CompPawnFog>();
                if (comp == null || comp.compInitialized) continue;

                comp.InitializeFog();
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref baselineColonistIds, "baselineColonistIds", LookMode.Value);
        }
    }
} 