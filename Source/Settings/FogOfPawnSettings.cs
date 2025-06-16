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
        public bool deceiverJoinersOnly = true;

        private const int MinXp = 100;
        private const int MaxXp = 1000;

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
            Scribe_Values.Look(ref deceiverJoinersOnly, "deceiverJoinersOnly", true);
        }

        public void DoWindowContents(Rect inRect)
        {
            var list = new Listing_Standard();
            list.Begin(inRect);

            // Deception intensity slider
            list.Label("FogOfPawn.Settings.DeceptionIntensity".Translate() + $": {(int)(deceptionIntensity * 100)}%", -1f, "FogOfPawn.Settings.DeceptionIntensityTooltip".Translate());
            deceptionIntensity = list.Slider(deceptionIntensity, 0f, 1f);
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
            list.CheckboxLabeled("FogOfPawn.Settings.DeceiverJoinerOnly".Translate(), ref deceiverJoinersOnly);

            list.End();

            // Apply instantly so any in-game logic reads the new values without waiting
            // for the player to close the settings window or restart the game.
            Write();
        }
    }
} 