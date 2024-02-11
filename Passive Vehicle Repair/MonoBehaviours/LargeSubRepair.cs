using Nautilus.Extensions;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AutomaticVehicleRepair.MonoBehaviours
{
    // TODO Review this file and update to your own requirements, or remove it altogether if not required
    /// <summary>
    /// Template MonoBehaviour class. Use this to add new functionality and behaviours to
    /// the game.
    /// </summary>
    internal class LargeSubRepair : MonoBehaviour
    {
        public LiveMixin liveMixin;
        public SubRoot root;
        public PowerRelay powerRelay;
        public CrushDamage crushDamage;
        CyclopsExternalDamageManager damageManager;
        public GameObject repairBotPrefab;
        public GameObject repairBot;
        public GameObject subTop;
        private GameObject transformObj;

        private bool isVanillaSub = false;
        private bool canShowNoPowerNotification = true;
        private bool active = true;
        private bool toggleKeyPressed = false;
        private float creationTime;
        private bool allowToggle = false;
        public bool repairBotActive = false;
        public int botMode = 0;
        private float lastRepair;
        private float lastDeployment;
        private float lastPath;
        public CyclopsDamagePoint damageTarget;
        private Transform repairBotRoot;
        private List<Transform> repairRootPoints = new List<Transform>();

        public bool upgradeInstalled = false;
        public void Start()
        {
            creationTime = Time.time;
            liveMixin = gameObject.GetComponent<LiveMixin>();
            if (liveMixin == null)
            {
                RepairPlugin.Log.LogError("Could not find LiveMixin in " + gameObject.name);
                this.enabled = false;
                return;
            }
            powerRelay = root.powerRelay;
            crushDamage = gameObject.GetComponent<CrushDamage>();

            if (gameObject.name == "Cyclops-MainPrefab(Clone)")
            {
                isVanillaSub = true;
            }
            if (RepairPlugin.config.repairType == RepairPlugin.RepairTypes.Upgrade)
            {
                active = false;
            }

            damageManager = root.damageManager;

            UWE.CoroutineHost.StartCoroutine(GetBuilderBot(this));
            GenerateRootPoints();
        }

        public void Update()
        {
            ToggleUpdate();

            if (repairBotActive && (!active || (!isVanillaSub && !RepairPlugin.config.moddedVehicles)))
            {
                DismissRepairBot();
                Destroy(repairBot);
                repairBot = null;
                SendNotification("Repair drone Disengaged.");
            }
            if (Player.main.GetCurrentSub() == root && !repairBotActive)
            {
                damageTarget = GetDamagePoint(damageManager);

                if (active && AllowedToRepair() && repairBotPrefab != null && damageTarget != null && liveMixin.health != 0 && (isVanillaSub || RepairPlugin.config.moddedVehicles))
                {
                    DeployRepairBot();
                }
                else if (repairBot != null)
                {
                    Destroy(repairBot);
                    repairBot = null;
                }
            }

            UpdateRepairBot();
        }

        public bool AllowedToRepair()
        {
            return liveMixin.health != liveMixin.maxHealth && RepairPlugin.config.submarines && (!BelowCrushDepth() || RepairPlugin.config.crushDepth) && EnoughPower() && Player.main.GetMode() != Player.Mode.Piloting;
        }

        private bool BelowCrushDepth()
        {
            return crushDamage.GetCanTakeCrushDamage() && crushDamage.GetDepth() > crushDamage.crushDepth;
        }

        private void ToggleUpdate()
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
                        SetActive(upgradeInstalled);
                        break;
                    }
            }
            if (GameInput.GetKeyDown(RepairPlugin.config.toggleKey) && !toggleKeyPressed && allowToggle)
            {
                if (EnoughPower() && Player.main.GetCurrentSub() == root)
                {
                    SetActive(!active);
                }
                toggleKeyPressed = true;
            }
            if (GameInput.GetKeyUp(RepairPlugin.config.toggleKey) && toggleKeyPressed)
            {
                toggleKeyPressed = false;
            }
        }

        public void SetActive(bool flag)
        {
            if (active != flag)
            {
                active = flag;
                if (active)
                {
                    ErrorMessage.AddMessage("Automatic repairing now online.");
                }
                else
                {
                    ErrorMessage.AddMessage("Automatic repairing now offline.");
                }
            }
        }
        private bool EnoughPower()
        {
            if (powerRelay != null)
            {
                float powerLeftScalar = powerRelay.GetPower() / powerRelay.GetMaxPower();
                if (powerRelay.GetPower() >= 0 && powerLeftScalar >= RepairPlugin.config.energyRequirement / 100)
                {
                    canShowNoPowerNotification = true;
                    return true;
                }
            }
            if (canShowNoPowerNotification && creationTime <= Time.time - 1)
            {
                ErrorMessage.AddMessage("Low energy levels detected, automatic reparing now offline.");
                canShowNoPowerNotification = false;
            }
            return false;
        }

        private void DeployRepairBot()
        {
            if (repairBot != null)
            {
                Destroy(repairBot);
            }
            repairBot = Instantiate(repairBotPrefab);
            repairBot.transform.position = subTop.transform.position; 
            repairBotActive = true;
            repairBotRoot = null;
            damageTarget = null;
            botMode = 0;
            repairBot.GetComponent<ConstructorBuildBot>().botId = 1;
            lastDeployment = Time.time;
            SendNotification("Repair drone Engaged.");
        }

        public Vector3 GetSubTopPosition()
        {
            Bounds aabb = this.GetAABB(gameObject);
            return (gameObject.transform.position + new Vector3(0, aabb.size.y + 5f, 0));
        }

        public BuildBotPath GetBotPath()
        {
            BuildBotPath path = damageManager.gameObject.GetComponent<BuildBotPath>();
            if (path == null)
            {
                path = damageManager.gameObject.AddComponent<BuildBotPath>();
            }
            return path;
        }
        private void UpdateRepairBot()
        {
            if (repairBot != null)
            {
                if (Time.time - lastDeployment >= 1f)
                {
                    repairBot.GetComponent<ConstructorBuildBot>().launch = true;
                }

                if (Player.main.GetMode() == Player.Mode.Piloting && repairBotActive)
                {
                    SendNotification("Repair drone Disengaged while piloting.");
                    DismissRepairBot();
                    Destroy(repairBot);
                    repairBot = null;
                    return;
                }

                if (!AllowedToRepair() && botMode != 0 && botMode != 4 && botMode != 5)
                {
                    SendNotification("Repair drone Disengaged.");
                    DismissRepairBot(); 
                    return;
                }

                if (liveMixin.health == 0)
                {
                    // Player Probably has bigger worries so no notification this time.
                    DismissRepairBot();
                    Destroy(repairBot);
                    repairBot = null;
                    return;
                }

                // Mode 0: Look for leaks and go to the top center of the sub.
                // Mode 1: Go to the nearest root point but remain above the sub.
                // Mode 2: Decend towards the root point.
                // Mode 3: Move to the leak and repair it.
                // Mode 4: Go back to the root point.
                // Mode 5: Stay at root point and ascend to the top of the sub.

                if (botMode == 0)
                {
                    damageTarget = GetDamagePoint(damageManager);

                    if (damageTarget == null || !repairBotActive)
                    {
                        // Go back to top of sub.
                        GetBotPath().points = subTop.GetComponents<Transform>();
                        SetPath(subTop);

                        if ((repairBot.transform.position - subTop.transform.position).magnitude <= 1)
                        {
                            DismissRepairBot();
                            Destroy(repairBot);
                            repairBot = null;

                            if (damageTarget == null)
                            {
                                SendNotification("No further breaches detected, repair drone Disengaged.");
                            }
                            else
                            {
                                SendNotification("Repair drone Disengaged.");
                            }
                        }
                        return;
                    }
                    else
                    {
                        // Found a damage point that is in need of repair.
                        botMode = 1;
                        repairBotRoot = GetClosestRootPoint(damageTarget.transform);
                        repairBot.GetComponent<ConstructorBuildBot>().FinishConstruction();
                    }
                }

                if (botMode == 1)
                {
                    if (repairBotRoot == null)
                    {
                        // If we did not find a suitable root point then return.
                        botMode = 0;
                        return;
                    }
                    if (!HasPath())
                    {
                        if (transformObj == null)
                        {
                            transformObj = new GameObject();
                        }
                        
                        // Go to the root point, but remain above the sub.
                        transformObj.transform.position = new Vector3 (repairBotRoot.transform.position.x, subTop.transform.position.y, repairBotRoot.transform.position.z);
                        Transform[] target = { transformObj.transform };
                        GetBotPath().points = target;

                        SetPath(transformObj);
                    }

                    if (Mathf.Abs(repairBot.transform.position.x - repairBotRoot.position.x) <= 1 && Mathf.Abs(repairBot.transform.position.z - repairBotRoot.position.z) <= 1)
                    {
                        botMode = 2;
                        repairBot.GetComponent<ConstructorBuildBot>().FinishConstruction();
                    }
                }

                if (botMode == 2)
                {
                    if (!HasPath())
                    {
                        Transform[] target = { repairBotRoot };
                        GetBotPath().points = target;

                        // Now we decened to the exact position of the root point.

                        SetPath(subTop);
                    }

                    if ((repairBot.transform.position - repairBotRoot.position).magnitude <= 2.5f)
                    {
                        botMode = 3;
                        repairBot.GetComponent<ConstructorBuildBot>().FinishConstruction();
                    }
                }

                if (botMode == 3)
                {
                    if (!HasPath())
                    {
                        Transform[] target = { damageTarget.transform };
                        GetBotPath().points = target;

                        BuildBotBeamPoints beamPoint = damageTarget.gameObject.GetComponent<BuildBotBeamPoints>();
                        if (beamPoint == null)
                        {
                            beamPoint = damageTarget.gameObject.AddComponent<BuildBotBeamPoints>();
                            Transform[] transforms = { beamPoint.transform };
                            beamPoint.beamPoints = transforms;
                        }

                        // Now we go to the exact positon of the leak.

                        SetPath(damageTarget.gameObject);
                    }

                    if ((repairBot.transform.position - damageTarget.transform.position).magnitude < 8f)
                    {
                        RepairPoint(damageTarget);
                    }
                }

                if (botMode == 4)
                {
                    if (!HasPath())
                    {
                        Transform[] target = { repairBotRoot };
                        GetBotPath().points = target;

                        SetPath(subTop);
                    }

                    if ((repairBot.transform.position - repairBotRoot.position).magnitude <= 2)
                    {
                        botMode = 5;
                        repairBot.GetComponent<ConstructorBuildBot>().FinishConstruction();
                    }
                }

                if (botMode == 5)
                {
                    if (!HasPath())
                    {
                        if (transformObj == null)
                        {
                            transformObj = new GameObject();
                        }

                        // Now we ascend to the top of the sub, but remain at the root point. 
                        transformObj.transform.position = new Vector3(repairBotRoot.transform.position.x, subTop.transform.position.y, repairBotRoot.transform.position.z);
                        Transform[] target = { transformObj.transform };
                        GetBotPath().points = target;

                        SetPath(transformObj);

                    }

                    if (Mathf.Abs(repairBot.transform.position.y - subTop.transform.position.y) <= 2)
                    {
                        // Now we can go back to the center of the sub and await new orders.
                        botMode = 0;
                        repairBot.GetComponent<ConstructorBuildBot>().FinishConstruction();
                    }
                }
            }
        }

        public bool HasPath()
        {
            return repairBot.GetComponent<ConstructorBuildBot>().path != null && Time.time - lastPath <= 5f;
        }

        private void SetPath(GameObject target)
        {
            repairBot.GetComponent<ConstructorBuildBot>().SetPath(GetBotPath(), target);
            lastPath = Time.time;
        }

        private void SendNotification(string message)
        {
            if (RepairPlugin.config.notifications)
            {
                ErrorMessage.AddMessage(message);
            }
        }

        public Transform GetClosestRootPoint(Transform from)
        {
            float closestDistance = 9999;
            Transform closest = null;
            foreach (Transform rootPoint in repairRootPoints)
            {
                float distance = (from.position - rootPoint.position).magnitude;
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = rootPoint;
                }
            }

            return closest;
        }

        public void RepairPoint(CyclopsDamagePoint damagePoint)
        {
            if (Time.time - lastRepair >= 0.1f)
            {
                if (EnoughPower() && !powerRelay.IsPowered() || (BelowCrushDepth() && !RepairPlugin.config.crushDepth) || !RepairPlugin.config.submarines)
                {
                    return;
                }
                lastRepair = Time.time;

                float healthCalculated = RepairPlugin.config.breachHealthPerHeal / 1000 * damagePoint.liveMixin.maxHealth + 0.1f;
                damagePoint.liveMixin.AddHealth(healthCalculated);

                float final;
                powerRelay.ConsumeEnergy(RepairPlugin.config.breachEnergyCost / 10, out final);
                canShowNoPowerNotification = true;

                if (!damagePoint.gameObject.activeSelf)
                {
                    // If we fully repaired it then we can go back to our root point.
                    botMode = 4;
                    repairBot.GetComponent<ConstructorBuildBot>().FinishConstruction();
                }

                return;
            }
        }

        private void DismissRepairBot()
        {
            repairBotActive = false;
            botMode = 4;
        }

        private CyclopsDamagePoint GetDamagePoint(CyclopsExternalDamageManager damageManager)
        {
            foreach (CyclopsDamagePoint damagePoint in damageManager.damagePoints)
            {
                if (damagePoint != null && damagePoint.gameObject.activeSelf && damagePoint.liveMixin != null && damagePoint.liveMixin.health != damagePoint.liveMixin.maxHealth)
                {
                    return damagePoint;
                }
            }
            return null;
        }

        private static IEnumerator GetBuilderBot(LargeSubRepair repairComp)
        {
            CoroutineTask<GameObject> task = CraftData.GetPrefabForTechTypeAsync(TechType.Constructor);
            yield return task;
            GameObject constructorPrefab = task.GetResult();
            Constructor constructor = constructorPrefab.GetComponent<Constructor>();
            if (constructor == null)
            {
                RepairPlugin.Log.LogError("Could not find constructor!");
                yield break;
            }
            repairComp.repairBotPrefab = constructor.buildBotPrefab;

            repairComp.subTop = new GameObject();
            repairComp.subTop.transform.SetParent(repairComp.transform);
            repairComp.subTop.transform.position = repairComp.GetSubTopPosition();
        }

        private void GenerateRootPoints()
        {
            GameObject front = new GameObject();
            GameObject back = new GameObject();
            GameObject right = new GameObject();
            GameObject left = new GameObject();
            GameObject fl = new GameObject();
            GameObject fr = new GameObject();
            GameObject bl = new GameObject();
            GameObject br = new GameObject();
            GameObject tf = new GameObject();
            GameObject tb = new GameObject();

            GameObject rootPointsRoot = new GameObject();
            rootPointsRoot.transform.SetParent(transform);
            rootPointsRoot.transform.localPosition = new Vector3(0,0,0);
            rootPointsRoot.transform.localRotation = Quaternion.identity;

            front.transform.SetParent(rootPointsRoot.transform);
            back.transform.SetParent(rootPointsRoot.transform);
            right.transform.SetParent(rootPointsRoot.transform);
            left.transform.SetParent(rootPointsRoot.transform);
            fl.transform.SetParent(rootPointsRoot.transform);
            fr.transform.SetParent(rootPointsRoot.transform);
            bl.transform.SetParent(rootPointsRoot.transform);
            br.transform.SetParent(rootPointsRoot.transform);
            tf.transform.SetParent(rootPointsRoot.transform);
            tb.transform.SetParent(rootPointsRoot.transform);

            Bounds bounds = GetAABB(gameObject);

            front.transform.localPosition = new Vector3(0, 0, bounds.size.z + 6f);
            back.transform.localPosition = new Vector3(0, 0, -bounds.size.z - 6f);
            right.transform.localPosition = new Vector3(bounds.size.x + 3f, 0, 0);
            left.transform.localPosition = new Vector3(-bounds.size.x - 3f, 0, 0);

            fr.transform.localPosition = new Vector3(bounds.size.x + 2f, 0, bounds.size.z / 2);
            fl.transform.localPosition = new Vector3(-bounds.size.x - 2f, 0, bounds.size.z / 2);
            br.transform.localPosition = new Vector3(bounds.size.x + 2f, 0, -bounds.size.z / 2);
            bl.transform.localPosition = new Vector3(-bounds.size.x - 2f, 0, -bounds.size.z / 2);

            tf.transform.localPosition = new Vector3(0, bounds.size.y + 6f, bounds.size.z / 2);
            tb.transform.localPosition = new Vector3(0, bounds.size.y + 6f, -bounds.size.z / 2);

            foreach (Transform child in rootPointsRoot.transform)
            {
                repairRootPoints.Add(child);
            }
        }

        private Bounds GetAABB(GameObject target)
        {
            FixedBounds component = target.GetComponent<FixedBounds>();
            Bounds result;
            if (component != null)
            {
                result = component.bounds;
            }
            else
            {
                result = UWE.Utils.GetEncapsulatedAABB(target, 20);
            }
            return result;
        }
    }
}
