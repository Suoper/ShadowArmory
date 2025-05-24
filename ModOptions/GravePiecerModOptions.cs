using ThunderRoad;
using UnityEngine;
using System.Collections.Generic;

namespace ShadowArmory
{
    public class GravepiecerConfig
    {
        private static readonly string currentUser = "Suoper";
        private static readonly string currentDateTime = "2025-04-09 20:36:32";

        // Core settings
        private static bool enabledGravepiercer = true;
        private static float cooldown = 3.0f;
        private static float spikeDamage = 15f;
        private static float spikeForce = 1000f;
        private static float spikeUpwardForce = 500f;
        private static float spikeRadius = 0.5f;
        private static float spikeDuration = 3.0f;
        private static float spikeDelay = 0.1f;

        // Effect settings
        private static int selectedSpikeEffectIndex = 0;
        private static int selectedImpactEffectIndex = 0;
        private static int selectedGroundEffectIndex = 0;
        private static int selectedEnemyEffectIndex = 0;

        // Cache for effect IDs
        private static List<string> effectIds = new List<string>();

        #region Mod Options

        [ModOptionCategory("Gravepiercer Settings", 1)]
        [ModOptionOrder(1)]
        [ModOption("Enable Gravepiercer", "Toggle the Gravepiercer ability")]
        public static bool EnabledGravepiercer
        {
            get => enabledGravepiercer;
            set => enabledGravepiercer = value;
        }

        [ModOptionCategory("Gravepiercer Settings", 1)]
        [ModOptionOrder(2)]
        [ModOptionSlider]
        [ModOption("Cooldown", "Time between uses", nameof(CooldownOptions))]
        public static float Cooldown
        {
            get => cooldown;
            set => cooldown = value;
        }

        [ModOptionCategory("Gravepiercer Settings", 2)]
        [ModOptionOrder(1)]
        [ModOptionSlider]
        [ModOption("Spike Damage", "Damage dealt by spikes", nameof(DamageOptions))]
        public static float SpikeDamage
        {
            get => spikeDamage;
            set => spikeDamage = value;
        }

        [ModOptionCategory("Gravepiercer Settings", 2)]
        [ModOptionOrder(2)]
        [ModOptionSlider]
        [ModOption("Spike Force", "Push force of spikes", nameof(ForceOptions))]
        public static float SpikeForce
        {
            get => spikeForce;
            set => spikeForce = value;
        }

        [ModOptionCategory("Gravepiercer Settings", 3)]
        [ModOptionOrder(1)]
        [ModOptionSlider]
        [ModOption("Spike Effect", "Visual effect for spikes", nameof(EffectOptions))]
        public static float SpikeEffectIndex
        {
            get => selectedSpikeEffectIndex;
            set => selectedSpikeEffectIndex = Mathf.RoundToInt(value);
        }

        [ModOptionCategory("Gravepiercer Settings", 3)]
        [ModOptionOrder(2)]
        [ModOptionSlider]
        [ModOption("Impact Effect", "Effect on spike emergence", nameof(EffectOptions))]
        public static float ImpactEffectIndex
        {
            get => selectedImpactEffectIndex;
            set => selectedImpactEffectIndex = Mathf.RoundToInt(value);
        }

        [ModOptionCategory("Gravepiercer Settings", 3)]
        [ModOptionOrder(3)]
        [ModOptionSlider]
        [ModOption("Ground Effect", "Effect on ground", nameof(EffectOptions))]
        public static float GroundEffectIndex
        {
            get => selectedGroundEffectIndex;
            set => selectedGroundEffectIndex = Mathf.RoundToInt(value);
        }

        [ModOptionCategory("Gravepiercer Settings", 3)]
        [ModOptionOrder(4)]
        [ModOptionSlider]
        [ModOption("Enemy Effect", "Effect on hit enemies", nameof(EffectOptions))]
        public static float EnemyEffectIndex
        {
            get => selectedEnemyEffectIndex;
            set => selectedEnemyEffectIndex = Mathf.RoundToInt(value);
        }

        #endregion

        #region Option Arrays

        public static ModOptionFloat[] CooldownOptions()
        {
            ModOptionFloat[] options = new ModOptionFloat[11];
            for (int i = 0; i <= 10; i++)
            {
                float value = 1.0f + (i * 0.5f);
                options[i] = new ModOptionFloat(value.ToString("F1") + "s", value);
            }
            return options;
        }

        public static ModOptionFloat[] DamageOptions()
        {
            ModOptionFloat[] options = new ModOptionFloat[11];
            for (int i = 0; i <= 10; i++)
            {
                float value = 5f + (i * 5f);
                options[i] = new ModOptionFloat(value.ToString("F0"), value);
            }
            return options;
        }

        public static ModOptionFloat[] ForceOptions()
        {
            ModOptionFloat[] options = new ModOptionFloat[11];
            for (int i = 0; i <= 10; i++)
            {
                float value = 500f + (i * 250f);
                options[i] = new ModOptionFloat(value.ToString("F0"), value);
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
        public static string SpikeEffectId => effectIds.Count > selectedSpikeEffectIndex ?
            effectIds[selectedSpikeEffectIndex] : "SpikeEffect";

        public static string ImpactEffectId => effectIds.Count > selectedImpactEffectIndex ?
            effectIds[selectedImpactEffectIndex] : "ImpactEffect";

        public static string GroundEffectId => effectIds.Count > selectedGroundEffectIndex ?
            effectIds[selectedGroundEffectIndex] : "GroundEffect";

        public static string EnemyEffectId => effectIds.Count > selectedEnemyEffectIndex ?
            effectIds[selectedEnemyEffectIndex] : "EnemyEffect";

        // Additional getters
        public static float SpikeRadius => spikeRadius;
        public static float SpikeDuration => spikeDuration;
        public static float SpikeDelay => spikeDelay;
        public static float SpikeUpwardForce => spikeUpwardForce;
    }
}