//using RimWorld;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Verse;

//namespace EventController_rQP
//{
//    [StaticConstructorOnStartup]
//    public static class StoryFilter_Init
//    {
//        public static readonly Dictionary<WorkTags, HashSet<TraitDef>> disabledWorkTagsTrait = [];
//        static StoryFilter_Init()
//        {
//            var traitDefs = DefDatabase<TraitDef>.AllDefs;
//            foreach (WorkTags workTag in Enum.GetValues(typeof(WorkTags)))
//            {
//                disabledWorkTagsTrait.Add(workTag, traitDefs.Where(t => (t.disabledWorkTags & workTag) != 0).ToHashSet());
//            }
//        }
//    }
//}
