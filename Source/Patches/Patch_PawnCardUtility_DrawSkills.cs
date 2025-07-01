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
            else if (comp.reportedSkills.TryGetValue(skill.def, out var reported) && reported.HasValue)
            {
                // Calculate the effective level including gene modifiers
                int reportedTrainedLevel = Mathf.RoundToInt(reported.Value);
                int aptitudeModifier = skill.Level - skill.levelInt;
                int effectiveLevel = Mathf.Clamp(reportedTrainedLevel + aptitudeModifier, 0, 20);
                
                // Show the effective level that would result from the reported trained level
                valueStr = $"Reported: {effectiveLevel}";
            }
            else
            {
                valueStr = "Unknown";
            }
            Widgets.Label(valueRect, valueStr);

            // Draw real/fake passions
            var passion = isRevealed ? skill.passion : comp.reportedPassions[skill.def];
            if (passion.HasValue && passion.Value > Passion.None)
            {
                var passionRect = new Rect(valueRect.xMax + 5f, rect.y, 24f, 24f);
                var passionIcon = (passion.Value == Passion.Major) ? ContentFinder<Texture2D>.Get("UI/Icons/PassionMajor") : ContentFinder<Texture2D>.Get("UI/Icons/PassionMinor");
                GUI.DrawTexture(passionRect, passionIcon);
            }
        }
    }
}
#endif 