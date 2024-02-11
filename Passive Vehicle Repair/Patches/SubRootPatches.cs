using AutomaticVehicleRepair.MonoBehaviours;
using AutomaticVehicleRepair.Upgrades;
using HarmonyLib;
using System.Collections.Generic;
using static VehicleUpgradeConsoleInput;

namespace AutomaticVehicleRepair.Patches
{
    // TODO Review this file and update to your own requirements, or remove it altogether if not required

    /// <summary>
    /// Sample Harmony Patch class. Suggestion is to use one file per patched class
    /// though you can include multiple patch classes in one file.
    /// Below is included as an example, and should be replaced by classes and methods
    /// for your mod.
    /// </summary>
    [HarmonyPatch(typeof(SubRoot))]
    public class SubRootPatches
    {
        private static List<SubRoot> subRoots = new List<SubRoot>();
        private static List<string> slots = new List<string>();

        [HarmonyPatch(nameof(SubRoot.OnSubModulesChanged))]
        [HarmonyPostfix]
        public static void OnSubModulesChanged_Postfix(SubRoot __instance, string slot, InventoryItem item)
        {
            if (item == null || item.techType != AR_Prefab_CyclopsRepairModule.info.TechType)
            {
                return;
            }

            // Remove Upgrade
            if (IsAnEnabledRepairModule(slot, __instance))
            {
                if (RepairUpgradesInSub(__instance) <= 1) 
                {
                    ToggleRepair(false, __instance);
                }
                slots.Remove(slot);
                subRoots.Remove(__instance);
                return;
            }

            // Add Upgrade
            else
            { 
                ToggleRepair(true, __instance);
                slots.Add(slot);
                subRoots.Add(__instance);
            }
        }

        private static bool IsAnEnabledRepairModule(string slot, SubRoot instance)
        {
            for(int i = 0; i < slots.Count; i++)
            {
                if (slot == slots[i] && instance == subRoots[i])
                {
                    return true;
                }
            }
            return false;
        }
        private static int RepairUpgradesInSub(SubRoot instance)
        {
            int num = 0;
            foreach(SubRoot subRoot in subRoots)
            {
                if (subRoot == instance)
                {
                    num++;
                }
            }
            return num;
        }
        private static void ToggleRepair(bool toggle, SubRoot subRoot)
        {
            if (subRoot != null)
            {
                LargeSubRepair subRepair = subRoot.gameObject.GetComponent<LargeSubRepair>();
                if (subRepair != null)
                {
                    subRepair.upgradeInstalled = toggle;
                }
            }
        }
    }
}