using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace EventController_rQP
{
    public static class MethodReplacerTools
    {
        public static void LoacateStartEnd(this List<CodeInstruction> codes, MethodInfo method, OpCode start, OpCode end, out int startLine, out int endLine)
        {
            var codelineMethod = 0;
            for (int i = 1; i < codes.Count; i++)
            {
                if (codes[i].Calls(method))
                {
                    codelineMethod = i;
                    break;
                }
            }
            if (codelineMethod == 0)
            {
                startLine = -1;
                endLine = -1;
                return;
            }
            startLine = 0;
            endLine = 0;
            for (int i = codelineMethod; i >= 0; i--)
            {
                if (codes[i].opcode == start)
                {
                    startLine = i;
                    break;
                }
            }
            for (int i = codelineMethod; i < codes.Count; i++)
            {
                if (codes[i].opcode == end)
                {
                    endLine = i;
                    break;
                }
            }
        }
        public static List<CodeInstruction> Head(this List<CodeInstruction> codes, int start)
        {
            List<CodeInstruction> head = new List<CodeInstruction>();
            for (int i = 0; i < start; i++)
            {
                head.Add(codes[i]);
            }
            return head;
        }
        public static List<CodeInstruction> Body(this List<CodeInstruction> replacer)
        {
            return replacer;
        }
        public static List<CodeInstruction> Tail(this List<CodeInstruction> codes, int endLine)
        {
            List<CodeInstruction> tail = new List<CodeInstruction>();
            for (int i = endLine + 1; i < codes.Count; i++)
            {
                tail.Add(codes[i]);
            }
            return tail;
        }
        public static List<CodeInstruction> Tail(this List<CodeInstruction> codes, int endLine, ref ILGenerator iLGenerator, Dictionary<Label, List<int>> labelsCodesIndex)
        {
            List<CodeInstruction> tail = new List<CodeInstruction>();
            List<Label> record = new List<Label>();
            for (int i = endLine + 1; i < codes.Count; i++)
            {
                if (codes[i].operand is Label l1)
                {
                    var label = iLGenerator.DefineLabel();
                    if (!record.Contains(l1))
                    {
                        for (int j = 0; j < labelsCodesIndex[l1].Count; j++)
                        {
                            var index = labelsCodesIndex[l1][j];
                            var labels = codes[index].labels;
                            for (int k = 0; k < labels.Count; k++)
                            {
                                labels[k] = label;
                                codes[index].labels = labels;
                            }
                        }
                    }
                    codes[i].operand = label;
                    tail.Add(codes[i]);
                    continue;
                }
                if (codes[i].operand is Label[] l2)
                {
                    var count = l2.Count();
                    for (int j = 0; j < count; j++)
                    {
                        var oringinal = l2[j];
                        var label = iLGenerator.DefineLabel();
                        if (!record.Contains(label))
                        {
                            for (int k = 0; k < labelsCodesIndex[l2[j]].Count; k++)
                            {
                                var index = labelsCodesIndex[l2[j]][k];
                                var labels = codes[index].labels;
                                for (int l = 0; l < labels.Count; l++)
                                {
                                    labels[l] = label;
                                    codes[index].labels = labels;
                                }
                            }
                        }
                        l2[j] = label;
                    }
                    codes[i].operand = l2;
                    tail.Add(codes[i]);
                    continue;
                }
                tail.Add(codes[i]);
                //il.Emit(OpCodes.Nop);
            }
            return tail;
        }
        public static void GetOpcodesLabelDictionary(this List<CodeInstruction> codes, /* out Dictionary<Label, List<int>> codesLabelsIndex,*/ out Dictionary<Label, List<int>> labelsCodesIndex)
        {
            //codesLabelsIndex = new Dictionary<Label, List<int>>();
            labelsCodesIndex = new Dictionary<Label, List<int>>();
            for (int i = 0; i < codes.Count; i++)
            {
                //if (codes[i].operand is Label l)
                //{
                //    if (codesLabelsIndex.Keys.Contains(l))
                //    {
                //        codesLabelsIndex[l].Add(i);
                //    }
                //    else
                //    {
                //        codesLabelsIndex.Add(l, new List<int>() { i });
                //    }
                //    continue;
                //}
                if (codes[i].labels != null)
                {
                    for (int j = 0; j < codes[i].labels.Count; j++)
                    {
                        var label = codes[i].labels[j];
                        if (labelsCodesIndex.Keys.Contains(label))
                        {
                            labelsCodesIndex[label].Add(i);
                        }
                        else
                        {
                            labelsCodesIndex.Add(label, new List<int>() { i });
                        }
                    }
                }
            }
        }
    }
}
