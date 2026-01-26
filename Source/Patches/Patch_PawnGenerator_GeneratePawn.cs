using HarmonyLib;
using RimWorld;
using Verse;
using FogOfPawn;

namespace FogOfPawn.Patches
{
    [HarmonyPatch(typeof(PawnGenerator), "GeneratePawn", new[] { typeof(PawnGenerationRequest) })]
    public static class Patch_PawnGenerator_GeneratePawn
    {
        public static void Postfix(Pawn __result, PawnGenerationRequest request)
        {
            if (__result == null)
            {
                return;
            }

            if (__result.RaceProps == null)
            {
                FogLog.Verbose($"Skipping fog for {__result?.LabelShort ?? "null pawn"} - RaceProps is null");
                return;
            }

            bool isSlave = __result.guest != null && __result.guest.GuestStatus == GuestStatus.Slave;

            if ((__result.Faction?.IsPlayer ?? false) ||
                request.Context == PawnGenerationContext.PlayerStarter ||
                __result.RaceProps?.Animal == true ||
                __result.RaceProps?.IsMechanoid == true ||
                __result.skills == null ||
                __result.mindState == null ||
                request.AllowDowned ||
                isSlave)
            {
                FogLog.Verbose($"Skipping fog for {__result.NameShortColored} (Context: {request.Context}, IsSlave: {isSlave}, Animal: {__result.RaceProps?.Animal}, Mech: {__result.RaceProps?.IsMechanoid})");
                var comp = __result.GetComp<CompPawnFog>();
                if (comp != null)
                {
                    comp.compInitialized = true;
                    comp.fullyRevealed  = true;
                    if (__result.skills?.skills != null)
                    {
                        foreach (var sk in __result.skills.skills)
                        {
                            if (sk?.def != null)
                                comp.revealedSkills.Add(sk.def);
                        }
                    }
                    if (__result.story?.traits?.allTraits != null)
                    {
                        foreach (var tr in __result.story.traits.allTraits)
                        {
                            if (tr?.def != null)
                                comp.revealedTraits.Add(tr.def);
                        }
                    }
                }
                return;
            }

            try
            {
                FogLog.Verbose($"Applying fog for newly generated pawn: {__result.NameShortColored}");

                FogInitializer.InitializeFogFor(__result, request);

                if (__result.Faction != null && __result.Faction.IsPlayer && __result.workSettings != null)
                {
                    __result.workSettings.EnableAndInitialize();
                }
            }
            catch (System.Exception ex)
            {
                Log.Error($"[FogOfPawn] Exception while initializing fog for {__result?.LabelShort ?? "null pawn"}: {ex}");
                
                var comp = __result?.GetComp<CompPawnFog>();
                if (comp != null)
                {
                    comp.compInitialized = true;
                    comp.fullyRevealed = true;
                }
            }
        }
    }
} 