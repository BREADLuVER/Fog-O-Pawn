using HarmonyLib;
using RimWorld;
using RimWorld.QuestGen;
using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace EventController_rQP
{
    [StaticConstructorOnStartup]
    public static class Init
    {
        static Init()
        {
            var harmony = new Harmony("EventController_rQP");
            Type eventWorker = typeof(EventController_Work);
            Dictionary<Tuple<string, PatchType>, MethodInfo> pairs = new Dictionary<Tuple<string, PatchType>, MethodInfo>()
            {
                //{ new Tuple<string, PatchType>("PawnGroupKindWorker_GeneratePawns",             PatchType.Both),       AccessTools.Method(typeof(PawnGroupKindWorker), nameof(PawnGroupKindWorker.GeneratePawns), new Type[] { typeof(PawnGroupMakerParms), typeof(PawnGroupMaker), typeof(bool) }) },
                { new Tuple<string, PatchType>("PawnGroupKindWorker_Trader_GeneratePawns",      PatchType.Both),       Tools.MethodTool(ParamValue.a, typeof(PawnGroupKindWorker_Trader), "GeneratePawns") },
                { new Tuple<string, PatchType>("PawnGroupKindWorker_GenerateTrader",            PatchType.Both),       Tools.MethodTool(ParamValue.a, typeof(PawnGroupKindWorker_Trader), "GenerateTrader")},
                { new Tuple<string, PatchType>("PawnGroupKindWorker_GenerateCarriers",          PatchType.Both),       Tools.MethodTool(ParamValue.a, typeof(PawnGroupKindWorker_Trader), "GenerateCarriers")},
                { new Tuple<string, PatchType>("PawnGroupKindWorker_GenerateGuards",            PatchType.Both),       Tools.MethodTool(ParamValue.a, typeof(PawnGroupKindWorker_Trader), "GenerateGuards")},
                { new Tuple<string, PatchType>("GenerateSkills",                                PatchType.Prefix),     Tools.MethodTool(ParamValue.b, typeof(PawnGenerator), "GenerateSkills")},
                { new Tuple<string, PatchType>("GenerateOrRedressPawnInternal",                 PatchType.Both),       Tools.MethodTool(ParamValue.b, typeof(PawnGenerator), "GenerateOrRedressPawnInternal")},
                { new Tuple<string, PatchType>("GiveAppropriateBioAndNameTo",                   PatchType.Both),       AccessTools.Method(typeof(PawnBioAndNameGenerator), nameof(PawnBioAndNameGenerator.GiveAppropriateBioAndNameTo))},
                { new Tuple<string, PatchType>("FillBackstorySlotShuffled",                     PatchType.Both),       AccessTools.Method(typeof(PawnBioAndNameGenerator), nameof(PawnBioAndNameGenerator.FillBackstorySlotShuffled))},
                { new Tuple<string, PatchType>("QuestNode_Root_RefugeePodCrash_GeneratePawn",   PatchType.Both),       AccessTools.Method(typeof(QuestNode_Root_RefugeePodCrash), nameof(QuestNode_Root_RefugeePodCrash.GeneratePawn))},
                { new Tuple<string, PatchType>("DamageUntilDowned",                             PatchType.Both),       AccessTools.Method(typeof(HealthUtility), nameof(HealthUtility.DamageUntilDowned))},
                { new Tuple<string, PatchType>("PreApplyDamage",                                PatchType.Both),       AccessTools.Method(typeof(Pawn_HealthTracker), nameof(Pawn_HealthTracker.PreApplyDamage))},
                { new Tuple<string, PatchType>("PreApplyDamageThing",                           PatchType.Both),       AccessTools.Method(typeof(Thing), nameof(Thing.PreApplyDamage))},
                { new Tuple<string, PatchType>("PreApplyDamageThingWithComps",                  PatchType.Both),       AccessTools.Method(typeof(ThingWithComps), nameof(ThingWithComps.PreApplyDamage))},
                { new Tuple<string, PatchType>("PreApplyDamagePawn",                            PatchType.Both),       AccessTools.Method(typeof(Pawn), nameof(Pawn.PreApplyDamage))},
                { new Tuple<string, PatchType>("AdjustXenotypeForFactionlessPawn",              PatchType.Prefix),     AccessTools.Method(typeof(PawnGenerator), nameof(PawnGenerator.AdjustXenotypeForFactionlessPawn))},
                { new Tuple<string, PatchType>("PawnGenerator_GeneratePawn",                    PatchType.Postfix),    AccessTools.Method(typeof(PawnGenerator), nameof(PawnGenerator.GeneratePawn), new Type[] {typeof(PawnGenerationRequest)})},
                { new Tuple<string, PatchType>("GetCreepjoinerSpecifics",                       PatchType.Transpiler), AccessTools.Method(typeof(CreepJoinerUtility), nameof(CreepJoinerUtility.GetCreepjoinerSpecifics))}
            };
            foreach (var item in pairs)
                try
                {
                    var variableName = item.Key.Item1;
                    var patches = item.Key.Item2;
                    item.Value.PatchTool(eventWorker, ref harmony, variableName, patches);
                }
                catch (Exception ex)
                { Log.Message("Patch Failed: " + $"*{item.Key.Item1}*".Colorize(UnityEngine.Color.blue) + "\n" + ex); }
            Log.Message("# Real Faction Guest Event Controller - Init Complete");
        }
    }
}
