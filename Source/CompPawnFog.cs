using Verse;
using RimWorld;

namespace FogOfPawn
{
    public class CompProperties_PawnFog : CompProperties
    {
        public CompProperties_PawnFog()
        {
            compClass = typeof(CompPawnFog);
        }
    }

    public class CompPawnFog : ThingComp
    {
        // Meta
        public bool compInitialized;
        public int ticksSinceJoin;

        // Example data; will be expanded in later phases
        public float truthfulness;

        /// <summary>
        /// Called once when the pawn first becomes player-owned.
        /// Populates randomised fog data.
        /// </summary>
        public void InitializeFog()
        {
            if (compInitialized) return;

            compInitialized = true;

            // Pick a random truthfulness roll 0-1; later will come from mod settings.
            truthfulness = Rand.Value;

            ticksSinceJoin = 0;

            Log.Message($"[FogOfPawn] Initialised fog for pawn {(parent as Pawn)?.LabelShort ?? parent.Label}. Truthfulness={truthfulness:F2}");
        }

        public override void CompTick()
        {
            base.CompTick();
            if (compInitialized)
                ticksSinceJoin++;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref compInitialized, "compInitialized", false);
            Scribe_Values.Look(ref ticksSinceJoin, "ticksSinceJoin", 0);
            Scribe_Values.Look(ref truthfulness, "truthfulness", 0f);
        }
    }
} 