using Verse;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse.Grammar;

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

        // Year-2 joiner scheduling
        private const int TicksPerYear = TicksPerSeason * 4;
        private int _joinerDueTick = -1;
        private bool _joinerFired;

        private const int ExtraJoinerCooldown = 60000 * 3; // 3 in-game days
        private int _lastJoinerTick = -1;

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

            if (_stories.Count == 0 && !_joinerFired && Find.TickManager.TicksGame >= _joinerDueTick)
            {
                TryFireScheduledJoiner();
            }

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

        private static void SendSleeperLetter(Pawn pawn, string rulePackDefName)
        {
            RulePackDef rp = DefDatabase<RulePackDef>.GetNamedSilentFail(rulePackDefName);
            if (rp != null)
            {
                GrammarRequest req = new GrammarRequest();
                req.Includes.Add(rp);
                req.Rules.AddRange(GrammarUtility.RulesForPawn("PAWN", pawn));
                string text = GrammarResolver.Resolve("r_text", req);
                Find.LetterStack.ReceiveLetter("Suspicion", text, LetterDefOf.NeutralEvent, pawn);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();

            if (Scribe.mode == LoadSaveMode.Saving)
            {
                var list = _stories.Select(s => new SleeperStoryScribe(s)).ToList();
                Scribe_Collections.Look(ref list, "sleeperStories", LookMode.Deep);
                Scribe_Values.Look(ref _joinerDueTick, "joinerDue", -1);
                Scribe_Values.Look(ref _joinerFired, "joinerFired", false);
                Scribe_Values.Look(ref _lastJoinerTick, "lastJoin", -1);
            }
            else if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                List<SleeperStoryScribe> list = null;
                Scribe_Collections.Look(ref list, "sleeperStories", LookMode.Deep);
                if (list != null)
                    _stories = list.Select(l => new SleeperStory { pawnId = l.pawnId, stage = l.stage, dueTick = l.dueTick }).ToList();
                Scribe_Values.Look(ref _joinerDueTick, "joinerDue", -1);
                Scribe_Values.Look(ref _joinerFired, "joinerFired", false);
                Scribe_Values.Look(ref _lastJoinerTick, "lastJoin", -1);
            }
        }

        public bool IsSleeperStoryActive(Pawn pawn)
        {
            return _stories.Any(s => s.pawnId == pawn.thingIDNumber);
        }

        public override void FinalizeInit()
        {
            base.FinalizeInit();

            if (_joinerDueTick < 0)
            {
                _joinerDueTick = Find.TickManager.TicksGame + TicksPerYear; // start of second year
            }
        }

        private void TryFireScheduledJoiner()
        {
            // Choose map
            Map map = Find.AnyPlayerHomeMap;
            if (map == null) return;

            // Decide Sleeper or Scammer (60/40)
            bool sleeper = Rand.Chance(0.6f);
            string defName = sleeper ? "Fog_SleeperJoinIncident" : "Fog_ScammerJoinIncident";
            IncidentDef def = DefDatabase<IncidentDef>.GetNamedSilentFail(defName);
            if (def == null)
            {
                Log.Warning("[FogOfPawn] Scheduled joiner incident def missing: " + defName);
                return;
            }

            IncidentParms parms = StorytellerUtility.DefaultParmsNow(def.category, map);
            parms.target = map;
            bool ok = def.Worker.TryExecute(parms);
            Log.Message("[FogOfPawn] Scheduled joiner fired: " + defName + " result=" + ok);
            _joinerFired = true;
            _lastJoinerTick = Find.TickManager.TicksGame;
        }

        public void ForceImmediateJoiner(bool allowRepeat)
        {
            if (!allowRepeat && _joinerFired) return;
            _joinerDueTick = 0;
            _joinerFired = false;
        }

        public bool HasSpawnedJoiner() => _joinerFired;
        public bool HasPendingJoiner() => _joinerDueTick <= Find.TickManager.TicksGame;

        public bool CooldownPassed => _lastJoinerTick < 0 || Find.TickManager.TicksGame - _lastJoinerTick > ExtraJoinerCooldown;
    }
} 