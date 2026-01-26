using RimWorld;
using UnityEngine;
using Verse;
using System.Linq;
using HarmonyLib;

namespace FogOfPawn
{
    public static class EffectiveSkillUtility
    {
        public static int GetEffectiveSkill(Pawn pawn, SkillDef def)
        {
            if (pawn?.skills == null) return 0;

            var sr = pawn.skills.GetSkill(def);
            if (sr == null) return 0;

            int realLevel = sr.levelInt;

            var comp = pawn.GetComp<CompPawnFog>();
            if (comp == null || !comp.compInitialized)
            {
                return realLevel;
            }

            if (comp.revealedSkills.Contains(def))
            {
                return realLevel;
            }

            if (comp.maskOffsets.TryGetValue(def, out int offset))
            {
                int maskedLevel = realLevel + offset;
                return Mathf.Clamp(maskedLevel, 0, 20);
            }

            if (comp.reportedSkills.TryGetValue(def, out var rep) && rep.HasValue)
            {
                int reportedLevel = Mathf.Clamp(Mathf.RoundToInt(rep.Value), 0, 20);
                return reportedLevel;
            }

            return comp.tier == DeceptionTier.Truthful ? realLevel : 0;
        }
        
        public static Passion GetEffectivePassion(Pawn pawn, SkillDef def)
        {
            if (pawn?.skills == null) return Passion.None;

            var sr = pawn.skills.GetSkill(def);
            if (sr == null) return Passion.None;

            Passion realPassion = sr.passion;

            var comp = pawn.GetComp<CompPawnFog>();
            if (comp == null || !comp.compInitialized)
            {
                return realPassion;
            }

            if (comp.revealedSkills.Contains(def))
            {
                return realPassion;
            }

            if (comp.passionOffsets.TryGetValue(def, out int offset))
            {
                int newPassionLevel = (int)realPassion + offset;
                return (Passion)Mathf.Clamp(newPassionLevel, 0, 2);
            }

            if (comp.reportedPassions.TryGetValue(def, out var rep) && rep.HasValue)
            {
                return rep.Value;
            }

            return realPassion;
        }
        
    }
} 