using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using System.Linq;

namespace FogOfPawn.Patches
{
    /// <summary>
    /// RimWorld 1.6 refactored the pawn card; the old PawnCardUtility.DrawSkills was removed.
    /// We locate the new SkillUI.DrawSkillsOf method via reflection at runtime and patch it dynamically.
    /// If it cannot be found we log a warning and gracefully skip the patch.
    /// The prefix replicates the vanilla drawing code but replaces skill values with the fogged / reported ones.
    /// </summary>
    [StaticConstructorOnStartup]
    public static class Patch_SkillUI_DrawSkillsOf
    {
#if FOGOPAWN_CUSTOM_DRAW
        // Textures for drawing passion icons (cached once).
        private static readonly Texture2D PassionMajorTex = ContentFinder<Texture2D>.Get("UI/Icons/PassionMajor");
        private static readonly Texture2D PassionMinorTex = ContentFinder<Texture2D>.Get("UI/Icons/PassionMinor");

        // Constants that roughly match vanilla layout. These may need tweaking as the UI evolves.
        private const float SkillRowHeight = 24f;
        private const float SkillRowSpacing = 3f;
        private const float LabelWidthPct = 0.4f;

        static Patch_SkillUI_DrawSkillsOf()
        {
            var harmony = new Harmony("FogOfPawn.SkillUI.DrawSkillsOf");

            try
            {
                // SkillUI lives either in the RimWorld namespace or at the root depending on build.
                Type skillUIType = AccessTools.TypeByName("RimWorld.SkillUI") ?? AccessTools.TypeByName("SkillUI");
                if (skillUIType == null)
                {
                    Log.Warning("[FogOfPawn] Could not locate SkillUI type. Skill fog will not be applied to the pawn card.");
                    return;
                }

                // DEBUG: Log what SkillUI we actually resolved.
                Log.Message($"[FogOfPawn DEBUG] Resolved SkillUI type = {skillUIType.FullName}");

                // Dump any DrawSkills* methods on that type so we can see their signatures.
                foreach (var dbg in skillUIType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
                {
                    if (dbg.Name.StartsWith("DrawSkill"))
                    {
                        Log.Message($"[FogOfPawn DEBUG] Candidate method {dbg.Name} (params: {string.Join(", ", dbg.GetParameters().Select(p => p.ParameterType.Name) )})");
                    }
                }

                // Primary attempt: DrawSkillsOf on SkillUI.
                MethodInfo targetMethod = FindMethod(skillUIType, "DrawSkillsOf") ?? FindMethod(skillUIType, "DrawSkills");

                // Fallback: brute-force search across all types if not found.
                if (targetMethod == null)
                {
                    var asm = skillUIType.Assembly;
                    foreach (var type in asm.GetTypes())
                    {
                        if (!type.IsClass || !type.IsAbstract) continue;
                        targetMethod = FindMethod(type, "DrawSkillsOf") ?? FindMethod(type, "DrawSkills");
                        if (targetMethod != null)
                        {
                            Log.Message($"[FogOfPawn] Found skill draw method on {type.FullName}.{targetMethod.Name}");
                            break;
                        }
                    }
                }

                if (targetMethod == null)
                {
                    Log.Warning("[FogOfPawn] Could not locate any DrawSkills method. Skill fog will not be applied.");
                    return;
                }

                harmony.Patch(targetMethod, prefix: new HarmonyMethod(typeof(Patch_SkillUI_DrawSkillsOf), nameof(Prefix)));
                Log.Message("[FogOfPawn] Patched SkillUI.DrawSkillsOf for skill fogging.");
            }
            catch (Exception ex)
            {
                Log.Error($"[FogOfPawn] Exception while patching SkillUI.DrawSkillsOf: {ex}");
            }
        }

        /// <summary>
        /// Prefix that draws the skill list using fogged data.
        /// We intentionally provide a flexible signature to match any original method
        /// (Rect rect, Pawn pawn, ...). Harmony will match parameters by name where possible.
        /// </summary>
        /// <param name="p">The pawn whose skills are being drawn.</param>
        /// <param name="offset">The offset within the container.</param>
        /// <param name="mode">The skill draw mode.</param>
        /// <param name="container">The container rect.</param>
        /// <returns>False to suppress vanilla drawing when the pawn has an initialized fog component; true otherwise.</returns>
        public static bool Prefix(Pawn p, Vector2 offset, object mode, Rect container)
        {
            var comp = p.GetComp<CompPawnFog>();
            if (comp == null || !comp.compInitialized)
            {
                // Fall back to vanilla draw if no fog component.
                return true;
            }

            // Begin a GUI group so we can draw using local coordinates like vanilla does.
            Widgets.BeginGroup(container);

            Rect localRect = new Rect(offset.x, offset.y, container.width, container.height);

            Log.Message($"[FogOfPawn DEBUG] DrawSkillsOf Prefix for {p.LabelShort}, localRect={localRect}, container={container}, offset={offset}");

            DrawSkillsWithFog(localRect, p, comp);

            Widgets.EndGroup();
            // Skip original method.
            return false;
        }

        private static void DrawSkillsWithFog(Rect rect, Pawn pawn, CompPawnFog comp)
        {
            Text.Font = GameFont.Small;
            Rect rowRect = new Rect(rect.x, rect.y, rect.width, SkillRowHeight);

            foreach (var skill in pawn.skills?.skills ?? Enumerable.Empty<SkillRecord>())
            {
                DrawSkillRow(rowRect, skill, comp);
                rowRect.y += SkillRowHeight + SkillRowSpacing;
            }
        }

        private static void DrawSkillRow(Rect rect, SkillRecord skill, CompPawnFog comp)
        {
            bool revealed = comp.revealedSkills.Contains(skill.def);

            // Label section
            float labelWidth = rect.width * LabelWidthPct;
            var labelRect = new Rect(rect.x, rect.y, labelWidth, rect.height);
            Widgets.Label(labelRect, skill.def.skillLabel.CapitalizeFirst());

            // Value section
            var valueRect = new Rect(labelRect.xMax + 6f, rect.y, 60f, rect.height);
            string valueStr;
            if (revealed)
            {
                valueStr = skill.Level.ToString();
            }
            else if (comp.reportedSkills.TryGetValue(skill.def, out var reported) && reported.HasValue)
            {
                valueStr = $"Reported: {reported.Value:F0}";
            }
            else
            {
                valueStr = "Unknown";
            }
            Widgets.Label(valueRect, valueStr);

            // Passion icon (if any)
            Passion passion = revealed ? skill.passion : (comp.reportedPassions.TryGetValue(skill.def, out var repPassion) && repPassion.HasValue ? repPassion.Value : Passion.None);
            if (passion > Passion.None)
            {
                var iconRect = new Rect(valueRect.xMax + 5f, rect.y, 24f, 24f);
                var tex = passion == Passion.Major ? PassionMajorTex : PassionMinorTex;
                GUI.DrawTexture(iconRect, tex);
            }
        }

        private static MethodInfo FindMethod(Type type, string methodName)
        {
            return type.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        }
#endif
    }
} 