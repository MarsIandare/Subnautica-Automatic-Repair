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
        private float lastDamage;
        private float lastHealth;
        private bool allowToggle = false;
        private float lastRepair;
        private bool isVanillaSub = false;

        public void Start()
        {
            creationTime = Time.time;
            liveMixin = vehicle.liveMixin;
            energyInterface = vehicle.gameObject.GetComponent<EnergyInterface>();
            crushDamage = vehicle.gameObject.GetComponent<CrushDamage>();
            
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
            if (!isVanillaSub && !RepairPlugin.config.moddedVehicles)
            {
                ToggleSound(false);
                return;
            }

            switch (RepairPlugin.config.repairType)
            {
                case RepairPlugin.RepairTypes.Passive:
                {
                        allowToggle = false;
                        if (!active)
                        {
                            ErrorMessage.AddMessage("Automatic repairing now online.");
                        }
                        active = true; break;
                }
                case RepairPlugin.RepairTypes.Toggle:
                {
                        allowToggle = true;
                        break;
                }
            }
            if (GameInput.GetKeyDown(RepairPlugin.config.toggleKey) && !toggleKeyPressed && allowToggle)
            {
                if (EnoughPower() && Player.main.currentMountedVehicle == vehicle)
                {
                    active = !active;
                    if (active)
                    {
                        ErrorMessage.AddMessage("Automatic repairing now online.");
                    }
                    else
                    {
                        ErrorMessage.AddMessage("Automatic repairing now offline.");
                        ToggleSound(false);
                    }
                }
                toggleKeyPressed = true;
            }
            if (GameInput.GetKeyUp(RepairPlugin.config.toggleKey) && toggleKeyPressed)
            {
                toggleKeyPressed = false;
            }
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

            if (liveMixin != null)
            {
                if (liveMixin.health <= lastHealth && liveMixin.health != lastHealth)
                {
                    lastDamage = Time.time;
                }
                lastHealth = liveMixin.health;
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
