using RimWorld;
using UnityEngine;
using Verse;
using System.Linq;
using HarmonyLib;

namespace FogOfPawn
{
    /// <summary>
    /// Utility helpers for reading skills that respect the fogged profile.
    /// All internal mod code should call <see cref="GetEffectiveSkill"/> instead of accessing
    /// <c>SkillRecord.Level</c> directly when specific behaviour is required.
    /// However, a Harmony patch is also applied to <c>SkillRecord.Level</c> so that the entire
    /// game – including external mods – automatically uses the masked value whenever appropriate.
    /// </summary>
    public static class EffectiveSkillUtility
    {
        /// <summary>
        /// Manually calculates the gene-modified skill level without triggering infinite recursion.
        /// This bypasses the Harmony patch by directly accessing gene data and calculating modifiers.
        /// </summary>
        public static int GetGeneModifiedSkillLevel(SkillRecord sr)
        {
            if (sr == null) return 0;

            // Start with the trained level (no genes)
            int baseLevel = sr.levelInt;

            // Get the pawn using reflection for cross-version compatibility
            Pawn pawn = null;
            try
            {
                pawn = AccessTools.Field(typeof(SkillRecord), "pawn")?.GetValue(sr) as Pawn;
                if (pawn == null)
                {
                    var prop = AccessTools.PropertyGetter(typeof(SkillRecord), "Pawn");
                    if (prop != null)
                        pawn = prop.Invoke(sr, null) as Pawn;
                }
            }
            catch (System.Exception ex)
            {
                FogLog.Verbose($"[GENE] Error accessing pawn from SkillRecord: {ex.Message}");
                return baseLevel;
            }

            if (pawn == null) return baseLevel;

            // Check if the pawn has genes (requires Biotech DLC)
            if (pawn.genes?.GenesListForReading == null)
                return baseLevel;

            try
            {
                int geneModifier = 0;
                
                // Iterate through all active genes
                foreach (var gene in pawn.genes.GenesListForReading)
                {
                    if (gene?.def?.aptitudes == null) continue;

                    // Check if this gene has aptitude modifiers for this skill
                    foreach (var aptitude in gene.def.aptitudes)
                    {
                        if (aptitude.skill == sr.def)
                        {
                            geneModifier += aptitude.level;
                            break; // Each gene can only modify a skill once
                        }
                    }
                }

                // Return the gene-modified level, clamped to valid range
                return Mathf.Clamp(baseLevel + geneModifier, 0, 20);
            }
            catch (System.Exception ex)
            {
                // If anything goes wrong with gene calculation, fall back to base level
                FogLog.Verbose($"[GENE] Error calculating gene modifiers for {pawn.LabelShort} {sr.def.label}: {ex.Message}");
                return baseLevel;
            }
        }

        /// <summary>
        /// Returns the effective (possibly masked) skill level for the given pawn.
        /// When the pawn is fogged and the skill has not yet been revealed this will
        /// return the masked value (real level + offset), otherwise the real level.
        /// </summary>
        public static int GetEffectiveSkill(Pawn pawn, SkillDef def)
        {
            if (pawn?.skills == null) return 0;

            var sr = pawn.skills.GetSkill(def);
            if (sr == null) return 0;

            // Get the gene-modified skill level (includes gene bonuses)
            int realLevel = GetGeneModifiedSkillLevel(sr);

            // If the pawn is not fogged, return the real gene-modified level
            var comp = pawn.GetComp<CompPawnFog>();
            if (comp == null || !comp.compInitialized)
            {
                return realLevel;
            }

            // Revealed skills always show the real value (including gene modifiers)
            if (comp.revealedSkills.Contains(def))
            {
                return realLevel;
            }

            // Apply mask offset if one exists
            if (comp.maskOffsets.TryGetValue(def, out int offset))
            {
                int maskedLevel = realLevel + offset;
                return Mathf.Clamp(maskedLevel, 0, 20);
            }

            // LEGACY: Support old format during migration
            if (comp.reportedSkills.TryGetValue(def, out var rep) && rep.HasValue)
            {
                int reportedLevel = Mathf.Clamp(Mathf.RoundToInt(rep.Value), 0, 20);
                return reportedLevel;
            }

            // No mask = truthful or unknown
            return comp.tier == DeceptionTier.Truthful ? realLevel : 0;
        }
        
        /// <summary>
        /// Returns the effective (possibly masked) passion for the given pawn and skill.
        /// </summary>
        public static Passion GetEffectivePassion(Pawn pawn, SkillDef def)
        {
            if (pawn?.skills == null) return Passion.None;

            var sr = pawn.skills.GetSkill(def);
            if (sr == null) return Passion.None;

            // Get the real current passion
            Passion realPassion = sr.passion;

            // If the pawn is not fogged, always return the real value
            var comp = pawn.GetComp<CompPawnFog>();
            if (comp == null || !comp.compInitialized)
            {
                return realPassion;
            }

            // Revealed skills always show the real passion
            if (comp.revealedSkills.Contains(def))
            {
                return realPassion;
            }

            // Apply passion offset if one exists
            if (comp.passionOffsets.TryGetValue(def, out int offset))
            {
                int newPassionLevel = (int)realPassion + offset;
                return (Passion)Mathf.Clamp(newPassionLevel, 0, 2);
            }

            // LEGACY: Support old format during migration
            if (comp.reportedPassions.TryGetValue(def, out var rep) && rep.HasValue)
            {
                return rep.Value;
            }

            // No mask = truthful
            return realPassion;
        }
        
        /// <summary>
        /// Test method to verify the gene integration and offset system work correctly (dev mode only)
        /// </summary>
        public static void TestGeneIntegration(Pawn pawn)
        {
            if (!Prefs.DevMode) return;
            
            var comp = pawn.GetComp<CompPawnFog>();
            if (comp == null) return;
            
            FogLog.Verbose($"=== GENE INTEGRATION TEST FOR {pawn.LabelShort} ===");
            FogLog.Verbose($"Tier: {comp.tier}");
            FogLog.Verbose($"Migrated: {comp.migratedToOffsets}");
            FogLog.Verbose($"Has genes: {pawn.genes?.GenesListForReading?.Count ?? 0} genes");
            
            foreach (var skill in pawn.skills.skills.Take(3)) // Test first 3 skills
            {
                int trainedLevel = skill.levelInt; // Raw trained level
                int geneModifiedLevel = GetGeneModifiedSkillLevel(skill); // With gene bonuses
                int effectiveLevel = GetEffectiveSkill(pawn, skill.def); // Final displayed level
                int offset = comp.maskOffsets.TryGetValue(skill.def, out int o) ? o : 0;
                bool revealed = comp.revealedSkills.Contains(skill.def);
                
                FogLog.Verbose($"  {skill.def.defName}: trained={trainedLevel}, genes={geneModifiedLevel}, effective={effectiveLevel}, offset={offset:+0;-0;+0}, revealed={revealed}");
                
                // Check for gene modifiers
                if (pawn.genes?.GenesListForReading != null)
                {
                    foreach (var gene in pawn.genes.GenesListForReading)
                    {
                        if (gene?.def?.aptitudes == null) continue;
                        
                        foreach (var aptitude in gene.def.aptitudes)
                        {
                            if (aptitude.skill == skill.def)
                            {
                                FogLog.Verbose($"    Gene {gene.def.defName}: {aptitude.level:+0;-0;+0} aptitude");
                                break;
                            }
                        }
                    }
                }
            }
            FogLog.Verbose($"=== END GENE INTEGRATION TEST ===");
        }
    }
} 