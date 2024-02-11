using HarmonyLib;
using AutomaticVehicleRepair.MonoBehaviours;

namespace AutomaticVehicleRepair.Patches
{
    // TODO Review this file and update to your own requirements, or remove it altogether if not required

    /// <summary>
    /// Sample Harmony Patch class. Suggestion is to use one file per patched class
    /// though you can include multiple patch classes in one file.
    /// Below is included as an example, and should be replaced by classes and methods
    /// for your mod.
    /// </summary>
    [HarmonyPatch(typeof(Vehicle))]
    internal class VehiclePatches
    {
        /// <param name="__instance"></param>
        [HarmonyPatch(nameof(Vehicle.Start))]
        [HarmonyPostfix]
        public static void Start_Postfix(Vehicle __instance)
        {
            RepairPlugin.Log.LogInfo("Adding Automatic Repair Component to " + __instance.gameObject.name);

            AutomaticVehicleRepairComp repairComp = __instance.gameObject.AddComponent<AutomaticVehicleRepairComp>();
            repairComp.vehicle = __instance;

            RepairPlugin.Log.LogInfo("Added Automatic Repair Component to " + __instance.gameObject.name);
        }
    }

    [HarmonyPatch(typeof(SubRoot))]
    internal class CyclopsPatches
    {
        /// <param name="__instance"></param>
        [HarmonyPatch(nameof(SubRoot.Start))]
        [HarmonyPostfix]
        public static void Start_Postfix(SubRoot __instance)
        {
            RepairPlugin.Log.LogInfo("Adding Large Sub Repair Component to " + __instance.gameObject.name);

            if (!__instance.GetComponent<LargeSubRepair>())
            {
                if (__instance.damageManager != null)
                {
                    LargeSubRepair repairComp = __instance.gameObject.AddComponent<LargeSubRepair>();
                    repairComp.root = __instance;

                    RepairPlugin.Log.LogInfo("Added Large Sub Repair Component to " + __instance.gameObject.name);
                    return;
                }
                else
                {
                    RepairPlugin.Log.LogWarning("Could not find damageManager in " + __instance.gameObject.name);
                }
            }
        }
    }
}