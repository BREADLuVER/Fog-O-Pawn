using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace FogOfPawn.Patches
{
    [HarmonyPatch(typeof(MentalState), nameof(MentalState.PostStart))]
    public static class Patch_MentalState_SocialFighting_PostStart
    {
        public static void Postfix(MentalState __instance)
        {
            if (!(__instance is MentalState_SocialFighting))
            {
                return;
            }
            
            Pawn pawn = __instance.pawn;
            if (pawn == null) return;
            
            var comp = pawn.GetComp<CompPawnFog>();
            if (comp == null) return;

            // Reveal Brawler trait if present
            Trait brawler = pawn.story?.traits?.GetTrait(TraitDefOf.Brawler);
            if (brawler != null)
            {
                comp.RevealTrait(brawler);
                // Optional: Notify the player
                // Find.LetterStack.ReceiveLetter("Trait Revealed: Brawler", $"{pawn.LabelShort}'s recent scuffle has revealed they are a Brawler.", LetterDefOf.NeutralEvent, pawn);
            }
            
            // Future: Could also check for other traits like Abrasive, etc.
        }
    }
} 