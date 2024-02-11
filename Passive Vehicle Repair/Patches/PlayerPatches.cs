using HarmonyLib;
using Nautilus.Handlers;
using Story;
using UnityEngine;

namespace AutomaticVehicleRepair.Patches
{
    [HarmonyPatch(typeof(Player))]
    internal class PlayerPatches
    {
        /// <summary>
        /// Here we add the upgrade bluprints if the repair mode is set to upgrade.
        /// </summary>
        /// <param name="__instance"></param>
        [HarmonyPatch(nameof(Player.Update))]
        [HarmonyPostfix]
        public static void Update_Postfix(Player __instance)
        {
            if (RepairPlugin.config.repairType == RepairPlugin.RepairTypes.Upgrade && !StoryGoalManager.main.IsGoalComplete("AR_UnlockRepairUpgrades") && Time.time > 20 && Time.timeScale > 0)
            {
                StoryGoal.Execute("AR_UnlockRepairUpgrades", Story.GoalType.Encyclopedia);
                Subtitles.Add("Downloading repair module blueprints to your databank...");
            }
        }
    }
}