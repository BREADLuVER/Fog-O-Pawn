using HarmonyLib;
using RimWorld;
using Verse;

namespace FogOfPawn.Patches
{
    [HarmonyPatch(typeof(MainMenuDrawer), "MainMenuOnGUI")]
    public static class Patch_MainMenuDrawer_MainMenuOnGUI
    {
        public static void Postfix()
        {
            Log.Message("[FogOfPawn] Hello World from MainMenuDrawer.MainMenuOnGUI Postfix!");
        }
    }
} 