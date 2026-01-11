using HarmonyLib;
using RimWorld;
using Verse;

namespace FogOfPawn.Patches
{
    /// <summary>
    /// Allows the disguise kit to be stored in storage zones by patching the storage validation
    /// </summary>
    [HarmonyPatch(typeof(StorageSettings), "AllowedToAccept", new[] { typeof(Thing) })]
    public static class Patch_Storage_DisguiseKit
    {
        public static void Postfix(Thing t, StorageSettings __instance, ref bool __result)
        {
            // If the storage settings would normally reject this item, but it's our disguise kit, allow it
            if (!__result && t?.def?.defName == "FogOfPawn_DisguiseKit")
            {
                __result = true;
            }
        }
    }

    /// <summary>
    /// Allows the disguise kit to be hauled to storage zones
    /// </summary>
    [HarmonyPatch(typeof(StoreUtility), "IsInValidStorage")]
    public static class Patch_StoreUtility_DisguiseKit
    {
        public static void Postfix(Thing t, ref bool __result)
        {
            // If the item is not in valid storage but it's our disguise kit, consider it valid
            if (!__result && t?.def?.defName == "FogOfPawn_DisguiseKit")
            {
                __result = true;
            }
        }
    }
} 