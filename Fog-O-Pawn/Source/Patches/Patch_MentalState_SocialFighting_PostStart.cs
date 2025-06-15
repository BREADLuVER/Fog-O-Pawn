using HarmonyLib;
using RimWorld;
using Verse;

namespace FogOfPawn.Patches
{
    [HarmonyPatch(typeof(MentalState_SocialFighting), "PostStart")]
    public static class Patch_MentalState_SocialFighting_PostStart
    {
        public static void Postfix(MentalState_SocialFighting __instance)
        {
            var pawn = __instance.pawn;
            var comp = pawn.GetComp<CompPawnFog>();
            if (comp == null) return;
            
            // Check for and reveal the Brawler trait
            var brawlerTrait = pawn.story.traits.GetTrait(TraitDefOf.Brawler);
            if (brawlerTrait != null)
            {
                comp.RevealTrait(brawlerTrait);
            }
            
            // Future: Could also check for other traits like Abrasive, etc.
        }
    }
} 