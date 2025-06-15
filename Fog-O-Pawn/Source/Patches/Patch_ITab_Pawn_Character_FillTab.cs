using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;
using FogOfPawn.UI;

namespace FogOfPawn.Patches
{
    [HarmonyPatch(typeof(ITab_Pawn_Character), "FillTab")]
    public static class Patch_ITab_Pawn_Character_FillTab
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var drawTraitRowInfo = AccessTools.Method(typeof(TraitUI), nameof(TraitUI.DrawTraitRow));
            var customDrawTraitRowInfo = AccessTools.Method(typeof(CustomTraitUIDrawer), nameof(CustomTraitUIDrawer.DrawTraitRow));

            foreach (var instruction in instructions)
            {
                if (instruction.Calls(drawTraitRowInfo))
                {
                    yield return new CodeInstruction(OpCodes.Call, customDrawTraitRowInfo);
                }
                else
                {
                    yield return instruction;
                }
            }
        }
    }
} 