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
            foreach (var skill in pawn.skills.skills)
            {
                // Determine deception type based on truthfulness
                float roll = Rand.Value;
                float truthfulness = comp.truthfulness;

                // More truthful pawns are more likely to be accurate
                // Probabilities are placeholders, will be moved to ModSettings
                bool accurate = roll < (0.7f * truthfulness + 0.1f); // 10-80%
                bool exaggerated = !accurate && roll < 0.9f; // ~10-20%
                // bool faked = !accurate && !exaggerated;                // ~10-20%

                if (accurate)
                {
                    comp.reportedSkills[skill.def] = skill.Level;
                    comp.reportedPassions[skill.def] = skill.passion;
                }
                else if (exaggerated)
                {
                    // Exaggerate: +2 to +6
                    comp.reportedSkills[skill.def] = Mathf.Clamp(skill.Level + Rand.Range(2, 6), 0, 20);
                    // 50% chance to also fake a passion if there is none.
                    comp.reportedPassions[skill.def] = (skill.passion == Passion.None && Rand.Chance(0.5f)) ? Passion.Minor : skill.passion;
                }
                else // Faked
                {
                    // Fake: report 6-10, when actual is 0-3
                    if (skill.Level <= 3)
                    {
                        comp.reportedSkills[skill.def] = Rand.Range(6, 10);
                        comp.reportedPassions[skill.def] = Rand.Chance(0.75f) ? Passion.Minor : Passion.None;
                    }
                    else
                    {
                        // Can't "fake" a high skill, so just report accurately.
                        comp.reportedSkills[skill.def] = skill.Level;
                        comp.reportedPassions[skill.def] = skill.passion;
                    }
                }
            }
        }
    }
} 