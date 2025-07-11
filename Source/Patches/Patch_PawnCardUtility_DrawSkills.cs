#if RW_HAS_PAWNCARDUTILITY
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace FogOfPawn.Patches
{
    [HarmonyPatch(typeof(PawnCardUtility), "DrawSkills")]
    public static class Patch_PawnCardUtility_DrawSkills
    {
        private const float SkillHeight = 24f;
        private const float SkillYSpacing = 3f;
        
        public static bool Prefix(Rect rect, Pawn pawn)
        {
            var comp = pawn.GetComp<CompPawnFog>();
            if (comp == null || !comp.compInitialized)
            {
                // If no comp or not ready, fall back to vanilla method.
                return true;
            }

            Text.Font = GameFont.Small;
            var skillRect = new Rect(rect.x, rect.y, rect.width, SkillHeight);

            foreach (var skill in pawn.skills.skills)
            {
                DrawSkillRow(skillRect, skill, comp);
                skillRect.y += SkillHeight + SkillYSpacing;
            }
            
            return false;
        }

        private static void DrawSkillRow(Rect rect, SkillRecord skill, CompPawnFog comp)
        {
            bool isRevealed = comp.revealedSkills.Contains(skill.def);
            
            // Label: "Shooting"
            GUI.color = new Color(0.9f, 0.9f, 0.9f, 1f);
            var labelRect = new Rect(rect.x, rect.y, rect.width * 0.4f, rect.height);
            Widgets.Label(labelRect, skill.def.skillLabel.CapitalizeFirst());
            
            // Value: "Reported: 8" or "8"
            var valueRect = new Rect(labelRect.xMax, rect.y, 100f, rect.height);
            string valueStr;
            if (isRevealed)
            {
                valueStr = skill.Level.ToString();
            }
            else if (comp.maskOffsets.TryGetValue(skill.def, out int offset))
            {
                // Use offset-based calculation (avoid recursion)
                int maskedLevel = Mathf.Clamp(skill.levelInt + offset, 0, 20);
                valueStr = $"Reported: {maskedLevel}";
            }
            else if (comp.reportedSkills.TryGetValue(skill.def, out var reported) && reported.HasValue)
            {
                // LEGACY: Support old format during migration
                int reportedLevel = Mathf.RoundToInt(reported.Value);
                valueStr = $"Reported: {reportedLevel}";
            }
            else
            {
                valueStr = "Unknown";
            }
            Widgets.Label(valueRect, valueStr);

            // Draw real/fake passions
            Passion displayPassion;
            if (isRevealed)
            {
                displayPassion = skill.passion;
            }
            else if (comp.passionOffsets.TryGetValue(skill.def, out int passionOffset))
            {
                // Use offset-based calculation
                int newPassionLevel = (int)skill.passion + passionOffset;
                displayPassion = (Passion)Mathf.Clamp(newPassionLevel, 0, 2);
            }
            else if (comp.reportedPassions.TryGetValue(skill.def, out var fakePassion) && fakePassion.HasValue)
            {
                // LEGACY: Support old format during migration
                displayPassion = fakePassion.Value;
            }
            else
            {
                displayPassion = Passion.None;
            }
            
            if (displayPassion > Passion.None)
            {
                var passionRect = new Rect(valueRect.xMax + 5f, rect.y, 24f, 24f);
                var passionIcon = (displayPassion == Passion.Major) ? ContentFinder<Texture2D>.Get("UI/Icons/PassionMajor") : ContentFinder<Texture2D>.Get("UI/Icons/PassionMinor");
                GUI.DrawTexture(passionRect, passionIcon);
            }
        }
    }
}
#endif 