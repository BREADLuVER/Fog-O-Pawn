using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace FogOfPawn
{
    public static class FogMaskUtility
    {
        #region Skill Masking
        
        public static bool ShouldMaskSkill(Pawn pawn, SkillDef skillDef, CompPawnFog comp)
        {
            if (!FogSettingsCache.Current.fogSkills) return false;
            
            if (comp == null || !comp.compInitialized) return false;
            if (comp.tier == DeceptionTier.Truthful || comp.fullyRevealed) return false;
            
            if (comp.revealedSkills.Contains(skillDef)) return false;
            
            bool hasMask = comp.maskOffsets.ContainsKey(skillDef);
            
            if (!hasMask && (comp.tier == DeceptionTier.DeceiverSleeper || comp.tier == DeceptionTier.DeceiverImposter))
            {
                return true;
            }
            
            return hasMask;
        }
        
        public static int GetMaskedSkillLevel(Pawn pawn, SkillDef skillDef, CompPawnFog comp)
        {
            var skill = pawn.skills?.GetSkill(skillDef);
            if (skill == null) return 0;
            
            int trainedLevel = skill.levelInt;
            
            if (comp.maskOffsets.TryGetValue(skillDef, out int offset))
            {
                return Mathf.Clamp(trainedLevel + offset, 0, 20);
            }
            
            if (comp.reportedSkills.TryGetValue(skillDef, out var reported) && reported.HasValue)
            {
                return Mathf.Clamp(Mathf.RoundToInt(reported.Value), 0, 20);
            }
            
            if (comp.tier == DeceptionTier.DeceiverSleeper || comp.tier == DeceptionTier.DeceiverImposter)
            {
                int seed = pawn.thingIDNumber ^ skillDef.index;
                return (seed % 5) + 2; 
            }
            
            return 0;
        }
        
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
            
            if (comp.reportedPassions.TryGetValue(skillDef, out var fakePassion) && fakePassion.HasValue)
            {
                return fakePassion.Value;
            }
            
            return Passion.None;
        }
        
        
        #endregion
        
        #region Trait Masking
        
        public static bool IsTraitVisible(Pawn pawn, TraitDef traitDef, CompPawnFog comp)
        {
            if (comp == null || !comp.compInitialized) return true;
            if (comp.tier == DeceptionTier.Truthful || comp.fullyRevealed) return true;
            if (!FogSettingsCache.Current.fogTraits) return true;
            
            return comp.revealedTraits.Contains(traitDef);
        }
        
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
        
        public static bool IsThoughtVisible(Pawn pawn, Thought thought, CompPawnFog comp)
        {
            if (comp == null || !comp.compInitialized) return true;
            if (comp.tier == DeceptionTier.Truthful || comp.fullyRevealed) return true;
            if (!FogSettingsCache.Current.fogTraits) return true;
            
            if (thought?.def?.requiredTraits != null)
            {
                foreach (var requiredTrait in thought.def.requiredTraits)
                {
                    if (!comp.revealedTraits.Contains(requiredTrait))
                    {
                        return false;
                    }
                }
            }
            
            
            return true;
        }
        
        public static List<Thought> GetVisibleThoughts(Pawn pawn, List<Thought> thoughts, CompPawnFog comp)
        {
            if (comp == null || !comp.compInitialized) return thoughts;
            if (comp.tier == DeceptionTier.Truthful || comp.fullyRevealed) return thoughts;
            
            return thoughts.Where(t => IsThoughtVisible(pawn, t, comp)).ToList();
        }
        
        #endregion
        
        #region Helper Overloads for Transpilers
        
        private static readonly System.Reflection.FieldInfo _pawnField = 
            HarmonyLib.AccessTools.Field(typeof(SkillRecord), "pawn");

        public static Pawn GetPawnFromSkillRecord(SkillRecord skill)
        {
            if (skill == null) return null;
            return _pawnField?.GetValue(skill) as Pawn;
        }

        public static int GetMaskedSkillLevel(SkillRecord skill)
        {
            if (skill == null) return 0;
            if (!RenderContext.IsRendering) return skill.levelInt;

            var pawn = GetPawnFromSkillRecord(skill);
            if (pawn == null) return skill.levelInt;
            
            var comp = pawn.GetComp<CompPawnFog>();
            if (comp == null || !comp.compInitialized) return skill.levelInt;
            
            if (ShouldMaskSkill(pawn, skill.def, comp))
            {
                return GetMaskedSkillLevel(pawn, skill.def, comp);
            }
            return skill.levelInt;
        }

        public static Passion GetMaskedPassion(SkillRecord skill)
        {
            if (skill == null) return Passion.None;
            if (!RenderContext.IsRendering) return skill.passion;

            var pawn = GetPawnFromSkillRecord(skill);
            if (pawn == null) return skill.passion;
            
            var comp = pawn.GetComp<CompPawnFog>();
            if (comp == null || !comp.compInitialized) return skill.passion;
            
            if (ShouldMaskSkill(pawn, skill.def, comp))
            {
                return GetMaskedPassion(pawn, skill.def, comp);
            }
            return skill.passion;
        }

        public static SkillRecord CreateFakeSkillRecord(SkillRecord original)
        {
            if (original == null) return null;
            
            var fake = (SkillRecord)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(SkillRecord));
            
            var fields = typeof(SkillRecord).GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            foreach (var field in fields)
            {
                try
                {
                    field.SetValue(fake, field.GetValue(original));
                }
                catch { } 
            }
            
            int maskedLevel = GetMaskedSkillLevel(original);
            Passion maskedPassion = GetMaskedPassion(original);
            
            fake.levelInt = maskedLevel;
            fake.passion = maskedPassion;
            
            return fake;
        }
        
        #endregion

        #region Initialization Helpers
        
        public static List<SkillDef> GetMaskableSkills(Pawn pawn)
        {
            if (pawn?.skills?.skills == null) return new List<SkillDef>();
            
            var maskable = new List<SkillDef>();
            
            foreach (var skill in pawn.skills.skills)
            {
                maskable.Add(skill.def);
            }
            
            return maskable;
        }
        
        #endregion
    }
}
