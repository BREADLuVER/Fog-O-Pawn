using HarmonyLib;
using RimWorld;
using Verse;

namespace FogOfPawn.Patches
{
    [HarmonyPatch(typeof(PawnGenerator), "GeneratePawn", new[] { typeof(PawnGenerationRequest) })]
    public static class Patch_PawnGenerator_GeneratePawn
    {
        public static void Postfix(Pawn __result, PawnGenerationRequest request)
        {
            if (__result == null)
            {
                return;
            }

            // Do not fog animals, mechanoids, or the player's starting colonists.
            // Downed refugees and slaves should also be revealed immediately.
            if (request.Context == PawnGenerationContext.PlayerStarter || 
                __result.skills == null || 
                __result.mindState == null ||
                request.AllowDowned ||
                __result.guest.GuestStatus == GuestStatus.Slave)
            {
                Log.Message($"[FogOfPawn] Skipping fog for {__result.NameShortColored} (Context: {request.Context}, IsSlave: {__result.guest.GuestStatus == GuestStatus.Slave}, AllowDowned: {request.AllowDowned})");
                return;
            }

            // Log that we are attempting to apply fog
            Log.Message($"[FogOfPawn] Applying fog for newly generated pawn: {__result.NameShortColored}");
            
            FogInitializer.InitializeFogFor(__result);
        }
    }
} 