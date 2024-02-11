using AutomaticVehicleRepair.MonoBehaviours;
using HarmonyLib;
using UnityEngine.UI;
using UnityEngine;
using static CyclopsHolographicHUD;
using System.Collections.Generic;

namespace AutomaticVehicleRepair.Patches
{
    // TODO Review this file and update to your own requirements, or remove it altogether if not required

    /// <summary>
    /// Sample Harmony Patch class. Suggestion is to use one file per patched class
    /// though you can include multiple patch classes in one file.
    /// Below is included as an example, and should be replaced by classes and methods
    /// for your mod.
    /// </summary>
    [HarmonyPatch(typeof(CyclopsHolographicHUD))]
    internal class HolographicHudPatches
    {
        private static bool lastState = false;
        /// <summary>
        /// Patches the Player Awake method with prefix code.
        /// </summary>
        /// <param name="__instance"></param>
        [HarmonyPatch(nameof(CyclopsHolographicHUD.Update))]
        [HarmonyPrefix]
        public static bool Update_Prefix(CyclopsHolographicHUD __instance, ref List<CyclopsHolographicHUD.DamageIcon> ___damageIcons, ref Transform ___subRootTransform)
        {
            LargeSubRepair largeSubRepair = ___subRootTransform.gameObject.GetComponent<LargeSubRepair>();
            if (largeSubRepair == null)
            {
                return true;
            }

            if (largeSubRepair.repairBotActive == false && lastState == false)
            {
                // No point in updating this thing if there are no bots active.
                // Hopefully it saves some performance.
                return true;
            }

            lastState = largeSubRepair.repairBotActive;

            if (___damageIcons == null)
            {
                RepairPlugin.Log.LogError("___damaeIcons is null!");
                return true;
            }

            foreach (CyclopsHolographicHUD.DamageIcon damageIcon in ___damageIcons)
            {
                if (damageIcon != null)
                {
                    if (damageIcon.damageIcon != null && damageIcon.refDamage != null)
                    {
                        if (largeSubRepair.damageTarget == damageIcon.refDamage.GetComponent<CyclopsDamagePoint>() && largeSubRepair.botMode >= 3 && largeSubRepair.repairBotActive)
                        {
                            damageIcon.damageIcon.GetComponent<CyclopsHolographicHUD_WarningPings>().warningPing.GetComponent<Image>().color = Color.green;
                            damageIcon.damageIcon.GetComponent<CyclopsHolographicHUD_WarningPings>().damageText.GetComponentInChildren<Image>().color = Color.green;
                            damageIcon.damageIcon.GetComponent<CyclopsHolographicHUD_WarningPings>().lineRenderer.startColor = Color.green;
                            damageIcon.damageIcon.GetComponent<CyclopsHolographicHUD_WarningPings>().lineRenderer.endColor = Color.green;
                            damageIcon.damageIcon.GetComponent<CyclopsHolographicHUD_WarningPings>().lineRenderer.gameObject.GetComponent<Image>().color = Color.green;
                        }
                        else
                        {
                            damageIcon.damageIcon.GetComponent<CyclopsHolographicHUD_WarningPings>().warningPing.GetComponent<Image>().color = new Color(1, 1, 1, 1);
                            damageIcon.damageIcon.GetComponent<CyclopsHolographicHUD_WarningPings>().damageText.GetComponentInChildren<Image>().color = new Color(1, 1, 1, 1);
                            damageIcon.damageIcon.GetComponent<CyclopsHolographicHUD_WarningPings>().lineRenderer.startColor = new Color(1, 0.9098f, 0.1922f, 1);
                            damageIcon.damageIcon.GetComponent<CyclopsHolographicHUD_WarningPings>().lineRenderer.endColor = new Color(1, 0.9098f, 0.1922f, 1);
                            damageIcon.damageIcon.GetComponent<CyclopsHolographicHUD_WarningPings>().lineRenderer.gameObject.GetComponent<Image>().color = new Color(1, 1, 1, 1);
                        }
                    }
                    else
                    {
                        RepairPlugin.Log.LogError("Could Not Find 'damageIcon' or 'refDamage' in damageIcon!");
                    }
                }
            }
            return true;
        }
    }
}