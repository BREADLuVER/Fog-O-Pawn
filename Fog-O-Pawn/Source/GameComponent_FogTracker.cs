using System.Linq;
using Verse;
using RimWorld;

namespace FogOfPawn
{
    public class GameComponent_FogTracker : GameComponent
    {
        private int _tickCounter;
        
        public GameComponent_FogTracker(Game game) : base()
        {
        }

        public override void FinalizeInit()
        {
            base.FinalizeInit();
            Log.Message("[FogOfPawn] FogTracker initialised.");
        }

        public override void GameComponentTick()
        {
            // Scan for new pawns every 250 ticks (approx 4 seconds) to reduce performance impact
            _tickCounter++;
            if (_tickCounter < 250) return;
            _tickCounter = 0;
            
            // Find all player-faction humanlikes on all maps
            foreach (var pawn in PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive_OfPlayerFaction_NoSlaves)
            {
                if (!pawn.RaceProps.Humanlike) continue;
                
                var comp = pawn.GetComp<CompPawnFog>();
                if (comp != null && !comp.compInitialized)
                {
                    InitializeFogFor(pawn, comp);
                }
            }
        }

        private void InitializeFogFor(Pawn pawn, CompPawnFog comp)
        {
            // Set a random "truthfulness" baseline for this pawn
            comp.truthfulness = Rand.Range(0f, 1f);
            
            // Mark as initialised so we don't do this again
            comp.compInitialized = true;
            
            Log.Message($"[FogOfPawn] Initialised fog for {pawn.NameShortColored}. Truthfulness: {comp.truthfulness:P0}");
        }

        public override void ExposeData()
        {
            // Nothing to save for now, but the override is kept for future needs.
        }
    }
} 