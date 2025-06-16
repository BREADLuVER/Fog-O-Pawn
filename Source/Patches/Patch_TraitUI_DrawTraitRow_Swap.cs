using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using UnityEngine;

namespace FogOfPawn.Patches
{
    /// <summary>
    /// Masks unrevealed traits at UI draw-time by temporarily swapping their TraitDef
    /// to a dummy "Unknown" placeholder. After the vanilla draw call finishes we
    /// restore the original def/degree so game logic remains untouched.
    /// </summary>
    [StaticConstructorOnStartup]
    public static class Patch_TraitUI_DrawTraitRow_Swap
    {
        // Lazy-constructed placeholder used during masking.
        private static readonly TraitDef UnknownTraitDef;

        // Sentinels so we only log once per session.
        private static readonly HashSet<string> _loggedPawns = new();

        static Patch_TraitUI_DrawTraitRow_Swap()
        {
            // Create lightweight placeholder TraitDef (not registered in DefDatabase; UI-only).
            UnknownTraitDef = new TraitDef
            {
                defName    = "FogOfPawn_UnknownTraitRuntime",   // runtime only
                label      = "FogOfPawn.UnknownTrait".Translate(),
                description= "FogOfPawn.UnknownTrait.Tooltip".Translate(),
                degreeDatas= new List<TraitDegreeData>
                {
                    new TraitDegreeData
                    {
                        degree      = 0,
                        label       = "FogOfPawn.UnknownTrait".Translate(),
                        description = "FogOfPawn.UnknownTrait.Tooltip".Translate()
                    }
                }
            };

            var harmony = new Harmony("FogOfPawn.TraitUI.Swap");
            var traitUIType = AccessTools.TypeByName("RimWorld.TraitUI") ?? AccessTools.TypeByName("TraitUI");
            if (traitUIType == null)
            {
                FogLog.Fail("TraitUIType", "Could not locate TraitUI type – trait fogging disabled.");
                return;
            }

            var drawMethod = AccessTools.Method(traitUIType, "DrawTraitRow");
            if (drawMethod == null)
            {
                FogLog.Fail("TraitUI.DrawTraitRow", "Could not locate TraitUI.DrawTraitRow – trait fogging disabled.");
                return;
            }

            harmony.Patch(drawMethod,
                prefix: new HarmonyMethod(typeof(Patch_TraitUI_DrawTraitRow_Swap), nameof(Prefix)),
                postfix: new HarmonyMethod(typeof(Patch_TraitUI_DrawTraitRow_Swap), nameof(Postfix)));

            FogLog.Reflect("TraitUI.DrawTraitRow.Patched", "TraitUI.DrawTraitRow patched for fog masking (swap mode).");
        }

        /// <summary>
        /// Swap unrevealed traits to the placeholder just for the duration of the row draw.
        /// We accept generic parameter order; Harmony will match by type.
        /// </summary>
        private static void Prefix(Rect rect, Pawn pawn, Trait trait, out (TraitDef, int)? __state)
        {
            __state = null;

            if (trait == null || pawn == null) return;
            if (!FogSettingsCache.Current.fogTraits) return;

            var comp = pawn.GetComp<CompPawnFog>();
            if (comp == null || !comp.compInitialized) return;
            if (comp.revealedTraits.Contains(trait.def)) return; // already revealed

            // Store original and swap.
            __state = (trait.def, trait.degree);
            trait.def    = UnknownTraitDef;
            trait.degree = 0;

            if (Prefs.DevMode && _loggedPawns.Add(pawn.thingIDNumber))
            {
                FogLog.Verbose($"Masking traits for {pawn.LabelShort} (draw-time swap mode)");
            }
        }

        /// <summary>
        /// Restore original TraitDef/degree after vanilla row rendering.
        /// </summary>
        private static void Postfix(Trait trait, (TraitDef, int)? __state)
        {
            if (__state == null) return;

            trait.def    = __state.Value.Item1;
            trait.degree = __state.Value.Item2;
        }
    }
} 