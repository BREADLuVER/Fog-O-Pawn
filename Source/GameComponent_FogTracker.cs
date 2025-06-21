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

        // Add joiner tracking fields and methods
        public bool hasSleeperJoiner;
        public bool hasImposterJoiner;
        public int totalFogJoiners;
        public int lastFogJoinerTick = -999999;

        // Constants
        private const int TicksPerDay = 60000;
        private const int GuaranteeDay = 90; // 1.5 years (60 days per year)
        private const int SpacingDays = 20;
        private const int OversaturationWindowDays = 120; // first 2 years

        public static GameComponent_FogTracker Get => Current.Game.GetComponent<GameComponent_FogTracker>();

        /// <summary>
        /// Determine if a new fogged joiner of the requested tier should be allowed to fire now.
        /// Implements guarantee, spacing and global-limit rules.
        /// </summary>
        public bool CanFireFoggedJoiner(DeceptionTier tier)
        {
            int now = Find.TickManager.TicksGame;

            // Prevent more than 2 fogged pawns in the first two years (120 days)
            if ((now < OversaturationWindowDays * TicksPerDay) && totalFogJoiners >= 2)
                return false;

            // Ensure at least 20 days between fogged joiners
            if (lastFogJoinerTick > 0 && (now - lastFogJoinerTick) < SpacingDays * TicksPerDay)
                return false;

            // Determine base chance according to the time curve
            float daysPassed = now / (float)TicksPerDay;
            bool isSleeper = tier == DeceptionTier.DeceiverSleeper;

            float chance = 0f;
            if (daysPassed <= 45f)
                chance = 0f;
            else if (daysPassed <= 90f)
                chance = isSleeper ? 0.10f : 0.20f;
            else if (daysPassed <= 150f)
                chance = 0.30f;
            else
                chance = 0.50f;

            // Hard guarantee after 90 days if a storyline hasn't happened yet.
            if (!hasSleeperJoiner && isSleeper && daysPassed >= GuaranteeDay)
                chance = 1f;
            if (!hasImposterJoiner && !isSleeper && daysPassed >= GuaranteeDay)
                chance = 1f;

            // Diminishing probability after we already have two fogged pawns overall
            if (totalFogJoiners >= 2 && Rand.Chance(0.5f))
                return false;

            return Rand.Chance(chance);
        }

        public void RegisterFoggedJoiner(DeceptionTier tier)
        {
            totalFogJoiners++;
            lastFogJoinerTick = Find.TickManager.TicksGame;
            if (tier == DeceptionTier.DeceiverSleeper)
                hasSleeperJoiner = true;
            else if (tier == DeceptionTier.DeceiverImposter)
                hasImposterJoiner = true;
        }

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

            // First, process any imposters that have been killed, banished or otherwise removed.
            CheckImposterOutcomes();

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
                    AddSleeperAscensionTrait(pawn);
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

        private static void SendSleeperLetter(Pawn pawn, string ruleDefName)
        {
            string label = "Strange Incident";

            string baseKey = ruleDefName + ".Text";
            TaggedString textTS = baseKey.Translate(pawn.Named("PAWN"));
            string text = textTS;

            // Check for numbered variants ( .Text.1 .. .Text.5 ) and randomly pick one if they exist.
            var variants = new List<string>();
            for (int i = 1; i <= 5; i++)
            {
                string vKey = baseKey + "." + i;
                if (Verse.Translator.CanTranslate(vKey))
                {
                    variants.Add(vKey.Translate(pawn.Named("PAWN")));
                }
            }
            if (variants.Count > 0)
            {
                text = variants.RandomElement();
            }
            else if (textTS.RawText.Contains(".Text"))
            {
                text = ruleDefName; // fallback label
            }

            Find.LetterStack.ReceiveLetter(label, text, LetterDefOf.NeutralEvent, pawn);
            FogLog.Verbose($"[SleeperLetter] Sent {ruleDefName} for {pawn.LabelShort}");
        }

        public override void ExposeData()
        {
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                var list = _stories.Select(s => new SleeperStoryScribe(s)).ToList();
                Scribe_Collections.Look(ref list, "sleeperStories", LookMode.Deep);
                Scribe_Values.Look(ref hasSleeperJoiner, "hasSleeperJoiner");
                Scribe_Values.Look(ref hasImposterJoiner, "hasImposterJoiner");
                Scribe_Values.Look(ref totalFogJoiners, "totalFogJoiners");
                Scribe_Values.Look(ref lastFogJoinerTick, "lastFogJoinerTick");
            }
            else if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                List<SleeperStoryScribe> list = null;
                Scribe_Collections.Look(ref list, "sleeperStories", LookMode.Deep);
                if (list != null)
                    _stories = list.Select(l => new SleeperStory { pawnId = l.pawnId, stage = l.stage, dueTick = l.dueTick }).ToList();
                Scribe_Values.Look(ref hasSleeperJoiner, "hasSleeperJoiner");
                Scribe_Values.Look(ref hasImposterJoiner, "hasImposterJoiner");
                Scribe_Values.Look(ref totalFogJoiners, "totalFogJoiners");
                Scribe_Values.Look(ref lastFogJoinerTick, "lastFogJoinerTick");
            }
        }

        public bool IsSleeperStoryActive(Pawn pawn)
        {
            return _stories.Any(s => s.pawnId == pawn.thingIDNumber);
        }

        private static void AddSleeperAscensionTrait(Pawn pawn)
        {
            if (pawn?.story?.traits == null) return;
            // Define pool of strong traits
            List<TraitDef> pool = new()
            {
                DefDatabase<TraitDef>.GetNamedSilentFail("Tough"),
                DefDatabase<TraitDef>.GetNamedSilentFail("Jogger"),
                DefDatabase<TraitDef>.GetNamedSilentFail("TriggerHappy"),
                DefDatabase<TraitDef>.GetNamedSilentFail("Optimist"),
                DefDatabase<TraitDef>.GetNamedSilentFail("IronWilled"),
                DefDatabase<TraitDef>.GetNamedSilentFail("Sanguine"),
                DefDatabase<TraitDef>.GetNamedSilentFail("Bloodlust"),
                DefDatabase<TraitDef>.GetNamedSilentFail("Nimble")
            };

            foreach (var td in pool.InRandomOrder())
            {
                if (td == null) continue;
                if (!pawn.story.traits.HasTrait(td))
                {
                    pawn.story.traits.GainTrait(new Trait(td));
                    break;
                }
            }
        }

        public void DevAdvanceSleeperStory(Pawn pawn)
        {
            if (pawn == null) return;
            for (int i = 0; i < _stories.Count; i++)
            {
                if (_stories[i].pawnId != pawn.thingIDNumber) continue;
                var entry = _stories[i];
                entry.dueTick = Find.TickManager.TicksGame - 1; // make it overdue
                _stories[i] = entry;
                ProcessStoryImmediate(pawn);
                return;
            }
            // Not found – start story and send first suspicion letter immediately
            StartSleeperStory(pawn);
            FogLog.Verbose($"[Dev] Started sleeper story for {pawn.LabelShort}");
        }

        private void ProcessStoryImmediate(Pawn pawn)
        {
            // Run the same logic as tick but only for this pawn
            for (int i = _stories.Count - 1; i >= 0; i--)
            {
                var entry = _stories[i];
                if (entry.pawnId != pawn.thingIDNumber) continue;

                if (Find.TickManager.TicksGame < entry.dueTick) continue;

                if (entry.stage == 1)
                {
                    SendSleeperLetter(pawn, "Fog_SleeperReveal_Suspicion");
                    entry.stage = 2;
                    entry.dueTick = Find.TickManager.TicksGame;
                    _stories[i] = entry;
                }
                else if (entry.stage == 2)
                {
                    SendSleeperLetter(pawn, "Fog_SleeperReveal_Anomaly");
                    entry.stage = 3;
                    entry.dueTick = Find.TickManager.TicksGame;
                    _stories[i] = entry;
                }
                else if (entry.stage == 3)
                {
                    AddSleeperAscensionTrait(pawn);
                    FogUtility.TriggerFullReveal(pawn, "SleeperStory");
                    _stories.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Grants a positive mood memory to all colonists once an imposter has been removed from the colony – by death, exile or banishment.
        /// </summary>
        private static void CheckImposterOutcomes()
        {
            ThoughtDef reliefThought = DefDatabase<ThoughtDef>.GetNamedSilentFail("Fog_ImposterNeutralized_Relief");
            if (reliefThought == null) return;

            // Build a candidate list containing:
            //   • all world pawns currently alive (used for banished imposters)
            //   • every pawn present on any map (alive or dead – covers killed imposters prior to being discarded)
            var candidates = new List<Pawn>();
            candidates.AddRange(Find.WorldPawns.AllPawnsAlive);
            foreach (var map in Find.Maps)
            {
                candidates.AddRange(map.mapPawns.AllPawns);
            }

            foreach (var pawn in candidates.Distinct())
            {
                var comp = pawn.GetComp<CompPawnFog>();
                if (comp == null) continue;
                if (comp.tier != DeceptionTier.DeceiverImposter) continue;
                if (comp.outcomeProcessed) continue;

                bool neutralized = pawn.Dead || pawn.Destroyed;

                // Banished or exiled pawns keep player faction but are no longer free colonists.
                if (!neutralized)
                {
                    neutralized = !pawn.IsFreeColonist;
                }

                FogLog.Verbose($"[CHECK] {pawn.LabelShort}: alive={ !pawn.Dead } faction={(pawn.Faction?.ToString()??"null")} player={(pawn.Faction?.IsPlayer==true)} neutralized={neutralized}");

                if (!neutralized) continue;

                // Give mood buff to every current colonist (maps + caravans)
                foreach (var map in Find.Maps)
                {
                    foreach (var col in map.mapPawns.FreeColonistsSpawned)
                    {
                        if (col.needs?.mood?.thoughts?.memories != null)
                        {
                            col.needs.mood.thoughts.memories.TryGainMemory(reliefThought, pawn);
                            FogLog.Verbose($"[MEMORY] {col.LabelShort} gained Relief memory because {pawn.LabelShort} neutralized.");
                        }
                    }
                }

                comp.outcomeProcessed = true;
                FogLog.Verbose($"[IMPOSTER REMOVED] Granted relief thought for {pawn.LabelShort}.");
            }
        }
    }
} 