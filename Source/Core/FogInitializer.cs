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
            if (pawn == null) return;
            
            var comp = pawn.GetComp<CompPawnFog>();
            if (comp == null || comp.compInitialized)
                return;

            // Additional safety checks
            if (pawn.skills == null || pawn.story == null)
            {
                FogLog.Verbose($"Skipping fog initialization for {pawn.LabelShort} - missing skills or story");
                comp.compInitialized = true;
                comp.fullyRevealed = true;
                return;
            }

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
            // Clear previous masks and apply new ones
            comp.maskOffsets.Clear();
            comp.passionOffsets.Clear();
            comp.revealedSkills.Clear();
            comp.revealedTraits.Clear();
            
            // Clear old data too (for compatibility)
            comp.reportedSkills.Clear();
            comp.reportedPassions.Clear();
            
            ApplyMasks(pawn, comp, FogSettingsCache.Current);
            ApplyTraitMasks(pawn, comp, FogSettingsCache.Current);
        }

        private static DeceptionTier ChooseTier(Pawn pawn, PawnGenerationRequest? request, FogOfPawnSettings settings)
        {
            float wTruth = Mathf.Max(0.01f, settings.pctTruthful);
            float wSlight = Mathf.Max(0f, settings.pctSlight);
            float wDeceiver = Mathf.Max(0f, settings.pctDeceiver);

            // Optional score-based scaling: more competent pawns are more likely deceivers.
            if (settings.scoreBasedLiarChance)
            {
                float pvScore = Mathf.Clamp01(GetPawnValue(pawn) / 500f); // 0 = worthless, 1 = highly skilled
                float factor = 1f + (1f - pvScore); // 2× weight for worst, 1× for best
                wDeceiver *= factor;
            }

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

            // Determine Sleeper vs Imposter based on pawn value
            float pv2 = GetPawnValue(pawn);
            float median2 = 250f;
            if (pv2 < median2)
            {
                return DeceptionTier.DeceiverImposter;
            }

            // Candidate for Sleeper – ensure it has no negative traits.
            bool hasBadTrait = pawn.story?.traits?.allTraits.Any(t => IsNegativeTrait(t.def)) ?? false;
            return hasBadTrait ? DeceptionTier.DeceiverImposter : DeceptionTier.DeceiverSleeper;
        }

        private static void ApplyMasks(Pawn pawn, CompPawnFog comp, FogOfPawnSettings settings)
        {
            if (!settings.fogSkills) return;
            if (pawn?.skills?.skills == null) return;

            switch (comp.tier)
            {
                case DeceptionTier.Truthful:
                    ApplyTruthful(pawn, comp);
                    break;
                case DeceptionTier.SlightlyDeceived:
                    ApplySlight(pawn, comp, settings);
                    break;
                case DeceptionTier.DeceiverImposter:
                    ApplyImposter(pawn, comp, settings);
                    break;
                case DeceptionTier.DeceiverSleeper:
                    ApplySleeper(pawn, comp);
                    break;
            }
        }

        private static void ApplyTruthful(Pawn pawn, CompPawnFog comp)
        {
            // Reveal everything; no reported overrides.
            if (pawn?.skills?.skills == null) return;
            
            foreach (var sk in pawn.skills.skills)
            {
                if (sk?.def != null)
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
                
                // Use gene-modified skill level for proper masking
                int currentLevel = EffectiveSkillUtility.GetGeneModifiedSkillLevel(skill);
                
                // Calculate the offset to apply
                int offset = understate ? -delta : delta;
                
                // Ensure the final masked level stays within bounds
                int maskedLevel = Mathf.Clamp(currentLevel + offset, 0, 20);
                offset = maskedLevel - currentLevel; // Adjust offset if clamping occurred
                
                if (offset != 0)
                {
                    comp.maskOffsets[skill.def] = offset;
                }
                
                // Sometimes add fake passion
                if (Rand.Chance(0.4f))
                {
                    Passion currentPassion = skill.passion;
                    Passion fakePassion = Rand.Chance(0.5f) ? Passion.Major : Passion.Minor;
                    
                    int passionOffset = (int)fakePassion - (int)currentPassion;
                    if (passionOffset != 0)
                    {
                        comp.passionOffsets[skill.def] = passionOffset;
                    }
                }
            }

            // All other skills are revealed (no mask applied)
            foreach (var sk in pawn.skills.skills)
            {
                if (!comp.maskOffsets.ContainsKey(sk.def))
                {
                    comp.revealedSkills.Add(sk.def);
                }
            }
        }

        private static void ApplyImposter(Pawn pawn, CompPawnFog comp, FogOfPawnSettings settings)
        {
            var skillsShuffled = pawn.skills.skills.InRandomOrder().ToList();
            
            // 1. High claimed skills (8-14) with passions (2-3 of them)
            int highCount = Mathf.Clamp(settings.imposterHighSkills, 1, 6);
            for (int i = 0; i < highCount && i < skillsShuffled.Count; i++)
            {
                var sk = skillsShuffled[i];
                
                // Use gene-modified skill level for proper masking
                int currentLevel = EffectiveSkillUtility.GetGeneModifiedSkillLevel(sk);
                
                // Target fake skill level
                int targetLevel = Rand.RangeInclusive(8, 14);
                
                // Calculate offset
                int offset = targetLevel - currentLevel;
                if (offset != 0)
                {
                    comp.maskOffsets[sk.def] = offset;
                }
                
                // Add fake passion (50% minor, 50% major)
                Passion currentPassion = sk.passion;
                Passion fakePassion = Rand.Chance(0.5f) ? Passion.Major : Passion.Minor;
                
                int passionOffset = (int)fakePassion - (int)currentPassion;
                if (passionOffset != 0)
                {
                    comp.passionOffsets[sk.def] = passionOffset;
                }
            }

            // 2. Mid-level claimed skills (4-8)
            int midCount = Mathf.Clamp(settings.imposterMidSkills, 0, 6);
            for (int i = highCount; i < highCount + midCount && i < skillsShuffled.Count; i++)
            {
                var sk = skillsShuffled[i];
                
                // Use gene-modified skill level for proper masking
                int currentLevel = EffectiveSkillUtility.GetGeneModifiedSkillLevel(sk);
                
                // Target fake skill level
                int targetLevel = Rand.RangeInclusive(4, 8);
                
                // Calculate offset
                int offset = targetLevel - currentLevel;
                if (offset != 0)
                {
                    comp.maskOffsets[sk.def] = offset;
                }
                
                // Sometimes add minor passion
                if (Rand.Chance(0.3f))
                {
                    Passion currentPassion = sk.passion;
                    Passion fakePassion = Passion.Minor;
                    
                    int passionOffset = (int)fakePassion - (int)currentPassion;
                    if (passionOffset != 0)
                    {
                        comp.passionOffsets[sk.def] = passionOffset;
                    }
                }
            }

            // 3. Low or truthful skills – reveal the rest so UI isn't Unknown
            for (int i = highCount + midCount; i < skillsShuffled.Count; i++)
            {
                comp.revealedSkills.Add(skillsShuffled[i].def);
            }
        }

        private static void ApplySleeper(Pawn pawn, CompPawnFog comp)
        {
            // Sleeper: any competent skill (≥6) is hidden by claiming to be poor (3-5).
            // All other low skills are revealed truthfully so the UI never shows "???".

            foreach (var skill in pawn.skills.skills)
            {
                // Use gene-modified skill level for proper masking
                int currentLevel = EffectiveSkillUtility.GetGeneModifiedSkillLevel(skill);
                
                if (currentLevel >= 6)
                {
                    // Target fake skill level (appear incompetent)
                    int targetLevel = Rand.RangeInclusive(3, 5);
                    
                    // Calculate offset (will be negative since we're hiding ability)
                    int offset = targetLevel - currentLevel;
                    if (offset != 0)
                    {
                        comp.maskOffsets[skill.def] = offset;
                    }
                    
                    // Keep the original passion visible so the low reported level isn't a complete giveaway
                    // This creates suspicious inconsistency: "Why does this terrible doctor have burning passion for medicine?"
                    // No passion offset needed - we want to show the real passion
                }
                else
                {
                    // Low skills are revealed truthfully
                    comp.revealedSkills.Add(skill.def);
                }
            }

#if DEBUG
            if (Prefs.DevMode)
            {
                int maskedCount = comp.maskOffsets.Count;
                FogLog.Verbose($"[PROFILE] {pawn.LabelShort}: Sleeper masks set for {maskedCount} skills (tier={comp.tier}).");
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

            bool forceHideBad = comp.tier == DeceptionTier.DeceiverImposter;

            bool biasBad = settings.biasBadTraitHiding;

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
                float effChance = settings.traitHideChance;
                if (biasBad && isBad)
                {
                    effChance = Mathf.Clamp01(effChance + GetDeceptionTendencyScore(trait.def));
                }

                bool hide = forceHideBad && isBad ? true : Rand.Value < effChance;
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

            // Ensure at least one trait is visible so the player always has some information.
            if (comp.revealedTraits.Count == 0 && hiddenList != null && hiddenList.Count > 0)
            {
                var traitToReveal = hiddenList.RandomElement();
                comp.revealedTraits.Add(traitToReveal.def);
                hiddenList.Remove(traitToReveal);
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

        // Deception tendency scores for individual traits (0–1). Higher values increase hide likelihood when bias toggle enabled.
        private static readonly Dictionary<string, float> _traitDeceptionScores = new()
        {
            {"Pyromaniac", 0.4f},
            {"Wimp", 0.25f},
            {"Nervous", 0.2f},
            {"Neurotic", 0.2f},
            {"Volatile", 0.25f},
            {"Gourmand", 0.15f}
        };

        private static float GetDeceptionTendencyScore(TraitDef def)
        {
            return _traitDeceptionScores.TryGetValue(def.defName, out var v) ? v : 0f;
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
        
        /// <summary>
        /// Safely calculates the aptitude modifier for a skill without triggering initialization issues.
        /// This returns the difference between effective level and trained level due to gene modifiers.
        /// For now, this is simplified to avoid crashes - proper gene support is disabled.
        /// </summary>
        private static int GetAptitudeModifier(Pawn pawn, SkillDef skillDef)
        {
            // For RimWorld 1.6 compatibility, we'll disable gene aptitude calculations
            // until proper implementation is complete to avoid crashes
            return 0;
            
            /* TODO: Implement proper gene aptitude support
            try
            {
                // Check if the pawn has genes (Biotech DLC)
                if (pawn.genes?.GenesListForReading == null)
                    return 0;
                
                int modifier = 0;
                foreach (var gene in pawn.genes.GenesListForReading)
                {
                    if (gene.def.statOffsets != null)
                    {
                        foreach (var statOffset in gene.def.statOffsets)
                        {
                            // Check if this stat offset affects the skill's learn rate or aptitude
                            if (IsSkillAptitudeStat(statOffset.stat, skillDef))
                            {
                                modifier += Mathf.RoundToInt(statOffset.value);
                            }
                        }
                    }
                }
                
                return modifier;
            }
            catch (System.Exception ex)
            {
                // If anything goes wrong, just return 0 (no modifier)
                FogLog.Verbose($"[GENE] Error calculating aptitude for {pawn.LabelShort} {skillDef.label}: {ex.Message}");
                return 0;
            }
            */
        }
        
        /// <summary>
        /// Checks if a stat affects the aptitude/level of a specific skill.
        /// Currently disabled to avoid crashes - will be implemented properly later.
        /// </summary>
        private static bool IsSkillAptitudeStat(StatDef stat, SkillDef skill)
        {
            // Disabled for now to avoid crashes
            return false;
        }
    }
} 