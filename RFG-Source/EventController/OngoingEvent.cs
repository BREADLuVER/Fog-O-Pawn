using System;

namespace EventController_rQP
{
    [Flags]
    public enum OngoingEvent
    {
        None = 0,
        TraderGroup = 1,
        Trader = 2,
        Carrier = 4,
        Guard = 8,
        End = 16,
        RefugeePodCrash = 32,
        InternalGen = 64,
        GenerateNewPawnInternal = 128,
        CreepJoiner = 256,
        FactionFix = 512,
        BackstoryFix = 1024,
        QuestGetPawn = 2048,
        DamageUntilDowned = 4096,
        FactionLeaderValidator = 8192,
        RequestValidator = 16384,
        AdjustXenotype = 32768,
        PreApplyDamage = 65536,
        PreApplyDamagePawn = 131072,
        PreApplyDamageThing = 262144,
        CreepJoinerValidator = 524288
    }
}
