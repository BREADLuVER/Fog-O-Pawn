using Verse;
using RimWorld;

namespace FogOfPawn
{
    public class GameComponent_FogTracker : GameComponent
    {
        public GameComponent_FogTracker(Game game) : base()
        {
        }

        public override void FinalizeInit()
        {
            base.FinalizeInit();
        }

        public override void GameComponentTick()
        {
            // Pawn initialization is now handled via a Harmony patch on PawnGenerator.GeneratePawn
        }

        public override void ExposeData()
        {
            // Nothing to save for now, but the override is kept for future needs.
        }
    }
} 