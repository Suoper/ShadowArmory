using ThunderRoad;
using UnityEngine;

namespace ShadowArmory
{
    public class ShadowCrystalConfig
    {
        private static readonly string currentUser = "SuoperAmbiguity";
        private static readonly string currentDateTime = "2025-05-10 19:25:04";

        #region Mod Options

        [ModOptionCategory("Crystal Settings", 1)]
        [ModOptionOrder(1)]
        [ModOptionSlider]
        [ModOption("Hover Height", "Height above hand where weapons hover", nameof(HoverHeightOptions))]
        public static float HoverHeight { get; set; } = 0.25f;

        [ModOptionCategory("Crystal Settings", 1)]
        [ModOptionOrder(2)]
        [ModOptionSlider]
        [ModOption("Search Radius", "How far to search for weapons", nameof(SearchRadiusOptions))]
        public static float SearchRadius { get; set; } = 2.0f;

        [ModOptionCategory("Crystal Settings", 1)]
        [ModOptionOrder(3)]
        [ModOptionSlider]
        [ModOption("Hover Speed", "How quickly weapons move to hover position", nameof(HoverSpeedOptions))]
        public static float HoverSpeed { get; set; } = 10.0f;

        [ModOptionCategory("Crystal Settings", 1)]
        [ModOptionOrder(4)]
        [ModOptionSlider]
        [ModOption("Gesture Threshold", "Distance required for gesture detection", nameof(GestureOptions))]
        public static float GestureThreshold { get; set; } = 0.12f;

        [ModOptionCategory("Crystal Settings", 2)]
        [ModOptionOrder(1)]
        [ModOption("Separate Hand Assignments", "Store different weapons for each hand")]
        public static bool SeparateHandAssignments { get; set; } = true;

        [ModOptionCategory("Crystal Settings", 2)]
        [ModOptionOrder(2)]
        [ModOption("Keep Hovering After Assign", "Continue hovering weapon after assigning")]
        public static bool KeepHoveringAfterAssign { get; set; } = false;

        [ModOptionCategory("Crystal Settings", 2)]
        [ModOptionOrder(3)]
        [ModOptionSlider]
        [ModOption("Pickup Cooldown", "Time between weapon pickups", nameof(CooldownOptions))]
        public static float PickupCooldown { get; set; } = 0.5f;

        [ModOptionCategory("Crystal Settings", 2)]
        [ModOptionOrder(4)]
        [ModOption("Cycle Weapons", "Auto-search for new weapons after assigning")]
        public static bool CycleWeapons { get; set; } = true;

        [ModOptionCategory("Crystal Settings", 3)]
        [ModOptionOrder(1)]
        [ModOption("Visual Feedback", "Show effects during operation")]
        public static bool UseVisualFeedback { get; set; } = true;

        [ModOptionCategory("Crystal Settings", 3)]
        [ModOptionOrder(2)]
        [ModOption("Haptic Feedback", "Controller vibration during operation")]
        public static bool UseHapticFeedback { get; set; } = true;

        [ModOptionCategory("Crystal Settings", 3)]
        [ModOptionOrder(3)]
        [ModOption("Audio Feedback", "Play sounds during operation")]
        public static bool UseAudioFeedback { get; set; } = true;

        [ModOptionCategory("Crystal Settings", 4)]
        [ModOptionOrder(1)]
        [ModOptionSlider]
        [ModOption("Physics Refresh Rate", "How often to refresh physics", nameof(RefreshRateOptions))]
        public static float PhysicsRefreshRate { get; set; } = 0.2f;

        [ModOptionCategory("Crystal Settings", 4)]
        [ModOptionOrder(2)]
        [ModOptionSlider]
        [ModOption("Position Responsiveness", "How responsive position updates are", nameof(LerpSpeedOptions))]
        public static float PositionLerpSpeed { get; set; } = 15.0f;

        [ModOptionCategory("Crystal Settings", 4)]
        [ModOptionOrder(3)]
        [ModOptionSlider]
        [ModOption("Rotation Responsiveness", "How responsive rotation updates are", nameof(LerpSpeedOptions))]
        public static float RotationLerpSpeed { get; set; } = 7.0f;

        [ModOptionCategory("Crystal Settings", 4)]
        [ModOptionOrder(4)]
        [ModOption("Auto-Release On Timeout", "Automatically release weapon if gesture takes too long")]
        public static bool AutoReleaseOnTimeout { get; set; } = true;

        [ModOptionCategory("Crystal Settings", 4)]
        [ModOptionOrder(5)]
        [ModOptionSlider]
        [ModOption("Gesture Timeout", "Max time for gesture before auto-release", nameof(TimeoutOptions))]
        public static float GestureTimeout { get; set; } = 3.0f;

        [ModOptionCategory("Crystal Settings", 4)]
        [ModOptionOrder(6)]
        [ModOption("Show Debug Log", "Display detailed debug information")]
        public static bool ShowDebugLog { get; set; } = false;

        #endregion

        #region Effect Names
        private static string HoverEffect = "effectShadowHover";
        private static string AssignEffect = "effectShadowAssign";
        #endregion

        #region Option Arrays

        public static ModOptionFloat[] HoverHeightOptions()
        {
            ModOptionFloat[] options = new ModOptionFloat[16];
            for (int i = 0; i < 16; i++)
            {
                float value = 0.05f + (i * 0.05f);
                options[i] = new ModOptionFloat(value.ToString("F2") + "m", value);
            }
            return options;
        }

        public static ModOptionFloat[] SearchRadiusOptions()
        {
            ModOptionFloat[] options = new ModOptionFloat[11];
            for (int i = 0; i < 11; i++)
            {
                float value = 0.5f + (i * 0.25f);
                options[i] = new ModOptionFloat(value.ToString("F2") + "m", value);
            }
            return options;
        }

        public static ModOptionFloat[] HoverSpeedOptions()
        {
            ModOptionFloat[] options = new ModOptionFloat[16];
            for (int i = 0; i < 16; i++)
            {
                float value = 2f + (i);
                options[i] = new ModOptionFloat(value.ToString("F0"), value);
            }
            return options;
        }

        public static ModOptionFloat[] LerpSpeedOptions()
        {
            ModOptionFloat[] options = new ModOptionFloat[21];
            for (int i = 0; i < 21; i++)
            {
                float value = 5f + i;
                options[i] = new ModOptionFloat(value.ToString("F0"), value);
            }
            return options;
        }

        public static ModOptionFloat[] GestureOptions()
        {
            ModOptionFloat[] options = new ModOptionFloat[16];
            for (int i = 0; i < 16; i++)
            {
                float value = 0.05f + (i * 0.02f);
                options[i] = new ModOptionFloat(value.ToString("F2") + "m", value);
            }
            return options;
        }

        public static ModOptionFloat[] CooldownOptions()
        {
            ModOptionFloat[] options = new ModOptionFloat[11];
            for (int i = 0; i < 11; i++)
            {
                float value = 0.1f + (i * 0.1f);
                options[i] = new ModOptionFloat(value.ToString("F1") + "s", value);
            }
            return options;
        }

        public static ModOptionFloat[] RefreshRateOptions()
        {
            ModOptionFloat[] options = new ModOptionFloat[11];
            for (int i = 0; i < 11; i++)
            {
                float value = 0.05f + (i * 0.05f);
                options[i] = new ModOptionFloat(value.ToString("F2") + "s", value);
            }
            return options;
        }

        public static ModOptionFloat[] TimeoutOptions()
        {
            ModOptionFloat[] options = new ModOptionFloat[11];
            for (int i = 0; i < 11; i++)
            {
                float value = 0.5f + (i * 0.5f);
                options[i] = new ModOptionFloat(value.ToString("F1") + "s", value);
            }
            return options;
        }

        #endregion

        #region Helper Methods

        // Helper method for debug logging that respects the debug setting
        public static void DebugLog(string message)
        {
            if (ShowDebugLog)
            {
                Debug.Log($"[{System.DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")}] {currentUser} - {message}");
            }
        }

        // Helper function to determine if haptic feedback should be sent
        public static bool ShouldUseHapticFeedback()
        {
            return UseHapticFeedback;
        }

        // Helper function to get effect names while respecting user settings
        public static string GetEffectId(string effectType)
        {
            if (!UseVisualFeedback) return null;

            switch (effectType.ToLower())
            {
                case "hover":
                    return HoverEffect;
                case "assign":
                    return AssignEffect;
                default:
                    return null;
            }
        }
        #endregion
    }
}