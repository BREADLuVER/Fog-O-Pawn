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
            if (comp == null || comp.tier != DeceptionTier.DeceiverScammer || comp.fullyRevealed) return;

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
    }
} 