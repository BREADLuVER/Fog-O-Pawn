using HarmonyLib;
using RimWorld;
using Verse;

namespace FogOfPawn.Patches
{
    [HarmonyPatch]
    public static class Patch_Pawn_Kill
    {
        static bool Prepare()
        {
            var method = TargetMethod();
            if (method == null)
            {
                Log.Warning("[FogOfPawn FAIL] Pawn.Kill(DamageInfo?, Hediff) not found – impostor death mood buff disabled.");
            }
            return method != null;
        }

        static System.Reflection.MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(Pawn), "Kill");
        }

        public static void Prefix(Pawn __instance)
        {
            if (__instance == null) return;
            FogUtility.GiveMoodBuffForImposterRemoval(__instance);
        }
    }
} 