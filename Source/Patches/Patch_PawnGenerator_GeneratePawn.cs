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

            // Skip cases where fogging is inappropriate or risky.
            //  • Player-starter pawns
            //  • Animals / mechanoids (no skills)
            //  • Pawns generated as downed refugees etc.
            //  • Slaves (guest tracker may be null!)
            bool isSlave = __result.guest != null && __result.guest.GuestStatus == GuestStatus.Slave;

            if ((__result.Faction?.IsPlayer ?? false) ||
                request.Context == PawnGenerationContext.PlayerStarter ||
                __result.RaceProps?.Animal == true ||
                __result.RaceProps?.IsMechanoid == true ||
                __result.skills == null ||
                __result.mindState == null ||
                request.AllowDowned ||
                isSlave)
            {
                Log.Message($"[FogOfPawn] Skipping fog for {__result.NameShortColored} (Context: {request.Context}, IsSlave: {isSlave}, Animal: {__result.RaceProps?.Animal}, Mech: {__result.RaceProps?.IsMechanoid})");
                return;
            }

            try
            {
                // Log that we are attempting to apply fog
                Log.Message($"[FogOfPawn] Applying fog for newly generated pawn: {__result.NameShortColored}");

                FogInitializer.InitializeFogFor(__result);
            }
            catch (System.Exception ex)
            {
                Log.Error($"[FogOfPawn] Exception while initializing fog for {__result}: {ex}");
            }
        }
    }
} 