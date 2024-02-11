using Nautilus.Assets.Gadgets;
using Nautilus.Assets.PrefabTemplates;
using Nautilus.Assets;
using Nautilus.Crafting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutomaticVehicleRepair.MonoBehaviours;
using static ICSharpCode.SharpZipLib.Zip.ExtendedUnixData;

namespace AutomaticVehicleRepair.Upgrades
{
    public static class AR_Prefab_VehicleRepairModule
    {
        public static PrefabInfo info { get; private set; }
        public static void Register()
        {
            info = PrefabInfo.WithTechType("VehicleRepairModule", "Vehicle Repair Module", "Allows repairing of your vehicle while in the comfort of your seat.").WithIcon(SpriteManager.Get(TechType.Welder));

            CustomPrefab prefab = new CustomPrefab(info);

            PrefabTemplate cloneTemp = new CloneTemplate(info, TechType.SeamothSonarModule);
            prefab.SetGameObject(cloneTemp);


            RecipeData recipe = new RecipeData(
                new CraftData.Ingredient(TechType.Welder, 1),
                new CraftData.Ingredient(TechType.CopperWire, 1)
            );

            CraftingGadget crafting = prefab.SetRecipe(recipe)
                .WithFabricatorType(CraftTree.Type.SeamothUpgrades)
                .WithStepsToFabricatorTab("CommonModules")
                .WithCraftingTime(7.5f);

            prefab.SetVehicleUpgradeModule(EquipmentType.VehicleModule, QuickSlotType.Selectable)
                .WithEnergyCost(0f)
                .WithOnModuleUsed((Vehicle inst, int slotID, float charge, float chargeScalar) =>
                {
                    // This works diffrently for the prawn suit, check ExosuitPatches for more info.
                    ToggleRepair(true, inst);
                })
                .WithOnModuleRemoved((Vehicle inst, int slotID) =>
                {
                    ToggleRepair(false, inst);
                })
                ;
            prefab.SetPdaGroupCategory(TechGroup.VehicleUpgrades, TechCategory.VehicleUpgrades);

            prefab.Register();
        }
        private static void ToggleRepair(bool toggle, Vehicle inst)
        {
            AutomaticVehicleRepairComp repairComp = inst.GetComponent<AutomaticVehicleRepairComp>();

            if (repairComp != null && repairComp.enabled)
            {
                repairComp.upgradeInstalled = toggle;
                if (toggle)
                    repairComp.lastDamage = 0;
            }
            else
            {
                LargeSubRepair largeSubRepair = inst.GetComponent<LargeSubRepair>();
                if (largeSubRepair != null && largeSubRepair.enabled)
                {
                    largeSubRepair.upgradeInstalled = toggle;
                }
                else
                {
                    string message = "Warning! Could not find a vehicle repair component in your vehicle! The module will not work!";
                    Subtitles.Add(message);
                    RepairPlugin.Log.LogWarning(message);
                }
            }
        }
    }
}
