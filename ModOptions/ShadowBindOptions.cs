using ThunderRoad;
using UnityEngine;
using System.Collections.Generic;

namespace ShadowArmory
{
    public class ShadowBindConfig
    {
        private static readonly string currentUser = "Suoper";
        private static readonly string currentDateTime = "2025-04-10 00:10:51";

        // Core settings
        private static bool enabledShadowBind = true;
        private static float bindDuration = 4.0f;
        private static float chargeTime = 0.5f;
        private static float bindRange = 15f;
        private static float targetingAngle = 200f;
        private static float bindForce = 50f;
        private static float holdForce = 25f;
        private static float chainHeight = 8f;
        private static float chainWidth = 0.1f;
        private static float runeIntensity = 1.0f;
        private static float shadowOpacity = 0.8f;

        // Effect settings
        private static int selectedChargingEffectIndex = 0;
        private static int selectedTargetingEffectIndex = 0;
        private static int selectedChainEffectIndex = 0;
        private static int selectedBindEffectIndex = 0;
        private static int selectedBindSoundIndex = 0;

        // Cache for effect IDs
        private static List<string> effectIds = new List<string>();
        private static List<string> soundIds = new List<string>();

        #region Mod Options

        [ModOptionCategory("Shadow Bind Settings", 1)]
        [ModOptionOrder(1)]
        [ModOption("Enable Shadow Bind", "Toggle the Shadow Bind ability")]
        public static bool EnabledShadowBind
        {
            get => enabledShadowBind;
            set => enabledShadowBind = value;
        }

        [ModOptionCategory("Shadow Bind Settings", 1)]
        [ModOptionOrder(2)]
        [ModOptionSlider]
        [ModOption("Bind Duration", "How long enemies stay bound", nameof(DurationOptions))]
        public static float BindDuration
        {
            get => bindDuration;
            set => bindDuration = value;
        }

        [ModOptionCategory("Shadow Bind Settings", 1)]
        [ModOptionOrder(3)]
        [ModOptionSlider]
        [ModOption("Charge Time", "Time needed to charge the bind", nameof(ChargeTimeOptions))]
        public static float ChargeTime
        {
            get => chargeTime;
            set => chargeTime = value;
        }

        [ModOptionCategory("Shadow Bind Settings", 1)]
        [ModOptionOrder(4)]
        [ModOptionSlider]
        [ModOption("Bind Range", "Maximum range of the bind", nameof(RangeOptions))]
        public static float BindRange
        {
            get => bindRange;
            set => bindRange = value;
        }

        [ModOptionCategory("Shadow Bind Settings", 1)]
        [ModOptionOrder(5)]
        [ModOptionSlider]
        [ModOption("Chain Width", "Width of the shadow chains", nameof(WidthOptions))]
        public static float ChainWidth
        {
            get => chainWidth;
            set => chainWidth = value;
        }

        [ModOptionCategory("Shadow Bind Settings", 1)]
        [ModOptionOrder(6)]
        [ModOptionSlider]
        [ModOption("Bind Force", "Initial force applied when binding", nameof(ForceOptions))]
        public static float BindForce
        {
            get => bindForce;
            set => bindForce = value;
        }

        [ModOptionCategory("Shadow Bind Settings", 1)]
        [ModOptionOrder(7)]
        [ModOptionSlider]
        [ModOption("Hold Force", "Continuous force to hold enemies", nameof(ForceOptions))]
        public static float HoldForce
        {
            get => holdForce;
            set => holdForce = value;
        }

        [ModOptionCategory("Shadow Bind Settings", 1)]
        [ModOptionOrder(8)]
        [ModOptionSlider]
        [ModOption("Chain Height", "Height of the chain anchor points", nameof(HeightOptions))]
        public static float ChainHeight
        {
            get => chainHeight;
            set => chainHeight = value;
        }

        [ModOptionCategory("Shadow Bind Settings", 1)]
        [ModOptionOrder(9)]
        [ModOptionSlider]
        [ModOption("Charging Effect", "Effect while charging", nameof(EffectOptions))]
        public static float ChargingEffectIndex
        {
            get => selectedChargingEffectIndex;
            set => selectedChargingEffectIndex = Mathf.RoundToInt(value);
        }

        [ModOptionCategory("Shadow Bind Settings", 1)]
        [ModOptionOrder(10)]
        [ModOptionSlider]
        [ModOption("Target Effect", "Effect on targeted enemy", nameof(EffectOptions))]
        public static float TargetingEffectIndex
        {
            get => selectedTargetingEffectIndex;
            set => selectedTargetingEffectIndex = Mathf.RoundToInt(value);
        }

        [ModOptionCategory("Shadow Bind Settings", 1)]
        [ModOptionOrder(11)]
        [ModOptionSlider]
        [ModOption("Chain Effect", "Visual effect for the chains", nameof(EffectOptions))]
        public static float ChainEffectIndex
        {
            get => selectedChainEffectIndex;
            set => selectedChainEffectIndex = Mathf.RoundToInt(value);
        }

        [ModOptionCategory("Shadow Bind Settings", 1)]
        [ModOptionOrder(12)]
        [ModOptionSlider]
        [ModOption("Bind Effect", "Effect when binding enemies", nameof(EffectOptions))]
        public static float BindEffectIndex
        {
            get => selectedBindEffectIndex;
            set => selectedBindEffectIndex = Mathf.RoundToInt(value);
        }

        [ModOptionCategory("Shadow Bind Settings", 1)]
        [ModOptionOrder(13)]
        [ModOptionSlider]
        [ModOption("Bind Sound", "Sound when binding enemies", nameof(SoundOptions))]
        public static float BindSoundIndex
        {
            get => selectedBindSoundIndex;
            set => selectedBindSoundIndex = Mathf.RoundToInt(value);
        }

        #endregion

        #region Option Arrays

        public static ModOptionFloat[] DurationOptions()
        {
            ModOptionFloat[] options = new ModOptionFloat[9];
            for (int i = 0; i < 9; i++)
            {
                float value = 2.0f + (i * 0.5f);
                options[i] = new ModOptionFloat(value.ToString("F1") + "s", value);
            }
            return options;
        }

        public static ModOptionFloat[] ChargeTimeOptions()
        {
            ModOptionFloat[] options = new ModOptionFloat[6];
            for (int i = 0; i < 6; i++)
            {
                float value = 0.3f + (i * 0.1f);
                options[i] = new ModOptionFloat(value.ToString("F1") + "s", value);
            }
            return options;
        }

        public static ModOptionFloat[] RangeOptions()
        {
            ModOptionFloat[] options = new ModOptionFloat[8];
            for (int i = 0; i < 8; i++)
            {
                float value = 10f + (i * 2.5f);
                options[i] = new ModOptionFloat(value.ToString("F1") + "m", value);
            }
            return options;
        }

        public static ModOptionFloat[] WidthOptions()
        {
            ModOptionFloat[] options = new ModOptionFloat[5];
            for (int i = 0; i < 5; i++)
            {
                float value = 0.05f + (i * 0.05f);
                options[i] = new ModOptionFloat((value * 100f).ToString("F0") + "%", value);
            }
            return options;
        }

        public static ModOptionFloat[] ForceOptions()
        {
            ModOptionFloat[] options = new ModOptionFloat[10];
            for (int i = 0; i < 10; i++)
            {
                float value = 10f + (i * 10f);
                options[i] = new ModOptionFloat(value.ToString("F0") + "N", value);
            }
            return options;
        }

        public static ModOptionFloat[] HeightOptions()
        {
            ModOptionFloat[] options = new ModOptionFloat[8];
            for (int i = 0; i < 8; i++)
            {
                float value = 5f + (i * 1f);
                options[i] = new ModOptionFloat(value.ToString("F1") + "m", value);
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

        public static ModOptionFloat[] SoundOptions()
        {
            List<ModOptionFloat> options = new List<ModOptionFloat>();
            soundIds.Clear();
            var soundList = Catalog.GetDataList<EffectData>();

            int index = 0;
            foreach (var soundData in soundList)
            {
                if (soundData.id.Contains("Sound") || soundData.id.Contains("Audio") ||
                    soundData.id.Contains("SFX") || soundData.id.Contains("sfx"))
                {
                    options.Add(new ModOptionFloat(soundData.id, index));
                    soundIds.Add(soundData.id);
                    index++;
                }
            }

            return options.ToArray();
        }

        #endregion

        #region Effect ID Getters

        public static string ChargingEffectId => effectIds.Count > selectedChargingEffectIndex ?
            effectIds[selectedChargingEffectIndex] : "ChargingEffect";

        public static string TargetingEffectId => effectIds.Count > selectedTargetingEffectIndex ?
            effectIds[selectedTargetingEffectIndex] : "TargetingEffect";

        public static string ChainEffectId => effectIds.Count > selectedChainEffectIndex ?
            effectIds[selectedChainEffectIndex] : "ChainEffect";

        public static string BindEffectId => effectIds.Count > selectedBindEffectIndex ?
            effectIds[selectedBindEffectIndex] : "BindEffect";

        public static string BindSoundId => soundIds.Count > selectedBindSoundIndex ?
            soundIds[selectedBindSoundIndex] : "BindSound";

        #endregion

        // Additional getters
        public static float TargetingAngle => targetingAngle;
    }
}