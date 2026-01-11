using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace FogOfPawn
{
    /// <summary>
    /// Central utility for determining what should be masked and returning masked values.
    /// This is the single source of truth for all masking decisions.
    /// </summary>
    public static class FogMaskUtility
    {
        #region Skill Masking
        
        /// <summary>
        /// Returns true if this skill should be masked (not revealed and no gene conflicts).
        /// Option A: Don't mask skills that have visible gene effects.
        /// </summary>
        public static bool ShouldMaskSkill(Pawn pawn, SkillDef skillDef, CompPawnFog comp)
        {
            // Check if skill fogging is enabled in settings
            if (!FogSettingsCache.Current.fogSkills) return false;
            
            if (comp == null || !comp.compInitialized) return false;
            if (comp.tier == DeceptionTier.Truthful || comp.fullyRevealed) return false;
            
            // If we have an offset (even 0), we should mask it.
            // If it's in revealedSkills, we should NOT mask it.
            if (comp.revealedSkills.Contains(skillDef)) return false;
            
            // Option A: Don't mask if this skill has visible gene aptitudes
            if (HasVisibleGeneAptitude(pawn, skillDef))
            {
                return false;
            }
            
            // If it's not revealed and not truthful, and not gene-conflicted, 
            // then we should mask it if we have an offset or if it's a deceiver.
            bool hasMask = comp.maskOffsets.ContainsKey(skillDef);
            
            // For Sleepers and Imposters, we want to mask everything that isn't explicitly revealed.
            if (!hasMask && (comp.tier == DeceptionTier.DeceiverSleeper || comp.tier == DeceptionTier.DeceiverImposter))
            {
                return true;
            }
            
            return hasMask;
        }
        
        /// <summary>
        /// Returns the masked skill level for display purposes.
        /// Only call this when ShouldMaskSkill returns true.
        /// </summary>
        public static int GetMaskedSkillLevel(Pawn pawn, SkillDef skillDef, CompPawnFog comp)
        {
            var skill = pawn.skills?.GetSkill(skillDef);
            if (skill == null) return 0;
            
            // Get the base trained level (without genes, to avoid recursion issues)
            int trainedLevel = skill.levelInt;
            
            if (comp.maskOffsets.TryGetValue(skillDef, out int offset))
            {
                return Mathf.Clamp(trainedLevel + offset, 0, 20);
            }
            
            // Legacy support
            if (comp.reportedSkills.TryGetValue(skillDef, out var reported) && reported.HasValue)
            {
                return Mathf.Clamp(Mathf.RoundToInt(reported.Value), 0, 20);
            }
            
            // Fallback - if we should mask but have no offset, return a deterministic low value
            // based on the pawn's ID and skill def to avoid flickering.
            if (comp.tier == DeceptionTier.DeceiverSleeper || comp.tier == DeceptionTier.DeceiverImposter)
            {
                int seed = pawn.thingIDNumber ^ skillDef.index;
                return (seed % 5) + 2; // Returns 2-6
            }
            
            return 0;
        }
        
        /// <summary>
        /// Returns the masked passion for display purposes.
        /// </summary>
        public static Passion GetMaskedPassion(Pawn pawn, SkillDef skillDef, CompPawnFog comp)
        {
            var skill = pawn.skills?.GetSkill(skillDef);
            if (skill == null) return Passion.None;
            
            Passion realPassion = skill.passion;
            
            if (comp.passionOffsets.TryGetValue(skillDef, out int offset))
            {
                int newLevel = (int)realPassion + offset;
                return (Passion)Mathf.Clamp(newLevel, 0, 2);
            }
            
            // Legacy support
            if (comp.reportedPassions.TryGetValue(skillDef, out var fakePassion) && fakePassion.HasValue)
            {
                return fakePassion.Value;
            }
            
            return Passion.None;
        }
        
        /// <summary>
        /// Checks if the pawn has any visible genes that affect this skill's aptitude.
        /// If so, we can't mask this skill without creating obvious inconsistencies.
        /// </summary>
        public static bool HasVisibleGeneAptitude(Pawn pawn, SkillDef skillDef)
        {
            if (pawn?.genes?.GenesListForReading == null) return false;
            
            foreach (var gene in pawn.genes.GenesListForReading)
            {
                if (gene?.def?.aptitudes == null) continue;
                
                foreach (var aptitude in gene.def.aptitudes)
                {
                    if (aptitude.skill == skillDef && aptitude.level != 0)
                    {
                        // This gene affects this skill - can't mask it
                        return true;
                    }
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// Returns the total gene aptitude bonus for a skill (for informational purposes).
        /// </summary>
        public static int GetGeneAptitudeBonus(Pawn pawn, SkillDef skillDef)
        {
            if (pawn?.genes?.GenesListForReading == null) return 0;
            
            int total = 0;
            foreach (var gene in pawn.genes.GenesListForReading)
            {
                if (gene?.def?.aptitudes == null) continue;
                
                foreach (var aptitude in gene.def.aptitudes)
                {
                    if (aptitude.skill == skillDef)
                    {
                        total += aptitude.level;
                    }
                }
            }
            
            return total;
        }
        
        #endregion
        
        #region Trait Masking
        
        /// <summary>
        /// Returns true if this trait should be visible to the player.
        /// </summary>
        public static bool IsTraitVisible(Pawn pawn, TraitDef traitDef, CompPawnFog comp)
        {
            if (comp == null || !comp.compInitialized) return true;
            if (comp.tier == DeceptionTier.Truthful || comp.fullyRevealed) return true;
            if (!FogSettingsCache.Current.fogTraits) return true;
            
            return comp.revealedTraits.Contains(traitDef);
        }
        
        /// <summary>
        /// Returns a filtered list of traits that should be visible to the player.
        /// </summary>
        public static List<Trait> GetVisibleTraits(Pawn pawn, CompPawnFog comp)
        {
            if (pawn?.story?.traits?.allTraits == null) return new List<Trait>();
            
            var allTraits = pawn.story.traits.allTraits;
            
            if (comp == null || !comp.compInitialized) return allTraits.ToList();
            if (comp.tier == DeceptionTier.Truthful || comp.fullyRevealed) return allTraits.ToList();
            if (!FogSettingsCache.Current.fogTraits) return allTraits.ToList();
            
            return allTraits.Where(t => comp.revealedTraits.Contains(t.def)).ToList();
        }
        
        #endregion
        
        #region Mood/Thought Masking
        
        /// <summary>
        /// Returns true if this thought should be visible in the mood breakdown.
        /// Thoughts from hidden traits are not visible.
        /// </summary>
        public static bool IsThoughtVisible(Pawn pawn, Thought thought, CompPawnFog comp)
        {
            if (comp == null || !comp.compInitialized) return true;
            if (comp.tier == DeceptionTier.Truthful || comp.fullyRevealed) return true;
            if (!FogSettingsCache.Current.fogTraits) return true;
            
            // Check if this thought requires a hidden trait
            if (thought?.def?.requiredTraits != null)
            {
                foreach (var requiredTrait in thought.def.requiredTraits)
                {
                    if (!comp.revealedTraits.Contains(requiredTrait))
                    {
                        // This thought requires a hidden trait - hide it
                        return false;
                    }
                }
            }
            
            // Also check nullifying traits (thoughts that are blocked by traits)
            // These would reveal that the pawn DOESN'T have a trait
            // For now, we allow these through
            
            return true;
        }
        
        /// <summary>
        /// Filters a list of thoughts to only include visible ones.
        /// </summary>
        public static List<Thought> GetVisibleThoughts(Pawn pawn, List<Thought> thoughts, CompPawnFog comp)
        {
            if (comp == null || !comp.compInitialized) return thoughts;
            if (comp.tier == DeceptionTier.Truthful || comp.fullyRevealed) return thoughts;
            
            return thoughts.Where(t => IsThoughtVisible(pawn, t, comp)).ToList();
        }
        
        #endregion
        
        #region Initialization Helpers
        
        /// <summary>
        /// When initializing masks, skip skills that have gene aptitudes.
        /// Returns the list of skill defs that CAN be masked.
        /// </summary>
        public static List<SkillDef> GetMaskableSkills(Pawn pawn)
        {
            if (pawn?.skills?.skills == null) return new List<SkillDef>();
            
            var maskable = new List<SkillDef>();
            
            foreach (var skill in pawn.skills.skills)
            {
                if (!HasVisibleGeneAptitude(pawn, skill.def))
                {
                    maskable.Add(skill.def);
                }
            }
            
            return maskable;
        }
        
        #endregion
    }
}
