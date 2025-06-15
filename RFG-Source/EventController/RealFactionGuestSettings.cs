using Verse;

namespace EventController_rQP
{
    public class RealFactionGuestSettings : ModSettings
    {
        /// <summary>
        /// The three settings our mod has.
        /// </summary>
        public static bool strictQuestGuest;
        public static bool alternativeFaction;
        public static float strictChance;
        public static bool factionLeaderValidator;
        public static bool debugOption;
        public static bool damageUntilDownedBypassShield;
        public static bool dontAdjustXenotypeForRabbie;
        public static bool creepJoinerValidator;
        public static bool creepJoinerGenerateNoLimit;
        /// <summary>
        /// The part that writes our settings to file. Note that saving is by ref.
        /// </summary>
        public override void ExposeData()
        {
            Scribe_Values.Look(ref strictQuestGuest, "strictQuestGuest", true);
            Scribe_Values.Look(ref factionLeaderValidator, "factionLeaderValidator", true);
            Scribe_Values.Look(ref damageUntilDownedBypassShield, "damageUntilDownedBypassShield", true);
            Scribe_Values.Look(ref creepJoinerValidator, "creepJoinerValidator", true);
            Scribe_Values.Look(ref creepJoinerGenerateNoLimit, "creepJoinerGenerateNoLimit", true);
            if (!creepJoinerValidator)
            {
                creepJoinerGenerateNoLimit = false;
            }
            if (ModsConfig.BiotechActive && ModsConfig.IsActive("RunneLatki.RabbieRaceMod"))
            {
                Scribe_Values.Look(ref dontAdjustXenotypeForRabbie, "dontAdjustXenotypeForRabbie", true);
            }
            else
            {
                Scribe_Values.Look(ref dontAdjustXenotypeForRabbie, "dontAdjustXenotypeForRabbie", false);
            }
            Scribe_Values.Look(ref alternativeFaction, "alternativeFaction", false);
            Scribe_Values.Look(ref strictChance, "guestChance", 1.0f);
            base.ExposeData();
        }
        public static void ResetToDefault()
        {
            creepJoinerGenerateNoLimit = true;
            creepJoinerValidator = true;
            strictQuestGuest = true;
            damageUntilDownedBypassShield = true;
            if (ModsConfig.BiotechActive && ModsConfig.IsActive("RunneLatki.RabbieRaceMod"))
            {
                dontAdjustXenotypeForRabbie = true;
            }
            factionLeaderValidator = true;
            debugOption = false;
            alternativeFaction = false;
            strictChance = 1f;
        }
    }
}