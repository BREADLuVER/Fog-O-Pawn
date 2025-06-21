using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using UnityEngine;
using Verse.AI;
using System.Linq;
using System.Reflection;

namespace FogOfPawn.Patches
{
    [HarmonyPatch]
    public static class Patch_FloatMenu_Interrogate
    {
        static bool Prepare()
        {
            var target = TargetMethod();
            if (target == null)
            {
                Log.Warning("[FogOfPawn FAIL] Could not find target method FloatMenuMakerMap.ChoicesAtFor. Interrogate option disabled.");
            }
            return target != null;
        }

        static System.Reflection.MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(FloatMenuMakerMap), "ChoicesAtFor");
        }

        // Signature: List<FloatMenuOption> ChoicesAtFor(Vector3 clickPos, Pawn pawn, bool suppressAutoTakeableGoto)
        public static void Postfix(Vector3 clickPos, Pawn pawn, ref List<FloatMenuOption> __result)
        {
            try
            {
                if (__result == null || pawn == null || !pawn.IsColonistPlayerControlled)
                    return;

                IntVec3 cell = IntVec3.FromVector3(clickPos);
                Map map = pawn.Map;
                if (map == null) return;

                Pawn target = cell.GetThingList(map).OfType<Pawn>().FirstOrDefault(p => p != pawn);
                if (target == null) return;

                FogLog.Verbose($"[InterrogatePatch] Adding option; pawn={pawn.LabelShort}, opts count before={__result.Count}");

                string label = $"Interrogate {target.LabelShort}";
                __result.Add(new FloatMenuOption(label, () =>
                {
                    try
                    {
                        Job job = JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("Fog_InterrogatePawn"), target);
                        pawn.jobs.TryTakeOrderedJob(job);
                    }
                    catch (System.Exception ex)
                    {
                        Log.Warning("[FogOfPawn ERROR] Exception starting Interrogate job: " + ex);
                    }
                }));
            }
            catch (System.Exception ex)
            {
                Log.Warning("[FogOfPawn ERROR] Interrogate postfix failed: " + ex);
            }
        }
    }
} 