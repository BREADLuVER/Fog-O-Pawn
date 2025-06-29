using System.Collections.Generic;
using RimWorld;
using Verse;
using System.Linq;

namespace FogOfPawn
{
    public static class FogUtility
    {
        public static bool RevealRandomFoggedAttribute(Pawn pawn, bool preferSkill = true)
        {
            if (pawn == null || pawn.Destroyed) return false;
            var comp = pawn.GetComp<CompPawnFog>();
            if (comp == null || !comp.compInitialized) return false;

            if (comp.tier == DeceptionTier.DeceiverSleeper)
                return false; // Sleepers only reveal via story beats

            var settings = FogSettingsCache.Current;
            List<System.Action> candidates = new();

            // Skills
            if (settings.fogSkills)
            {
                foreach (var sk in pawn.skills.skills)
                {
                    if (!comp.revealedSkills.Contains(sk.def))
                    {
                        if ((preferSkill && settings.allowSocialSkillReveal) || (!preferSkill && settings.allowPassiveSkillReveal))
                        {
                            candidates.Add(() => comp.RevealSkill(sk.def));
                        }
                    }
                }
            }

            // Traits
            if (settings.fogTraits && pawn.story?.traits != null)
            {
                foreach (var tr in pawn.story.traits.allTraits)
                {
                    if (!comp.revealedTraits.Contains(tr.def))
                    {
                        if ((preferSkill && settings.allowSocialTraitReveal) || (!preferSkill && settings.allowPassiveTraitReveal))
                        {
                            var captured = tr; // closure copy
                            candidates.Add(() => comp.RevealTrait(captured));
                        }
                    }
                }
            }

            if (candidates.Count == 0) return false;

            candidates.RandomElement()();
            return true;
        }

        public static void TriggerFullReveal(Pawn pawn, string reasonKey)
        {
            if (pawn == null) return;

            var comp = pawn.GetComp<CompPawnFog>();
            if (comp == null) return;

            if (comp.fullyRevealed) return;

            if (comp.tier == DeceptionTier.DeceiverImposter)
            {
                TryAddImposterTrait(pawn);
            }

            comp.RevealAll();
            comp.fullyRevealed = true;

            // Force disguise kit logic (Imposter only)
            comp.GetType().GetMethod("MaybeDropDisguiseKit", System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)?.Invoke(comp, null);

            if (ShouldNotifyPlayer(pawn))
            {
            string labelKey = $"Fog.FullReveal.{reasonKey}.Label";
            string textKey  = $"Fog.FullReveal.{reasonKey}.Text";
            string label = labelKey.Translate(pawn.Named("PAWN"));
            string text  = textKey.Translate(pawn.Named("PAWN"));
            Find.LetterStack.ReceiveLetter(label, text, LetterDefOf.PositiveEvent, pawn);
            }

            // Reputation damage for imposter
            if (comp.tier == DeceptionTier.DeceiverImposter)
            {
                var thought = DefDatabase<ThoughtDef>.GetNamedSilentFail("Fog_ImposterRevealed_Betrayed");
                if (thought != null)
                {
                    foreach (var other in pawn.MapHeld?.mapPawns?.FreeColonistsSpawned ?? Enumerable.Empty<Pawn>())
                    {
                        if (other == pawn) continue;
                        other.needs?.mood?.thoughts?.memories?.TryGainMemory(thought, pawn);
                    }
                }
            }

            if (comp.tier == DeceptionTier.DeceiverSleeper && ShouldNotifyPlayer(pawn))
            {
                SleeperChoiceUtility.SendChoiceLetter(pawn);
            }

            FogLog.Verbose($"[FULL REVEAL] {pawn.LabelShort} ({comp.tier}) via {reasonKey}");
        }

        private static void TryAddImposterTrait(Pawn pawn)
        {
            if (pawn?.story?.traits == null) return;
            List<TraitDef> badPool = new()
            {
                DefDatabase<TraitDef>.GetNamedSilentFail("Volatile"),
                DefDatabase<TraitDef>.GetNamedSilentFail("Nervous"),
                DefDatabase<TraitDef>.GetNamedSilentFail("ChemicalInterest"),
                DefDatabase<TraitDef>.GetNamedSilentFail("ChemicalFascination"),
                DefDatabase<TraitDef>.GetNamedSilentFail("Pyromaniac"),
                DefDatabase<TraitDef>.GetNamedSilentFail("Gourmand")
            };
            foreach (var td in badPool.InRandomOrder())
            {
                if (td != null && !pawn.story.traits.HasTrait(td))
                {
                    pawn.story.traits.GainTrait(new Trait(td));
                    break;
                }
            }
        }

        public static bool ShouldNotifyPlayer(Pawn pawn)
        {
            return pawn.Faction?.IsPlayer == true || pawn.IsPrisonerOfColony || pawn.IsSlaveOfColony;
        }

        public static void GiveMoodBuffForImposterRemoval(Pawn pawn)
        {
            // Pawn must be a player colonist at the time of removal.
            if (pawn?.Faction != Faction.OfPlayer)
            {
                return;
            }

            var comp = pawn.GetComp<CompPawnFog>();
            if (comp == null || comp.tier != DeceptionTier.DeceiverImposter)
            {
                return;
            }

            // At this point, the pawn is a player-faction imposter who is being removed.
            var thoughtDef = DefDatabase<ThoughtDef>.GetNamedSilentFail("Fog_ImposterRemoved");
            if (thoughtDef == null)
            {
                FogLog.Fail("MissingThoughtDef", "Could not find ThoughtDef 'Fog_ImposterRemoved'.");
                return;
            }

            // Apply mood buff to all colonists on the same map.
            var map = pawn.MapHeld;
            if (map == null) return;

            foreach (var colonist in map.mapPawns.FreeColonistsSpawned)
            {
                colonist.needs?.mood?.thoughts?.memories?.TryGainMemory(thoughtDef);
            }

            FogLog.Verbose($"Applied 'Imposter Removed' mood buff to colonists for the removal of {pawn.LabelShort}.");
        }
    }
} 