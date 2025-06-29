using ThunderRoad;
using UnityEngine;

namespace ShadowArmory
{
    /// <summary>
    /// Configuration options for SkillOrbModule
    /// </summary>
    public class SkillOrbConfig
    {
        private readonly string currentUser = "Suoper";
        private readonly string currentDateTime = "2025-06-29 03:30:38";

        // Orb positioning
        private static float orbDistance = 0.8f;
        private static float orbRadius = 0.15f;

        // Orbital motion
        private static float orbitSpeed = 30f;
        private static float orbitRadius = 0.2f;
        private static bool enableOrbitalMotion = true;

        // Visual feedback
        private static bool showOrbVisuals = true;
        private static float orbTransparency = 0.3f;

        // Debug options
        private static bool enableDebugLogging = true;
        private static bool enableCollisionFeedback = true;

        #region Public Properties

        [ModOptionCategory("8. Skill Orb Module", 8)]
        [ModOptionOrder(1)]
        [ModOptionSlider(0.2f, 2.0f)]
        [ModOption("Orb Distance", "Distance from center to direction orbs")]
        public static float OrbDistance
        {
            get => orbDistance;
            set => orbDistance = value;
        }

        [ModOptionCategory("8. Skill Orb Module", 8)]
        [ModOptionOrder(2)]
        [ModOptionSlider(0.05f, 0.5f)]
        [ModOption("Orb Radius", "Size of collision detection radius for orbs")]
        public static float OrbRadius
        {
            get => orbRadius;
            set => orbRadius = value;
        }

        [ModOptionCategory("8. Skill Orb Module", 8)]
        [ModOptionOrder(3)]
        [ModOptionSlider(5f, 100f)]
        [ModOption("Orbit Speed", "Speed of weapon orbital motion")]
        public static float OrbitSpeed
        {
            get => orbitSpeed;
            set => orbitSpeed = value;
        }

        [ModOptionCategory("8. Skill Orb Module", 8)]
        [ModOptionOrder(4)]
        [ModOptionSlider(0.1f, 0.5f)]
        [ModOption("Orbit Radius", "Radius of weapon orbit around orb")]
        public static float OrbitRadius
        {
            get => orbitRadius;
            set => orbitRadius = value;
        }

        [ModOptionCategory("8. Skill Orb Module", 8)]
        [ModOptionOrder(5)]
        [ModOption("Enable Orbital Motion", "Allow weapons to orbit around orbs")]
        public static bool EnableOrbitalMotion
        {
            get => enableOrbitalMotion;
            set => enableOrbitalMotion = value;
        }

        [ModOptionCategory("8. Skill Orb Module", 8)]
        [ModOptionOrder(6)]
        [ModOption("Show Orb Visuals", "Display visual orbs for debugging")]
        public static bool ShowOrbVisuals
        {
            get => showOrbVisuals;
            set => showOrbVisuals = value;
        }

        [ModOptionCategory("8. Skill Orb Module", 8)]
        [ModOptionOrder(7)]
        [ModOptionSlider(0.1f, 1.0f)]
        [ModOption("Orb Transparency", "Transparency of visual orbs")]
        public static float OrbTransparency
        {
            get => orbTransparency;
            set => orbTransparency = value;
        }

        [ModOptionCategory("8. Skill Orb Module", 8)]
        [ModOptionOrder(8)]
        [ModOption("Enable Debug Logging", "Show debug messages in console")]
        public static bool EnableDebugLogging
        {
            get => enableDebugLogging;
            set => enableDebugLogging = value;
        }

        [ModOptionCategory("8. Skill Orb Module", 8)]
        [ModOptionOrder(9)]
        [ModOption("Enable Collision Feedback", "Provide haptic feedback on weapon collision")]
        public static bool EnableCollisionFeedback
        {
            get => enableCollisionFeedback;
            set => enableCollisionFeedback = value;
        }

        #endregion

        /// <summary>
        /// Logs debug message if debug logging is enabled
        /// </summary>
        public static void DebugLog(string message)
        {
            if (enableDebugLogging)
            {
                Debug.Log(message);
            }
        }
    }
}