using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using System.Linq;

namespace FogOfPawn.Patches
{
    /// <summary>
    /// Fallback trait masking for UI mods or game versions where TraitUI.DrawTraitRow isn't used.
    /// We temporarily replace unrevealed traits with an Unknown placeholder before the tab draws,
    /// then restore the original list in a Postfix.
    /// </summary>
    [StaticConstructorOnStartup]
    public static class Patch_ITab_Pawn_Character_FillTab_Swap
    {
        private static readonly TraitDef UnknownTraitDef = new TraitDef
        {
            defName = "FogOfPawn_UnknownTraitRuntime",
            label = "FogOfPawn.UnknownTrait".Translate(),
            description = "FogOfPawn.UnknownTrait.Tooltip".Translate(),
            degreeDatas = Enumerable.Range(0,5).Select(i => new TraitDegreeData
            {
                degree = i - 2,
                label = "FogOfPawn.UnknownTrait".Translate(),
                description = "FogOfPawn.UnknownTrait.Tooltip".Translate()
            }).ToList()
        };

        private static readonly HashSet<int> _loggedPawns = new();

        static Patch_ITab_Pawn_Character_FillTab_Swap()
        {
            var harmony = new Harmony("FogOfPawn.ITabCharacter.Swap");
            var tabType = AccessTools.TypeByName("RimWorld.ITab_Pawn_Character");
            if (tabType == null)
            {
                FogLog.Fail("ITab_Pawn_Character", "Could not locate ITab_Pawn_Character – fallback trait masking disabled.");
                return;
            }

            var method = AccessTools.Method(tabType, "FillTab");
            if (method == null)
            {
                FogLog.Fail("ITab_Pawn_Character.FillTab", "Could not find FillTab – fallback trait masking disabled.");
                return;
            }

            harmony.Patch(method,
                prefix: new HarmonyMethod(typeof(Patch_ITab_Pawn_Character_FillTab_Swap), nameof(Prefix)),
                postfix: new HarmonyMethod(typeof(Patch_ITab_Pawn_Character_FillTab_Swap), nameof(Postfix)));

            FogLog.Reflect("ITab_Pawn_Character.FillTab.Patched", "Fallback trait masking via ITab_Pawn_Character.FillTab.");
        }

        private static void Prefix(RimWorld.ITab_Pawn_Character __instance, out List<(Trait trait, TraitDef originalDef, int originalDegree)> __state)
        {
            __state = null;

            var pawn = Traverse.Create(__instance).Property("SelPawn").GetValue<Pawn>();
            if (pawn == null) return;
            if (!FogSettingsCache.Current.fogTraits) return;

            var comp = pawn.GetComp<CompPawnFog>();
            if (comp == null || !comp.compInitialized) return;
            if (pawn.story?.traits == null) return;

            List<(Trait, TraitDef, int)> swapped = null;

            foreach (var tr in pawn.story.traits.allTraits)
            {
                if (!comp.revealedTraits.Contains(tr.def))
                {
                    swapped ??= new();
                    swapped.Add((tr, tr.def, tr.Degree));
                    tr.def    = UnknownTraitDef;
                    Traverse.Create(tr).Field("tipCachedInt").SetValue(null);
                }
            }

            if (swapped != null)
            {
                __state = swapped;

                if (Prefs.DevMode && _loggedPawns.Add(pawn.thingIDNumber))
                {
                    FogLog.Verbose($"Fallback mask applied for traits of {pawn.LabelShort}.");
                }
            }
        }

        private static void Postfix(List<(Trait trait, TraitDef originalDef, int originalDegree)> __state)
        {
            if (__state == null) return;
            foreach (var (trait, def, degree) in __state)
            {
                trait.def    = def;
                Traverse.Create(trait).Field("tipCachedInt").SetValue(null);
            }
        }
    }
} 