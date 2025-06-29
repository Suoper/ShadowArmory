// Simple validation tests for SkillOrbModule
// This is not a Unity test framework test, just basic validation

using System;
using UnityEngine;

namespace ShadowArmory
{
    public static class SkillOrbValidation
    {
        public static void ValidateConfiguration()
        {
            // Test configuration values are reasonable
            if (SkillOrbConfig.OrbitSpeed <= 0)
                Debug.LogError("SkillOrb: OrbitSpeed must be positive");
            
            if (SkillOrbConfig.WeaponDistanceFromOrb <= 0)
                Debug.LogError("SkillOrb: WeaponDistanceFromOrb must be positive");
            
            if (SkillOrbConfig.OrbDistanceFromPlayer <= 0)
                Debug.LogError("SkillOrb: OrbDistanceFromPlayer must be positive");
            
            if (SkillOrbConfig.TriggerRadius <= 0)
                Debug.LogError("SkillOrb: TriggerRadius must be positive");
            
            if (SkillOrbConfig.OrbScale <= 0)
                Debug.LogError("SkillOrb: OrbScale must be positive");
            
            if (SkillOrbConfig.OrbUpdateInterval <= 0)
                Debug.LogError("SkillOrb: OrbUpdateInterval must be positive");
            
            Debug.Log("SkillOrb configuration validation completed");
        }
        
        public static void ValidateOrbDirectionsEnum()
        {
            // Ensure all expected orb directions exist
            var directions = Enum.GetValues(typeof(SkillOrbModule.OrbDirection));
            
            if (directions.Length != 6)
            {
                Debug.LogError($"SkillOrb: Expected 6 orb directions, found {directions.Length}");
                return;
            }
            
            string[] expectedDirections = { "Up", "Down", "Left", "Right", "Forward", "Backward" };
            
            foreach (string expected in expectedDirections)
            {
                if (!Enum.IsDefined(typeof(SkillOrbModule.OrbDirection), expected))
                {
                    Debug.LogError($"SkillOrb: Missing expected direction: {expected}");
                }
            }
            
            Debug.Log("SkillOrb directions validation completed");
        }
    }
}