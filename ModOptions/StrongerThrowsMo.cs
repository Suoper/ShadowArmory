using ThunderRoad;
using UnityEngine;
using System.Collections.Generic;

namespace ShadowArmory
{
    public class StrongerThrowsConfig
    {
        private static readonly string currentUser = "Suoper";
        private static readonly string currentDateTime = "2025-04-09 02:50:26";

        // Core settings
        private static bool enabledThrows = true;
        private static float maxChargeTime = 2.0f;
        private static float minVelocityMultiplier = 1.5f;
        private static float maxVelocityMultiplier = 5.0f;
        private static float throwArcStrength = 5.0f;
        private static float spinForce = 10.0f;

        // Visual settings
        private static bool showChargeEffect = true;
        private static bool showThrowEffect = true;
        private static int selectedChargeEffectIndex = 0;
        private static int selectedThrowEffectIndex = 0;
        private static Color minChargeColor = new Color(0.2f, 0.5f, 1.0f, 0.5f);
        private static Color maxChargeColor = new Color(1.0f, 0.2f, 0.2f, 1.0f);

        // Cache for effect IDs
        private static List<string> effectIds = new List<string>();

        #region Mod Options

        [ModOptionCategory("Throw Settings", 1)]
        [ModOptionOrder(1)]
        [ModOption("Enable Stronger Throws", "Toggle enhanced throwing")]
        public static bool EnabledThrows
        {
            get => enabledThrows;
            set => enabledThrows = value;
        }

        [ModOptionCategory("Throw Settings", 1)]
        [ModOptionOrder(2)]
        [ModOptionSlider]
        [ModOption("Max Charge Time", "Maximum time to charge throw", nameof(ChargeTimeOptions))]
        public static float MaxChargeTime
        {
            get => maxChargeTime;
            set => maxChargeTime = value;
        }

        [ModOptionCategory("Throw Settings", 1)]
        [ModOptionOrder(3)]
        [ModOptionSlider]
        [ModOption("Min Velocity Multiplier", "Minimum throw power multiplier", nameof(MultiplierOptions))]
        public static float MinVelocityMultiplier
        {
            get => minVelocityMultiplier;
            set => minVelocityMultiplier = value;
        }

        [ModOptionCategory("Throw Settings", 1)]
        [ModOptionOrder(4)]
        [ModOptionSlider]
        [ModOption("Max Velocity Multiplier", "Maximum throw power multiplier", nameof(MultiplierOptions))]
        public static float MaxVelocityMultiplier
        {
            get => maxVelocityMultiplier;
            set => maxVelocityMultiplier = value;
        }

        [ModOptionCategory("Throw Settings", 1)]
        [ModOptionOrder(5)]
        [ModOptionSlider]
        [ModOption("Throw Arc Strength", "How much upward arc to add", nameof(ArcOptions))]
        public static float ThrowArcStrength
        {
            get => throwArcStrength;
            set => throwArcStrength = value;
        }

        [ModOptionCategory("Throw Settings", 1)]
        [ModOptionOrder(6)]
        [ModOptionSlider]
        [ModOption("Spin Force", "How much spin to add to throws", nameof(SpinOptions))]
        public static float SpinForce
        {
            get => spinForce;
            set => spinForce = value;
        }

        [ModOptionCategory("Throw Settings", 2)]
        [ModOptionOrder(1)]
        [ModOption("Show Charge Effect", "Display visual effect while charging")]
        public static bool ShowChargeEffect
        {
            get => showChargeEffect;
            set => showChargeEffect = value;
        }

        [ModOptionCategory("Throw Settings", 2)]
        [ModOptionOrder(2)]
        [ModOption("Show Throw Effect", "Display visual effect when throwing")]
        public static bool ShowThrowEffect
        {
            get => showThrowEffect;
            set => showThrowEffect = value;
        }

        [ModOptionCategory("Throw Settings", 2)]
        [ModOptionOrder(3)]
        [ModOptionSlider]
        [ModOption("Charge Effect", "Effect to show while charging", nameof(EffectOptions))]
        public static float ChargeEffectIndex
        {
            get => selectedChargeEffectIndex;
            set => selectedChargeEffectIndex = Mathf.RoundToInt(value);
        }

        [ModOptionCategory("Throw Settings", 2)]
        [ModOptionOrder(4)]
        [ModOptionSlider]
        [ModOption("Throw Effect", "Effect to show when throwing", nameof(EffectOptions))]
        public static float ThrowEffectIndex
        {
            get => selectedThrowEffectIndex;
            set => selectedThrowEffectIndex = Mathf.RoundToInt(value);
        }

        #endregion

        #region Option Arrays

        public static ModOptionFloat[] ChargeTimeOptions()
        {
            ModOptionFloat[] options = new ModOptionFloat[11];
            for (int i = 0; i <= 10; i++)
            {
                float value = 0.5f + (i * 0.25f);
                options[i] = new ModOptionFloat(value.ToString("F1") + "s", value);
            }
            return options;
        }

        public static ModOptionFloat[] MultiplierOptions()
        {
            ModOptionFloat[] options = new ModOptionFloat[11];
            for (int i = 0; i <= 10; i++)
            {
                float value = 1.0f + (i * 0.5f);
                options[i] = new ModOptionFloat(value.ToString("F1") + "x", value);
            }
            return options;
        }

        public static ModOptionFloat[] ArcOptions()
        {
            ModOptionFloat[] options = new ModOptionFloat[11];
            for (int i = 0; i <= 10; i++)
            {
                float value = i * 1.0f;
                options[i] = new ModOptionFloat(value.ToString("F1"), value);
            }
            return options;
        }

        public static ModOptionFloat[] SpinOptions()
        {
            ModOptionFloat[] options = new ModOptionFloat[11];
            for (int i = 0; i <= 10; i++)
            {
                float value = i * 2.0f;
                options[i] = new ModOptionFloat(value.ToString("F1"), value);
            }
            return options;
        }

        public static ModOptionFloat[] EffectOptions()
        {
            List<ModOptionFloat> options = new List<ModOptionFloat>();
            effectIds.Clear();
            var effectList = Catalog.GetDataList<EffectData>();

            int index = 0;
            foreach (var effectData in effectList)
            {
                options.Add(new ModOptionFloat(effectData.id, index));
                effectIds.Add(effectData.id);
                index++;
            }

            return options.ToArray();
        }

        #endregion

        // Effect getters
        public static string ChargeEffectId
        {
            get
            {
                if (effectIds.Count > selectedChargeEffectIndex)
                    return effectIds[selectedChargeEffectIndex];
                return "ThrowChargeEffect";
            }
        }

        public static string ThrowEffectId
        {
            get
            {
                if (effectIds.Count > selectedThrowEffectIndex)
                    return effectIds[selectedThrowEffectIndex];
                return "ThrowReleaseEffect";
            }
        }

        public static Color MinChargeColor => minChargeColor;
        public static Color MaxChargeColor => maxChargeColor;
    }
}