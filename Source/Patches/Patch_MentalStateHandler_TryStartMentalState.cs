using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace FogOfPawn.Patches
{
    [HarmonyPatch(typeof(MentalStateHandler), "TryStartMentalState")]
    public static class Patch_MentalStateHandler_TryStartMentalState
    {
        public static void Postfix(bool __result, MentalStateHandler __instance, MentalStateDef stateDef)
        {
            if (!__result) return;
            Pawn pawn = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();
            if (pawn == null) return;
            var comp = pawn.GetComp<CompPawnFog>();
            if (comp == null || comp.fullyRevealed) return;

            if (comp.tier == DeceptionTier.DeceiverScammer)
            {
                if (ShouldRevealOnMoodBreak(comp))
                {
                    FogUtility.TriggerFullReveal(pawn, "ScammerMoodBreak");
                }
                return;
            }

            if (comp.tier == DeceptionTier.SlightlyDeceived)
            {
                if (ShouldRevealOnMoodBreak(comp))
                {
                    FogUtility.TriggerFullReveal(pawn, "SlightMoodBreak");
                }
                return;
            }

            // If the mental state aligns with a hidden trait, trigger reveal
            bool reveal = false;
            if (stateDef.defName == "FireStartingSpree" && pawn.story?.traits?.HasTrait(TraitDefOf.Pyromaniac) == true)
                reveal = true;
            else if (stateDef.defName == "Binging_DrugExtreme" && (pawn.story?.traits?.HasTrait(DefDatabase<TraitDef>.GetNamedSilentFail("ChemicalFascination")) == true || pawn.story?.traits?.HasTrait(DefDatabase<TraitDef>.GetNamedSilentFail("ChemicalInterest")) == true))
                reveal = true;
            else if (stateDef.defName == "WanderFoodBinge" && pawn.story?.traits?.HasTrait(DefDatabase<TraitDef>.GetNamedSilentFail("Gourmand")) == true)
                reveal = true;

            if (reveal)
            {
                FogUtility.TriggerFullReveal(pawn, "ScammerMoodBreak");
            }
        }

        private static bool ShouldRevealOnMoodBreak(CompPawnFog comp)
        {
            // Base 50% chance, increasing linearly with time in colony (days) up to 100% after ~25 days.
            float daysSinceJoin = comp.ticksSinceJoin / 60000f; // 60k ticks per day
            float chance = 0.5f + Mathf.Clamp01(daysSinceJoin * 0.02f); // +2% per day
            return Rand.Chance(chance);
        }
    }
} 