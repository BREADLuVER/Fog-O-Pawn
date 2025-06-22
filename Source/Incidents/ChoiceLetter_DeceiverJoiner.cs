using System.Collections.Generic;
using RimWorld;
using Verse;
using System;

namespace FogOfPawn
{
    /// <summary>
    /// Letter that mimics vanilla "Wanderer joins" accept/reject/postpone behaviour but routes the callbacks
    /// through custom actions so we can still perform our Sleeper / Imposter bookkeeping.
    /// </summary>
    public class ChoiceLetter_DeceiverJoiner : ChoiceLetter
    {
        public Pawn pawn;
        public Action acceptAction;
        public Action rejectAction;

        public override IEnumerable<DiaOption> Choices
        {
            get
            {
                // Accept
                DiaOption accept = new DiaOption("AcceptButton".Translate())
                {
                    resolveTree = true,
                    action = () =>
                    {
                        Log.Message($"[FogOfPawn] Executing 'Accept' action for {pawn.Name.ToStringShort}.");
                        acceptAction?.Invoke();
                        Find.LetterStack.RemoveLetter(this);
                    }
                };
                yield return accept;

                // Reject
                DiaOption reject = new DiaOption("RejectLetter".Translate())
                {
                    resolveTree = true,
                    action = () =>
                    {
                        Log.Message($"[FogOfPawn] Executing 'Reject' action for {pawn.Name.ToStringShort}.");
                        rejectAction?.Invoke();
                        Find.LetterStack.RemoveLetter(this);
                    }
                };
                yield return reject;

                // Postpone (does nothing, keep letter open)
                DiaOption postpone = new DiaOption("PostponeLetter".Translate())
                {
                    resolveTree = false
                };
                yield return postpone;
            }
        }
    }
} 