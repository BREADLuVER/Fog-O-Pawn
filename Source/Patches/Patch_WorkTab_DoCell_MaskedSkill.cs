using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Verse;
using RimWorld;
using System.Reflection.Emit;

namespace FogOfPawn.Patches
{
    /// <summary>
    /// Transpiler that forces the Work tab to display only the masked average skill (no capacity multipliers).
    /// This runs only when WorkTabFallbackMask is false (default). If another mod also transpiles the same
    /// method users can switch to the fallback in mod settings.
    /// </summary>
    [StaticConstructorOnStartup]
    public static class Patch_WorkTab_DoCell_MaskedSkill
    {
        private static readonly MethodInfo _alwaysOneMI = AccessTools.Method(typeof(Patch_WorkTab_DoCell_MaskedSkill), nameof(AlwaysOne));

        static Patch_WorkTab_DoCell_MaskedSkill()
        {
            if (FogOfPawnMod.Settings?.workTabFallbackMask == true)
                return; // user switched to safer mode

            try
            {
                var harmony = new Harmony("FogOfPawn.WorkTabTranspiler");
                Type workerType = AccessTools.TypeByName("RimWorld.PawnColumnWorker_WorkPriority") ??
                                   AccessTools.TypeByName("PawnColumnWorker_WorkPriority");
                if (workerType == null)
                {
                    FogLog.Fail("WorkPriorityType", "Could not locate PawnColumnWorker_WorkPriority – Work tab transpiler disabled.");
                    return;
                }

                var doCell = workerType.GetMethod("DoCell", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (doCell == null)
                {
                    FogLog.Fail("DoCell", "Could not locate DoCell on WorkPriority – transpiler disabled.");
                    return;
                }

                harmony.Patch(doCell, transpiler: new HarmonyMethod(typeof(Patch_WorkTab_DoCell_MaskedSkill), nameof(Transpiler)));
                FogLog.Reflect("WorkTabTranspilerPatched", "Work tab DoCell transpiler applied.");
            }
            catch (Exception ex)
            {
                Log.Error("[FogOfPawn] Exception applying WorkTab DoCell transpiler: " + ex);
            }
        }

        // Returns constant 1 to neutralise capacity multipliers.
        public static float AlwaysOne(Pawn pawn, PawnCapacityDef def) => 1f;

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (var instr in instructions)
            {
                if (instr.opcode == OpCodes.Call || instr.opcode == OpCodes.Callvirt)
                {
                    var mi = instr.operand as MethodInfo;
                    if (mi != null && mi.DeclaringType?.Name == "PawnCapacityUtility")
                    {
                        // Replace any call to capacity level functions with AlwaysOne
                        instr.operand = _alwaysOneMI;
                    }
                }
                yield return instr;
            }
        }
    }
} 