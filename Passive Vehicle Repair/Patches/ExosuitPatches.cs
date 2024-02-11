using AutomaticVehicleRepair.MonoBehaviours;
using AutomaticVehicleRepair.Upgrades;
using HarmonyLib;

namespace AutomaticVehicleRepair.Patches
{
    [HarmonyPatch(typeof(Exosuit))]
    internal class ExosuitPatches
    {
        /// <summary>
        /// Without this patch we aren't able to use the repair upgrade in a prawn suit.
        /// </summary>
        /// <param name="__instance"></param>
        [HarmonyPatch(nameof(Exosuit.SlotLeftDown))]
        [HarmonyPrefix]
        public static bool SlotLeftDown_Prefix(Exosuit __instance)
        {
            if (__instance.ignoreInput || __instance.activeSlot < 0)
            {
                return true;
            }
            TechType techType;
            if (__instance.GetQuickSlotType(__instance.activeSlot, out techType) == QuickSlotType.Selectable && __instance.ConsumeEnergy(techType) && techType == AR_Prefab_VehicleRepairModule.info.TechType)
            {
                AutomaticVehicleRepairComp repairComp = __instance.GetComponent<AutomaticVehicleRepairComp>();
                if (repairComp != null)
                {
                    repairComp.lastDamage = 0;
                    repairComp.upgradeInstalled = true;
                }
                return false;
            }

            return true;
        }
    }
}