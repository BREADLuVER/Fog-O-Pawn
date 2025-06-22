using RimWorld;
using Verse;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace FogOfPawn
{
    /// <summary>
    /// Utility that shows an unobtrusive ChoiceLetter (blue notification on the right-hand side) offering the player
    /// the option to accept or reject a special sleeper / imposter wanderer. This mirrors the behaviour of the
    /// vanilla Wanderer joins event and avoids the intrusive Dialog_NodeTree previously used.
    /// </summary>
    public static class JoinerChoiceUtility
    {
        /// <summary>
        /// Creates and dispatches a letter with Accept / Reject choices for the supplied pawn.
        /// </summary>
        /// <param name="pawn">Pawn that is offering to join.</param>
        /// <param name="tier">The deception tier of the pawn (sleeper / imposter) – only used for tracking.</param>
        /// <param name="acceptAction">Action to execute when the player accepts the pawn.</param>
        /// <param name="rejectAction">Action to execute when the player rejects the pawn.</param>
        public static void ShowJoinerChoice(Pawn pawn, DeceptionTier tier, Action acceptAction, Action rejectAction)
        {
            if (pawn == null) return;

            ChoiceLetter_DeceiverJoiner letter = LetterMaker.MakeLetter(LetterDefOf.PositiveEvent) as ChoiceLetter_DeceiverJoiner;
            if (letter == null)
            {
                letter = new ChoiceLetter_DeceiverJoiner();
                letter.def = LetterDefOf.PositiveEvent;
            }

            letter.pawn = pawn;
            letter.acceptAction = acceptAction;
            letter.rejectAction = rejectAction;
            
            TaggedString label = "Wanderer joins: " + pawn.Name.ToStringShort;
            TaggedString text = "FogOfPawn.SpecialJoiner.Text".Translate(pawn.Named("PAWN"));

            SetLetterFields(letter, label, text, new LookTargets(pawn));
            
            Log.Message($"[FogOfPawn] Created joiner letter for {pawn.Name.ToStringShort}. Label: '{label}', Text: '{text}'");

            Find.LetterStack.ReceiveLetter(letter);
        }

        private static void SetLetterFields(ChoiceLetter_DeceiverJoiner letter, TaggedString label, TaggedString text, LookTargets lookTargets)
        {
            var flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

            var labelField = typeof(Letter).GetField("label", flags);
            if (labelField != null)
            {
                labelField.SetValue(letter, label);
                Log.Message($"[FogOfPawn] Set 'label' field via reflection.");
            }
            else
            {
                Log.Warning($"[FogOfPawn] Could not find 'label' field on Letter.");
            }

            var lookTargetsField = typeof(Letter).GetField("lookTargets", flags);
            if (lookTargetsField != null)
            {
                lookTargetsField.SetValue(letter, lookTargets);
                Log.Message($"[FogOfPawn] Set 'lookTargets' field via reflection.");
            }
            else
            {
                Log.Warning($"[FogOfPawn] Could not find 'lookTargets' field on Letter.");
            }

            var textField = typeof(ChoiceLetter).GetField("text", flags);
            if (textField != null)
            {
                textField.SetValue(letter, text);
                Log.Message($"[FogOfPawn] Set 'text' field via reflection.");
            }
            else
            {
                Log.Warning($"[FogOfPawn] Could not find 'text' field on ChoiceLetter.");
            }
        }
    }
} 