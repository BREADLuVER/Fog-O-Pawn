using RimWorld;
using Verse;

namespace FogOfPawn
{
    /// <summary>
    /// Base helper for our special joiner incidents; subclasses specify the deception tier.
    /// </summary>
    public abstract class IncidentWorker_SpecialJoinerBase : IncidentWorker
    {
        protected abstract DeceptionTier WantedTier { get; }

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            // Require a player home map like vanilla wanderer join.
            Map map = (Map)parms.target;
            return map?.IsPlayerHome == true && base.CanFireNowSub(parms);
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            if (map == null) return false;

            // Generate a human pawn similar to WandererJoin (spacer refugee kind guarantees skills).
            PawnKindDef kind = PawnKindDefOf.SpaceRefugee;
            Pawn pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(kind, null, PawnGenerationContext.NonPlayer, -1, forceGenerateNewPawn: true, allowDead: false, allowDowned: false, colonistRelationChanceFactor: 0f));

            // Make them spawn at edge like wanderer join.
            IntVec3 spawnCell = CellFinder.RandomClosewalkCellNear(map.Center, map, 12);
            GenSpawn.Spawn(pawn, spawnCell, map);

            pawn.SetFactionDirect(Faction.OfPlayer);

            // Ensure all player-specific trackers are created so UI code doesn't null-ref.
            PawnComponentsUtility.AddAndRemoveDynamicComponents(pawn);
            pawn.workSettings?.EnableAndInitializeIfNotAlreadyInitialized();

            // Apply fog tier.
            var comp = pawn.GetComp<CompPawnFog>();
            if (comp != null)
            {
                comp.tier = WantedTier;
                comp.tierManuallySet = true;
                FogInitializer.RegenerateMasksFor(pawn, comp);
            }

            // Send standard letter.
            string label = "FogOfPawn.SpecialJoiner.Label".Translate(pawn.Named("PAWN"));
            string text  = "FogOfPawn.SpecialJoiner.Text".Translate(pawn.Named("PAWN"));
            Find.LetterStack.ReceiveLetter(label, text, LetterDefOf.PositiveEvent, pawn);
            return true;
        }
    }

    public class IncidentWorker_SleeperJoin : IncidentWorker_SpecialJoinerBase
    {
        protected override DeceptionTier WantedTier => DeceptionTier.DeceiverSleeper;
    }

    public class IncidentWorker_ScammerJoin : IncidentWorker_SpecialJoinerBase
    {
        protected override DeceptionTier WantedTier => DeceptionTier.DeceiverScammer;
    }
} 