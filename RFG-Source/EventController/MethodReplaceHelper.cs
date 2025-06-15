using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Verse;

namespace EventController_rQP
{
    public class MethodReplaceHelper
    {
        public MethodReplaceHelper()
        {
            this.dynamicMethod = new DynamicMethod("temp", typeof(void), null);
        }
        public readonly DynamicMethod dynamicMethod;
        private MethodInfo methodInfo { get; set; }
        private OpCode opCodeStart { get; set; }
        private OpCode opCodeEnd { get; set; }
        private List<CodeInstruction> codes { get; set; }
        private List<CodeInstruction> replacer { get; set; }
        private bool ready { get; set; }
        private bool simple { get; set; }
        public List<CodeInstruction> Run()
        {
            if (!ready)
            {
                return null;
            }
            var ILGenerator = dynamicMethod.GetILGenerator();
            ILGenerator.Emit(OpCodes.Ret);
            codes.LoacateStartEnd(methodInfo, opCodeStart, opCodeEnd, out int startLine, out int endLine);
            var head = codes.Head(startLine);
            var body = replacer.Body();
            List<CodeInstruction> tail = [];
            if (simple)
            {
                tail = codes.Tail(endLine);
            }
            else
            {
                codes.GetOpcodesLabelDictionary(out Dictionary<Label, List<int>> labelsCodesIndex);
                tail = codes.Tail(endLine, ref ILGenerator, labelsCodesIndex);
            }
            if (tail.Empty() || tail.Last().opcode != OpCodes.Ret)
            {
                return codes;
            }
            List<CodeInstruction> codes1 = new List<CodeInstruction>();
            codes1.AddRange(head);
            codes1.AddRange(body);
            codes1.AddRange(tail);
            return codes1;
        }
        public void SetAllNeededProperties(MethodInfo methodInfo, OpCode opCodeStart, OpCode opCodeEnd, List<CodeInstruction> Codes, List<CodeInstruction> Replacer, bool simple)
        {
            this.methodInfo = methodInfo;
            this.opCodeStart = opCodeStart;
            this.opCodeEnd = opCodeEnd;
            this.codes = Codes;
            this.replacer = Replacer;
            this.simple = simple;
            this.ready = true;
        }
    }
}
