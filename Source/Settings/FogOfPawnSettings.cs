using Verse;
using UnityEngine;

namespace FogOfPawn
{
    public class FogOfPawnSettings : ModSettings
    {
        public float deceptionIntensity = 0.5f; 
        public int xpToReveal = 1000;
        public bool fogSkills = true;
        public bool fogTraits = true;
        public bool verboseLogging = false;

        public int socialRevealPct = 5; 
        public float passiveRevealDays = 6f; 

        public bool allowSocialSkillReveal = true;
        public bool allowSocialTraitReveal = true;
        public bool allowPassiveSkillReveal = true;
        public bool allowPassiveTraitReveal = true;

        public int maxAlteredSkills = 3;
        public bool allowUnderstate = true;
        public bool deceiverJoinersOnly = false;
        
        public bool limitDeceiversToColonists
        {
            get => deceiverJoinersOnly;
            set => deceiverJoinersOnly = value;
        }

        public float traitHideChance = 0.3f; 

        public int sleeperCombatXp = 5000;
        public int imposterSkillXp = 4000;
        public float passiveDailyRevealPct = 1f; 
        public int disguiseKitWealth = 2000;

        public int imposterHighSkills = 3; 
        public int imposterMidSkills = 3; 
        public bool spawnDisguiseKitOnReveal = false;

        public int pctTruthful = 65;
        public int pctSlight = 30;
        public int pctDeceiver = 5;

        public int alteredSkillRange = 6;

        public int slightSkillXp = 1000;

        public bool biasBadTraitHiding = false; 

        public bool scoreBasedLiarChance = false; 

        public int positiveMoodRevealPct = 5; 
        public int positiveMoodThresholdPct = 70; 

        public bool applySkillPenalties = true;
        public float skillPenaltyPct = 5f; 

        public int moodBreakBasePct = 50; 
        public float moodBreakPerDayPct = 2f; 

        public float lateJoinerChancePct = 1.5f;

        public float prisonerRevealChancePct = 1.5f; 
        public float prisonerCrackChancePct = 2f; 

        private const int MinXp = 1000;
        private const int MaxXp = 5000;

        private bool _sectionSpawnOpen = true;
        private bool _sectionGeneralOpen = true;
        private bool _sectionTogglesOpen = false;
        private bool _sectionAmbientOpen = false;
        private bool _sectionAdvancedOpen = false;
        private bool _sectionMoodOpen = false;
        private bool _sectionFullRevealOpen = false;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref deceptionIntensity, "deceptionIntensity", 0.5f);
            Scribe_Values.Look(ref xpToReveal, "xpToReveal", 1000);
            Scribe_Values.Look(ref fogSkills, "fogSkills", true);
            Scribe_Values.Look(ref fogTraits, "fogTraits", true);
            Scribe_Values.Look(ref verboseLogging, "verboseLogging", false);

            Scribe_Values.Look(ref socialRevealPct, "socialRevealPct", 5);
            Scribe_Values.Look(ref passiveRevealDays, "passiveRevealDays", 6f);

            Scribe_Values.Look(ref allowSocialSkillReveal, "allowSocialSkillReveal", true);
            Scribe_Values.Look(ref allowSocialTraitReveal, "allowSocialTraitReveal", true);
            Scribe_Values.Look(ref allowPassiveSkillReveal, "allowPassiveSkillReveal", true);
            Scribe_Values.Look(ref allowPassiveTraitReveal, "allowPassiveTraitReveal", true);

            Scribe_Values.Look(ref maxAlteredSkills, "maxAlteredSkills", 3);
            Scribe_Values.Look(ref allowUnderstate, "allowUnderstate", true);
            Scribe_Values.Look(ref deceiverJoinersOnly, "deceiverJoinersOnly", true);

            Scribe_Values.Look(ref traitHideChance, "traitHideChance", 0.3f);

            Scribe_Values.Look(ref sleeperCombatXp, "sleeperCombatXp", 5000);
            Scribe_Values.Look(ref imposterSkillXp, "imposterSkillXp", 4000);
            Scribe_Values.Look(ref passiveDailyRevealPct, "passiveDailyRevealPct", 1f);
            Scribe_Values.Look(ref disguiseKitWealth, "disguiseKitWealth", 2000);

            Scribe_Values.Look(ref imposterHighSkills, "imposterHighSkills", 3);
            Scribe_Values.Look(ref imposterMidSkills, "imposterMidSkills", 3);
            Scribe_Values.Look(ref spawnDisguiseKitOnReveal, "spawnDisguiseKitOnReveal", true);

            Scribe_Values.Look(ref pctTruthful, "pctTruthful", 65);
            Scribe_Values.Look(ref pctSlight, "pctSlight", 30);
            Scribe_Values.Look(ref pctDeceiver, "pctDeceiver", 5);

            Scribe_Values.Look(ref alteredSkillRange, "alteredSkillRange", 6);

            Scribe_Values.Look(ref slightSkillXp, "slightSkillXp", 2000);

            Scribe_Values.Look(ref biasBadTraitHiding, "biasBadTraitHiding", false);
            Scribe_Values.Look(ref scoreBasedLiarChance, "scoreBasedLiarChance", false);

            Scribe_Values.Look(ref positiveMoodRevealPct, "positiveMoodRevealPct", 5);
            Scribe_Values.Look(ref positiveMoodThresholdPct, "positiveMoodThresholdPct", 70);
            
            Scribe_Values.Look(ref applySkillPenalties, "applySkillPenalties", true);
            Scribe_Values.Look(ref skillPenaltyPct, "skillPenaltyPct", 5f);

            Scribe_Values.Look(ref moodBreakBasePct, "moodBreakBasePct", 50);
            Scribe_Values.Look(ref moodBreakPerDayPct, "moodBreakPerDayPct", 2f);

            Scribe_Values.Look(ref lateJoinerChancePct, "lateJoinerChancePct", 1.5f);

            Scribe_Values.Look(ref prisonerRevealChancePct, "prisonerRevealChancePct", 1.5f);
            Scribe_Values.Look(ref prisonerCrackChancePct, "prisonerCrackChancePct", 2f);
        }

        public void DoWindowContents(Rect inRect)
        {
            float viewHeight = CalculateViewHeight();
            Rect viewRect = new Rect(0, 0, inRect.width - 16f, viewHeight);
            Widgets.BeginScrollView(inRect, ref _scrollPos, viewRect);

            var list = new Listing_Standard();
            list.Begin(viewRect);

            DrawSectionHeader(list, "FogOfPawn.Settings.Section.SpawnComposition".Translate(), ref _sectionSpawnOpen);
            if (_sectionSpawnOpen)
            {
                list.Gap(4f);
                
                list.CheckboxLabeled("FogOfPawn.Settings.DeceiverJoinerOnly".Translate(), ref deceiverJoinersOnly, "FogOfPawn.Settings.DeceiverJoinerOnly_Tooltip".Translate());

                list.Gap(8f);

                list.Label("FogOfPawn.Settings.Truthful".Translate() + ": " + pctTruthful + "%");
                pctTruthful = Mathf.Clamp((int)list.Slider(pctTruthful, 0, 100), 0, 100);

                int remaining = 100 - pctTruthful;
                if (pctSlight > remaining) pctSlight = remaining;
                list.Label("FogOfPawn.Settings.Slight".Translate() + ": " + pctSlight + "%");
                pctSlight = Mathf.Clamp((int)list.Slider(pctSlight, 0, remaining), 0, remaining);

                pctDeceiver = 100 - pctTruthful - pctSlight;
                list.Label("FogOfPawn.Settings.Deceiver".Translate() + ": " + pctDeceiver + "%  " + "FogOfPawn.Settings.Computed".Translate());
                pctDeceiver = Mathf.Clamp(pctDeceiver, 0, 100);
                
                list.Gap(4f);
                DrawHighlightedLabel(list, "FogOfPawn.Settings.CurrentComposition".Translate(pctTruthful, pctSlight, pctDeceiver));
                
                list.Gap(8f);
            }

            DrawSectionHeader(list, "FogOfPawn.Settings.Section.General".Translate(), ref _sectionGeneralOpen);
            if (_sectionGeneralOpen)
            {
                list.Gap(4f);

                list.Label("FogOfPawn.Settings.XPToReveal".Translate() + $": {xpToReveal}", -1f, "FogOfPawn.Settings.XPToRevealTooltip".Translate());
                xpToReveal = (int)list.Slider(xpToReveal, MinXp, MaxXp);

                list.Label("FogOfPawn.Settings.SlightSkillXP".Translate() + ": " + slightSkillXp);
                slightSkillXp = (int)list.Slider(slightSkillXp, 500, 5000);

                list.Gap(8f);
            }

            DrawSectionHeader(list, "FogOfPawn.Settings.Section.Toggles".Translate(), ref _sectionTogglesOpen);
            if (_sectionTogglesOpen)
            {
                list.Gap(4f);
                
                list.CheckboxLabeled("FogOfPawn.Settings.FogSkills".Translate(), ref fogSkills);
                list.CheckboxLabeled("FogOfPawn.Settings.FogTraits".Translate(), ref fogTraits);

                list.Gap(8f);

                list.Label("FogOfPawn.Settings.TraitHideChance".Translate() + $": {(int)(traitHideChance*100)}%", -1f, "FogOfPawn.Settings.TraitHideChanceTooltip".Translate());
                traitHideChance = list.Slider(traitHideChance, 0f, 1f);

                list.Gap(4f);
                list.CheckboxLabeled("FogOfPawn.Settings.VerboseLogging".Translate(), ref verboseLogging, "FogOfPawn.Settings.VerboseLogging_Tooltip".Translate());

                list.Gap(8f);
            }

            DrawSectionHeader(list, "FogOfPawn.Settings.Section.Ambient".Translate(), ref _sectionAmbientOpen);
            if (_sectionAmbientOpen)
            {
                list.Gap(4f);

                list.Label("FogOfPawn.Settings.SocialRevealPct".Translate() + $": {socialRevealPct}%", -1f, "FogOfPawn.Settings.SocialRevealPctTooltip".Translate());
                socialRevealPct = (int)list.Slider(socialRevealPct, 0, 100);

                list.CheckboxLabeled("    " + "FogOfPawn.Settings.AllowSocialSkillReveal".Translate(), ref allowSocialSkillReveal);
                list.CheckboxLabeled("    " + "FogOfPawn.Settings.AllowSocialTraitReveal".Translate(), ref allowSocialTraitReveal);

                list.Gap(8f);

                list.Label("FogOfPawn.Settings.PassiveRevealDays".Translate() + $": {passiveRevealDays:F1}", -1f, "FogOfPawn.Settings.PassiveRevealDaysTooltip".Translate());
                passiveRevealDays = Mathf.Clamp(list.Slider(passiveRevealDays, 1f, 20f), 1f, 20f);

                list.CheckboxLabeled("    " + "FogOfPawn.Settings.AllowPassiveSkillReveal".Translate(), ref allowPassiveSkillReveal);
                list.CheckboxLabeled("    " + "FogOfPawn.Settings.AllowPassiveTraitReveal".Translate(), ref allowPassiveTraitReveal);

                list.Gap(8f);

                list.Label("FogOfPawn.Settings.MaxAlteredSkills".Translate() + $": {maxAlteredSkills}");
                maxAlteredSkills = (int)list.Slider(maxAlteredSkills, 1, 5);

                list.Label("FogOfPawn.Settings.AlterRange".Translate() + $": ±{alteredSkillRange}");
                alteredSkillRange = (int)list.Slider(alteredSkillRange, 2, 10);

                list.CheckboxLabeled("FogOfPawn.Settings.AllowUnderstate".Translate(), ref allowUnderstate);

                list.Gap(8f);
            }

            DrawSectionHeader(list, "FogOfPawn.Settings.Section.Advanced".Translate(), ref _sectionAdvancedOpen);
            if (_sectionAdvancedOpen)
            {
                list.Gap(4f);

                list.CheckboxLabeled("FogOfPawn.Settings.BiasBadTraitHiding".Translate(), ref biasBadTraitHiding, "FogOfPawn.Settings.BiasBadTraitHiding_Tooltip".Translate());
                list.CheckboxLabeled("FogOfPawn.Settings.ScoreBasedLiarChance".Translate(), ref scoreBasedLiarChance, "FogOfPawn.Settings.ScoreBasedLiarChance_Tooltip".Translate());

                list.Gap(8f);

                list.CheckboxLabeled("FogOfPawn.Settings.ApplySkillPenalties".Translate(), ref applySkillPenalties, "FogOfPawn.Settings.ApplySkillPenalties_Tooltip".Translate());
                if (applySkillPenalties)
                {
                    list.Label("    " + "FogOfPawn.Settings.SkillPenaltyPct".Translate() + $": {skillPenaltyPct:F0}%");
                    skillPenaltyPct = Mathf.Round(list.Slider(skillPenaltyPct, 1f, 20f));
                }

                list.Gap(8f);

                list.Label("FogOfPawn.Settings.LateJoinerChance".Translate() + $": {lateJoinerChancePct:F1}%", -1f, "FogOfPawn.Settings.LateJoinerChance_Tooltip".Translate());
                lateJoinerChancePct = Mathf.Clamp(list.Slider(lateJoinerChancePct, 0f, 5f), 0f, 5f);

                list.Gap(8f);

                DrawSubHeader(list, "FogOfPawn.Settings.SubSection.Prisoner".Translate());
                list.Label("FogOfPawn.Settings.PrisonerRevealChance".Translate() + $": {prisonerRevealChancePct:F1}%", -1f, "FogOfPawn.Settings.PrisonerRevealChance_Tooltip".Translate());
                prisonerRevealChancePct = Mathf.Clamp(list.Slider(prisonerRevealChancePct, 0f, 10f), 0f, 10f);

                list.Label("FogOfPawn.Settings.PrisonerCrackChance".Translate() + $": {prisonerCrackChancePct:F1}%", -1f, "FogOfPawn.Settings.PrisonerCrackChance_Tooltip".Translate());
                prisonerCrackChancePct = Mathf.Clamp(list.Slider(prisonerCrackChancePct, 0f, 20f), 0f, 20f);

                list.Gap(8f);
            }

            DrawSectionHeader(list, "FogOfPawn.Settings.Section.Mood".Translate(), ref _sectionMoodOpen);
            if (_sectionMoodOpen)
            {
                list.Gap(4f);

                DrawSubHeader(list, "FogOfPawn.Settings.SubSection.HighMood".Translate());
                list.Label("FogOfPawn.Settings.PositiveMoodRevealPct".Translate() + $": {positiveMoodRevealPct}%", -1f, "FogOfPawn.Settings.PositiveMoodRevealPctTooltip".Translate());
                positiveMoodRevealPct = (int)list.Slider(positiveMoodRevealPct, 0, 100);

                list.Label("FogOfPawn.Settings.PositiveMoodThresholdPct".Translate() + $": {positiveMoodThresholdPct}%", -1f, "FogOfPawn.Settings.PositiveMoodThresholdPctTooltip".Translate());
                positiveMoodThresholdPct = (int)list.Slider(positiveMoodThresholdPct, 50, 100);

                list.Gap(8f);

                DrawSubHeader(list, "FogOfPawn.Settings.SubSection.MoodBreak".Translate());
                list.Label("FogOfPawn.Settings.MoodBreakBasePct".Translate() + $": {moodBreakBasePct}%", -1f, "FogOfPawn.Settings.MoodBreakBasePctTooltip".Translate());
                moodBreakBasePct = (int)list.Slider(moodBreakBasePct, 0, 100);

                list.Label("FogOfPawn.Settings.MoodBreakPerDayPct".Translate() + $": {moodBreakPerDayPct:F1}%", -1f, "FogOfPawn.Settings.MoodBreakPerDayPctTooltip".Translate());
                moodBreakPerDayPct = Mathf.Clamp(list.Slider(moodBreakPerDayPct, 0f, 10f), 0f, 10f);

                list.Gap(8f);
            }

            DrawSectionHeader(list, "FogOfPawn.Settings.Section.FullReveal".Translate(), ref _sectionFullRevealOpen);
            if (_sectionFullRevealOpen)
            {
                list.Gap(4f);

                DrawSubHeader(list, "FogOfPawn.Settings.SubSection.Sleeper".Translate());
                list.Label("FogOfPawn.Settings.SleeperCombatXP".Translate() + ": " + sleeperCombatXp);
                sleeperCombatXp = (int)list.Slider(sleeperCombatXp, 500, 10000);

                list.Gap(8f);

                DrawSubHeader(list, "FogOfPawn.Settings.SubSection.Imposter".Translate());
                list.Label("FogOfPawn.Settings.ImposterSkillXP".Translate() + ": " + imposterSkillXp);
                imposterSkillXp = (int)list.Slider(imposterSkillXp, 500, 10000);

                list.Label("FogOfPawn.Settings.ImposterHighSkills".Translate() + ": " + imposterHighSkills);
                imposterHighSkills = (int)list.Slider(imposterHighSkills, 1, 6);

                list.Label("FogOfPawn.Settings.ImposterMidSkills".Translate() + ": " + imposterMidSkills);
                imposterMidSkills = (int)list.Slider(imposterMidSkills, 0, 6);

                list.Gap(8f);

                DrawSubHeader(list, "FogOfPawn.Settings.SubSection.PassiveReveal".Translate());
                list.Label("FogOfPawn.Settings.PassiveDailyRevealPct".Translate() + ": " + passiveDailyRevealPct.ToString("F1") + "%");
                passiveDailyRevealPct = list.Slider(passiveDailyRevealPct, 0f, 20f);

                list.Gap(8f);

                DrawSubHeader(list, "FogOfPawn.Settings.SubSection.DisguiseKit".Translate());
                list.CheckboxLabeled("FogOfPawn.Settings.SpawnDisguiseKitOnReveal".Translate(), ref spawnDisguiseKitOnReveal, "FogOfPawn.Settings.SpawnDisguiseKitOnReveal_Tooltip".Translate());

                list.Label("FogOfPawn.Settings.DisguiseKitWealth".Translate() + ": " + disguiseKitWealth);
                disguiseKitWealth = (int)list.Slider(disguiseKitWealth, 0, 10000);

                list.Gap(8f);
            }

            list.Gap(16f);
            list.GapLine();
            list.Gap(8f);

            Rect buttonRect = list.GetRect(32f);
            buttonRect.width = 200f;
            buttonRect.x = (viewRect.width - buttonRect.width) / 2f;

            Color prevColor = GUI.color;
            GUI.color = new Color(0.8f, 0.3f, 0.3f);
            if (Widgets.ButtonText(buttonRect, "FogOfPawn.Settings.ResetDefaults".Translate()))
            {
                ResetDefaults();
            }
            GUI.color = prevColor;

            list.Gap(16f);

            list.End();
            Widgets.EndScrollView();

            Write();
        }

        private void DrawSectionHeader(Listing_Standard list, string label, ref bool isOpen)
        {
            Rect headerRect = list.GetRect(28f);
            
            Color bgColor = isOpen ? new Color(0.2f, 0.2f, 0.25f, 0.8f) : new Color(0.15f, 0.15f, 0.18f, 0.6f);
            Widgets.DrawBoxSolid(headerRect, bgColor);
            
            Widgets.DrawBox(headerRect);

            string arrow = isOpen ? "▼ " : "▶ ";
            
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            Rect labelRect = headerRect.ContractedBy(4f);
            labelRect.x += 4f;
            Widgets.Label(labelRect, arrow + label);
            Text.Anchor = TextAnchor.UpperLeft;

            if (Widgets.ButtonInvisible(headerRect))
            {
                isOpen = !isOpen;
            }

            Widgets.DrawHighlightIfMouseover(headerRect);
            
            list.Gap(4f);
        }

        private void DrawSubHeader(Listing_Standard list, string label)
        {
            Text.Font = GameFont.Small;
            GUI.color = new Color(0.8f, 0.8f, 0.6f);
            list.Label("• " + label);
            GUI.color = Color.white;
            list.Gap(2f);
        }

        private void DrawHighlightedLabel(Listing_Standard list, string text)
        {
            Rect rect = list.GetRect(24f);
            Widgets.DrawBoxSolid(rect, new Color(0.1f, 0.3f, 0.1f, 0.3f));
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(rect, text);
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private float CalculateViewHeight()
        {
            float height = 120f; 

            height += 7 * 36f; 

            if (_sectionSpawnOpen) height += 220f;
            if (_sectionGeneralOpen) height += 120f;
            if (_sectionTogglesOpen) height += 200f;
            if (_sectionAmbientOpen) height += 350f;
            if (_sectionAdvancedOpen) height += 340f; 
            if (_sectionMoodOpen) height += 280f;
            if (_sectionFullRevealOpen) height += 400f;

            return height + 50f; 
        }

        private Vector2 _scrollPos = Vector2.zero;

        public void ResetDefaults()
        {
            pctTruthful = 65;
            pctSlight = 30;
            pctDeceiver = 5;
            deceiverJoinersOnly = false;

            deceptionIntensity = 0.5f;
            xpToReveal = 1000;
            slightSkillXp = 1000;

            fogSkills = true;
            fogTraits = true;
            verboseLogging = false;
            traitHideChance = 0.3f;

            socialRevealPct = 5;
            passiveRevealDays = 6f;
            allowSocialSkillReveal = true;
            allowSocialTraitReveal = true;
            allowPassiveSkillReveal = true;
            allowPassiveTraitReveal = true;
            maxAlteredSkills = 3;
            alteredSkillRange = 6;
            allowUnderstate = true;

            biasBadTraitHiding = false;
            scoreBasedLiarChance = false;
            applySkillPenalties = true;
            skillPenaltyPct = 5f;
            lateJoinerChancePct = 1.5f;
            prisonerRevealChancePct = 1.5f;
            prisonerCrackChancePct = 2f;

            positiveMoodRevealPct = 5;
            positiveMoodThresholdPct = 70;
            moodBreakBasePct = 50;
            moodBreakPerDayPct = 2f;

            sleeperCombatXp = 5000;
            imposterSkillXp = 4000;
            imposterHighSkills = 3;
            imposterMidSkills = 3;
            passiveDailyRevealPct = 1f;
            spawnDisguiseKitOnReveal = false;
            disguiseKitWealth = 2000;
        }
    }
} 