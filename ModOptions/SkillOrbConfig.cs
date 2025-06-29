using UnityEngine;

namespace ShadowArmory
{
    /// <summary>
    /// Configuration options for the Skill Orb Module
    /// </summary>
    public static class SkillOrbConfig
    {
        // Orbital Motion Settings
        public static float OrbitSpeed = 60f; // degrees per second
        public static float WeaponDistanceFromOrb = 0.8f;
        public static float OrbDistanceFromPlayer = 1.5f;
        public static float OrbHeightOffset = 1.5f;
        
        // Trigger Settings
        public static float TriggerRadius = 1.2f;
        
        // Visual Settings
        public static Color OrbColor = new Color(0.5f, 0.8f, 1f, 0.6f);
        public static float OrbScale = 0.3f;
        
        // Performance Settings
        public static float OrbUpdateInterval = 0.1f; // seconds
        
        // Debugging
        public static bool EnableDebugLogs = true;
        
        // Feature toggles
        public static bool EnableDynamicOrbPositioning = true;
        public static bool EnableAutoWeaponRelease = true;
    }
}