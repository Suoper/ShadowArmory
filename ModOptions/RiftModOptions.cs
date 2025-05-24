using ThunderRoad;
using UnityEngine;
using System.Collections.Generic;

namespace ShadowArmory
{
    public class RiftFormConfig
    {
        private static readonly string currentUser = "Suoper";
        private static readonly string currentDateTime = "2025-04-09 02:02:11";

        // Core settings
        private static float duration = 60f;
        private static float cooldownTime = 300f;
        private static float gestureCooldown = 1.5f;
        private static float spellCheckGracePeriod = 300f;
        private static string requiredSpellId = "Shadow";
        private static string effectId = "RiftFormEffect";
        private static string materialId = "Material";
        private static float gestureDistance = 0.15f;
        private static float gestureVelocity = 2.0f;

        // Flight settings
        private static float flySpeed = 15f;
        private static float flyUpForce = 15f;
        private static float flyControlSpeed = 5f;

        // Combat settings
        private static float punchMultiplier = 20f;
        private static float positionMultiplier = 12f;
        private static float rotationMultiplier = 12f;

        // Visual settings
        private static float colorR = 0.2f;
        private static float colorG = 0.6f;
        private static float colorB = 1.0f;
        private static float colorA = 0.8f;

        #region Mod Options

        [ModOptionCategory("Rift Form Settings", 1)]
        [ModOptionOrder(1)]
        [ModOptionSlider]
        [ModOption("Duration", "How long Rift Form lasts (seconds)", nameof(DurationOptions))]
        public static float Duration
        {
            get => duration;
            set => duration = value;
        }

        [ModOptionCategory("Rift Form Settings", 1)]
        [ModOptionOrder(2)]
        [ModOptionSlider]
        [ModOption("Cooldown Time", "Time between uses (seconds)", nameof(CooldownOptions))]
        public static float CooldownTime
        {
            get => cooldownTime;
            set => cooldownTime = value;
        }

        [ModOptionCategory("Rift Form Settings", 1)]
        [ModOptionOrder(3)]
        [ModOptionSlider]
        [ModOption("Gesture Cooldown", "Time between gesture attempts", nameof(GestureCooldownOptions))]
        public static float GestureCooldown
        {
            get => gestureCooldown;
            set => gestureCooldown = value;
        }

        [ModOptionCategory("Rift Form Settings", 1)]
        [ModOptionOrder(4)]
        [ModOptionSlider]
        [ModOption("Fly Speed", "Movement speed while flying", nameof(SpeedOptions))]
        public static float FlySpeed
        {
            get => flySpeed;
            set => flySpeed = value;
        }

        [ModOptionCategory("Rift Form Settings", 1)]
        [ModOptionOrder(5)]
        [ModOptionSlider]
        [ModOption("Fly Up Force", "Upward force while flying", nameof(SpeedOptions))]
        public static float FlyUpForce
        {
            get => flyUpForce;
            set => flyUpForce = value;
        }

        [ModOptionCategory("Rift Form Settings", 1)]
        [ModOptionOrder(6)]
        [ModOptionSlider]
        [ModOption("Fly Control Speed", "How responsive flying controls are", nameof(ControlOptions))]
        public static float FlyControlSpeed
        {
            get => flyControlSpeed;
            set => flyControlSpeed = value;
        }

        [ModOptionCategory("Rift Form Settings", 2)]
        [ModOptionOrder(1)]
        [ModOptionSlider]
        [ModOption("Punch Multiplier", "How much stronger punches are", nameof(MultiplierOptions))]
        public static float PunchMultiplier
        {
            get => punchMultiplier;
            set => punchMultiplier = value;
        }

        [ModOptionCategory("Rift Form Settings", 2)]
        [ModOptionOrder(2)]
        [ModOptionSlider]
        [ModOption("Position Multiplier", "Strength of position-based effects", nameof(MultiplierOptions))]
        public static float PositionMultiplier
        {
            get => positionMultiplier;
            set => positionMultiplier = value;
        }

        [ModOptionCategory("Rift Form Settings", 2)]
        [ModOptionOrder(3)]
        [ModOptionSlider]
        [ModOption("Rotation Multiplier", "Strength of rotation-based effects", nameof(MultiplierOptions))]
        public static float RotationMultiplier
        {
            get => rotationMultiplier;
            set => rotationMultiplier = value;
        }

        [ModOptionCategory("Rift Form Settings", 3)]
        [ModOptionOrder(1)]
        [ModOptionSlider]
        [ModOption("Effect Red", "Red component of the rift color", nameof(ColorOptions))]
        public static float ColorR
        {
            get => colorR;
            set => colorR = value;
        }

        [ModOptionCategory("Rift Form Settings", 3)]
        [ModOptionOrder(2)]
        [ModOptionSlider]
        [ModOption("Effect Green", "Green component of the rift color", nameof(ColorOptions))]
        public static float ColorG
        {
            get => colorG;
            set => colorG = value;
        }

        [ModOptionCategory("Rift Form Settings", 3)]
        [ModOptionOrder(3)]
        [ModOptionSlider]
        [ModOption("Effect Blue", "Blue component of the rift color", nameof(ColorOptions))]
        public static float ColorB
        {
            get => colorB;
            set => colorB = value;
        }

        [ModOptionCategory("Rift Form Settings", 3)]
        [ModOptionOrder(4)]
        [ModOptionSlider]
        [ModOption("Effect Alpha", "Transparency of the rift effect", nameof(ColorOptions))]
        public static float ColorA
        {
            get => colorA;
            set => colorA = value;
        }

        #endregion

        #region Option Arrays

        public static ModOptionFloat[] DurationOptions()
        {
            ModOptionFloat[] options = new ModOptionFloat[13];
            for (int i = 0; i < 13; i++)
            {
                float value = 15f + (i * 15f); // 15 to 180 seconds
                options[i] = new ModOptionFloat(value.ToString("F0") + "s", value);
            }
            return options;
        }

        public static ModOptionFloat[] CooldownOptions()
        {
            ModOptionFloat[] options = new ModOptionFloat[11];
            for (int i = 0; i < 11; i++)
            {
                float value = 60f + (i * 60f); // 1 to 10 minutes
                options[i] = new ModOptionFloat((value / 60f).ToString("F0") + "m", value);
            }
            return options;
        }

        public static ModOptionFloat[] GestureCooldownOptions()
        {
            ModOptionFloat[] options = new ModOptionFloat[11];
            for (int i = 0; i < 11; i++)
            {
                float value = 0.5f + (i * 0.5f); // 0.5 to 5.5 seconds
                options[i] = new ModOptionFloat(value.ToString("F1") + "s", value);
            }
            return options;
        }

        public static ModOptionFloat[] SpeedOptions()
        {
            ModOptionFloat[] options = new ModOptionFloat[21];
            for (int i = 0; i < 21; i++)
            {
                float value = 5f + (i * 2.5f); // 5 to 55
                options[i] = new ModOptionFloat(value.ToString("F1"), value);
            }
            return options;
        }

        public static ModOptionFloat[] ControlOptions()
        {
            ModOptionFloat[] options = new ModOptionFloat[11];
            for (int i = 0; i < 11; i++)
            {
                float value = 1f + (i * 1f); // 1 to 11
                options[i] = new ModOptionFloat(value.ToString("F1"), value);
            }
            return options;
        }

        public static ModOptionFloat[] MultiplierOptions()
        {
            ModOptionFloat[] options = new ModOptionFloat[21];
            for (int i = 0; i < 21; i++)
            {
                float value = 1f + (i * 2f); // 1 to 41
                options[i] = new ModOptionFloat(value.ToString("F1") + "x", value);
            }
            return options;
        }

        public static ModOptionFloat[] ColorOptions()
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

        // Helper method to get the current rift color
        public static Color GetRiftColor()
        {
            return new Color(colorR, colorG, colorB, colorA);
        }

        // Getter methods for other properties
        public static string RequiredSpellId => requiredSpellId;
        public static string EffectId => effectId;
        public static string MaterialId => materialId;
        public static float GestureDistance => gestureDistance;
        public static float GestureVelocity => gestureVelocity;
        public static float SpellCheckGracePeriod => spellCheckGracePeriod;
    }
}