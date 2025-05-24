using UnityEngine;
using System.Collections.Generic;
using ThunderRoad;

namespace ShadowArmory
{
    /// <summary>
    /// Configuration class for Shadow Armory mod settings
    /// </summary>
    public class ShadowConfig
    {
        // ==========================================
        // PRIVATE FIELDS ORGANIZED BY CATEGORY
        // ==========================================

        // General Settings
        private static float CooldownDuration = 1.5f;
        public static bool CleanupOnSpellEnd = false;

        // Gesture & Weapon Selection
        public static int selectedWeaponIndex = 0;
        private static int upWeaponIndex = 0;
        private static int downWeaponIndex = 0;
        private static int leftWeaponIndex = 0;
        private static int rightWeaponIndex = 0;
        private static float ArmSpread = 1.2f;
        private static int forwardWeaponIndex = 0;
        private static int inWeaponIndex = 0;
        private static int outWeaponIndex = 0;


        // Rift Settings
        private static bool useRift = true;
        private static float riftPositionOffset = 0f;
        private static bool destroyRiftOnPull = true;

        // Rift Visual Effects
        private static float riftBrightness = 10.0f;
        private static float riftOpeningSpeed = 1.0f;
        private static float riftDistortionStrength = 0.1f;
        private static float riftGlowIntensity = 10.0f;
        private static float riftPulseSpeed = 1.0f;
        private static float riftRimPower = 2.0f;
        private static float riftStarsBrightness = 5.0f;
        public static int selectedEffectIndex = 0;

        // Rift Colors
        private static float riftGlowColorR = 0.5f;
        private static float riftGlowColorG = 1.0f;
        private static float riftGlowColorB = 1.0f;
        private static float riftPulseColor1R = 0.5f;
        private static float riftPulseColor1G = 0.0f;
        private static float riftPulseColor1B = 0.5f;
        private static float riftPulseColor2R = 1.0f;
        private static float riftPulseColor2G = 1.0f;
        private static float riftPulseColor2B = 0.0f;
        private static float riftRimColorR = 1.0f;
        private static float riftRimColorG = 0.5f;
        private static float riftRimColorB = 0.5f;

        // Portal Effects
        private static float brightness = 1.0f;
        private static float openingSpeed = 1.0f;
        private static float distortionStrength = 0.1f;

        // Weapon Settings
        private static bool useBlackWeaponEffect = true;
        private static float WeaponStickOutDistance = 0.2f;

        // Weapon Effects
        private static bool applyShaderToWeapons = true;
        private static bool useCustomWeaponShader = true;
        private static bool traileffect = false;

        // Weapon Shader Effects
        private static float weaponBrightness = 1.0f;
        private static float weaponOpeningSpeed = 1.0f;
        private static float weaponDistortionStrength = 0.1f;
        private static float glowIntensity = 2.0f;
        private static float pulseSpeed = 1.0f;
        private static float rimPower = 2.0f;
        private static float starsBrightness = 5.0f;

        // Weapon Shader Colors
        private static Color glowColor = new Color(0.5f, 1f, 1f, 1f);
        private static Color pulseColor1 = new Color(0.5f, 0f, 0.5f, 1f);
        private static Color pulseColor2 = new Color(1f, 1f, 0f, 1f);
        private static Color rimColor = new Color(1f, 0.5f, 0.5f, 1f);

        // Physics Settings
        private static bool useSpringConstraints = true;
        private static float SpringStrength = 2000f;
        private static float SpringDamping = 20f;
        private static float ReleaseDistance = 0.5f;

        private enum Direction
        {
            X,
            Y,
            Z
        }

        // ==========================================
        // PUBLIC PROPERTIES WITH MODOPTION ATTRIBUTES
        // ==========================================

        // =================
        // 1. GENERAL SETTINGS
        // =================

        [ModOptionCategory("1. General Settings", 1)]
        [ModOptionOrder(1)]
        [ModOption("Cooldown Duration", "How long before you can cast again", nameof(CooldownOptions))]
        public static float CooldownDurationOption
        {
            get => CooldownDuration;
            set => CooldownDuration = value;
        }

        [ModOptionCategory("1. General Settings", 1)]
        [ModOptionOrder(2)]
        [ModOption("Clean Up On Spell End", "When disabled, created objects will remain after the spell ends")]
        public static bool CleanupOnSpellEndOption
        {
            get => CleanupOnSpellEnd;
            set => CleanupOnSpellEnd = value;
        }

        [ModOptionCategory("1. General Settings", 1)]
        [ModOptionOrder(3)]
        [ModOptionTooltip("How Far the arm Spread is for The Form to activate")]
        [ModOptionSlider]
        [ModOption("Arm Spread Distance", "Set how far apart your arms need to be", nameof(ArmSpreadOptions))]
        public static float ArmSpreads
        {
            get => ArmSpread;
            set => ArmSpread = value;
        }

        // =================
        // 2. WEAPON SELECTION
        // =================


        [ModOptionCategory("2. Weapon Selection", 2)]
        [ModOptionOrder(6)]
        [ModOptionTooltip("Select the weapon to summon with FORWARD gesture")]
        [ModOptionSlider]
        [ModOption("FORWARD Gesture Weapon", "Select weapon for FORWARD gesture", nameof(WeaponOptions), defaultValueIndex = 0)]
        public static float ForwardWeaponSlider
        {
            get => forwardWeaponIndex;
            set => forwardWeaponIndex = (int)value;
        }

        [ModOptionCategory("2. Weapon Selection", 2)]
        [ModOptionOrder(7)]
        [ModOptionTooltip("Select the weapon to summon with IN gesture (toward body)")]
        [ModOptionSlider]
        [ModOption("IN Gesture Weapon(Left)", "Select weapon for IN gesture", nameof(WeaponOptions), defaultValueIndex = 0)]
        public static float InWeaponSlider
        {
            get => inWeaponIndex;
            set => inWeaponIndex = (int)value;
        }

        [ModOptionCategory("2. Weapon Selection", 2)]
        [ModOptionOrder(8)]
        [ModOptionTooltip("Select the weapon to summon with OUT gesture (away from body)")]
        [ModOptionSlider]
        [ModOption("OUT Gesture Weapon(Right)", "Select weapon for OUT gesture", nameof(WeaponOptions), defaultValueIndex = 0)]
        public static float OutWeaponSlider
        {
            get => outWeaponIndex;
            set => outWeaponIndex = (int)value;
        }

        [ModOptionCategory("2. Weapon Selection", 2)]
        [ModOptionOrder(1)]
        [ModOptionTooltip("Select the default weapon to summon")]
        [ModOptionSlider]
        [ModOption("Default Weapon", "Select the default weapon to summon", nameof(WeaponOptions), defaultValueIndex = 0)]
        public static float WeaponSlider
        {
            get => selectedWeaponIndex;
            set => selectedWeaponIndex = (int)value;
        }

        [ModOptionCategory("2. Weapon Selection", 2)]
        [ModOptionOrder(2)]
        [ModOptionTooltip("Select the weapon to summon with UP gesture")]
        [ModOptionSlider]
        [ModOption("UP Gesture Weapon", "Select weapon for UP gesture", nameof(WeaponOptions), defaultValueIndex = 0)]
        public static float UpWeaponSlider
        {
            get => upWeaponIndex;
            set => upWeaponIndex = (int)value;
        }

        [ModOptionCategory("2. Weapon Selection", 2)]
        [ModOptionOrder(3)]
        [ModOptionTooltip("Select the weapon to summon with DOWN gesture")]
        [ModOptionSlider]
        [ModOption("DOWN Gesture Weapon", "Select weapon for DOWN gesture", nameof(WeaponOptions), defaultValueIndex = 0)]
        public static float DownWeaponSlider
        {
            get => downWeaponIndex;
            set => downWeaponIndex = (int)value;
        }
        

        // =================
        // 3. RIFT SETTINGS
        // =================

        [ModOptionCategory("3. Rift Settings", 3)]
        [ModOptionOrder(1)]
        [ModOption("Use Rift", "Enable or disable the visual rift portal")]
        public static bool UseRift
        {
            get => useRift;
            set => useRift = value;
        }

        [ModOptionCategory("3. Rift Settings", 3)]
        [ModOptionOrder(2)]
        [ModOption("Rift Position Offset", "Distance of the rift from your hand", nameof(OffsetOptions))]
        public static float RiftPositionOffset
        {
            get => riftPositionOffset;
            set => riftPositionOffset = value;
        }

        [ModOptionCategory("3. Rift Settings", 3)]
        [ModOptionOrder(3)]
        [ModOption("Destroy Rift on Pull", "Should the rift disappear when weapon is pulled out?")]
        public static bool DestroyRiftOnPull
        {
            get => destroyRiftOnPull;
            set => destroyRiftOnPull = value;
        }

        [ModOptionCategory("3. Rift Settings", 3)]
        [ModOptionOrder(4)]
        [ModOptionTooltip("Select the effect to use when opening a rift")]
        [ModOptionSlider]
        [ModOption("Rift Effect", "Select the visual effect for the rift", nameof(EffectOptions), defaultValueIndex = 0)]
        public static float EffectSlider
        {
            get => selectedEffectIndex;
            set => selectedEffectIndex = (int)value;
        }

        // =================
        // 4. RIFT VISUAL EFFECTS
        // =================

        [ModOptionCategory("4. Rift Visual Effects", 4)]
        [ModOptionOrder(1)]
        [ModOptionSlider]
        [ModOption("Rift Brightness", "Adjust the brightness of the rift", nameof(BrightnessOptions))]
        public static float RiftBrightness
        {
            get => riftBrightness;
            set => riftBrightness = value;
        }

        [ModOptionCategory("4. Rift Visual Effects", 4)]
        [ModOptionOrder(2)]
        [ModOptionSlider]
        [ModOption("Rift Opening Speed", "Adjust how fast the rift opens", nameof(OpeningSpeedOptions))]
        public static float RiftOpeningSpeed
        {
            get => riftOpeningSpeed;
            set => riftOpeningSpeed = value;
        }

        [ModOptionCategory("4. Rift Visual Effects", 4)]
        [ModOptionOrder(3)]
        [ModOptionSlider]
        [ModOption("Rift Distortion", "Adjust the strength of the rift distortion", nameof(DistortionOptions))]
        public static float RiftDistortionStrength
        {
            get => riftDistortionStrength;
            set => riftDistortionStrength = value;
        }

        [ModOptionCategory("4. Rift Visual Effects", 4)]
        [ModOptionOrder(4)]
        [ModOptionSlider]
        [ModOption("Rift Glow Intensity", "Adjust the intensity of the rift glow", nameof(BrightnessOptions))]
        public static float RiftGlowIntensity
        {
            get => riftGlowIntensity;
            set => riftGlowIntensity = value;
        }

        [ModOptionCategory("4. Rift Visual Effects", 4)]
        [ModOptionOrder(5)]
        [ModOptionSlider]
        [ModOption("Rift Pulse Speed", "Adjust the speed of the rift pulse", nameof(BrightnessOptions))]
        public static float RiftPulseSpeed
        {
            get => riftPulseSpeed;
            set => riftPulseSpeed = value;
        }

        [ModOptionCategory("4. Rift Visual Effects", 4)]
        [ModOptionOrder(6)]
        [ModOptionSlider]
        [ModOption("Rift Rim Power", "Adjust the power of the rift rim effect", nameof(BrightnessOptions))]
        public static float RiftRimPower
        {
            get => riftRimPower;
            set => riftRimPower = value;
        }

        [ModOptionCategory("4. Rift Visual Effects", 4)]
        [ModOptionOrder(7)]
        [ModOptionSlider]
        [ModOption("Rift Stars Brightness", "Adjust the brightness of the rift stars", nameof(BrightnessOptions))]
        public static float RiftStarsBrightness
        {
            get => riftStarsBrightness;
            set => riftStarsBrightness = value;
        }

        // =================
        // 5. RIFT COLORS
        // =================

        [ModOptionCategory("5. Rift Colors", 5)]
        [ModOptionOrder(1)]
        [ModOptionSlider]
        [ModOption("Rift Glow Color - Red", "Adjust the red component of the rift glow", nameof(ColorComponentOptions))]
        public static float RiftGlowColorR
        {
            get => riftGlowColorR;
            set => riftGlowColorR = value;
        }

        [ModOptionCategory("5. Rift Colors", 5)]
        [ModOptionOrder(2)]
        [ModOptionSlider]
        [ModOption("Rift Glow Color - Green", "Adjust the green component of the rift glow", nameof(ColorComponentOptions))]
        public static float RiftGlowColorG
        {
            get => riftGlowColorG;
            set => riftGlowColorG = value;
        }

        [ModOptionCategory("5. Rift Colors", 5)]
        [ModOptionOrder(3)]
        [ModOptionSlider]
        [ModOption("Rift Glow Color - Blue", "Adjust the blue component of the rift glow", nameof(ColorComponentOptions))]
        public static float RiftGlowColorB
        {
            get => riftGlowColorB;
            set => riftGlowColorB = value;
        }

        [ModOptionCategory("5. Rift Colors", 5)]
        [ModOptionOrder(4)]
        [ModOptionSlider]
        [ModOption("Rift Pulse Color 1 - Red", "Adjust the red component of first pulse color", nameof(ColorComponentOptions))]
        public static float RiftPulseColor1R
        {
            get => riftPulseColor1R;
            set => riftPulseColor1R = value;
        }

        [ModOptionCategory("5. Rift Colors", 5)]
        [ModOptionOrder(5)]
        [ModOptionSlider]
        [ModOption("Rift Pulse Color 1 - Green", "Adjust the green component of first pulse color", nameof(ColorComponentOptions))]
        public static float RiftPulseColor1G
        {
            get => riftPulseColor1G;
            set => riftPulseColor1G = value;
        }

        [ModOptionCategory("5. Rift Colors", 5)]
        [ModOptionOrder(6)]
        [ModOptionSlider]
        [ModOption("Rift Pulse Color 1 - Blue", "Adjust the blue component of first pulse color", nameof(ColorComponentOptions))]
        public static float RiftPulseColor1B
        {
            get => riftPulseColor1B;
            set => riftPulseColor1B = value;
        }

        [ModOptionCategory("5. Rift Colors", 5)]
        [ModOptionOrder(7)]
        [ModOptionSlider]
        [ModOption("Rift Pulse Color 2 - Red", "Adjust the red component of second pulse color", nameof(ColorComponentOptions))]
        public static float RiftPulseColor2R
        {
            get => riftPulseColor2R;
            set => riftPulseColor2R = value;
        }

        [ModOptionCategory("5. Rift Colors", 5)]
        [ModOptionOrder(8)]
        [ModOptionSlider]
        [ModOption("Rift Pulse Color 2 - Green", "Adjust the green component of second pulse color", nameof(ColorComponentOptions))]
        public static float RiftPulseColor2G
        {
            get => riftPulseColor2G;
            set => riftPulseColor2G = value;
        }

        [ModOptionCategory("5. Rift Colors", 5)]
        [ModOptionOrder(9)]
        [ModOptionSlider]
        [ModOption("Rift Pulse Color 2 - Blue", "Adjust the blue component of second pulse color", nameof(ColorComponentOptions))]
        public static float RiftPulseColor2B
        {
            get => riftPulseColor2B;
            set => riftPulseColor2B = value;
        }

        [ModOptionCategory("5. Rift Colors", 5)]
        [ModOptionOrder(10)]
        [ModOptionSlider]
        [ModOption("Rift Rim Color - Red", "Adjust the red component of the rim effect", nameof(ColorComponentOptions))]
        public static float RiftRimColorR
        {
            get => riftRimColorR;
            set => riftRimColorR = value;
        }

        [ModOptionCategory("5. Rift Colors", 5)]
        [ModOptionOrder(11)]
        [ModOptionSlider]
        [ModOption("Rift Rim Color - Green", "Adjust the green component of the rim effect", nameof(ColorComponentOptions))]
        public static float RiftRimColorG
        {
            get => riftRimColorG;
            set => riftRimColorG = value;
        }

        [ModOptionCategory("5. Rift Colors", 5)]
        [ModOptionOrder(12)]
        [ModOptionSlider]
        [ModOption("Rift Rim Color - Blue", "Adjust the blue component of the rim effect", nameof(ColorComponentOptions))]
        public static float RiftRimColorB
        {
            get => riftRimColorB;
            set => riftRimColorB = value;
        }

        // =================
        // 6. PORTAL EFFECTS
        // =================

        [ModOptionCategory("6. Portal Effects", 6)]
        [ModOptionOrder(1)]
        [ModOptionSlider]
        [ModOption("Portal Brightness", "Adjust the brightness of the portal", nameof(BrightnessOptions))]
        public static float Brightness
        {
            get => brightness;
            set => brightness = Mathf.Clamp(value, 0.01f, 10f);
        }

        [ModOptionCategory("6. Portal Effects", 6)]
        [ModOptionOrder(2)]
        [ModOptionSlider]
        [ModOption("Portal Opening Speed", "Adjust how fast the portal opens", nameof(OpeningSpeedOptions))]
        public static float OpeningSpeed
        {
            get => openingSpeed;
            set => openingSpeed = value;
        }

        [ModOptionCategory("6. Portal Effects", 6)]
        [ModOptionOrder(3)]
        [ModOptionSlider]
        [ModOption("Portal Distortion", "Adjust the strength of the portal distortion", nameof(DistortionOptions))]
        public static float DistortionStrength
        {
            get => distortionStrength;
            set => distortionStrength = value;
        }

        // =================
        // 7. WEAPON SETTINGS
        // =================

        [ModOptionCategory("7. Weapon Settings(NeededForShaders)", 7)]
        [ModOptionOrder(1)]
        [ModOption("Black Weapon Effect", "Make summoned weapons appear black")]
        public static bool UseBlackWeaponEffect
        {
            get => useBlackWeaponEffect;
            set => useBlackWeaponEffect = value;
        }

        [ModOptionCategory("7. Weapon Settings(NeededForShaders)", 7)]
        [ModOptionOrder(2)]
        [ModOption("Weapon Stick Out Distance", "How far the weapon sticks out of the rift", nameof(StickOutOptions))]
        public static float WeaponStickOutDistanceOption
        {
            get => WeaponStickOutDistance;
            set => WeaponStickOutDistance = value;
        }

        [ModOptionCategory("7. Weapon Settings(NeededForShaders)", 7)]
        [ModOptionOrder(3)]
        [ModOption("Apply Portal Effects to Weapons", "Apply the portal shader effects to spawned weapons")]
        public static bool ApplyShaderToWeapons
        {
            get => applyShaderToWeapons;
            set => applyShaderToWeapons = value;
        }

        [ModOptionCategory("7. Weapon Settings(NeededForShaders)", 7)]
        [ModOptionOrder(4)]
        [ModOption("Apply Trail Effects to Weapons", "Apply the Trail effects to spawned weapons")]
        public static bool Trail
        {
            get => traileffect;
            set => traileffect = value;
        }

        [ModOptionCategory("7. Weapon Settings(NeededForShaders)", 7)]
        [ModOptionOrder(5)]
        [ModOption("Use Portal Shader on Weapons", "Replace weapon materials with the portal shader")]
        public static bool UseCustomWeaponShader
        {
            get => useCustomWeaponShader;
            set => useCustomWeaponShader = value;
        }

        // =================
        // 8. WEAPON SHADER EFFECTS
        // =================

        [ModOptionCategory("8. Weapon Shader Effects", 8)]
        [ModOptionOrder(1)]
        [ModOptionSlider]
        [ModOption("Weapon Brightness", "Adjust the brightness of the weapon shader", nameof(BrightnessOptions))]
        public static float WeaponBrightness
        {
            get => weaponBrightness;
            set => weaponBrightness = value;
        }

        [ModOptionCategory("8. Weapon Shader Effects", 8)]
        [ModOptionOrder(2)]
        [ModOptionSlider]
        [ModOption("Weapon Opening Speed", "Adjust how fast the weapon shader animates", nameof(OpeningSpeedOptions))]
        public static float WeaponOpeningSpeed
        {
            get => weaponOpeningSpeed;
            set => weaponOpeningSpeed = value;
        }

        [ModOptionCategory("8. Weapon Shader Effects", 8)]
        [ModOptionOrder(3)]
        [ModOptionSlider]
        [ModOption("Weapon Distortion", "Adjust the strength of the weapon shader distortion", nameof(DistortionOptions))]
        public static float WeaponDistortionStrength
        {
            get => weaponDistortionStrength;
            set => weaponDistortionStrength = value;
        }

        [ModOptionCategory("8. Weapon Shader Effects", 8)]
        [ModOptionOrder(4)]
        [ModOptionSlider]
        [ModOption("Glow Intensity", "Adjust the intensity of the weapon glow", nameof(BrightnessOptions))]
        public static float GlowIntensity
        {
            get => glowIntensity;
            set => glowIntensity = value;
        }

        [ModOptionCategory("8. Weapon Shader Effects", 8)]
        [ModOptionOrder(5)]
        [ModOptionSlider]
        [ModOption("Pulse Speed", "Adjust the speed of the color pulse effect", nameof(BrightnessOptions))]
        public static float PulseSpeed
        {
            get => pulseSpeed;
            set => pulseSpeed = value;
        }

        [ModOptionCategory("8. Weapon Shader Effects", 8)]
        [ModOptionOrder(6)]
        [ModOptionSlider]
        [ModOption("Rim Power", "Adjust the power of the rim lighting effect", nameof(BrightnessOptions))]
        public static float RimPower
        {
            get => rimPower;
            set => rimPower = value;
        }

        [ModOptionCategory("8. Weapon Shader Effects", 8)]
        [ModOptionOrder(7)]
        [ModOptionSlider]
        [ModOption("Stars Brightness", "Adjust the brightness of the stars effect", nameof(BrightnessOptions))]
        public static float StarsBrightness
        {
            get => starsBrightness;
            set => starsBrightness = value;
        }

        // =================
        // 9. WEAPON SHADER COLORS
        // =================

        [ModOptionCategory("9. Weapon Shader Colors", 9)]
        [ModOptionOrder(1)]
        [ModOptionTooltip("Set the red component of the glow color")]
        [ModOptionSlider]
        [ModOption("Glow Color - Red", "Adjust the red component of the glow", nameof(ColorComponentOptions))]
        public static float GlowColorR
        {
            get => glowColor.r;
            set => glowColor = new Color(value, glowColor.g, glowColor.b, 1f);
        }

        [ModOptionCategory("9. Weapon Shader Colors", 9)]
        [ModOptionOrder(2)]
        [ModOptionTooltip("Set the green component of the glow color")]
        [ModOptionSlider]
        [ModOption("Glow Color - Green", "Adjust the green component of the glow", nameof(ColorComponentOptions))]
        public static float GlowColorG
        {
            get => glowColor.g;
            set => glowColor = new Color(glowColor.r, value, glowColor.b, 1f);
        }

        [ModOptionCategory("9. Weapon Shader Colors", 9)]
        [ModOptionOrder(3)]
        [ModOptionTooltip("Set the blue component of the glow color")]
        [ModOptionSlider]
        [ModOption("Glow Color - Blue", "Adjust the blue component of the glow", nameof(ColorComponentOptions))]
        public static float GlowColorB
        {
            get => glowColor.b;
            set => glowColor = new Color(glowColor.r, glowColor.g, value, 1f);
        }

        [ModOptionCategory("9. Weapon Shader Colors", 9)]
        [ModOptionOrder(4)]
        [ModOptionTooltip("Set the red component of pulse color 1")]
        [ModOptionSlider]
        [ModOption("Pulse Color 1 - Red", "Adjust the red component of first pulse color", nameof(ColorComponentOptions))]
        public static float PulseColor1R
        {
            get => pulseColor1.r;
            set => pulseColor1 = new Color(value, pulseColor1.g, pulseColor1.b, 1f);
        }

        [ModOptionCategory("9. Weapon Shader Colors", 9)]
        [ModOptionOrder(5)]
        [ModOptionTooltip("Set the green component of pulse color 1")]
        [ModOptionSlider]
        [ModOption("Pulse Color 1 - Green", "Adjust the green component of first pulse color", nameof(ColorComponentOptions))]
        public static float PulseColor1G
        {
            get => pulseColor1.g;
            set => pulseColor1 = new Color(pulseColor1.r, value, pulseColor1.b, 1f);
        }

        [ModOptionCategory("9. Weapon Shader Colors", 9)]
        [ModOptionOrder(6)]
        [ModOptionTooltip("Set the blue component of pulse color 1")]
        [ModOptionSlider]
        [ModOption("Pulse Color 1 - Blue", "Adjust the blue component of first pulse color", nameof(ColorComponentOptions))]
        public static float PulseColor1B
        {
            get => pulseColor1.b;
            set => pulseColor1 = new Color(pulseColor1.r, pulseColor1.g, value, 1f);
        }

        [ModOptionCategory("9. Weapon Shader Colors", 9)]
        [ModOptionOrder(7)]
        [ModOptionTooltip("Set the red component of pulse color 2")]
        [ModOptionSlider]
        [ModOption("Pulse Color 2 - Red", "Adjust the red component of second pulse color", nameof(ColorComponentOptions))]
        public static float PulseColor2R
        {
            get => pulseColor2.r;
            set => pulseColor2 = new Color(value, pulseColor2.g, pulseColor2.b, 1f);
        }

        [ModOptionCategory("9. Weapon Shader Colors", 9)]
        [ModOptionOrder(8)]
        [ModOptionTooltip("Set the green component of pulse color 2")]
        [ModOptionSlider]
        [ModOption("Pulse Color 2 - Green", "Adjust the green component of second pulse color", nameof(ColorComponentOptions))]
        public static float PulseColor2G
        {
            get => pulseColor2.g;
            set => pulseColor2 = new Color(pulseColor2.r, value, pulseColor2.b, 1f);
        }

        [ModOptionCategory("9. Weapon Shader Colors", 9)]
        [ModOptionOrder(9)]
        [ModOptionTooltip("Set the blue component of pulse color 2")]
        [ModOptionSlider]
        [ModOption("Pulse Color 2 - Blue", "Adjust the blue component of second pulse color", nameof(ColorComponentOptions))]
        public static float PulseColor2B
        {
            get => pulseColor2.b;
            set => pulseColor2 = new Color(pulseColor2.r, pulseColor2.g, value, 1f);
        }

        [ModOptionCategory("9. Weapon Shader Colors", 9)]
        [ModOptionOrder(10)]
        [ModOptionTooltip("Set the red component of the rim color")]
        [ModOptionSlider]
        [ModOption("Rim Color - Red", "Adjust the red component of the rim effect", nameof(ColorComponentOptions))]
        public static float RimColorR
        {
            get => rimColor.r;
            set => rimColor = new Color(value, rimColor.g, rimColor.b, 1f);
        }

        [ModOptionCategory("9. Weapon Shader Colors", 9)]
        [ModOptionOrder(11)]
        [ModOptionTooltip("Set the green component of the rim color")]
        [ModOptionSlider]
        [ModOption("Rim Color - Green", "Adjust the green component of the rim effect", nameof(ColorComponentOptions))]
        public static float RimColorG
        {
            get => rimColor.g;
            set => rimColor = new Color(rimColor.r, value, rimColor.b, 1f);
        }

        [ModOptionCategory("9. Weapon Shader Colors", 9)]
        [ModOptionOrder(12)]
        [ModOptionTooltip("Set the blue component of the rim color")]
        [ModOptionSlider]
        [ModOption("Rim Color - Blue", "Adjust the blue component of the rim effect", nameof(ColorComponentOptions))]
        public static float RimColorB
        {
            get => rimColor.b;
            set => rimColor = new Color(rimColor.r, rimColor.g, value, 1f);
        }

        // =================
        // 10. PHYSICS SETTINGS
        // =================

        [ModOptionCategory("10. Physics Settings", 10)]
        [ModOptionOrder(1)]
        [ModOption("Use Spring Constraints", "Use spring physics for weapon holding")]
        public static bool UseSpringConstraints
        {
            get => useSpringConstraints;
            set => useSpringConstraints = value;
        }

        [ModOptionCategory("10. Physics Settings", 10)]
        [ModOptionOrder(2)]
        [ModOption("Spring Strength", "How strongly the weapon is held in place", nameof(SpringStrengthOptions))]
        public static float SpringStrengthOption
        {
            get => SpringStrength;
            set => SpringStrength = value;
        }

        [ModOptionCategory("10. Physics Settings", 10)]
        [ModOptionOrder(3)]
        [ModOption("Spring Damping", "How smoothly the weapon moves", nameof(DampingOptions))]
        public static float SpringDampingOption
        {
            get => SpringDamping;
            set => SpringDamping = value;
        }

        [ModOptionCategory("10. Physics Settings", 10)]
        [ModOptionOrder(4)]
        [ModOption("Release Distance", "How far to pull before weapon releases", nameof(ReleaseDistanceOptions))]
        public static float ReleaseDistanceOption
        {
            get => ReleaseDistance;
            set => ReleaseDistance = value;
        }

        // ==========================================
        // PROPERTY ACCESSORS
        // ==========================================

        public static int UpWeaponIndex => upWeaponIndex;
        public static int DownWeaponIndex => downWeaponIndex;
        public static int LeftWeaponIndex => leftWeaponIndex;
        public static int RightWeaponIndex => rightWeaponIndex;
        public static int ForwardWeaponIndex => forwardWeaponIndex;
        public static int InWeaponIndex => inWeaponIndex;
        public static int OutWeaponIndex => outWeaponIndex;


        // ==========================================
        // OPTION ARRAYS
        // ==========================================

        #region Option Arrays

        public static ModOptionFloat[] ColorComponentOptions()
        {
            ModOptionFloat[] options = new ModOptionFloat[101]; // 0 to 1 in 0.01 increments
            for (int i = 0; i <= 100; i++)
            {
                float value = i / 100f;
                options[i] = new ModOptionFloat(value.ToString("F2"), value);
            }
            return options;
        }

        public static ModOptionFloat[] ArmSpreadOptions()
        {
            ModOptionFloat[] options = new ModOptionFloat[11]; // 1.0 to 2.0 in 0.1 increments
            for (int i = 0; i <= 10; i++)
            {
                float value = 1f + (i * 0.1f);
                options[i] = new ModOptionFloat(value.ToString("F1"), value);
            }
            return options;
        }

        public static ModOptionFloat[] CooldownOptions()
        {
            ModOptionFloat[] options = new ModOptionFloat[21];
            for (int i = 0; i <= 20; i++)
            {
                float value = i * 0.5f;
                options[i] = new ModOptionFloat(value.ToString("F1") + "s", value);
            }
            return options;
        }

        public static ModOptionFloat[] BrightnessOptions()
        {
            try
            {
                ModOptionFloat[] options = new ModOptionFloat[37]; // 0.01 to 10 with varying steps
                float[] steps = {
                    0.01f, 0.02f, 0.05f, 0.1f, 0.2f, 0.3f, 0.4f, 0.5f, 0.6f, 0.7f, 0.8f, 0.9f,
                    1.0f, 1.25f, 1.5f, 1.75f, 2.0f, 2.25f, 2.5f, 2.75f, 3.0f, 3.25f, 3.5f, 3.75f,
                    4.0f, 4.5f, 5.0f, 5.5f, 6.0f, 6.5f, 7.0f, 7.5f, 8.0f, 8.5f, 9.0f, 9.5f, 10.0f
                };

                for (int i = 0; i < steps.Length; i++)
                {
                    options[i] = new ModOptionFloat(steps[i].ToString("F2") + "x", steps[i]);
                }
                return options;
            }
            catch
            {
                return new ModOptionFloat[] { new ModOptionFloat("Default", 1.0f) };
            }
        }


        public static ModOptionFloat[] OpeningSpeedOptions()
        {
            ModOptionFloat[] options = new ModOptionFloat[20]; // 0.1 to 10 as per shader range
            for (int i = 0; i < 20; i++)
            {
                float value = 0.1f + (i * 0.5f); // Increments of 0.5
                options[i] = new ModOptionFloat(value.ToString("F1") + "x", value);
            }
            return options;
        }

        public static ModOptionFloat[] DistortionOptions()
        {
            ModOptionFloat[] options = new ModOptionFloat[11]; // 0 to 1 in 0.1 increments
            for (int i = 0; i <= 10; i++)
            {
                float value = i * 0.1f;
                options[i] = new ModOptionFloat(value.ToString("F1"), value);
            }
            return options;
        }

        public static ModOptionFloat[] RotationOptions()
        {
            ModOptionFloat[] options = new ModOptionFloat[37]; // -180 to 180 in 10-degree increments
            for (int i = 0; i < 37; i++)
            {
                float value = -180f + (i * 10f);
                options[i] = new ModOptionFloat(value.ToString("F0") + "°", value);
            }
            return options;
        }

        public static ModOptionFloat[] OffsetOptions()
        {
            ModOptionFloat[] options = new ModOptionFloat[5];
            for (int i = 0; i < 5; i++)
            {
                float value = 0f + (i * 0.1f);
                options[i] = new ModOptionFloat(value.ToString("F1") + "m", value);
            }
            return options;
        }

        public static ModOptionFloat[] StickOutOptions()
        {
            ModOptionFloat[] options = new ModOptionFloat[5];
            for (int i = 0; i < 5; i++)
            {
                float value = 0.1f + (i * 0.1f);
                options[i] = new ModOptionFloat(value.ToString("F1") + "m", value);
            }
            return options;
        }

        public static ModOptionFloat[] SpringStrengthOptions()
        {
            ModOptionFloat[] options = new ModOptionFloat[10];
            for (int i = 0; i < 10; i++)
            {
                float value = 500f + (i * 500f);
                options[i] = new ModOptionFloat(value.ToString("F0"), value);
            }
            return options;
        }

        public static ModOptionFloat[] DampingOptions()
        {
            ModOptionFloat[] options = new ModOptionFloat[21];
            for (int i = 0; i <= 20; i++)
            {
                float value = i * 5f;
                options[i] = new ModOptionFloat(value.ToString("F0"), value);
            }
            return options;
        }

        public static ModOptionFloat[] ReleaseDistanceOptions()
        {
            ModOptionFloat[] options = new ModOptionFloat[9];
            for (int i = 0; i < 9; i++)
            {
                float value = 0.2f + (i * 0.1f);
                options[i] = new ModOptionFloat(value.ToString("F1") + "m", value);
            }
            return options;
        }

        public static ModOptionFloat[] WeaponOptions()
        {
            List<ModOptionFloat> options = new List<ModOptionFloat>();
            var weaponList = Catalog.GetDataList<ItemData>();

            int index = 0;
            foreach (var itemData in weaponList)
            {
                if (itemData.type == ItemData.Type.Weapon)
                {
                    options.Add(new ModOptionFloat(itemData.displayName, index));
                    index++;
                }
            }

            return options.ToArray();
        }

        public static ModOptionFloat[] EffectOptions()
        {
            List<ModOptionFloat> options = new List<ModOptionFloat>();
            var effectList = Catalog.GetDataList<EffectData>();

            int index = 0;
            foreach (var effectData in effectList)
            {
                options.Add(new ModOptionFloat(effectData.id, index));
                index++;
            }

            return options.ToArray();
        }
        #endregion
    }
}