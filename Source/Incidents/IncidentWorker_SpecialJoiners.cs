using RimWorld;
using Verse;
using System;
using System.Collections.Generic;
using System.Linq;

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
            Map map = (Map)parms.target;
            if (map?.IsPlayerHome != true) return false;
            if (!base.CanFireNowSub(parms)) return false;

            var tracker = GameComponent_FogTracker.Get;
            return tracker?.CanFireFoggedJoiner(WantedTier) == true;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            if (map == null) return false;

            PawnKindDef kind = PawnKindDefOf.SpaceRefugee;
            Pawn pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(kind, null, PawnGenerationContext.NonPlayer, -1, forceGenerateNewPawn: true, allowDead: false, allowDowned: false, colonistRelationChanceFactor: 0f));

            if (WantedTier == DeceptionTier.DeceiverSleeper)
            {
                var skillList = pawn.skills.skills.InRandomOrder().Take(6);
                foreach (var sk in skillList)
                {
                    if (sk.Level < 10) sk.Level = Rand.RangeInclusive(10, 14);
                }
            }
            
            // string label = "FogOfPawn.SpecialJoiner.Label".Translate();
            // string text = "FogOfPawn.SpecialJoiner.Text".Translate(pawn.Named("PAWN"));
            
            Action acceptAction = () => {
                var comp = pawn.GetComp<CompPawnFog>();
                if (comp != null)
                {
                    comp.tier = WantedTier;
                    comp.tierManuallySet = true;
                    FogInitializer.RegenerateMasksFor(pawn, comp);
                }

                if (RCellFinder.TryFindRandomPawnEntryCell(out IntVec3 spawnCell, map, CellFinder.EdgeRoadChance_Neutral, false, null))
                {
                    GenSpawn.Spawn(pawn, spawnCell, map, WipeMode.Vanish);
                    pawn.SetFaction(Faction.OfPlayer, null);
                    PawnComponentsUtility.AddAndRemoveDynamicComponents(pawn);
                    pawn.workSettings?.EnableAndInitializeIfNotAlreadyInitialized();
                    GameComponent_FogTracker.Get?.RegisterFoggedJoiner(WantedTier);
                    // Duplicate alert suppressed – ChoiceLetter already informed the player.
                    // Messages.Message("LetterWandererJoins".Translate(pawn.Named("PAWN")), pawn, MessageTypeDefOf.PositiveEvent);
                }
            };

            Action rejectAction = () => {
                if (pawn != null && !pawn.Destroyed)
                {
                    pawn.Destroy(DestroyMode.Vanish);
                }
            };

            JoinerChoiceUtility.ShowJoinerChoice(pawn, WantedTier, acceptAction, rejectAction);
            
            return true;
        }
    }

    public class IncidentWorker_SleeperJoin : IncidentWorker_SpecialJoinerBase
    {
        protected override DeceptionTier WantedTier => DeceptionTier.DeceiverSleeper;
    }

    public class IncidentWorker_ImposterJoin : IncidentWorker_SpecialJoinerBase
    {
        protected override DeceptionTier WantedTier => DeceptionTier.DeceiverImposter;
    }
} 