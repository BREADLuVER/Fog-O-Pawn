using RimWorld;
using UnityEngine;
using Verse;

namespace FogOfPawn
{
    public static class FogInitializer
    {
        public static void InitializeFogFor(Pawn pawn)
        {
            var comp = pawn.GetComp<CompPawnFog>();
            if (comp == null || comp.compInitialized)
            {
                return;
            }

            // Set a random "truthfulness" baseline for this pawn
            comp.truthfulness = Rand.Range(0f, 1f);

            // Initialize skills and passions
            InitializeSkills(pawn, comp);

            // Mark as initialised so we don't do this again
            comp.compInitialized = true;

            Log.Message($"[FogOfPawn] Initialized fog for {pawn.NameShortColored}. Truthfulness: {comp.truthfulness:P0}");
        }

        private static void InitializeSkills(Pawn pawn, CompPawnFog comp)
        {
            var settings = FogSettingsCache.Current;
            if (!settings.fogSkills) return;

            float chaos = Mathf.Clamp01(settings.deceptionIntensity);

            foreach (var skill in pawn.skills.skills)
            {
                // Determine deception type based on pawn truthfulness and global chaos factor.
                float roll = Rand.Value;
                float truthfulBias = Mathf.Lerp(0.7f, 0.1f, chaos); // 0.7 when chaos=0, 0.1 when chaos=1

                bool accurate     = roll < (truthfulBias * comp.truthfulness + 0.05f);
                bool deceptive = !accurate; // any form of lie

                bool exaggerated = false;
                bool understated = false;
                bool faked = false;

                if (deceptive)
                {
                    // Split deceptive outcomes evenly unless chaos shifts towards faked claims
                    float lieRoll = Rand.Value;
                    float exaggerateCutoff = 0.45f - chaos * 0.15f; // fewer exaggerations at high chaos
                    float understateCutoff  = exaggerateCutoff + 0.45f; // roughly symmetry unless chaos pushes

                    if (lieRoll < exaggerateCutoff)
                        exaggerated = true;
                    else if (lieRoll < understateCutoff)
                        understated = true;
                    else
                        faked = true;
                }

                if (accurate)
                {
                    comp.reportedSkills[skill.def] = skill.Level;
                    comp.reportedPassions[skill.def] = skill.passion;
                }
                else if (exaggerated)
                {
                    // Exaggerate: +2 to +6 (range scales with chaos)
                    int bump = Rand.Range(2, chaos < 0.5f ? 4 : 6);
                    comp.reportedSkills[skill.def] = Mathf.Clamp(skill.Level + bump, 0, 20);
                    comp.reportedPassions[skill.def] = (skill.passion == Passion.None && Rand.Chance(0.5f)) ? Passion.Minor : skill.passion;
                }
                else if (understated)
                {
                    // Downplay: -2 to -4 but never below 0
                    int drop = Rand.Range(2, chaos < 0.5f ? 3 : 4);
                    comp.reportedSkills[skill.def] = Mathf.Clamp(skill.Level - drop, 0, 20);
                    // Passion may also be hidden (pretend None)
                    comp.reportedPassions[skill.def] = skill.passion == Passion.None ? Passion.None : (Rand.Chance(0.5f) ? Passion.None : skill.passion);
                }
                else // Faked
                {
                    if (skill.Level <= 3)
                    {
                        int fakeMin = chaos < 0.5f ? 4 : 6;
                        comp.reportedSkills[skill.def] = Rand.Range(fakeMin, 10);
                        comp.reportedPassions[skill.def] = Rand.Chance(0.75f) ? Passion.Minor : Passion.None;
                    }
                    else
                    {
                        comp.reportedSkills[skill.def] = skill.Level;
                        comp.reportedPassions[skill.def] = skill.passion;
                    }
                }
            }
        }
    }
} 