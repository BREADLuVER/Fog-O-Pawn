using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace EventController_rQP
{
    internal static class FactionFilter_Work
    {
        public static void FactionFilter(ref Pawn pawn, ref FactionDef factionType)
        {
            //Log.Message("O'FactionFilter: " + factionType + "、" + pawn.kindDef + "、" + pawn);
            var race = pawn.kindDef.race;
            var body = pawn.kindDef.RaceProps.body;
            if (pawn.Faction != null)
            {
                if (!pawn.Faction.IsPlayer && pawn.Faction.def == factionType
                    || factionType.modContentPack.IsOfficialMod
                    || factionType.isPlayer)
                {
                    return;
                }
                if (pawn.Faction.IsPlayer)
                {
                    var factions1 = GetValidFactions(race, body);
                    if (!factions1.Empty())
                    {
                        factionType = factions1.RandomElement();
                    }
                    return;
                }
            }
            var factionRaces = EventController_Work.GetFactionPawnRaces();
            var races = factionRaces.TryGetValue(factionType);
            var factionBodies = EventController_Work.GetFactionPawnBodies();
            var bodies = factionBodies.TryGetValue(factionType);
            if (races != null && bodies != null)
            {
                if (races.Contains(race) && bodies.Contains(body))
                {
                    //Log.Message("A'FactionFilter: " + factionType + "、" + race + "、" + body);
                    return;
                }
            }
            var factions = GetValidFactions(race, body);
            if (!factions.Empty())
            {
                factionType = factions.RandomElement();
            }
            else
            {
                factionType = EventController_Work.GetHumanlikeVanillaFactions().RandomElement();
            }
            //Log.Message("E'FactionFilter: " + factionType);
        }
        public static List<FactionDef> GetValidFactions(ThingDef race, BodyDef body)
        {
            var factionKinds = EventController_Work.GetFactionPawnKinds();
            List<FactionDef> factions = new();
            foreach (var faction in factionKinds.Keys)
            {
                foreach (var kind in factionKinds[faction])
                {
                    if (kind.race == race && kind.RaceProps.body == body)
                    {
                        factions.Add(faction);
                        break;
                    }
                }
            }
            return factions;
        }
        public static void ExcludeStoryCategories(Pawn pawn, ref List<BackstoryCategoryFilter> backstoryCategories, FactionDef factionType)
        {
            if (factionType != null)
            {
                if (factionType.defName.StartsWith("OG_Mi") && !pawn.kindDef.defName.StartsWith("OG_"))
                {
                    for (global::System.Int32 i = 0; i < backstoryCategories.Count; i++)
                    {
                        if (backstoryCategories[i].categories == null)
                        {
                            return;
                        }
                        else if (backstoryCategories[i].categories.Contains("Imperium_Soldier"))
                        {
                            backstoryCategories[i].categories = new List<string>() { "Imperium_Standard" };
                        }
                    }
                }
            }
        }
        public static void IncludeStoryCategories(Pawn pawn, BackstorySlot slot, ref List<BackstoryCategoryFilter> backstoryCategories)
        {
            if (backstoryCategories.Where(f => f.categories == null).Where(f => f.categoriesChildhood == null || f.categoriesAdulthood == null).Any())
            {
                var fallback = EventController_Work.GetFallbackBackstroy();
                for (global::System.Int32 i = 0; i < backstoryCategories.Count; i++)
                {
                    if (backstoryCategories[i].categories != null)
                    {
                        continue;
                    }
                    if (slot == BackstorySlot.Childhood && backstoryCategories[i].categoriesChildhood == null)
                    {
                        var childhood = IncludeStoryCategoriesInner(pawn);
                        if (childhood.Any())
                        {
                            backstoryCategories[i].categoriesChildhood = childhood.ToList();
                        }
                        else
                        {
                            backstoryCategories[i].categoriesChildhood = fallback.ToList();
                        }
                    }
                    if (slot == BackstorySlot.Adulthood && backstoryCategories[i].categoriesAdulthood == null)
                    {
                        var adulthood = IncludeStoryCategoriesInner(pawn);
                        if (adulthood.Any())
                        {
                            backstoryCategories[i].categoriesAdulthood = adulthood.ToList();
                        }
                        else
                        {
                            backstoryCategories[i].categoriesAdulthood = fallback.ToList();
                        }
                    }
                }
            }
        }
        public static HashSet<string> IncludeStoryCategoriesInner(Pawn pawn)
        {
            HashSet<string> categories = [];
            var validFactions = GetValidFactions(pawn.kindDef.race, pawn.RaceProps.body);
            var factionpawnkinds = EventController_Work.GetFactionPawnKinds();
            foreach (var faction in validFactions)
            {
                foreach (var kind in factionpawnkinds[faction])
                {
                    if (kind.backstoryCategories != null)
                    {
                        categories.UnionWith(kind.backstoryCategories);
                    }
                    if (kind.backstoryFilters != null)
                    {
                        foreach (var filter in kind.backstoryFilters)
                        {
                            categories.UnionWith(filter.categories);
                        }
                    }
                }
            }
            return categories;
        }
    }
}
