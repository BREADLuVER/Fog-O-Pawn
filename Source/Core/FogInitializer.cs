using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using System.Collections.Generic;

namespace FogOfPawn
{
    public static class FogInitializer
    {
        public static void InitializeFogFor(Pawn pawn, PawnGenerationRequest? request = null)
        {
            if (pawn == null) return;
            
            var comp = pawn.GetComp<CompPawnFog>();
            if (comp == null || comp.compInitialized)
                return;

            if (pawn.skills == null || pawn.story == null)
            {
                FogLog.Verbose($"Skipping fog initialization for {pawn.LabelShort} - missing skills or story (will retry on next load phase)");
                return;
            }

            var settings = FogSettingsCache.Current;

            if (!comp.tierManuallySet)
            {
                comp.tier = ChooseTier(pawn, request, settings);
            }

            ApplyMasks(pawn, comp, settings);

            ApplyTraitMasks(pawn, comp, settings);

            comp.compInitialized = true;

            FogLog.Verbose($"Initialized fog for {pawn.NameShortColored}. Tier={comp.tier}");
        }

        public static void RegenerateMasksFor(Pawn pawn, CompPawnFog comp)
        {
            comp.maskOffsets.Clear();
            comp.passionOffsets.Clear();
            comp.revealedSkills.Clear();
            comp.revealedTraits.Clear();
            
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

            if (settings.scoreBasedLiarChance)
            {
                float pvScore = Mathf.Clamp01(GetPawnValue(pawn) / 500f); 
                float factor = 1f + (1f - pvScore); 
                wDeceiver *= factor;
            }

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

            if (settings.deceiverJoinersOnly && request.HasValue && request.Value.Context == PawnGenerationContext.NonPlayer)
            {
                return DeceptionTier.Truthful;
            }

            float pv2 = GetPawnValue(pawn);
            float median2 = 250f;
            if (pv2 < median2)
            {
                return DeceptionTier.DeceiverImposter;
            }

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

            var maskableSkills = FogMaskUtility.GetMaskableSkills(pawn);
            var skillsToAlter = maskableSkills.InRandomOrder().Take(count);
            
            foreach (var skillDef in skillsToAlter)
            {
                var skill = pawn.skills.GetSkill(skillDef);
                if (skill == null) continue;
                
                bool understate = settings.allowUnderstate && Rand.Chance(0.5f);
                int range = Mathf.Clamp(settings.alteredSkillRange, 2, 10);
                int delta = Rand.RangeInclusive(2, range);
                
                int currentLevel = skill.levelInt;
                
                int offset = understate ? -delta : delta;
                
                int maskedLevel = Mathf.Clamp(currentLevel + offset, 0, 20);
                offset = maskedLevel - currentLevel; 
                
                comp.maskOffsets[skillDef] = offset;
                
                if (Rand.Chance(0.4f))
                {
                    Passion currentPassion = skill.passion;
                    Passion fakePassion = Rand.Chance(0.5f) ? Passion.Major : Passion.Minor;
                    
                    int passionOffset = (int)fakePassion - (int)currentPassion;
                    if (passionOffset != 0)
                    {
                        comp.passionOffsets[skillDef] = passionOffset;
                    }
                }
            }

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
            var maskableSkills = FogMaskUtility.GetMaskableSkills(pawn);
            var skillsShuffled = maskableSkills.InRandomOrder().ToList();
            
            FogLog.Verbose($"[IMPOSTER INIT] {pawn.LabelShort}: maskable skills={maskableSkills.Count}, total skills={pawn.skills.skills.Count}");
            
            var targetedSkills = new HashSet<SkillDef>();
            
            int highCount = Mathf.Clamp(settings.imposterHighSkills, 1, 6);
            for (int i = 0; i < highCount && i < skillsShuffled.Count; i++)
            {
                var skillDef = skillsShuffled[i];
                var sk = pawn.skills.GetSkill(skillDef);
                if (sk == null) continue;
                
                targetedSkills.Add(skillDef);
                
                int currentLevel = sk.levelInt;
                
                int targetLevel = Rand.RangeInclusive(8, 14);
                
                int offset = targetLevel - currentLevel;
                comp.maskOffsets[skillDef] = offset;
                
                FogLog.Verbose($"  [HIGH] {skillDef.defName}: real={currentLevel}, target={targetLevel}, offset={offset}");
                
                Passion currentPassion = sk.passion;
                Passion fakePassion = Rand.Chance(0.5f) ? Passion.Major : Passion.Minor;
                
                int passionOffset = (int)fakePassion - (int)currentPassion;
                if (passionOffset != 0)
                {
                    comp.passionOffsets[skillDef] = passionOffset;
                }
            }

            int midCount = Mathf.Clamp(settings.imposterMidSkills, 0, 6);
            for (int i = highCount; i < highCount + midCount && i < skillsShuffled.Count; i++)
            {
                var skillDef = skillsShuffled[i];
                var sk = pawn.skills.GetSkill(skillDef);
                if (sk == null) continue;
                
                targetedSkills.Add(skillDef);
                
                int currentLevel = sk.levelInt;
                
                int targetLevel = Rand.RangeInclusive(4, 8);
                
                int offset = targetLevel - currentLevel;
                comp.maskOffsets[skillDef] = offset;
                
                FogLog.Verbose($"  [MID] {skillDef.defName}: real={currentLevel}, target={targetLevel}, offset={offset}");
                
                if (Rand.Chance(0.3f))
                {
                    Passion currentPassion = sk.passion;
                    Passion fakePassion = Passion.Minor;
                    
                    int passionOffset = (int)fakePassion - (int)currentPassion;
                    if (passionOffset != 0)
                    {
                        comp.passionOffsets[skillDef] = passionOffset;
                    }
                }
            }

            foreach (var sk in pawn.skills.skills)
            {
                if (!comp.maskOffsets.ContainsKey(sk.def))
                {
                    comp.revealedSkills.Add(sk.def);
                }
            }
            
            FogLog.Verbose($"[IMPOSTER INIT] Complete: maskOffsets={comp.maskOffsets.Count}, revealedSkills={comp.revealedSkills.Count}");
        }

        private static void ApplySleeper(Pawn pawn, CompPawnFog comp)
        {

            var maskableSkills = FogMaskUtility.GetMaskableSkills(pawn);

            foreach (var skill in pawn.skills.skills)
            {
                int currentLevel = skill.levelInt;
                
                if (currentLevel >= 6)
                {
                    int targetLevel = Rand.RangeInclusive(3, 5);
                    
                    int offset = targetLevel - currentLevel;
                    
                    comp.maskOffsets[skill.def] = offset;
                    
                }
                else
                {
                    comp.revealedSkills.Add(skill.def);
                }
            }

            if (Prefs.DevMode)
            {
                int maskedCount = comp.maskOffsets.Count;
                FogLog.Verbose($"[PROFILE] {pawn.LabelShort}: Sleeper masks set for {maskedCount} skills (tier={comp.tier}).");
            }
        }

        private static void ApplyTraitMasks(Pawn pawn, CompPawnFog comp, FogOfPawnSettings settings)
        {
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

            if (comp.revealedTraits.Count == 0 && hiddenList != null && hiddenList.Count > 0)
            {
                var traitToReveal = hiddenList.RandomElement();
                comp.revealedTraits.Add(traitToReveal.def);
                hiddenList.Remove(traitToReveal);
            }

            if (Prefs.DevMode && hiddenList != null && _loggedTraitMask.Add(pawn.thingIDNumber))
            {
                string summary = string.Join(", ", hiddenList.Select(t => t.def.defName));
                FogLog.Verbose($"[PROFILE] {pawn.LabelShort}: HiddenTraits=[{summary}]");
            }
        }

        public static readonly HashSet<string> _knownBadTraitDefNames = new()
        {
            "Pyromaniac", "Gourmand", "ChemicalInterest", "ChemicalFascination", "Jealous", "Greedy",
            "Volatile", "Nervous", "Slothful", "Lazy", "Sickly", "Wimp", "Pessimist", "Ugly", "StaggeringlyUgly",
            "AnnoyingVoice", "CreepyBreathing", "Recluse", "Abrasive", "Misandrist", "Misogynist", "SlowLearner",
            "Depressive", "ToxSickly"
        };

        private static bool IsPositiveTrait(TraitDef def) => false; 
        public static bool IsNegativeTrait(TraitDef def)
        {
            return _knownBadTraitDefNames.Contains(def.defName);
        }

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
    }
} 