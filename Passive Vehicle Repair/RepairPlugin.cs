using AutomaticVehicleRepair.Upgrades;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Microsoft.SqlServer.Server;
using Nautilus.Handlers;
using Nautilus.Options.Attributes;
using UnityEngine;
using static RootMotion.FinalIK.GenericPoser;
using Story;

namespace AutomaticVehicleRepair
{
    // TODO Review this file and update to your own requirements.

    [BepInPlugin(MyGUID, PluginName, VersionString)]
    public class RepairPlugin : BaseUnityPlugin
    {
        private const string MyGUID = "Marslandare.AutomaticVehicleRepair";
        private const string PluginName = "AutomaticVehicleRepair";
        private const string VersionString = "1.0.0";

        private static readonly Harmony Harmony = new Harmony(MyGUID);
        public static ManualLogSource Log = new ManualLogSource(PluginName);
        internal static Options config { get; } = OptionsPanelHandler.RegisterModOptions<Options>();

        /// <summary>
        /// Initialise the configuration settings and patch methods
        /// </summary>
        private void Awake()
        {
            // Apply all of our patches
            Logger.LogInfo($"PluginName: {PluginName}, VersionString: {VersionString} is loading...");
            Harmony.PatchAll();
            Logger.LogInfo($"PluginName: {PluginName}, VersionString: {VersionString} is loaded.");

            AR_Prefab_CyclopsRepairModule.Register();
            AR_Prefab_VehicleRepairModule.Register();

            StoryGoalHandler.RegisterOnGoalUnlockData("AR_UnlockRepairUpgrades", blueprints: new Story.UnlockBlueprintData[]
            {
                new Story.UnlockBlueprintData() {techType = AR_Prefab_VehicleRepairModule.info.TechType, unlockType = Story.UnlockBlueprintData.UnlockType.Available},
                new Story.UnlockBlueprintData() {techType = AR_Prefab_CyclopsRepairModule.info.TechType, unlockType = Story.UnlockBlueprintData.UnlockType.Available},
            });

            CraftDataHandler.SetEquipmentType(AR_Prefab_CyclopsRepairModule.info.TechType, EquipmentType.CyclopsModule);

            // Sets up our static Log, so it can be used elsewhere in code.
            // .e.g.
            // Passive_Vehicle_RepairPlugin.Log.LogDebug("Debug Message to BepInEx log file");
            Log = Logger;
        }

        public enum RepairTypes 
        {
            Passive,
            Toggle,
            Upgrade
        }

        [Menu("Automatic Repair")]
        public class Options : Nautilus.Json.ConfigFile
        {
            [Choice("Repair Type", Tooltip = "MODES: Passive: Automatically repairs your vehicles. Toggle: Same but allows you toggle repairing on and off. Upgrade: Vehicles will only repair themselves when you've installed repair upgrades, using this option also unlocks the upgrades.")]
            public RepairTypes repairType = RepairTypes.Passive;

            [Keybind("Toggle Key", Tooltip = "You can bind a key to toggle automatic repair (requires toggle repair type)")]
            public KeyCode toggleKey = KeyCode.R;

            [Slider("Hull Points per Repair", DefaultValue = 1f, Min = 1f, Max = 10f, Step = 0.25f, Tooltip = "How much vehicle health that will be restored per repair")]
            public float healthPerHeal = 1f;

            [Slider("Repairing Interval", Format = "{0:F1}", DefaultValue = 0.4f, Min = 0.1f, Max = 5f, Step = 0.1f, Tooltip = "How often vehicles will self-repair")]
            public float healInterval = 0.4f;

            [Slider("Repairing Energy Cost", Format = "{0:F1}%", DefaultValue = 0.5f, Min = 0f, Max = 8f, Step = 0.1f, Tooltip = "How many % of energy that will be used when self-repairing.")]
            public float healCost = 0.5f;

            [Slider("Minimum Energy Requirement", Format = "{0:F1}%", DefaultValue = 20f, Min = 0f, Max = 70f, Step = 2.5f, Tooltip = "Minimum % of energy required to allow self-repairing")]
            public float energyRequirement = 20f;

            [Slider("Stunned Period", DefaultValue = 5f, Min = 0f, Max = 20f, Step = 1f, Tooltip = "After a vehicle has taken damage the repair system will be stunned for this long (does not affect upgrades)")]
            public float stunnedTime = 5f;

            [Toggle("Play Repair Sound")]
            public bool repairSound = true;

            [Toggle("Works below Crush-Depth")]
            public bool crushDepth = false;

            [Toggle("Works for Modded Vehicles")]
            public bool moddedVehicles = true;

            [Toggle("Works on Large Submarines", Tooltip = "When enabled, submarines will send out a repair drone to repair damages.")]
            public bool submarines = true;

            [Slider("Drone Repair Strength", Format = "{0:F1}%", DefaultValue = 10f, Min = 0.5f, Max = 20f, Step = 0.5f, Tooltip = "How many % of health that will be restored to a breach every second when repairing.")]
            public float breachHealthPerHeal = 10f;

            [Slider("Drone Repair Cost", DefaultValue = 4f, Min = 0f, Max = 10f, Step = 0.5f, Tooltip = "How much energy that will be used every second when repairing a breach.")]
            public float breachEnergyCost = 4f;

            [Toggle("Show Drone Notifications")]
            public bool notifications = true;
        }
    }
}
