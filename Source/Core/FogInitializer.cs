using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using System.Collections.Generic;

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
            float wTruth = Mathf.Max(0.01f, settings.pctTruthful);
            float wSlight = Mathf.Max(0f, settings.pctSlight);
            float wDeceiver = Mathf.Max(0f, settings.pctDeceiver);

            // After weight assignment add normalization
            float total = wTruth + wSlight + wDeceiver;
            if (total <= 0f)
            {
                wTruth = 1f; wSlight = wDeceiver = 0f;
            }
            else
            {
                wTruth /= total;
                wSlight /= total;
                wDeceiver /= total;
            }

            float roll = Rand.Value;
            if (roll < wTruth) return DeceptionTier.Truthful;
            if (roll < wTruth + wSlight) return DeceptionTier.SlightlyDeceived;

            // Deceiver – apply restriction only if toggle enabled
            if (settings.deceiverJoinersOnly && request.HasValue && request.Value.Context == PawnGenerationContext.NonPlayer)
            {
                // Restricted: Non-player spawned pawns cannot be Deceivers
                return DeceptionTier.Truthful;
            }

            // Determine Sleeper vs Scammer based on pawn value
            float pv2 = GetPawnValue(pawn);
            float median2 = 250f;
            return pv2 < median2 ? DeceptionTier.DeceiverScammer : DeceptionTier.DeceiverSleeper;
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
                int range = Mathf.Clamp(settings.alteredSkillRange, 2, 10);
                int delta = Rand.RangeInclusive(2, range);
                int reported = Mathf.Clamp(skill.levelInt + (understate ? -delta : delta), 0, 20);
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
            var settings = FogSettingsCache.Current;
            var skillsShuffled = pawn.skills.skills.InRandomOrder().ToList();

            // 1. High claimed skills (8-14) with passions (2-3 of them)
            int highCount = Mathf.Clamp(settings.scammerHighSkills, 1, 6);
            for (int i = 0; i < highCount && i < skillsShuffled.Count; i++)
            {
                var sk = skillsShuffled[i];
                comp.reportedSkills[sk.def] = Rand.RangeInclusive(8, 14);
                // 50% minor, 50% major passion for first few
                comp.reportedPassions[sk.def] = Rand.Chance(0.5f) ? Passion.Major : Passion.Minor;
            }

            // 2. Mid-level claimed skills (4-8)
            int midCount = Mathf.Clamp(settings.scammerMidSkills, 0, 6);
            for (int i = highCount; i < highCount + midCount && i < skillsShuffled.Count; i++)
            {
                var sk = skillsShuffled[i];
                comp.reportedSkills[sk.def] = Rand.RangeInclusive(4, 8);
                if (Rand.Chance(0.3f))
                    comp.reportedPassions[sk.def] = Passion.Minor;
            }

            // 3. Low or truthful skills – reveal the rest so UI isn't Unknown
            for (int i = highCount + midCount; i < skillsShuffled.Count; i++)
            {
                comp.revealedSkills.Add(skillsShuffled[i].def);
            }
        }

        private static void ApplySleeper(Pawn pawn, CompPawnFog comp)
        {
            // Sleeper: any competent skill (≥6) is lied about – shown as poor (2-4).
            // All other low skills are revealed truthfully so the UI never shows "???".

            foreach (var skill in pawn.skills.skills)
            {
                if (skill.levelInt >= 6)
                {
                    // Present as mediocre (3–5) rather than abysmal; looks believable but still underwhelming.
                    comp.reportedSkills[skill.def] = Rand.RangeInclusive(3, 5);
                    // Keep the original passion visible so the low reported level isn't a giveaway.
                    comp.reportedPassions[skill.def] = skill.passion;
                }
                else
                {
                    comp.revealedSkills.Add(skill.def);
                }
            }

#if DEBUG
            if (Prefs.DevMode)
            {
                int repCount = comp.reportedSkills.Count;
                FogLog.Verbose($"[PROFILE] {pawn.LabelShort}: Sleeper masks set for {repCount} skills (tier={comp.tier}).");
            }
#endif
        }

        /// <summary>
        /// Decide which traits start hidden based on a simple random chance per trait.
        /// Revealed traits are added to <see cref="CompPawnFog.revealedTraits"/>; anything
        /// not present is considered fogged and will be masked in the UI.
        /// </summary>
        private static void ApplyTraitMasks(Pawn pawn, CompPawnFog comp, FogOfPawnSettings settings)
        {
            // Truthful pawns (and those where trait fogging is disabled) should start with every trait visible.
            if (comp.tier == DeceptionTier.Truthful)
            {
                foreach (var t in pawn.story?.traits?.allTraits ?? Enumerable.Empty<Trait>())
                {
                    comp.revealedTraits.Add(t.def);
                }
                return;
            }

            bool forceHideBad = comp.tier == DeceptionTier.DeceiverScammer;

            if (!settings.fogTraits || pawn.story?.traits == null)
            {
                // Reveal all traits when trait fogging is disabled or pawn has none.
                foreach (var t in pawn.story?.traits?.allTraits ?? Enumerable.Empty<Trait>())
                {
                    comp.revealedTraits.Add(t.def);
                }
                return;
            }

            List<Trait> hiddenList = null;
            foreach (var trait in pawn.story.traits.allTraits)
            {
                bool isBad = IsNegativeTrait(trait.def);
                bool hide = forceHideBad && isBad ? true : Rand.Value < settings.traitHideChance;
                if (!hide)
                {
                    comp.revealedTraits.Add(trait.def);
                }
                else
                {
                    hiddenList ??= new List<Trait>();
                    hiddenList.Add(trait);
                }
            }

            // Dev logging – list hidden traits once per pawn per session
            if (Prefs.DevMode && hiddenList != null && _loggedTraitMask.Add(pawn.thingIDNumber))
            {
                string summary = string.Join(", ", hiddenList.Select(t => t.def.defName));
                FogLog.Verbose($"[PROFILE] {pawn.LabelShort}: HiddenTraits=[{summary}]");
            }
        }

        private static readonly HashSet<string> _knownBadTraitDefNames = new()
        {
            "Pyromaniac", "Gourmand", "ChemicalInterest", "ChemicalFascination", "Jealous", "Greedy",
            "Volatile", "Nervous", "Slothful", "Lazy", "Sickly"
        };

        private static bool IsPositiveTrait(TraitDef def) => false; // placeholder
        private static bool IsNegativeTrait(TraitDef def)
        {
            return _knownBadTraitDefNames.Contains(def.defName);
        }

        public static float GetPawnValue(Pawn p)
        {
            float skillScore = p.skills.skills.Sum(s => s.Level);
            float passionScore = p.skills.skills.Count(s => s.passion == Passion.Minor) * 2 +
                                 p.skills.skills.Count(s => s.passion == Passion.Major) * 4;
            int goodTraits = p.story?.traits?.allTraits.Count(t => IsPositiveTrait(t.def)) ?? 0;
            int badTraits = p.story?.traits?.allTraits.Count(t => IsNegativeTrait(t.def)) ?? 0;

            return skillScore + passionScore + (goodTraits * 5) - (badTraits * 3);
        }

        private static readonly HashSet<int> _loggedTraitMask = new();
    }
} 