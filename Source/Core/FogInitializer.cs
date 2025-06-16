using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace FogOfPawn
{
    public static class FogInitializer
    {
        // Entry point from patch – assigns tier (unless pre-set) and applies masks.
        public static void InitializeFogFor(Pawn pawn, PawnGenerationRequest? request = null)
        {
            var comp = pawn.GetComp<CompPawnFog>();
            if (comp == null || comp.compInitialized)
                return;

            var settings = FogSettingsCache.Current;

            // Assign tier only if not manually set by dev tools.
            if (!comp.tierManuallySet)
            {
                comp.tier = ChooseTier(pawn, request, settings);
            }

            ApplyMasks(pawn, comp, settings);

            // Apply trait fog masks after skills
            ApplyTraitMasks(pawn, comp, settings);

            comp.compInitialized = true;

            FogLog.Verbose($"Initialized fog for {pawn.NameShortColored}. Tier={comp.tier}");
        }

        public static void RegenerateMasksFor(Pawn pawn, CompPawnFog comp)
        {
            // clear previous reported skills/passions then apply again.
            comp.reportedSkills.Clear();
            comp.reportedPassions.Clear();
            comp.revealedSkills.Clear();
            comp.revealedTraits.Clear();
            ApplyMasks(pawn, comp, FogSettingsCache.Current);
            ApplyTraitMasks(pawn, comp, FogSettingsCache.Current);
        }

        private static DeceptionTier ChooseTier(Pawn pawn, PawnGenerationRequest? request, FogOfPawnSettings settings)
        {
            // Base weights
            float wTruth = 0.90f;
            float wSlight = 0.10f;
            float wDeceiver = 0.03f;

            // target weights when slider=1 (truth lower)
            float wTruthTarget = 0.80f;
            float wSlightTarget = 0.17f;
            float wDeceiverTarget = 0.03f;

            float f = settings.deceptionIntensity; // 0-1
            wTruth = Mathf.Lerp(wTruth, wTruthTarget, f);
            wSlight = Mathf.Lerp(wSlight, wSlightTarget, f);
            wDeceiver = Mathf.Lerp(wDeceiver, wDeceiverTarget, f);

            // Normalise
            float total = wTruth + wSlight + wDeceiver;
            wTruth /= total; wSlight /= total; wDeceiver /= total;

            float roll = Rand.Value;
            if (roll < wTruth) return DeceptionTier.Truthful;
            if (roll < wTruth + wSlight) return DeceptionTier.SlightlyDeceived;

            // Deceiver – further constraints
            if (settings.deceiverJoinersOnly && request.HasValue && request.Value.Context != PawnGenerationContext.NonPlayer)
            {
                // compute pawn value
                float pv = GetPawnValue(pawn);
                float median = 250f; // TODO calculate map median later
                return pv < median ? DeceptionTier.DeceiverScammer : DeceptionTier.DeceiverSleeper;
            }

            return DeceptionTier.Truthful;
        }

        private static void ApplyMasks(Pawn pawn, CompPawnFog comp, FogOfPawnSettings settings)
        {
            if (!settings.fogSkills) return;

            switch (comp.tier)
            {
                case DeceptionTier.Truthful:
                    ApplyTruthful(pawn, comp);
                    break;
                case DeceptionTier.SlightlyDeceived:
                    ApplySlight(pawn, comp, settings);
                    break;
                case DeceptionTier.DeceiverScammer:
                    ApplyScammer(pawn, comp);
                    break;
                case DeceptionTier.DeceiverSleeper:
                    ApplySleeper(pawn, comp);
                    break;
            }
        }

        private static void ApplyTruthful(Pawn pawn, CompPawnFog comp)
        {
            // Reveal everything; no reported overrides.
            foreach (var sk in pawn.skills.skills)
            {
                comp.revealedSkills.Add(sk.def);
            }
        }

        private static void ApplySlight(Pawn pawn, CompPawnFog comp, FogOfPawnSettings settings)
        {
            int maxAltered = Mathf.Max(1, settings.maxAlteredSkills);
            int count = Rand.RangeInclusive(1, maxAltered);

            var skillsToAlter = pawn.skills.skills.InRandomOrder().Take(count);
            foreach (var skill in skillsToAlter)
            {
                bool understate = settings.allowUnderstate && Rand.Chance(0.5f);
                int delta = Rand.RangeInclusive(2, 5);
                int reported = Mathf.Clamp(skill.Level + (understate ? -delta : delta), 0, 20);
                comp.reportedSkills[skill.def] = reported;

                if (Rand.Chance(0.4f))
                {
                    comp.reportedPassions[skill.def] = skill.passion == Passion.None ? Passion.Minor : skill.passion;
                }
            }

            // all other skills revealed
            foreach (var sk in pawn.skills.skills)
            {
                if (!comp.reportedSkills.ContainsKey(sk.def))
                {
                    comp.revealedSkills.Add(sk.def);
                }
            }
        }

        private static void ApplyScammer(Pawn pawn, CompPawnFog comp)
        {
            foreach (var skill in pawn.skills.skills)
            {
                if (skill.Level <= 6)
                {
                    comp.reportedSkills[skill.def] = Rand.RangeInclusive(8, 12);
                    if (Rand.Chance(0.5f))
                        comp.reportedPassions[skill.def] = Passion.Minor;
                }
            }
        }

        private static void ApplySleeper(Pawn pawn, CompPawnFog comp)
        {
            foreach (var skill in pawn.skills.skills)
            {
                if (skill.Level >= 12)
                {
                    comp.reportedSkills[skill.def] = Rand.RangeInclusive(3, 6);
                    comp.reportedPassions[skill.def] = Passion.None;
                }
            }
        }

        /// <summary>
        /// Decide which traits start hidden based on a simple random chance per trait.
        /// Revealed traits are added to <see cref="CompPawnFog.revealedTraits"/>; anything
        /// not present is considered fogged and will be masked in the UI.
        /// </summary>
        private static void ApplyTraitMasks(Pawn pawn, CompPawnFog comp, FogOfPawnSettings settings)
        {
            if (!settings.fogTraits || pawn.story?.traits == null)
            {
                // Reveal all traits when trait fogging is disabled or pawn has none.
                foreach (var t in pawn.story?.traits?.allTraits ?? Enumerable.Empty<Trait>())
                {
                    comp.revealedTraits.Add(t.def);
                }
                return;
            }

            foreach (var trait in pawn.story.traits.allTraits)
            {
                bool hide = Rand.Value < settings.traitHideChance;
                if (!hide)
                {
                    comp.revealedTraits.Add(trait.def);
                }
            }
        }

        private static bool IsPositiveTrait(TraitDef def) => false; // TODO better metric
        private static bool IsNegativeTrait(TraitDef def) => false;

        public static float GetPawnValue(Pawn p)
        {
            float skillScore = p.skills.skills.Sum(s => s.Level);
            float passionScore = p.skills.skills.Count(s => s.passion == Passion.Minor) * 2 +
                                 p.skills.skills.Count(s => s.passion == Passion.Major) * 4;
            int goodTraits = p.story?.traits?.allTraits.Count(t => IsPositiveTrait(t.def)) ?? 0;
            int badTraits = p.story?.traits?.allTraits.Count(t => IsNegativeTrait(t.def)) ?? 0;

            return skillScore + passionScore + (goodTraits * 5) - (badTraits * 3);
        }
    }
} 