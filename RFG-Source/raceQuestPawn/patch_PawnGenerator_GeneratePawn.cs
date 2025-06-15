using EventController_rQP;
using HarmonyLib;
using RimWorld;
using Verse;

namespace raceQuestPawn;

[HarmonyPatch(typeof(PawnGenerator), nameof(PawnGenerator.GeneratePawn), typeof(PawnGenerationRequest))]
public class patch_PawnGenerator_GeneratePawn
{
    [HarmonyPriority(1000)]
    public static void Prefix(ref PawnGenerationRequest request)
    {
        try
        {
            if (Current.ProgramState != ProgramState.Playing)
            {
                return;
            }

            if (request.Faction == null)
            {
                return;
            }

            if (request.KindDef.RaceProps != null && (
                    request.KindDef.RaceProps.Animal
                    || request.KindDef.RaceProps.intelligence <= Intelligence.ToolUser
                ))
            {
                return;
            }

            if (request.Faction is { IsPlayer: true })
            {
                return;
            }

            if (request.KindDef == PawnKindDefOf.WildMan
                || request.KindDef.trader)
            {
                return;
            }

            if (request.IsCreepJoiner)
            {
                return;
            }

            if ((EventController_Work.ongoingEvents & OngoingEvent.TraderGroup) != 0)
            {
                return;
            }

            if (PawnValidator_CrossWork.IsNotFromVanilla())
            {
                return;
            }

            //new TestTool().TestTool_ForceRabbie(ref request);
            //Log.Message($"request : {(request.Faction != null ? request.Faction.def.defName : "none")}, {(request.KindDef != null ? request.KindDef.defName : "none")}");

            var faction = request.Faction.def;
            var kinddef = request.KindDef;

            bool default_filter = faction.modContentPack.PackageId != kinddef.modContentPack.PackageId;
            if (RealFactionGuestSettings.alternativeFaction && default_filter)
            {
                var factionpawnraces = EventController_Work.GetFactionPawnRaces();

                if (factionpawnraces.ContainsKey(faction))
                {
                    default_filter = factionpawnraces[faction].Contains(kinddef.race);
                }
            }
            bool chance = Rand.Chance(RealFactionGuestSettings.strictChance);
            bool strict = chance && default_filter;
            if (strict
                && (request.Faction?.def.modContentPack != null
                && (!request.Faction.def.modContentPack.PackageId.StartsWith("ludeon")
                || request.Faction.def.modContentPack.PackageId.EndsWith("rimworld.biotech")))
               )
            {
                // 팩션이 있을때
                float combatPower = kinddef.combatPower;
                PawnKindDef p_make = null;

                if (faction.pawnGroupMakers != null)
                {
                    p_make = ChoosePawn.ChoosePawnKind(faction.pawnGroupMakers, combatPower, true);
                }

                if (p_make != null)
                {
                    request.KindDef = p_make;
                }
                //Log.Message($"A : {request.KindDef}");
                //Log.Message($"A : {p_make.defName} : {p_make.combatPower}");

                return;
            }
        }
        catch
        {
            // ignored
        }
        return;
    }
}