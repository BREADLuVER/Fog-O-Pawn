using HarmonyLib;
using RimWorld;
using Verse;
using UnityEngine;

namespace FogOfPawn.Patches
{
    /// <summary>
    /// Ensures every read of <see cref="SkillRecord.Level"/> returns the masked value if the pawn is still fogged.
    /// This provides a transparent illusion: UI, work priorities, job drivers – everything sees the fake level
    /// until the skill is actually revealed. Once revealed we fall back to vanilla behaviour automatically.
    /// </summary>
    [HarmonyPatch(typeof(SkillRecord), "get_Level")]
    public static class Patch_SkillRecord_GetLevel_Mask
    {
        static void Postfix(SkillRecord __instance, ref int __result)
        {
            try
            {
                // Retrieve pawn via reflection (field or property) for cross-version safety.
                Pawn pawn = null;
                if (__instance != null)
                {
                    // Try common field/property names.
                    pawn = AccessTools.Field(typeof(SkillRecord), "pawn")?.GetValue(__instance) as Pawn;
                    if (pawn == null)
                    {
                        var prop = AccessTools.PropertyGetter(typeof(SkillRecord), "Pawn");
                        if (prop != null)
                            pawn = prop.Invoke(__instance, null) as Pawn;
                    }
                }
                if (pawn == null) return;

                // Base effective value (masked or real).
                int baseVal = EffectiveSkillUtility.GetEffectiveSkill(pawn, __instance.def);

                // Add random performance jitter for scammers – makes them occasionally botch jobs.
                var comp = pawn.GetComp<CompPawnFog>();
                if (comp != null && !comp.fullyRevealed && comp.tier == DeceptionTier.DeceiverScammer)
                {
                    // Seed based on pawn, skill and current hour so result is stable for short stretches.
                    int seed = pawn.thingIDNumber ^ __instance.def.shortHash ^ (Find.TickManager.TicksGame / 2500);
                    Rand.PushState(seed);
                    if (Rand.Chance(0.15f))
                    {
                        // 3–6 level penalty simulating a conspicuous failure.
                        baseVal = Mathf.Max(0, baseVal - Rand.RangeInclusive(3, 6));
                    }
                    Rand.PopState();
                }

                // Mild performance wobble for Slightly-Deceived – rare and small.
                else if (comp != null && !comp.fullyRevealed && comp.tier == DeceptionTier.SlightlyDeceived)
                {
                    int seed = pawn.thingIDNumber ^ __instance.def.shortHash ^ (Find.TickManager.TicksGame / 2500) ^ 0x5A5A;
                    Rand.PushState(seed);
                    if (Rand.Chance(0.05f))
                    {
                        // 1–3 level penalty – just enough to raise suspicion.
                        baseVal = Mathf.Max(0, baseVal - Rand.RangeInclusive(1, 3));
                    }
                    Rand.PopState();
                }

                __result = baseVal;
            }
            catch (System.Exception ex)
            {
                // Avoid hard failure – just emit a warning once.
                Log.Warning("[FogOfPawn] Exception in SkillMask patch: " + ex);
            }
        }
    }
} 