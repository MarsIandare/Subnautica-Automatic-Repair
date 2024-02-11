using UnityEngine;
using System.Collections;
using Nautilus.Extensions;
using Nautilus.Utility;

namespace AutomaticVehicleRepair.MonoBehaviours
{
    // TODO Review this file and update to your own requirements, or remove it altogether if not required
    /// <summary>
    /// Template MonoBehaviour class. Use this to add new functionality and behaviours to
    /// the game.
    /// </summary>
    internal class AutomaticVehicleRepairComp : MonoBehaviour
    {
        public Vehicle vehicle;
        public LiveMixin liveMixin;
        public EnergyInterface energyInterface;
        public CrushDamage crushDamage;
        public FMODASRPlayer weldSound;
        public SubRoot subRoot;

        private float energyPerPercentage;
        private float healthPerPercentage;
        private float timer = 0;
        private bool canShowNoPowerNotification = true;
        private bool active = true;
        private bool toggleKeyPressed = false;
        private float creationTime;
        public float lastDamage;
        private float lastHealth;
        private bool allowToggle = false;
        private float lastRepair;
        private bool isVanillaSub = false;

        public bool upgradeInstalled = false;

        public void Start()
        {
            creationTime = Time.time;
            liveMixin = vehicle.liveMixin;
            energyInterface = vehicle.gameObject.GetComponent<EnergyInterface>();
            crushDamage = vehicle.gameObject.GetComponent<CrushDamage>();

            if (RepairPlugin.config.repairType == RepairPlugin.RepairTypes.Upgrade)
            {
                active = upgradeInstalled;
            }
            
            int num = energyInterface.sources.Length;
            float totalEnergy = 0f;

            for (int i = 0; i < num; ++i)
            {
                if (energyInterface.sources[i] != null)
                {
                    totalEnergy += 200;
                }
            }

            energyPerPercentage = totalEnergy / 100;
            healthPerPercentage = liveMixin.maxHealth / 100;
            
            if (gameObject.name == "SeaMoth(Clone)" || gameObject.name == "Exosuit(Clone)")
            {
                isVanillaSub = true;
            }

            subRoot = gameObject.GetComponent<SubRoot>();

            UWE.CoroutineHost.StartCoroutine(CheckForRoot(gameObject, this));
            UWE.CoroutineHost.StartCoroutine(GetWelderSound(gameObject, this));
        }
        public void Update()
        {
            switch (RepairPlugin.config.repairType)
            {
                case RepairPlugin.RepairTypes.Passive:
                {
                        allowToggle = false;
                        SetActive(true);
                        break;
                }
                case RepairPlugin.RepairTypes.Toggle:
                {
                        allowToggle = true;
                        break;
                }
                case RepairPlugin.RepairTypes.Upgrade:
                    {
                        allowToggle = false;
                        bool inVehicle = Player.main.currentMountedVehicle == vehicle;
                        bool deactivated = inVehicle && GameInput.GetKeyUp(KeyCode.Mouse0) && Time.timeScale > 0;
                        if (deactivated)
                        {
                            upgradeInstalled = false;
                        }
                        else if (inVehicle && upgradeInstalled)
                        {
                            HandReticle main = HandReticle.main;
                            float healthFraction = liveMixin.GetHealthFraction();
                            bool powerd = EnoughPower();
                            bool crushDepth = BelowCrushDepth() && !RepairPlugin.config.crushDepth;
                            bool mod = isVanillaSub || RepairPlugin.config.moddedVehicles;
                            if (healthFraction < 1 && powerd && !crushDepth && mod)
                            {
                                main.SetProgress(healthFraction);
                                main.SetText(HandReticle.TextType.Hand, "Weld", true, GameInput.Button.None);
                                main.SetIcon(HandReticle.IconType.Progress, 1.5f);
                            }
                            else
                            {
                                if (!mod)
                                {
                                    main.SetText(HandReticle.TextType.Hand, "Modded Vehicle!", false, GameInput.Button.None);
                                    main.SetText(HandReticle.TextType.HandSubscript, "Allow repairing of modded vehicles in the mod's options.", false, GameInput.Button.None);
                                }
                                else if (!powerd)
                                {
                                    main.SetText(HandReticle.TextType.Hand, "Low Power!", false, GameInput.Button.None);
                                    main.SetText(HandReticle.TextType.HandSubscript, "Repairing Disabled!", false, GameInput.Button.None);
                                }
                                else
                                {
                                    if (crushDepth)
                                    {
                                        main.SetText(HandReticle.TextType.Hand, "Below Crush Depth!", false, GameInput.Button.None);
                                        main.SetText(HandReticle.TextType.HandSubscript, "Repairing Disabled!", false, GameInput.Button.None);
                                    }
                                    else
                                    {
                                        main.SetText(HandReticle.TextType.Hand, "Fully Repaired", false, GameInput.Button.None);
                                    }
                                }
                            }
                        }
                        SetActive(upgradeInstalled && inVehicle);
                        break; 
                    }
            }
            if (!isVanillaSub && !RepairPlugin.config.moddedVehicles)
            {
                ToggleSound(false);
                return;
            }

            // Toggle
            if (GameInput.GetKeyDown(RepairPlugin.config.toggleKey) && !toggleKeyPressed && allowToggle)
            {
                if (EnoughPower() && Player.main.currentMountedVehicle == vehicle)
                {
                    SetActive(!active);
                }
                toggleKeyPressed = true;
            }
            if (GameInput.GetKeyUp(RepairPlugin.config.toggleKey) && toggleKeyPressed)
            {
                toggleKeyPressed = false;
            }

            // Update heal interval if it was changed to a shorter interval.
            if (timer - Time.time >= RepairPlugin.config.healInterval)
            {
                timer = Time.time + RepairPlugin.config.healInterval;
                return;
            }

            if (timer <= Time.time)
            {
                timer = Time.time + RepairPlugin.config.healInterval;
                if (liveMixin != null && active && energyInterface != null)
                {
                    if (EnoughPower())
                    {
                        if (liveMixin.health != liveMixin.maxHealth && !HasTakenRecentDamage())
                        {
                            if (BelowCrushDepth() && !RepairPlugin.config.crushDepth)
                            {
                                ToggleSound(false);
                                return;
                            }

                            liveMixin.AddHealth(healthPerPercentage * RepairPlugin.config.healthPerHeal);
                            energyInterface.ConsumeEnergy(energyPerPercentage * RepairPlugin.config.healCost);
                            canShowNoPowerNotification = true;
                            lastRepair = Time.time;
                            if (weldSound != null && RepairPlugin.config.repairSound)
                            {
                                if (liveMixin.health != liveMixin.maxHealth)
                                {
                                    weldSound.Play();
                                }
                                else
                                {
                                    weldSound.Stop();
                                }
                            }
                        }
                        else
                        {
                            ToggleSound(false);
                        }
                    }
                    else if (canShowNoPowerNotification && creationTime <= Time.time - 1)
                    {
                        ErrorMessage.AddMessage("Low energy levels detected, automatic reparing now offline.");
                        canShowNoPowerNotification = false;
                        ToggleSound(false);
                    }
                }
                else
                {
                    ToggleSound(false);
                }
            }

            if (Time.time >= lastRepair + 2f || !RepairPlugin.config.repairSound || Player.main.currentMountedVehicle != vehicle)
            {
                ToggleSound(false);
            }

            // Update last damage for stun period.
            if (liveMixin != null)
            {
                if (liveMixin.health <= lastHealth && liveMixin.health != lastHealth)
                {
                    lastDamage = Time.time;
                }
                lastHealth = liveMixin.health;
            }
        }
        
        public void SetActive(bool flag)
        {
            if (flag == active)
                return;

            active = flag;

            if (Player.main.currentMountedVehicle == vehicle && RepairPlugin.config.repairType != RepairPlugin.RepairTypes.Upgrade)
            {
                if (active)
                {
                    ErrorMessage.AddMessage("Repairing now online.");
                }
                else
                {
                    ErrorMessage.AddMessage("Repairing now offline.");
                    ToggleSound(false);
                }
            }
        }
        public float EnergyRemaining(EnergyMixin[] sources)
        {
            int num = sources.Length;
            float energyPercentage = 0f;
            for (int i = 0; i < num; ++i)
            {
                if (sources[i] != null)
                {
                    energyPercentage += sources[i].GetEnergyScalar() / sources.Length;
                }
            }
            return energyPercentage;
        }

        private bool EnoughPower()
        {
            if (energyInterface != null)
            {
                if (EnergyRemaining(energyInterface.sources) >= RepairPlugin.config.energyRequirement / 100)
                {
                    return true;
                }
            }
            return false;
        }
        private void ToggleSound(bool toggle)
        {
            if (weldSound != null)
            {
                if (toggle)
                {
                    weldSound.Play();
                }
                else
                {
                    weldSound.Stop();
                }
            }
        }
        private bool BelowCrushDepth()
        {
            return crushDamage.GetCanTakeCrushDamage() && crushDamage.GetDepth() > crushDamage.crushDepth;
        }
        private bool HasTakenRecentDamage()
        {
            return Time.time <= lastDamage + RepairPlugin.config.stunnedTime;
        }
        private static IEnumerator GetWelderSound(GameObject parent, AutomaticVehicleRepairComp repairComp)
        {
            CoroutineTask<GameObject> task = CraftData.GetPrefabForTechTypeAsync(TechType.Welder);
            yield return task;
            GameObject gameObjectPrefab = task.GetResult();
            FMODASRPlayer orgWeldSound = gameObjectPrefab.GetComponent<Welder>().weldSound;

            repairComp.weldSound = parent.AddComponent<FMODASRPlayer>();
            FMOD_StudioEventEmitter startSound = parent.AddComponent<FMOD_StudioEventEmitter>();
            startSound.CopyComponent(orgWeldSound.startLoopSound);

            repairComp.weldSound.startLoopSound = startSound;
        }

        private static IEnumerator CheckForRoot(GameObject root, AutomaticVehicleRepairComp repairComp)
        {
            yield return new WaitForSeconds(2f);

            SubRoot subRoot = root.GetComponent<SubRoot>();

            if (subRoot != null && subRoot.damageManager != null)
            {
                repairComp.enabled = false;
            }
        }
    }
}
