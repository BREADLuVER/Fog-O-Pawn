using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace EventController_rQP
{
    public static class ChoosePawn
    {
        public static PawnKindDef ChoosePawnKind(List<PawnGroupMaker> plans, float combatPower, bool flag = true)
        {

            PawnKindDef p = null;
            if ((EventController_Work.ongoingEvents & OngoingEvent.Trader) != 0)
            {
                var traders = plans.Where(t => t.traders != null).Select(t => t.traders);
                if (traders.Any())
                {
                    p = ChoosePawnKindInner(traders, combatPower, flag);
                }
            }
            //carrier is skipped.
            else if ((EventController_Work.ongoingEvents & OngoingEvent.Guard) != 0)
            {
                var guards = plans.Where(t => t.guards != null).Select(t => t.guards);
                if (guards.Any())
                {
                    p = ChoosePawnKindInner(guards, combatPower, flag);
                }
            }
            if (p == null)
            {
                var options = plans.Where(t => t.options != null).Select(t => t.options);
                p = ChoosePawnKindInner(options, combatPower, flag);
            }
            return p;
        }
        private static PawnKindDef ChoosePawnKindInner(IEnumerable<List<PawnGenOption>> options, float combatPower, bool flag = true)
        {
            var pawnKinds =
                from p in options
                from t in p
                where t.kind.RaceProps != null
                && t.kind.RaceProps.Humanlike
                && t.kind.RaceProps.intelligence == Intelligence.Humanlike
                //&& (RealFactionGuestSettings.creepJoinerGenerateNoLimit ? t.kind is not CreepJoinerFormKindDef : true)
                select t.kind;
            var pawnToChoose =
                from p in pawnKinds
                where Mathf.Abs(p.combatPower - combatPower) < 30f
                select p;
            var pawnEquals =
                from p in pawnToChoose
                where p.combatPower == combatPower
                select p;
            if (pawnToChoose.Any())
            {
                // only one = request combatPower
                if (pawnEquals.Any())
                {
                    return pawnEquals.ToHashSet().RandomElement();
                }
                // other situations, get random one
                else
                {
                    return pawnToChoose.ToHashSet().RandomElement();
                }
            }
            else if (RealFactionGuestSettings.strictQuestGuest && flag)
            {
                return ChoosePawnKindInner_A(pawnKinds, combatPower);
            }
            return null;
        }
        private static PawnKindDef ChoosePawnKindInner_A(IEnumerable<PawnKindDef> pawnKinds, float combatPower)
        {
            IEnumerable<PawnKindDef> pawnEquals = [];
            var combatPowerArray =
                (from p in pawnKinds
                 select p.combatPower).ToArray();
            var nearpower = combatPowerArray.MinBy(a => Mathf.Abs(a - combatPower));
            pawnEquals =
                from p in pawnKinds
                where p.combatPower == nearpower
                select p;
            if (pawnEquals.Any())
            {
                return pawnEquals.ToHashSet().RandomElement();
            }
            return null;
        }
    }
}
