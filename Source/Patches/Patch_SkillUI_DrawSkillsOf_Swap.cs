using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using FogOfPawn; // FogLog
using System.Linq;
using System.Reflection;
using System.Text; // Added for StringBuilder

namespace FogOfPawn.Patches
{
    /// <summary>
    /// Safer approach – keep vanilla SkillUI.DrawSkillsOf rendering but temporarily mask
    /// skill level & passion just for the duration of the draw call.
    /// </summary>
    [StaticConstructorOnStartup]
    public static class Patch_SkillUI_DrawSkillsOf_Swap
    {
        private static readonly HashSet<int> LoggedPawns = new HashSet<int>();

        // We resolve the target method via reflection at static-init because SkillUI lives in game asm.
        static Patch_SkillUI_DrawSkillsOf_Swap()
        {
            try
            {
                var harmony = new Harmony("FogOfPawn.SkillUI.Swap");
                var skillUIType = AccessTools.TypeByName("RimWorld.SkillUI") ?? AccessTools.TypeByName("SkillUI");
                if (skillUIType == null)
                {
                    FogLog.Fail("SkillUIType", "Could not locate SkillUI type – skill fogging disabled.");
                    return;
                }

                // Resolve any DrawSkills* method (static or instance) that has a Pawn parameter.
                var method = skillUIType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)
                                         .FirstOrDefault(m => (m.Name == "DrawSkillsOf" || m.Name == "DrawSkills") &&
                                                              m.GetParameters().Any(p => p.ParameterType == typeof(Pawn)));
                if (method == null)
                {
                    FogLog.Fail("SkillUI.DrawSkills", "Could not locate SkillUI.DrawSkillsOf/DrawSkills – skill fogging disabled.");
                    return;
                }

                FogLog.Verbose($"[SkillUI] Patching {method.DeclaringType.FullName}.{method.Name} with {method.GetParameters().Length} parameters.");

                harmony.Patch(method,
                    prefix: new HarmonyMethod(typeof(Patch_SkillUI_DrawSkillsOf_Swap), nameof(Prefix)),
                    postfix: new HarmonyMethod(typeof(Patch_SkillUI_DrawSkillsOf_Swap), nameof(Postfix)));
                FogLog.Reflect("SkillUI.DrawSkillsOf.Patched", "SkillUI.DrawSkillsOf patched for fog masking (swap mode).");
            }
            catch (System.Exception ex)
            {
                Log.Error($"[FogOfPawn] Failed to patch SkillUI.DrawSkillsOf: {ex}");
            }
        }

        // Cache original data per-call so we can restore in Postfix.
        static void Prefix(Pawn p, out Dictionary<SkillRecord, (int level, Passion passion)> __state)
        {
            __state = null;

            if (!FogSettingsCache.Current.fogSkills) return;

            var comp = p.GetComp<CompPawnFog>();
            if (comp == null || !comp.compInitialized) return;
            if (comp.tier == DeceptionTier.Truthful || comp.fullyRevealed) return; // Never mask truthful/revealed pawns

            var cache = new Dictionary<SkillRecord, (int level, Passion passion)>();
            StringBuilder sb = Prefs.DevMode ? new StringBuilder() : null;

            foreach (var sk in p.skills.skills)
            {
                if (!comp.revealedSkills.Contains(sk.def))
                {
                    int originalLevel = sk.levelInt;
                    Passion originalPassion = sk.passion;
                    cache[sk] = (originalLevel, originalPassion);

                    if (Prefs.DevMode)
                    {
                        sb ??= new StringBuilder();
                    }

                    // Calculate effective level using offset system
                    int effectiveLevel;
                    if (comp.maskOffsets.TryGetValue(sk.def, out int offset))
                    {
                        // Apply offset to trained level (avoiding recursion)
                        effectiveLevel = Mathf.Clamp(sk.levelInt + offset, 0, 20);
                        
                        // Set the displayed level directly
                        sk.levelInt = effectiveLevel;
                        
                        sb?.AppendLine($"  {sk.def.label}: trained {originalLevel} → masked {effectiveLevel} (offset {offset:+0;-0;+0})");
                    }
                    else if (comp.reportedSkills.TryGetValue(sk.def, out var rep) && rep.HasValue)
                    {
                        // LEGACY: Support old format during migration
                        int reportedLevel = Mathf.Clamp(Mathf.RoundToInt(rep.Value), 0, 20);
                        sk.levelInt = reportedLevel;
                        sb?.AppendLine($"  {sk.def.label}: real {originalLevel} → reported {reportedLevel} (legacy)");
                    }
                    else
                    {
                        // Unknown skill - show as 0
                        sk.levelInt = 0;
                        sb?.AppendLine($"  {sk.def.label}: real {originalLevel} → Unknown");
                    }

                    // Calculate effective passion using offset system
                    if (comp.passionOffsets.TryGetValue(sk.def, out int passionOffset))
                    {
                        int newPassionLevel = (int)originalPassion + passionOffset;
                        sk.passion = (Passion)Mathf.Clamp(newPassionLevel, 0, 2);
                    }
                    else if (comp.reportedPassions.TryGetValue(sk.def, out var fakePassion) && fakePassion.HasValue)
                    {
                        // LEGACY: Support old format during migration
                        sk.passion = fakePassion.Value;
                    }
                    else
                    {
                        sk.passion = Passion.None;
                    }
                }
            }

            if (cache.Count > 0)
            {
                __state = cache;

                if (Prefs.DevMode && !LoggedPawns.Contains(p.thingIDNumber))
                {
                    LoggedPawns.Add(p.thingIDNumber);
                    if (sb != null)
                        FogLog.Verbose($"Masking skills for {p.LabelShort}\n{sb}");
                }
            }
        }

        private static void Postfix(Dictionary<SkillRecord, (int level, Passion passion)> __state)
        {
            if (__state == null) return; // nothing to restore

            foreach (var kv in __state)
            {
                kv.Key.levelInt = kv.Value.level;
                kv.Key.passion  = kv.Value.passion;
            }
        }
    }
} 