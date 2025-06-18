using HarmonyLib;
using RimWorld;
using Verse;

namespace FogOfPawn.Patches
{
    [HarmonyPatch(typeof(Pawn_InteractionsTracker), nameof(Pawn_InteractionsTracker.TryInteractWith))]
    public static class Patch_Pawn_InteractionsTracker_TryInteractWith
    {
        // bool return value indicates success
        public static void Postfix(bool __result, Pawn ___pawn, Pawn recipient, InteractionDef intDef)
        {
            if (!__result) return;

            var settings = FogSettingsCache.Current;
            if (settings.socialRevealPct <= 0) return;

            // Actor
            if (Rand.Chance(settings.socialRevealPct / 100f))
            {
                FogUtility.RevealRandomFoggedAttribute(___pawn, preferSkill: true);
            }

            // Recipient
            if (Rand.Chance(settings.socialRevealPct / 100f))
            {
                FogUtility.RevealRandomFoggedAttribute(recipient, preferSkill: true);
            }

            // new: if the interaction is an insult and target is scammer -> full reveal
            if (intDef == InteractionDefOf.Insult && recipient != null)
            {
                var scamComp = recipient.GetComp<CompPawnFog>();
                if (scamComp != null && scamComp.tier == DeceptionTier.DeceiverScammer && !scamComp.fullyRevealed)
                {
                    FogUtility.TriggerFullReveal(recipient, "ScammerCalledOut");
                    return;
                }
            }
        }
    }
} 