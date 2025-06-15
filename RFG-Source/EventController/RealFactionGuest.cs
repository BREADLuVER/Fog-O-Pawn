using UnityEngine;
using Verse;

namespace EventController_rQP
{
    public class RealFactionGuest : Mod
    {
        /// <summary>
        /// A reference to our settings.
        /// </summary>
        public static RealFactionGuestSettings settings;
        //private Vector2 scrollPos = Vector2.zero;
        public static Rect lastRect = default;
        public static bool test = false;

        /// <summary>
        /// A mandatory constructor which resolves the reference to our settings.
        /// </summary>
        /// <param name="content"></param>
        public RealFactionGuest(ModContentPack content) : base(content)
        {
            settings = GetSettings<RealFactionGuestSettings>();
        }
        public static Rect RectB(Listing_Standard listingStandard)
        {
            lastRect = new Rect(listingStandard.GetRect(Text.LineHeight));
            return lastRect;
        }

        //public static Rect RectA(float x, float y, float width, float height)
        //{
        //    lastRect = new Rect(x, y, width, height);
        //    return lastRect;
        //}

        /// <summary>
        /// The (optional) GUI part to set your settings.
        /// </summary>
        /// <param name="inRect">A Unity Rect with the size of the settings window.</param>
        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            //Widgets.BeginScrollView(inRect, ref this.scrollPos, viewRect);
            listingStandard.Begin(inRect);
            Rect rectc = RectB(listingStandard);
            rectc.width = 150;
            rectc.x = listingStandard.ColumnWidth / 5 * 4;
            rectc.height = Text.LineHeight + 10;
            if (Widgets.ButtonText(rectc, "ResetAllSetting".Translate()))
            {
                RealFactionGuestSettings.ResetToDefault();
            }
            Rect rectd = RectB(listingStandard);
            rectc.width = 150;
            rectc.x = listingStandard.ColumnWidth / 5 * 1;
            rectc.height = Text.LineHeight + 10;
            Widgets.CheckboxLabeled(rectd, "RFGSetting_debugOption".Translate(), ref RealFactionGuestSettings.debugOption, false, null, null, true);

            listingStandard.Gap(Text.LineHeight + 10);
            if (ModsConfig.BiotechActive)
            {
                listingStandard.CheckboxLabeled("RFGSetting_dontAdjustXenotypeForRabbie".Translate(), ref RealFactionGuestSettings.dontAdjustXenotypeForRabbie, "RFGSetting_dontAdjustXenotypeForRabbie_Tips".Translate());
            }
            listingStandard.CheckboxLabeled("RFGSetting_StrictQuestGuest".Translate(), ref RealFactionGuestSettings.strictQuestGuest, "RFGSetting_StrictQuestGuest_Tips".Translate());
            listingStandard.CheckboxLabeled("RFGSetting_AlternateFactions".Translate(), ref RealFactionGuestSettings.alternativeFaction, "RFGSetting_AlternateFactions_Tips".Translate());

            //Rect rectb = new Rect(listingStandard.ColumnWidth - 225, Text.LineHeight + 30, 450, Text.LineHeight + 10);
            listingStandard.Label("RFGSetting_ToggleFrequency".Translate() + $"{RealFactionGuestSettings.strictChance}", -1, "RFGSetting_ToggleFrequency_Tips".Translate());
            RealFactionGuestSettings.strictChance = Mathf.RoundToInt(listingStandard.Slider(RealFactionGuestSettings.strictChance, 0f, 1f) * 100f) / 100f;
            listingStandard.Gap();
            if (RealFactionGuestSettings.debugOption)
            {
                listingStandard.CheckboxLabeled("RFGSetting_damageUntilDownedBypassShield".Translate(), ref RealFactionGuestSettings.damageUntilDownedBypassShield, "RFGSetting_damageUntilDownedBypassShield_Tips".Translate());
                listingStandard.CheckboxLabeled("RFGSetting_factionLeaderValidator".Translate(), ref RealFactionGuestSettings.factionLeaderValidator, "RFGSetting_factionLeaderValidator_Tips".Translate());
                listingStandard.CheckboxLabeled("RFGSetting_creepJoinerValidator".Translate(), ref RealFactionGuestSettings.creepJoinerValidator, "RFGSetting_creepJoinerValidator_Tips".Translate());
                if (RealFactionGuestSettings.creepJoinerValidator)
                {
                    listingStandard.CheckboxLabeled("RFGSetting_creepJoinerGenerateNoLimit".Translate(), ref RealFactionGuestSettings.creepJoinerGenerateNoLimit, "RFGSetting_creepJoinerGenerateNoLimit_Tips".Translate());
                }
            }
            listingStandard.End();
            //Widgets.EndScrollView();
            base.DoSettingsWindowContents(inRect);
        }

        /// <summary>
        /// Override SettingsCategory to show up in the list of settings.
        /// Using .Translate() is optional, but does allow for localisation.
        /// </summary>
        /// <returns>The (translated) mod name.</returns>
        public override string SettingsCategory()
        {
            return "RealFactionGuest".Translate();
        }
    }
}
