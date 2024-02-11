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
    // Compared to the Vehicle Repair Module, the Cyclops Repair Module dosen't have much code in it, we just use it for the nautilus upgrade functionality and visuals.
    public static class AR_Prefab_CyclopsRepairModule
    {
        public static PrefabInfo info { get; private set; }
        public static void Register()
        {
            info = PrefabInfo.WithTechType("CyclopsRepairModule", "Repair-Drone Module", "Equips the submarine with a helpful repair drone that ventures out to fix leaks.").WithIcon(SpriteManager.Get(TechType.Welder));

            CustomPrefab prefab = new CustomPrefab(info);

            PrefabTemplate cloneTemp = new CloneTemplate(info, TechType.SeamothSonarModule);
            prefab.SetGameObject(cloneTemp);


            RecipeData recipe = new RecipeData(
                new CraftData.Ingredient(TechType.Welder, 1),
                new CraftData.Ingredient(TechType.CopperWire, 1),
                new CraftData.Ingredient(TechType.WiringKit, 1),
                new CraftData.Ingredient(TechType.Titanium, 2),
                new CraftData.Ingredient(TechType.Battery, 1)
            );

            CraftingGadget crafting = prefab.SetRecipe(recipe)
                .WithFabricatorType(CraftTree.Type.CyclopsFabricator)
                .WithCraftingTime(7.5f);

            prefab.SetVehicleUpgradeModule(EquipmentType.SeamothModule, QuickSlotType.Passive); // We overide this in the plugins file.
            prefab.SetPdaGroupCategory(TechGroup.Cyclops, TechCategory.CyclopsUpgrades);

            // You can find more code in SubRootPatches.

            prefab.Register();
        }
    }
}
