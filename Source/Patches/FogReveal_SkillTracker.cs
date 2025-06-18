using HarmonyLib;
using RimWorld;
using Verse;

namespace FogOfPawn.Patches
{
    [HarmonyPatch(typeof(SkillRecord), nameof(SkillRecord.Learn))]
    public static class FogReveal_SkillTracker
    {
        private static readonly HarmonyLib.AccessTools.FieldRef<SkillRecord, Pawn> _pawnRef = HarmonyLib.AccessTools.FieldRefAccess<SkillRecord, Pawn>("pawn");

        public static void Postfix(SkillRecord __instance, float xp, bool direct)
        {
            Pawn pawn = _pawnRef(__instance);
            if (pawn == null) return;
            var comp = pawn.GetComp<CompPawnFog>();
            if (comp == null || comp.tier == DeceptionTier.Truthful) return;

            // Sleeper story arc overrides immediate combat-XP reveal. Skip any XP trigger for sleepers.
            if (comp.tier == DeceptionTier.DeceiverSleeper)
                return;

            if (comp.tier == DeceptionTier.DeceiverScammer)
            {
                if (__instance.Level < 4)
                {
                    string key = "skillxp_" + __instance.def.defName;
                    if (!comp.tempData.TryGetValue(key, out float cur)) cur = 0f;
                    cur += xp;
                    comp.tempData[key] = cur;

                    if (cur >= FogSettingsCache.Current.scammerSkillXp)
                    {
                        FogUtility.TriggerFullReveal(pawn, "ScammerCaughtLearning");
                    }
                }
            }
        }
    }
} 