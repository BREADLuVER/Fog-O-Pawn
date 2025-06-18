using Verse;
using UnityEngine;

namespace FogOfPawn
{
    public class FogOfPawnSettings : ModSettings
    {
        public float deceptionIntensity = 0.5f; // 0 honest, 1 chaotic
        public int xpToReveal = 300;
        public bool fogSkills = true;
        public bool fogTraits = true;
        public bool fogGenes = true;
        public bool fogAddictions = true;

        // Ambient reveal tuning
        public int socialRevealPct = 10; // % chance per successful interaction
        public float passiveRevealDays = 6f; // MTBDays per pawn

        public bool allowSocialSkillReveal = true;
        public bool allowSocialTraitReveal = true;
        public bool allowPassiveSkillReveal = true;
        public bool allowPassiveTraitReveal = true;

        public int maxAlteredSkills = 3;
        public bool allowUnderstate = true;
        public bool deceiverJoinersOnly = false;
        // Renamed – keep old field for backward compatibility
        public bool limitDeceiversToColonists
        {
            get => deceiverJoinersOnly;
            set => deceiverJoinersOnly = value;
        }

        public float traitHideChance = 0.3f; // 0 none, 1 all hidden

        // Full reveal mechanics
        public int sleeperCombatXp = 5000;
        public int scammerSkillXp = 4000;
        public float passiveDailyRevealPct = 1f; // 1%
        public int disguiseKitWealth = 2000;

        // Scammer balancing sliders
        public int scammerHighSkills = 3; // # of high claimed skills 8-14
        public int scammerMidSkills = 3; // # of mid claimed skills 4-8

        // Add fields after deceptionIntensity
        public int pctTruthful = 90;
        public int pctSlight = 9;
        public int pctDeceiver = 1;

        private const int MinXp = 1000;
        private const int MaxXp = 5000;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref deceptionIntensity, "deceptionIntensity", 0.5f);
            Scribe_Values.Look(ref xpToReveal, "xpToReveal", 300);
            Scribe_Values.Look(ref fogSkills, "fogSkills", true);
            Scribe_Values.Look(ref fogTraits, "fogTraits", true);
            Scribe_Values.Look(ref fogGenes, "fogGenes", true);
            Scribe_Values.Look(ref fogAddictions, "fogAddictions", true);

            Scribe_Values.Look(ref socialRevealPct, "socialRevealPct", 10);
            Scribe_Values.Look(ref passiveRevealDays, "passiveRevealDays", 6f);

            Scribe_Values.Look(ref allowSocialSkillReveal, "allowSocialSkillReveal", true);
            Scribe_Values.Look(ref allowSocialTraitReveal, "allowSocialTraitReveal", true);
            Scribe_Values.Look(ref allowPassiveSkillReveal, "allowPassiveSkillReveal", true);
            Scribe_Values.Look(ref allowPassiveTraitReveal, "allowPassiveTraitReveal", true);

            Scribe_Values.Look(ref maxAlteredSkills, "maxAlteredSkills", 3);
            Scribe_Values.Look(ref allowUnderstate, "allowUnderstate", true);
            Scribe_Values.Look(ref deceiverJoinersOnly, "deceiverJoinersOnly", false);

            Scribe_Values.Look(ref traitHideChance, "traitHideChance", 0.3f);

            Scribe_Values.Look(ref sleeperCombatXp, "sleeperCombatXp", 5000);
            Scribe_Values.Look(ref scammerSkillXp, "scammerSkillXp", 4000);
            Scribe_Values.Look(ref passiveDailyRevealPct, "passiveDailyRevealPct", 1f);
            Scribe_Values.Look(ref disguiseKitWealth, "disguiseKitWealth", 2000);

            Scribe_Values.Look(ref scammerHighSkills, "scammerHighSkills", 3);
            Scribe_Values.Look(ref scammerMidSkills, "scammerMidSkills", 3);

            Scribe_Values.Look(ref pctTruthful, "pctTruthful", 90);
            Scribe_Values.Look(ref pctSlight, "pctSlight", 9);
            Scribe_Values.Look(ref pctDeceiver, "pctDeceiver", 1);
        }

        public void DoWindowContents(Rect inRect)
        {
            // Simple scroll view because we now have a lot of settings.
            float viewHeight = 1000f;
            Rect viewRect = new Rect(0, 0, inRect.width - 16f, viewHeight);
            Widgets.BeginScrollView(inRect, ref _scrollPos, viewRect);

            var list = new Listing_Standard();
            list.Begin(viewRect);

            // Spawn distribution sliders (must total >0; they will be normalised internally)
            list.Label("Spawn composition weights (will be normalised):");
            list.Label("Truthful: " + pctTruthful + "%");
            pctTruthful = (int)list.Slider(pctTruthful, 0, 100);
            list.Label("Slightly-Deceived: " + pctSlight + "%");
            pctSlight = (int)list.Slider(pctSlight, 0, 100);
            list.Label("Scammer/Sleeper: " + pctDeceiver + "%");
            pctDeceiver = (int)list.Slider(pctDeceiver, 0, 100);

            list.GapLine();
            
            // NEW: Deceiver storyline toggle right after composition
            list.CheckboxLabeled("FogOfPawn.Settings.DeceiverJoinerOnly".Translate(), ref deceiverJoinersOnly, "FogOfPawn.Settings.DeceiverJoinerOnly_Tooltip".Translate());

            // Display normalised result
            {
                float sum = pctTruthful + pctSlight + pctDeceiver;
                if (sum < 0.01f) sum = 1f;
                float t = pctTruthful / sum;
                float s = pctSlight / sum;
                float d = pctDeceiver / sum;
                list.Label($"Current composition → Truthful {(int)(t*100)}%  | Slight {(int)(s*100)}%  | Deceiver {(int)(d*100)}%");
            }

            list.GapLine();

            // XP to reveal
            list.Label("FogOfPawn.Settings.XPToReveal".Translate() + $": {xpToReveal}", -1f, "FogOfPawn.Settings.XPToRevealTooltip".Translate());
            xpToReveal = (int)list.Slider(xpToReveal, MinXp, MaxXp);
            list.GapLine();

            // Toggles
            list.CheckboxLabeled("FogOfPawn.Settings.FogSkills".Translate(), ref fogSkills);
            list.CheckboxLabeled("FogOfPawn.Settings.FogTraits".Translate(), ref fogTraits);
            list.CheckboxLabeled("FogOfPawn.Settings.FogGenes".Translate(), ref fogGenes);
            list.CheckboxLabeled("FogOfPawn.Settings.FogAddictions".Translate(), ref fogAddictions);

            list.GapLine();

            // Ambient reveal tuning section
            list.Label("FogOfPawn.Settings.SocialRevealPct".Translate() + $": {socialRevealPct} %", -1f, "FogOfPawn.Settings.SocialRevealPctTooltip".Translate());
            socialRevealPct = (int)list.Slider(socialRevealPct, 0, 100);

            list.Label("FogOfPawn.Settings.PassiveRevealDays".Translate() + $": {passiveRevealDays:F1}", -1f, "FogOfPawn.Settings.PassiveRevealDaysTooltip".Translate());
            passiveRevealDays = Mathf.Clamp(list.Slider(passiveRevealDays, 1f, 20f), 1f, 20f);

            list.CheckboxLabeled("FogOfPawn.Settings.AllowSocialSkillReveal".Translate(), ref allowSocialSkillReveal);
            list.CheckboxLabeled("FogOfPawn.Settings.AllowSocialTraitReveal".Translate(), ref allowSocialTraitReveal);
            list.CheckboxLabeled("FogOfPawn.Settings.AllowPassiveSkillReveal".Translate(), ref allowPassiveSkillReveal);
            list.CheckboxLabeled("FogOfPawn.Settings.AllowPassiveTraitReveal".Translate(), ref allowPassiveTraitReveal);

            list.GapLine();
            list.Label("FogOfPawn.Settings.MaxAlteredSkills".Translate() + $": {maxAlteredSkills}");
            maxAlteredSkills = (int)list.Slider(maxAlteredSkills, 1, 5);
            list.CheckboxLabeled("FogOfPawn.Settings.AllowUnderstate".Translate(), ref allowUnderstate);

            list.GapLine();
            list.Label("FogOfPawn.Settings.TraitHideChance".Translate() + $": {(int)(traitHideChance*100)} %", -1f, "FogOfPawn.Settings.TraitHideChanceTooltip".Translate());
            traitHideChance = list.Slider(traitHideChance, 0f, 1f);
            list.GapLine();

            list.Label("FogOfPawn.Settings.FullRevealHeader".Translate());
            list.Label("Sleeper combat XP: " + sleeperCombatXp);
            sleeperCombatXp = (int)list.Slider(sleeperCombatXp, 500, 10000);

            list.Label("Scammer low-skill XP: " + scammerSkillXp);
            scammerSkillXp = (int)list.Slider(scammerSkillXp, 500, 10000);

            list.Label("Passive daily reveal chance: " + passiveDailyRevealPct.ToString("F1") + "%");
            passiveDailyRevealPct = list.Slider(passiveDailyRevealPct, 0f, 20f);

            list.Label("Disguise kit wealth reduction: " + disguiseKitWealth);
            disguiseKitWealth = (int)list.Slider(disguiseKitWealth, 0, 10000);
            list.GapLine();

            list.Label("Scammer high claimed skills: " + scammerHighSkills);
            scammerHighSkills = (int)list.Slider(scammerHighSkills, 1, 6);

            list.Label("Scammer mid claimed skills: " + scammerMidSkills);
            scammerMidSkills = (int)list.Slider(scammerMidSkills, 0, 6);

            list.End();
            Widgets.EndScrollView();

            // Apply instantly so any in-game logic reads the new values without waiting
            // for the player to close the settings window or restart the game.
            Write();
        }

        private Vector2 _scrollPos = Vector2.zero;
    }
} 