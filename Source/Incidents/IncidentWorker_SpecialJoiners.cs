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

                // 2. Ensure a fitting trait load-out.  We try to add up to three helpful/aggressive traits that
                //    are not already present and do not conflict.
                if (pawn.story?.traits != null)
                {
                    List<TraitDef> poolCombat = new()
                    {
                        DefDatabase<TraitDef>.GetNamedSilentFail("Tough"),
                        DefDatabase<TraitDef>.GetNamedSilentFail("Jogger"),
                        DefDatabase<TraitDef>.GetNamedSilentFail("TriggerHappy")
                    };
                    List<TraitDef> poolMood = new()
                    {
                        DefDatabase<TraitDef>.GetNamedSilentFail("Optimist"),
                        DefDatabase<TraitDef>.GetNamedSilentFail("IronWilled"),
                        DefDatabase<TraitDef>.GetNamedSilentFail("Sanguine")
                    };
                    List<TraitDef> poolAggro = new()
                    {
                        DefDatabase<TraitDef>.GetNamedSilentFail("Bloodlust"),
                        DefDatabase<TraitDef>.GetNamedSilentFail("Nimble")
                    };

                    addedTraits = new List<TraitDef>();

                    void TryAdd(TraitDef def)
                    {
                        if (def != null && !pawn.story.traits.HasTrait(def))
                        {
                            pawn.story.traits.GainTrait(new Trait(def));
                            addedTraits.Add(def);
                        }
                    }

                    TryAdd(poolCombat.RandomElement());
                    TryAdd(poolMood.RandomElement());
                    TryAdd(poolAggro.RandomElement());

                    // 'addedTraits' will later be used to force these traits to start hidden.
                }
            }
            else if (WantedTier == DeceptionTier.DeceiverScammer)
            {
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
            }

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