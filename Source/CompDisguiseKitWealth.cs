using RimWorld;
using Verse;
using System.Collections.Generic;
using UnityEngine;

namespace FogOfPawn
{
    public class CompProperties_DisguiseKitWealth : CompProperties
    {
        public float wealthReductionPercent = 0.5f; 

        public CompProperties_DisguiseKitWealth()
        {
            compClass = typeof(CompDisguiseKitWealth);
        }
    }

    public class CompDisguiseKitWealth : ThingComp
    {
        public CompProperties_DisguiseKitWealth Props => (CompProperties_DisguiseKitWealth)props;

        public float GetWealthReductionMultiplier()
        {
            return 1f - Props.wealthReductionPercent;
        }

        public override string CompInspectStringExtra()
        {
            return "FogOfPawn.DisguiseKit.InspectString".Translate();
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (var g in base.CompGetGizmosExtra())
            {
                yield return g;
            }

            if (!(parent.ParentHolder is Pawn_ApparelTracker))
            {
                yield return new Command_Action
                {
                    defaultLabel = "FogOfPawn.DisguiseKit.DestroyLabel".Translate(),
                    defaultDesc = "FogOfPawn.DisguiseKit.DestroyDesc".Translate(),
                    icon = ContentFinder<Texture2D>.Get("UI/Designators/Cancel", true),
                    action = () =>
                    {
                        parent.Destroy();
                    }
                };
            }
        }
    }
} 