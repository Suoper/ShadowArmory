using ThunderRoad;
using UnityEngine;
using System.Collections.Generic;

namespace ShadowArmory
{
    public class ShadowFormConfig
    {
        private static readonly string currentUser = "Suoper";
        private static readonly string currentDateTime = "2025-04-09 02:06:08";

        // Core settings
        private static float duration = 15f;
        private static float cooldownTime = 15f;
        private static float gestureCooldown = 1f;
        private static float transparencyLevel = 0.3f;
        private static float armSpread = 1.2f;

        // Combat settings
        private static float positionMultiplier = 4.5f;
        private static float rotationMultiplier = 4.5f;

        // Detection settings
        private static float creatureCheckRadius = 50f;
        private static float creatureCheckInterval = 3f;

        // Effect IDs
        private static string shadowEffectId = "ShadowFormEffect";
        private static string shadowMaterialId = "SixStars.Shadow.Matrial";
        private static string strengthEffectId = "WarriorStrengthEffect";

        #region Mod Options

        [ModOptionCategory("Shadow Form Settings", 1)]
        [ModOptionOrder(1)]
        [ModOptionSlider]
        [ModOption("Duration", "How long Shadow Form lasts", nameof(DurationOptions))]
        public static float Duration
        {
            get => duration;
            set => duration = value;
        }

        [ModOptionCategory("Shadow Form Settings", 1)]
        [ModOptionOrder(2)]
        [ModOptionSlider]
        [ModOption("Cooldown Time", "Time between uses", nameof(CooldownOptions))]
        public static float CooldownTime
        {
            get => cooldownTime;
            set => cooldownTime = value;
        }

        [ModOptionCategory("Shadow Form Settings", 1)]
        [ModOptionOrder(3)]
        [ModOptionSlider]
        [ModOption("Gesture Cooldown", "Time between gesture attempts", nameof(GestureCooldownOptions))]
        public static float GestureCooldown
        {
            get => gestureCooldown;
            set => gestureCooldown = value;
        }

        [ModOptionCategory("Shadow Form Settings", 1)]
        [ModOptionOrder(4)]
        [ModOptionSlider]
        [ModOption("Arm Spread Required", "How far apart arms need to be", nameof(ArmSpreadOptions))]
        public static float ArmSpread
        {
            get => armSpread;
            set => armSpread = value;
        }

        [ModOptionCategory("Shadow Form Settings", 1)]
        [ModOptionOrder(5)]
        [ModOptionSlider]
        [ModOption("Transparency Level", "How transparent you become", nameof(TransparencyOptions))]
        public static float TransparencyLevel
        {
            get => transparencyLevel;
            set => transparencyLevel = value;
        }

        [ModOptionCategory("Shadow Form Settings", 2)]
        [ModOptionOrder(1)]
        [ModOptionSlider]
        [ModOption("Position Multiplier", "Strength multiplier for position", nameof(MultiplierOptions))]
        public static float PositionMultiplier
        {
            get => positionMultiplier;
            set => positionMultiplier = value;
        }

        [ModOptionCategory("Shadow Form Settings", 2)]
        [ModOptionOrder(2)]
        [ModOptionSlider]
        [ModOption("Rotation Multiplier", "Strength multiplier for rotation", nameof(MultiplierOptions))]
        public static float RotationMultiplier
        {
            get => rotationMultiplier;
            set => rotationMultiplier = value;
        }

        [ModOptionCategory("Shadow Form Settings", 3)]
        [ModOptionOrder(1)]
        [ModOptionSlider]
        [ModOption("Check Radius", "Range to check for creatures", nameof(RadiusOptions))]
        public static float CreatureCheckRadius
        {
            get => creatureCheckRadius;
            set => creatureCheckRadius = value;
        }

        [ModOptionCategory("Shadow Form Settings", 3)]
        [ModOptionOrder(2)]
        [ModOptionSlider]
        [ModOption("Check Interval", "How often to check for creatures", nameof(IntervalOptions))]
        public static float CreatureCheckInterval
        {
            get => creatureCheckInterval;
            set => creatureCheckInterval = value;
        }

        #endregion

        #region Option Arrays

        public static ModOptionFloat[] DurationOptions()
        {
            ModOptionFloat[] options = new ModOptionFloat[13];
            for (int i = 0; i < 13; i++)
            {
                float value = 5f + (i * 5f); // 5 to 65 seconds
                options[i] = new ModOptionFloat(value.ToString("F0") + "s", value);
            }
            return options;
        }

        public static ModOptionFloat[] CooldownOptions()
        {
            ModOptionFloat[] options = new ModOptionFloat[13];
            for (int i = 0; i < 13; i++)
            {
                float value = 5f + (i * 5f); // 5 to 65 seconds
                options[i] = new ModOptionFloat(value.ToString("F0") + "s", value);
            }
            return options;
        }

        public static ModOptionFloat[] GestureCooldownOptions()
        {
            ModOptionFloat[] options = new ModOptionFloat[11];
            for (int i = 0; i < 11; i++)
            {
                float value = 0.5f + (i * 0.25f); // 0.5 to 3 seconds
                options[i] = new ModOptionFloat(value.ToString("F2") + "s", value);
            }
            return options;
        }

        public static ModOptionFloat[] ArmSpreadOptions()
        {
            ModOptionFloat[] options = new ModOptionFloat[11];
            for (int i = 0; i < 11; i++)
            {
                float value = 0.8f + (i * 0.1f); // 0.8 to 1.8 meters
                options[i] = new ModOptionFloat(value.ToString("F1") + "m", value);
            }
            return options;
        }

        public static ModOptionFloat[] TransparencyOptions()
        {
            ModOptionFloat[] options = new ModOptionFloat[11];
            for (int i = 0; i < 11; i++)
            {
                float value = i * 0.1f; // 0 to 1
                options[i] = new ModOptionFloat((value * 100f).ToString("F0") + "%", value);
            }
            return options;
        }

        public static ModOptionFloat[] MultiplierOptions()
        {
            ModOptionFloat[] options = new ModOptionFloat[11];
            for (int i = 0; i < 11; i++)
            {
                float value = 1f + (i * 0.5f); // 1x to 6x
                options[i] = new ModOptionFloat(value.ToString("F1") + "x", value);
            }
            return options;
        }

        public static ModOptionFloat[] RadiusOptions()
        {
            ModOptionFloat[] options = new ModOptionFloat[11];
            for (int i = 0; i < 11; i++)
            {
                float value = 10f + (i * 10f); // 10 to 110 meters
                options[i] = new ModOptionFloat(value.ToString("F0") + "m", value);
            }
            return options;
        }

        public static ModOptionFloat[] IntervalOptions()
        {
            ModOptionFloat[] options = new ModOptionFloat[11];
            for (int i = 0; i < 11; i++)
            {
                float value = 0.5f + (i * 0.5f); // 0.5 to 5.5 seconds
                options[i] = new ModOptionFloat(value.ToString("F1") + "s", value);
            }
            return options;
        }

        #endregion

        // Getters for effect IDs
        public static string ShadowEffectId => shadowEffectId;
        public static string ShadowMaterialId => shadowMaterialId;
        public static string StrengthEffectId => strengthEffectId;
    }
}