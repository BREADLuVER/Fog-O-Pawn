using System.Collections.Generic;
using System.Linq;
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
        // Sentinels so we only log once per session.
        private static readonly HashSet<int> _loggedPawns = new();

        static Patch_TraitUI_DrawTraitRow_Swap()
        {
            var harmony = new Harmony("FogOfPawn.TraitUI.Swap");
            // In RW 1.6 the old RimWorld.TraitUI class was removed; that triggers the edge-case path below.
            // We simply fall back to masking via ITab_Pawn_Character.FillTab, so this is an expected scenario
            // rather than a hard failure.  Log with Reflect (dev-visible) instead of Fail.
            var traitUIType = AccessTools.TypeByName("RimWorld.TraitUI") ?? AccessTools.TypeByName("TraitUI");
            if (traitUIType == null)
            {
                FogLog.Reflect("TraitUITypeMissing", "TraitUI class missing (RW 1.6) – using fallback trait masking.");
                return;
            }

            var drawMethod = AccessTools.Method(traitUIType, "DrawTraitRow");
            if (drawMethod == null)
            {
                FogLog.Reflect("TraitUI.DrawTraitRowMissing", "DrawTraitRow method missing – using fallback trait masking.");
                return;
            }

            harmony.Patch(drawMethod,
                prefix: new HarmonyMethod(typeof(Patch_TraitUI_DrawTraitRow_Swap), nameof(Prefix)));

            FogLog.Reflect("TraitUI.DrawTraitRow.Patched", "TraitUI.DrawTraitRow patched for fog masking (swap mode).");
        }

        // Prefix now returns bool – false skips vanilla DrawTraitRow, effectively hiding the row.
        private static bool Prefix(Rect rect, Pawn pawn, Trait trait)
        {
            if (trait == null || pawn == null) return true;
            if (!FogSettingsCache.Current.fogTraits) return true;

            var comp = pawn.GetComp<CompPawnFog>();
            if (comp == null || !comp.compInitialized) return true;

            if (!comp.revealedTraits.Contains(trait.def))
            {
                // Hidden trait → skip drawing
                if (Prefs.DevMode && _loggedPawns.Add(pawn.thingIDNumber))
                {
                    FogLog.Verbose($"Hiding trait row for {pawn.LabelShort} (unrevealed trait {trait.def.defName})");
                }
                return false; // cancel original DrawTraitRow
            }

            return true; // draw normally
        }
    }
} 