using System;
using UnityEngine;
using Verse;
using RimWorld;

namespace FogOfPawn.UI
{
    public class Dialog_Accuse : Window
    {
        private string text;
        private string title;
        private Pawn pawn;
        private Action confirmAction;

        public override Vector2 InitialSize => new Vector2(500f, 300f);

        public Dialog_Accuse(string text, string title, Pawn pawn, Action confirmAction)
        {
            this.text = text;
            this.title = title;
            this.pawn = pawn;
            this.confirmAction = confirmAction;
            this.forcePause = true;
            this.absorbInputAroundWindow = true;
            this.closeOnAccept = true;
            this.closeOnCancel = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, 0f, inRect.width, 42f), title);
            Text.Font = GameFont.Small;

            Rect textRect = new Rect(0f, 42f, inRect.width, inRect.height - 42f - 40f);
            Widgets.Label(textRect, text);

            Rect confirmRect = new Rect(0f, inRect.height - 35f, inRect.width / 2f - 10f, 35f);
            if (Widgets.ButtonText(confirmRect, "FogOfPawn.Accuse.Dialog.Confirm".Translate()))
            {
                confirmAction?.Invoke();
                Close();
            }
            TooltipHandler.TipRegion(confirmRect, "FogOfPawn.Accuse.Tooltip.Confirm".Translate());

            Rect cancelRect = new Rect(inRect.width / 2f + 10f, inRect.height - 35f, inRect.width / 2f - 10f, 35f);
            if (Widgets.ButtonText(cancelRect, "FogOfPawn.Accuse.Dialog.Cancel".Translate()))
            {
                Close();
            }
            TooltipHandler.TipRegion(cancelRect, "FogOfPawn.Accuse.Tooltip.Cancel".Translate());
        }
    }
}
