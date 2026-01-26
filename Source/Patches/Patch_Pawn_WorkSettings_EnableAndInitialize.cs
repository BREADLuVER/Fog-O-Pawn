using HarmonyLib;
using RimWorld;
using Verse;
using FogOfPawn;

namespace FogOfPawn.Patches
{
    [HarmonyPatch(typeof(Pawn_WorkSettings), "EnableAndInitialize")]
    public static class Patch_Pawn_WorkSettings_EnableAndInitialize
    {
        public static void Prefix()
        {
            RenderContext.BeginRender();
        }

        public static void Finalizer()
        {
            RenderContext.EndRender();
        }
    }
}
