using Verse;
using UnityEngine;

namespace DiceFramework
{
    /// <summary>
    /// Simple animated dice roll window: cycles random numbers for a brief time before revealing the final results.
    /// Can display one or two dice blocks with optional labels.
    /// </summary>
    public class DiceRollWindow : Window
    {
        private readonly int _finalA;
        private readonly int _finalB;
        private readonly string _labelA;
        private readonly string _labelB;
        private readonly string _title;
        private readonly System.Action _onClose;

        private const float AnimateSeconds = 1.2f;
        private readonly float _startTime;
        private bool Settled => Time.realtimeSinceStartup - _startTime >= AnimateSeconds;

        public override Vector2 InitialSize => new Vector2(260f, 140f);

        public DiceRollWindow(string title, string labelA, int finalA, string labelB, int finalB, System.Action onClose = null)
        {
            _title = title;
            _labelA = labelA;
            _finalA = finalA;
            _labelB = labelB;
            _finalB = finalB;
            _onClose = onClose;
            _startTime = Time.realtimeSinceStartup;
            layer = WindowLayer.GameUI;
            doCloseX = false;
            doCloseButton = false;
            forcePause = true;
            absorbInputAroundWindow = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 30f), _title);
            Text.Font = GameFont.Small;

            float colWidth = inRect.width / 2f - 10f;
            Rect left = new Rect(inRect.x, inRect.y + 40f, colWidth, 60f);
            Rect right = new Rect(inRect.x + colWidth + 20f, inRect.y + 40f, colWidth, 60f);

            DrawDie(left, _labelA, _finalA, Settled);
            DrawDie(right, _labelB, _finalB, Settled);

            if (Settled)
            {
                if (Widgets.ButtonText(new Rect(inRect.x + inRect.width / 2f - 40f, inRect.y + inRect.height - 35f, 80f, 30f), "OK"))
                {
                    Close();
                }
            }
        }

        private void DrawDie(Rect rect, string label, int final, bool settled)
        {
            Widgets.DrawBoxSolid(rect, new Color(0.2f, 0.2f, 0.2f, 0.3f));
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(new Rect(rect.x, rect.y, rect.width, 20f), label);
            int value = settled ? final : Rand.RangeInclusive(1, 20);
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(rect.x, rect.y + 20f, rect.width, 40f), value.ToString());
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        public override void PostClose()
        {
            base.PostClose();
            _onClose?.Invoke();
        }
    }
} 