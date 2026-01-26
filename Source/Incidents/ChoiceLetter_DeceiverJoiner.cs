using System.Collections.Generic;
using RimWorld;
using Verse;
using System;

namespace FogOfPawn
{
    public class ChoiceLetter_DeceiverJoiner : ChoiceLetter
    {
        public Pawn pawn;
        public Action acceptAction;
        public Action rejectAction;

        public override IEnumerable<DiaOption> Choices
        {
            get
            {
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

                DiaOption postpone = new DiaOption("PostponeLetter".Translate())
                {
                    resolveTree = false
                };
                yield return postpone;
            }
        }
    }
} 