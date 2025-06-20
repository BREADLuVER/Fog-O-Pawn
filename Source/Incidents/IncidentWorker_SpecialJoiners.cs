using RimWorld;
using Verse;
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

            // Generate a human pawn similar to WandererJoin (spacer refugee kind guarantees skills).
            PawnKindDef kind = PawnKindDefOf.SpaceRefugee;
            Pawn pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(kind, null, PawnGenerationContext.NonPlayer, -1, forceGenerateNewPawn: true, allowDead: false, allowDowned: false, colonistRelationChanceFactor: 0f));

            // Customise the pawn if this is a Sleeper deception tier.
            List<TraitDef> addedTraits = null; // collect traits we add so we can hide them later

            if (WantedTier == DeceptionTier.DeceiverSleeper)
            {
                // 1. Boost a handful of skills so the pawn is genuinely competent.
                var skillList = pawn.skills.skills.InRandomOrder().Take(6);
                foreach (var sk in skillList)
                {
                    if (sk.Level < 10)
                        sk.Level = Rand.RangeInclusive(10, 14);
                }

                // Sleeper will gain a powerful trait later when they "wake up" (stage 3 reveal)
            }
            else if (WantedTier == DeceptionTier.DeceiverScammer)
            {
                // Comment out trait granting loop
                /*
                // Give bad traits that are initially hidden.
                List<TraitDef> badPool = new()
                {
                    DefDatabase<TraitDef>.GetNamedSilentFail("Volatile"),
                    DefDatabase<TraitDef>.GetNamedSilentFail("Nervous"),
                    DefDatabase<TraitDef>.GetNamedSilentFail("ChemicalInterest"),
                    DefDatabase<TraitDef>.GetNamedSilentFail("ChemicalFascination"),
                    DefDatabase<TraitDef>.GetNamedSilentFail("Pyromaniac"),
                    DefDatabase<TraitDef>.GetNamedSilentFail("Gourmand")
                };

                var addedBad = new List<TraitDef>();
                if (pawn.story?.traits != null)
                {
                    foreach (var td in badPool.InRandomOrder().Take(3))
                    {
                        if (td != null && !pawn.story.traits.HasTrait(td))
                        {
                            pawn.story.traits.GainTrait(new Trait(td));
                            addedBad.Add(td);
                        }
                    }
                }

                // Remember these traits so we can hide them later
                addedTraits = addedBad;
                */
            }

            // Find a valid entry point at the edge of the map.
            if (!RCellFinder.TryFindRandomPawnEntryCell(out IntVec3 spawnCell, map, CellFinder.EdgeRoadChance_Neutral, false, null))
            {
                // Failed to find a spawn point; abort the incident and discard the pawn.
                if (pawn != null && !pawn.Destroyed)
                {
                    pawn.Destroy(DestroyMode.Vanish);
                }
                return false;
            }
            GenSpawn.Spawn(pawn, spawnCell, map, WipeMode.Vanish);

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

                // Hide any special Sleeper traits we just added.
                if (WantedTier == DeceptionTier.DeceiverSleeper && comp != null && addedTraits?.Count > 0)
                {
                    foreach (var td in addedTraits)
                        comp.revealedTraits.Remove(td);
                }

                if (WantedTier == DeceptionTier.DeceiverScammer && comp != null && addedTraits?.Count > 0)
                {
                    foreach (var td in addedTraits)
                        comp.revealedTraits.Remove(td);
                }
            }

            // Send standard letter.
            string label = "FogOfPawn.SpecialJoiner.Label".Translate(pawn.Named("PAWN"));
            string text  = "FogOfPawn.SpecialJoiner.Text".Translate(pawn.Named("PAWN"));
            Find.LetterStack.ReceiveLetter(label, text, LetterDefOf.PositiveEvent, pawn);

            // After successfully spawning pawn (before return true) register joiner
            var tracker2 = GameComponent_FogTracker.Get;
            tracker2?.RegisterFoggedJoiner(WantedTier);

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