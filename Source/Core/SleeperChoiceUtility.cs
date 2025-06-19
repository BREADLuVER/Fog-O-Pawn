using RimWorld;
using Verse;
using Verse.AI;
using System;
using System.Linq;

namespace FogOfPawn
{
    public static class SleeperChoiceUtility
    {
        public static void SendChoiceLetter(Pawn pawn)
        {
            if (pawn == null) return;

            TaggedString title = "Fog_SleeperChoice.Label".Translate(pawn.Named("PAWN"));

            string baseKey = "Fog_SleeperChoice.Text";
            TaggedString text  = baseKey.Translate(pawn.Named("PAWN"));

            var variants = new System.Collections.Generic.List<TaggedString>();
            for (int i = 1; i <= 5; i++)
            {
                string vKey = baseKey + "." + i;
                var raw = vKey.Translate(pawn.Named("PAWN"));
                if (!raw.RawText.NullOrEmpty() && raw.RawText != vKey)
                    variants.Add(raw);
            }
            if (variants.Count > 0)
                text = variants.RandomElement();

            DiaNode node = new DiaNode(text);

            // Keep option
            DiaOption keepOpt = new DiaOption("Fog_SleeperOutcome_Keep".Translate())
            {
                resolveTree = true,
            };
            keepOpt.action = () =>
            {
                // Positive memory to pawn for being accepted
                var thought = DefDatabase<ThoughtDef>.GetNamedSilentFail("Fog_SleeperKept_Trusted");
                if (thought != null)
                {
                    foreach (var col in pawn.MapHeld?.mapPawns?.FreeColonistsSpawned ?? Enumerable.Empty<Pawn>())
                    {
                        col.needs?.mood?.thoughts?.memories?.TryGainMemory(thought, pawn);
                    }
                }
            };
            node.options.Add(keepOpt);

            // Capture option
            DiaOption capOpt = new DiaOption("Fog_SleeperOutcome_Capture".Translate())
            {
                resolveTree = true,
            };
            capOpt.action = () =>
            {
                // Attempt arrest by nearest warden
                var warden = pawn.MapHeld?.mapPawns?.FreeColonistsSpawned?.FirstOrDefault(p => !p.Dead && !p.Downed && p != pawn);
                if (warden != null)
                {
                    Job job = JobMaker.MakeJob(JobDefOf.Arrest, pawn);
                    warden.jobs?.TryTakeOrderedJob(job, JobTag.Misc);
                }
            };
            node.options.Add(capOpt);

            // Exile option
            DiaOption exileOpt = new DiaOption("Fog_SleeperOutcome_Exile".Translate())
            {
                resolveTree = true,
            };
            exileOpt.action = () =>
            {
                PawnBanishUtility.Banish(pawn, true);
            };
            node.options.Add(exileOpt);

            Window dialog = new Dialog_NodeTree(node, true, false, title);
            Find.WindowStack.Add(dialog);
        }
    }
} 