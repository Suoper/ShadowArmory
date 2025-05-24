using ThunderRoad;
using UnityEngine;
using System.Collections.Generic;

namespace ShadowArmory
{
    public class ShadowDamper : MonoBehaviour
    {
        private readonly string currentUser = "Suoper";
        private readonly string currentDateTime = "2025-04-09 02:08:32";

        private static Dictionary<DamagerData, (float damperIn, float damperOut)> originalDampers
            = new Dictionary<DamagerData, (float, float)>();

        private Creature creature;
        private bool isEnabled = false;

        public void Initialize(Creature owner)
        {
            creature = owner;
            isEnabled = ShadowDamperConfig.EnableDamping;
            Debug.Log($"[{currentDateTime}] {currentUser} - Initializing Shadow Damper with enabled state: {isEnabled}");
            UpdateAllDampers();
        }

        public void UpdateAllDampers()
        {
            if (!isEnabled || !ShadowDamperConfig.EnableDamping)
            {
                Debug.Log($"[{currentDateTime}] {currentUser} - Shadow Damper is disabled, skipping update");
                return;
            }

            foreach (CatalogCategory category in Catalog.data)
            {
                if (category.category == Category.Damager)
                {
                    foreach (CatalogData data in category.catalogDatas)
                    {
                        if (data is DamagerData damagerData)
                        {
                            UpdateDamper(damagerData);
                        }
                    }
                }
            }
            Debug.Log($"[{currentDateTime}] {currentUser} - Updated all dampers with in multiplier: {ShadowDamperConfig.DamperInMultiplier} and out multiplier: {ShadowDamperConfig.DamperOutMultiplier}");
        }

        private void UpdateDamper(DamagerData damagerData)
        {
            if (!originalDampers.ContainsKey(damagerData))
            {
                originalDampers[damagerData] = (
                    damagerData.penetrationHeldDamperIn,
                    damagerData.penetrationHeldDamperOut
                );
                Debug.Log($"[{currentDateTime}] {currentUser} - Stored original damper values for {damagerData.id}");
            }

            var original = originalDampers[damagerData];

            damagerData.penetrationHeldDamperIn = original.damperIn * ShadowDamperConfig.DamperInMultiplier;
            damagerData.penetrationHeldDamperOut = original.damperOut * ShadowDamperConfig.DamperOutMultiplier;
        }

        private void OnDestroy()
        {
            Debug.Log($"[{currentDateTime}] {currentUser} - Shadow Damper being destroyed, restoring original values");

            foreach (var kvp in originalDampers)
            {
                var damagerData = kvp.Key;
                var original = kvp.Value;

                damagerData.penetrationHeldDamperIn = original.damperIn;
                damagerData.penetrationHeldDamperOut = original.damperOut;
            }

            originalDampers.Clear();
            isEnabled = false;

            Debug.Log($"[{currentDateTime}] {currentUser} - Shadow Damper destroyed");
        }
    }
}