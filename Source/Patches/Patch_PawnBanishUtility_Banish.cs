using HarmonyLib;
using RimWorld;
using Verse;

namespace FogOfPawn.Patches
{
    [HarmonyPatch]
    public static class Patch_PawnBanishUtility_Banish
    {
        // Skip if we cannot find an overload that takes (Pawn, bool) – signature changed.
        static bool Prepare()
        {
            var method = TargetMethod();
            if (method == null)
            {
                Log.Warning("[FogOfPawn FAIL] PawnBanishUtility.Banish(Pawn,bool) not found – impostor exile mood buff disabled.");
            }
            return method != null;
        }

        static System.Reflection.MethodBase TargetMethod()
        {
            // Prefer the (Pawn,bool) overload used by SleeperChoiceUtility; adjust if future versions add more params.
            return AccessTools.Method(typeof(PawnBanishUtility), "Banish", new[] { typeof(Pawn), typeof(bool) });
        }

        public static void Prefix(Pawn pawn)
        {
            if (pawn == null) return;
            FogUtility.GiveMoodBuffForImposterRemoval(pawn);
        }
    }
} 