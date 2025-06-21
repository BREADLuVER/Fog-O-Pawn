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

            float baseChance = settings.socialRevealPct / 100f;

            float ActorFactor = ___pawn?.skills?.GetSkill(RimWorld.SkillDefOf.Social)?.Level / 20f ?? 0f;
            float RecipFactor = recipient?.skills?.GetSkill(RimWorld.SkillDefOf.Social)?.Level / 20f ?? 0f;

            if (ActorFactor > 0 && Rand.Chance(baseChance * ActorFactor))
            {
                FogUtility.RevealRandomFoggedAttribute(___pawn, preferSkill: true);
            }

            if (RecipFactor > 0 && Rand.Chance(baseChance * RecipFactor))
            {
                FogUtility.RevealRandomFoggedAttribute(recipient, preferSkill: true);
            }

            // new: if the interaction is an insult and target is imposter -> full reveal
            if (intDef == InteractionDefOf.Insult && recipient != null)
            {
                var scamComp = recipient.GetComp<CompPawnFog>();
                if (scamComp != null && scamComp.tier == DeceptionTier.DeceiverImposter && !scamComp.fullyRevealed)
                {
                    FogUtility.TriggerFullReveal(recipient, "ImposterCalledOut");
                    return;
                }
            }
        }
    }
} 