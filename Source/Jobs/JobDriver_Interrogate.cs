using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;
using DiceFramework;

namespace FogOfPawn.Jobs
{
    public class JobDriver_Interrogate : JobDriver
    {
        private const int WaitTicks = 180; // 3 real seconds at normal speed

        private Pawn TargetPawn => (Pawn)job.targetA.Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed) => pawn.Reserve(TargetPawn, job, 1, -1, null, errorOnFailed);

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(TargetIndex.A);
            this.FailOn(() => TargetPawn.Dead || TargetPawn.Downed);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);

            var wait = Toils_General.WaitWith(TargetIndex.A, WaitTicks, true, true);
            wait.AddPreTickAction(() => pawn.rotationTracker.FaceCell(TargetPawn.Position));
            wait.AddFinishAction(DoInterrogation);
            yield return wait;
        }

        private void DoInterrogation()
        {
            if (TargetPawn == null) return;
            var comp = TargetPawn.GetComp<CompPawnFog>();
            if (comp == null) return;

            // daily lockout
            int now = Find.TickManager.TicksGame;
            if (now - comp.lastInterrogatedTick < 60000) return;
            comp.lastInterrogatedTick = now;

            int interrogatorRoll = DiceRoller.D20() + DiceRoller.D20() + Mathf.RoundToInt(pawn.skills.GetSkill(SkillDefOf.Social).Level);
            int suspectRoll = DiceRoller.D20() + DiceRoller.D20() + Mathf.RoundToInt(TargetPawn.skills.GetSkill(SkillDefOf.Social).Level) + 5;

            bool success = interrogatorRoll >= suspectRoll;

            Find.WindowStack.Add(new DiceFramework.DiceRollWindow("Interrogation", pawn.LabelShort, interrogatorRoll, TargetPawn.LabelShort, suspectRoll, () =>
            {
                string msg = success ? "Interrogation succeeded" : "Interrogation failed";
                Messages.Message($"{pawn.LabelShort} interrogated {TargetPawn.LabelShort}: {msg} (you {interrogatorRoll} vs {suspectRoll})", TargetPawn, success ? MessageTypeDefOf.PositiveEvent : MessageTypeDefOf.NeutralEvent);

                if (success)
                {
                    FogUtility.RevealRandomFoggedAttribute(TargetPawn, preferSkill: true);
                }
            }));
            // failure handled in callback
        }
    }
} 