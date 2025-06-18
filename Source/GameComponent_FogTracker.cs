using Verse;
using RimWorld;
using System.Collections.Generic;
using System.Linq;

namespace FogOfPawn
{
    public class GameComponent_FogTracker : GameComponent
    {
        public GameComponent_FogTracker(Game game) : base() { }

        private struct SleeperStory
        {
            public int pawnId;
            public int stage;   // 1=suspicion,2=anomaly,3=queued reveal
            public int dueTick; // absolute game tick when next stage fires
        }

        private List<SleeperStory> _stories = new();

        // interval control
        private const int CheckInterval = 2500; // roughly 1 in-game hour
        private const int TicksPerSeason = 900000; // 15 days
        private static int NextBeatIntervalTicks => Rand.Range(TicksPerSeason + TicksPerSeason / 2, TicksPerSeason * 2); // 1.5–2 seasons

        #region Scribing helpers
        private class SleeperStoryScribe : IExposable
        {
            public int pawnId;
            public int stage;
            public int dueTick;

            public SleeperStoryScribe() { }
            public SleeperStoryScribe(SleeperStory s)
            {
                pawnId = s.pawnId;
                stage = s.stage;
                dueTick = s.dueTick;
            }

            public void ExposeData()
            {
                Scribe_Values.Look(ref pawnId, "pawnId");
                Scribe_Values.Look(ref stage, "stage");
                Scribe_Values.Look(ref dueTick, "dueTick");
            }
        }
        #endregion

        public override void GameComponentTick()
        {
            if (Find.TickManager.TicksGame % CheckInterval != 0) return;

            if (_stories.Count == 0) return;

            for (int i = _stories.Count - 1; i >= 0; i--)
            {
                var entry = _stories[i];
                if (Find.TickManager.TicksGame < entry.dueTick) continue;

                Pawn pawn = Find.WorldPawns.AllPawnsAlive.FirstOrDefault(p => p.thingIDNumber == entry.pawnId);
                if (pawn == null || pawn.DestroyedOrNull() || pawn.Dead)
                {
                    _stories.RemoveAt(i);
                    continue;
                }

                if (entry.stage == 1)
                {
                    SendSleeperLetter(pawn, "Fog_SleeperReveal_Suspicion");
                    entry.stage = 2;
                    entry.dueTick = Find.TickManager.TicksGame + NextBeatIntervalTicks;
                    _stories[i] = entry;
                }
                else if (entry.stage == 2)
                {
                    SendSleeperLetter(pawn, "Fog_SleeperReveal_Anomaly");
                    entry.stage = 3;
                    entry.dueTick = Find.TickManager.TicksGame + NextBeatIntervalTicks;
                    _stories[i] = entry;
                }
                else if (entry.stage == 3)
                {
                    FogUtility.TriggerFullReveal(pawn, "SleeperStory");
                    _stories.RemoveAt(i);
                }
            }
        }

        public void StartSleeperStory(Pawn pawn)
        {
            if (pawn == null) return;
            if (_stories.Any(s => s.pawnId == pawn.thingIDNumber)) return; // already running

            var entry = new SleeperStory
            {
                pawnId = pawn.thingIDNumber,
                stage = 2, // we already sent suspicion now
                dueTick = Find.TickManager.TicksGame + NextBeatIntervalTicks // first beat interval
            };
            _stories.Add(entry);

            SendSleeperLetter(pawn, "Fog_SleeperReveal_Suspicion");
        }

        private static void SendSleeperLetter(Pawn pawn, string key)
        {
            string label = (key + ".Label").Translate(pawn.Named("PAWN"));
            string text = (key + ".Text").Translate(pawn.Named("PAWN"));
            Find.LetterStack.ReceiveLetter(label, text, LetterDefOf.NeutralEvent, pawn);
        }

        public override void ExposeData()
        {
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                var list = _stories.Select(s => new SleeperStoryScribe(s)).ToList();
                Scribe_Collections.Look(ref list, "sleeperStories", LookMode.Deep);
            }
            else if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                List<SleeperStoryScribe> list = null;
                Scribe_Collections.Look(ref list, "sleeperStories", LookMode.Deep);
                if (list != null)
                    _stories = list.Select(l => new SleeperStory { pawnId = l.pawnId, stage = l.stage, dueTick = l.dueTick }).ToList();
            }
        }

        public bool IsSleeperStoryActive(Pawn pawn)
        {
            return _stories.Any(s => s.pawnId == pawn.thingIDNumber);
        }
    }
} 