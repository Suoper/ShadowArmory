using ThunderRoad;
using UnityEngine;
using System.Collections.Generic;

namespace ShadowArmory
{
    public class ShadowDamperConfig
    {
        private static readonly string currentUser = "Suoper";
        private static readonly string currentDateTime = "2025-04-09 02:08:32";

        // Core settings
        private static float damperInMultiplier = 0.3f;
        private static float damperOutMultiplier = 0.3f;
        private static bool enableDamping = true;

        #region Mod Options

        [ModOptionCategory("Shadow Damper Settings", 1)]
        [ModOptionOrder(1)]
        [ModOption("Enable Damping", "Toggle the damping system")]
        public static bool EnableDamping
        {
            get => enableDamping;
            set => enableDamping = value;
        }

        [ModOptionCategory("Shadow Damper Settings", 1)]
        [ModOptionOrder(2)]
        [ModOptionSlider]
        [ModOption("Inward Damping", "How much to reduce inward penetration damping", nameof(DamperOptions))]
        public static float DamperInMultiplier
        {
            get => damperInMultiplier;
            set => damperInMultiplier = value;
        }

        [ModOptionCategory("Shadow Damper Settings", 1)]
        [ModOptionOrder(3)]
        [ModOptionSlider]
        [ModOption("Outward Damping", "How much to reduce outward penetration damping", nameof(DamperOptions))]
        public static float DamperOutMultiplier
        {
            get => damperOutMultiplier;
            set => damperOutMultiplier = value;
        }

        #endregion

        #region Option Arrays

        public static ModOptionFloat[] DamperOptions()
        {
            ModOptionFloat[] options = new ModOptionFloat[11];
            for (int i = 0; i < 11; i++)
            {
                float value = i * 0.1f; // 0 to 1
                options[i] = new ModOptionFloat((value * 100f).ToString("F0") + "%", value);
            }
            return options;
        }

        #endregion
    }
}