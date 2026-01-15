using HarmonyLib;
using RimWorld;
using Verse;
using FogOfPawn;

namespace FogOfPawn.Patches
{
    /// <summary>
    /// Patches Pawn_WorkSettings.EnableAndInitialize to ensure that when a pawn joins
    /// or is initialized, their default work priorities are set based on their MASKED
    /// skills rather than their real skills.
    /// This fixes the issue where an Imposter with a fake high skill/passion would have that work type disabled/unticked by default.
    /// </summary>
    [HarmonyPatch(typeof(Pawn_WorkSettings), "EnableAndInitialize")]
    public static class Patch_Pawn_WorkSettings_EnableAndInitialize
    {
        public static void Prefix()
        {
            // Force RenderContext (masking) to be active during initialization logic.
            // Even though this is "game logic" (setting priorities), it's "setup logic" that
            // should reflect the persona the pawn is presenting.
            RenderContext.BeginRender();
        }

        public static void Finalizer()
        {
            RenderContext.EndRender();
        }
    }
}
