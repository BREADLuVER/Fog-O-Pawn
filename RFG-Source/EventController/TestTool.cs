using RimWorld;
using RimWorld.QuestGen;
using System;
using Verse;

namespace EventController_rQP
{
    public class TestTool
    {
        public void TestTool_ForceCreepJoiner(ref PawnGenerationRequest request)
        {
            if (QuestGen_Get.GetMap() != null)
            {
                if (!request.KindDef.factionLeader)
                {
                    request.KindDef = DefDatabase<CreepJoinerFormKindDef>.AllDefs.RandomElement();
                    request.IsCreepJoiner = true;
                }
            }
        }
        public void TestTool_ForceRefugee(ref PawnGenerationRequest request)
        {
            request.KindDef = PawnKindDefOf.Refugee;
            request.Faction = null;
        }
        public void TestTool_ForceRabbie(ref PawnGenerationRequest request)
        {
            throw new NotImplementedException();
        }
        public void TestTool_ForceGenerateWithCertainFaction(ref PawnGenerationRequest request)
        {
            throw new NotImplementedException();
        }
        public void TestTool_ForceGenerateWithCertainApparel(ref PawnGenerationRequest request)
        {
            throw new NotImplementedException();
        }
    }
}
