using System.Collections.Generic;
using RimWorld;
using Verse;
using System.Linq;

namespace FogOfPawn
{
    public static class FogUtility
    {
        public static bool RevealRandomFoggedAttribute(Pawn pawn, bool preferSkill = true)
        {
            if (pawn == null || pawn.Destroyed) return false;
            var comp = pawn.GetComp<CompPawnFog>();
            if (comp == null || !comp.compInitialized) return false;

            var settings = FogSettingsCache.Current;
            List<System.Action> candidates = new();

            // Skills
            if (settings.fogSkills)
            {
                foreach (var sk in pawn.skills.skills)
                {
                    if (!comp.revealedSkills.Contains(sk.def))
                    {
                        if ((preferSkill && settings.allowSocialSkillReveal) || (!preferSkill && settings.allowPassiveSkillReveal))
                        {
                            candidates.Add(() => comp.RevealSkill(sk.def));
                        }
                    }
                }
            }

            // Traits
            if (settings.fogTraits && pawn.story?.traits != null)
            {
                foreach (var tr in pawn.story.traits.allTraits)
                {
                    if (!comp.revealedTraits.Contains(tr.def))
                    {
                        if ((preferSkill && settings.allowSocialTraitReveal) || (!preferSkill && settings.allowPassiveTraitReveal))
                        {
                            var captured = tr; // closure copy
                            candidates.Add(() => comp.RevealTrait(captured));
                        }
                    }
                }
            }

            if (candidates.Count == 0) return false;

            candidates.RandomElement()();
            return true;
        }

        public static void TriggerFullReveal(Pawn pawn, string reasonKey)
        {
            if (pawn == null) return;

            var comp = pawn.GetComp<CompPawnFog>();
            if (comp == null) return;

            if (comp.fullyRevealed) return;

            comp.RevealAll();
            comp.fullyRevealed = true;

            // Force disguise kit logic (Scammer only)
            comp.GetType().GetMethod("MaybeDropDisguiseKit", System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)?.Invoke(comp, null);

            // Send letter
            string labelKey = $"Fog.FullReveal.{reasonKey}.Label";
            string textKey  = $"Fog.FullReveal.{reasonKey}.Text";
            string label = labelKey.Translate(pawn.Named("PAWN"));
            string text  = textKey.Translate(pawn.Named("PAWN"));
            Find.LetterStack.ReceiveLetter(label, text, LetterDefOf.PositiveEvent, pawn);

            // Reputation damage for scammer
            if (comp.tier == DeceptionTier.DeceiverScammer)
            {
                var thought = DefDatabase<ThoughtDef>.GetNamedSilentFail("Fog_ScammerRevealed_Betrayed");
                if (thought != null)
                {
                    foreach (var other in pawn.MapHeld?.mapPawns?.FreeColonistsSpawned ?? Enumerable.Empty<Pawn>())
                    {
                        if (other == pawn) continue;
                        other.needs?.mood?.thoughts?.memories?.TryGainMemory(thought, pawn);
                    }
                }
            }

            FogLog.Verbose($"[FULL REVEAL] {pawn.LabelShort} ({comp.tier}) via {reasonKey}");
        }
    }
} 