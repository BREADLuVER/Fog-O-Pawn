using RimWorld;
using UnityEngine;
using Verse;

namespace FogOfPawn.UI
{
    [StaticConstructorOnStartup]
    public static class CustomTraitUIDrawer
    {
        private static readonly Texture2D UnknownTraitIcon = ContentFinder<Texture2D>.Get("UI/Icons/QuestionMark");

        public static void DrawTraitRow(Rect rect, Trait trait, Pawn pawn)
        {
            var comp = pawn.GetComp<CompPawnFog>();
            if (comp == null || comp.revealedTraits.Contains(trait.def))
            {
                // Revealed or no comp, use vanilla drawer
                TraitUI.DrawTraitRow(rect, trait, pawn);
                return;
            }

            // Draw placeholder for unrevealed trait
            var labelRect = new Rect(rect.x, rect.y, rect.width, rect.height);
            var iconRect = new Rect(rect.x, rect.y, 24f, 24f);

            GUI.color = Color.gray;
            Widgets.DrawTextureFitted(iconRect, UnknownTraitIcon, 1f);
            Widgets.Label(labelRect, "   " + "FogOfPawn.UnknownTrait".Translate());
            GUI.color = Color.white;
            
            TooltipHandler.TipRegion(rect, "FogOfPawn.UnknownTrait.Tooltip".Translate());
        }
    }
} 