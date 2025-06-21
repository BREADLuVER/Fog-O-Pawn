using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace DiceFramework
{
    /// <summary>
    /// Lightweight dice-rolling helpers (d20, percentile, etc.). No Fog-specific logic here so this file can be moved to its own mod.
    /// </summary>
    public static class DiceRoller
    {
        /// <summary>
        /// Roll <paramref name="dice"/> dice each with <paramref name="sides"/> sides.
        /// Returns a list of individual rolls; caller can sum or inspect for nat-20, etc.
        /// </summary>
        public static List<int> Roll(int dice, int sides)
        {
            if (dice <= 0 || sides <= 1) throw new ArgumentException("Invalid dice parameters");
            var list = new List<int>(dice);
            for (int i = 0; i < dice; i++)
                list.Add(Rand.RangeInclusive(1, sides));
            return list;
        }

        /// <summary>
        /// Convenience wrapper for 1d20.
        /// </summary>
        public static int D20() => Rand.RangeInclusive(1, 20);

        /// <summary>
        /// Roll 2d20 and keep the higher value (advantage).
        /// </summary>
        public static int D20Advantage()
        {
            int a = D20();
            int b = D20();
            return Math.Max(a, b);
        }

        /// <summary>
        /// Roll 2d20 and keep the lower value (disadvantage).
        /// </summary>
        public static int D20Disadvantage()
        {
            int a = D20();
            int b = D20();
            return Math.Min(a, b);
        }

        /// <summary>
        /// Perform a contested d20 check. Each side rolls 2d20 (advantage style) and adds a bonus.
        /// Returns +1 if A wins, -1 if B wins, 0 tie.
        /// </summary>
        public static int ContestedAdvantage(int bonusA, int bonusB)
        {
            int a = D20Advantage() + bonusA;
            int b = D20Advantage() + bonusB;
            return Math.Sign(a - b);
        }
    }
} 