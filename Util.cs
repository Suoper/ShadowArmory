using ThunderRoad;
using System;
using UnityEngine;

namespace ShadowArmory
{
    public static class ShadowCrystalUtil
    {
        private static readonly string currentUser = "SuoperOh";
        private static readonly string currentDateTime = "2025-04-13 20:32:43";

        // Convert from Shadow's WeaponDirection to T1ShadowCrystal's WeaponDirection
        public static T1ShadowCrystal.WeaponDirection ConvertDirection(Shadow.WeaponDirection direction)
        {
            switch (direction)
            {
                case Shadow.WeaponDirection.Up:
                    return T1ShadowCrystal.WeaponDirection.Up;
                case Shadow.WeaponDirection.Down:
                    return T1ShadowCrystal.WeaponDirection.Down;
                case Shadow.WeaponDirection.Left:
                    return T1ShadowCrystal.WeaponDirection.Left;
                case Shadow.WeaponDirection.Right:
                    return T1ShadowCrystal.WeaponDirection.Right;
                case Shadow.WeaponDirection.Forward:
                    return T1ShadowCrystal.WeaponDirection.Forward;
                case Shadow.WeaponDirection.Backward:
                    return T1ShadowCrystal.WeaponDirection.Backward;
                default:
                    return T1ShadowCrystal.WeaponDirection.None;
            }
        }

        // Get weapon ID from crystal for a given direction
        public static string GetWeaponFromCrystal(Item crystalItem, Shadow.WeaponDirection direction)
        {
            if (crystalItem == null || crystalItem.itemId != "T1ShadowCrystal" || crystalItem.data == null)
            {
                return null;
            }

            Debug.Log($"[{currentDateTime}] {currentUser} - Looking for weapon assignments on crystal {crystalItem.itemId}");

            try
            {
                // Try to access module through item's data
                foreach (ItemModule module in crystalItem.data.modules)
                {
                    // Check if this is our module by name
                    if (module.GetType().Name == "T1ShadowCrystal")
                    {
                        Debug.Log($"[{currentDateTime}] {currentUser} - Found T1ShadowCrystal module");

                        // Use reflection to call the GetWeaponForDirection method
                        try
                        {
                            var methodInfo = module.GetType().GetMethod("GetWeaponForDirection");
                            if (methodInfo != null)
                            {
                                // Convert our enum to the module's enum type
                                T1ShadowCrystal.WeaponDirection convertedDirection = ConvertDirection(direction);

                                // Call the method via reflection
                                object result = methodInfo.Invoke(module, new object[] { (int)convertedDirection });
                                if (result != null)
                                {
                                    string weaponId = result.ToString();
                                    Debug.Log($"[{currentDateTime}] {currentUser} - Got weapon ID {weaponId} for direction {direction}");
                                    return weaponId;
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Debug.LogError($"[{currentDateTime}] {currentUser} - Error calling GetWeaponForDirection: {e.Message}");
                        }
                    }
                }

                // If we can't find/use the module directly, fallback to PlayerPrefs
                T1ShadowCrystal.WeaponDirection crystalDirection = ConvertDirection(direction);
                string key = $"T1ShadowCrystal_Direction_{crystalDirection}";
                if (PlayerPrefs.HasKey(key))
                {
                    string weaponId = PlayerPrefs.GetString(key);
                    Debug.Log($"[{currentDateTime}] {currentUser} - Found weapon {weaponId} in PlayerPrefs for direction {direction}");
                    return weaponId;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{currentDateTime}] {currentUser} - Error in GetWeaponFromCrystal: {ex.Message}");
            }

            // If all else fails, return default weapons
            return GetDefaultWeaponForDirection(direction);
        }

        // Get default weapon for direction
        private static string GetDefaultWeaponForDirection(Shadow.WeaponDirection direction)
        {
            switch (direction)
            {
                case Shadow.WeaponDirection.Up:
                    return "SwordShortCommon";
                case Shadow.WeaponDirection.Down:
                    return "DaggerCommon";
                case Shadow.WeaponDirection.Left:
                    return "AxeShortHatchet";
                case Shadow.WeaponDirection.Right:
                    return "ShieldPartisan";
                case Shadow.WeaponDirection.Forward:
                    return "SpearFighter";
                case Shadow.WeaponDirection.Backward:
                    return "BowRecurve";
                default:
                    return "SwordShortCommon";
            }
        }
    }
}