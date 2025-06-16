using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using FogOfPawn; // FogLog

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
            var harmony = new Harmony("FogOfPawn.SkillUI.Swap");
            var skillUIType = AccessTools.TypeByName("RimWorld.SkillUI") ?? AccessTools.TypeByName("SkillUI");
            if (skillUIType == null)
            {
                FogLog.Fail("SkillUIType", "Could not locate SkillUI type – skill fogging disabled.");
                return;
            }

            var method = AccessTools.Method(skillUIType, "DrawSkillsOf");
            if (method == null)
            {
                FogLog.Fail("SkillUI.DrawSkillsOf", "Could not locate SkillUI.DrawSkillsOf – skill fogging disabled.");
                return;
            }

            harmony.Patch(method,
                prefix: new HarmonyMethod(typeof(Patch_SkillUI_DrawSkillsOf_Swap), nameof(Prefix)),
                postfix: new HarmonyMethod(typeof(Patch_SkillUI_DrawSkillsOf_Swap), nameof(Postfix)));
            FogLog.Reflect("SkillUI.DrawSkillsOf.Patched", "SkillUI.DrawSkillsOf patched for fog masking (swap mode).");
        }

        // Cache original data per-call so we can restore in Postfix.
        private static void Prefix(Pawn p, Vector2 offset, object mode, Rect container,
                                   out Dictionary<SkillRecord, (int level, Passion passion)> __state)
        {
            __state = null;

            if (!FogSettingsCache.Current.fogSkills)
            {
                return;
            }

            var comp = p.GetComp<CompPawnFog>();
            if (comp == null || !comp.compInitialized) return;

            var cache = new Dictionary<SkillRecord, (int, Passion)>();
            System.Text.StringBuilder sb = null;

            foreach (var sk in p.skills.skills)
            {
                if (!comp.revealedSkills.Contains(sk.def))
                {
                    int originalLevel = sk.levelInt;
                    cache[sk] = (originalLevel, sk.passion);

                    if (Prefs.DevMode)
                    {
                        sb ??= new System.Text.StringBuilder();
                    }

                    // Substitute reported / unknown values.
                    if (comp.reportedSkills.TryGetValue(sk.def, out var rep) && rep.HasValue)
                    {
                        sk.levelInt = Mathf.Clamp(Mathf.RoundToInt(rep.Value), 0, 20);
                        sb.AppendLine($"  {sk.def.label}: real {originalLevel} → reported {sk.levelInt}");
                    }
                    else
                    {
                        sk.levelInt = 0; // Unknown – show 0, vanilla turns that into "-"
                        sb.AppendLine($"  {sk.def.label}: real {originalLevel} → Unknown");
                    }

                    if (comp.reportedPassions.TryGetValue(sk.def, out var fakePassion) && fakePassion.HasValue)
                    {
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