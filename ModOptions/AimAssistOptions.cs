using ThunderRoad;
using UnityEngine;
using System.Collections.Generic;

namespace ShadowArmory
{
    public class AimAssistConfig
    {
        private static readonly string currentUser = "Suoper";
        private static readonly string currentDateTime = "2025-04-09 02:33:44";

        public static RagdollPart.Type[] TargetPriority = new[]
        {
            RagdollPart.Type.Head,
            RagdollPart.Type.Torso,
            RagdollPart.Type.LeftArm,
            RagdollPart.Type.RightArm,
            RagdollPart.Type.LeftLeg,
            RagdollPart.Type.RightLeg
        };

        // Core Settings
        private static bool aimAssistEnabled = true;
        private static float targetRange = 20f;
        private static float updateInterval = 0.1f;
        private static float aimAssistStrength = 0.8f;

        // Priority Settings
        private static float headPriority = 1.0f;
        private static float torsoPriority = 0.8f;
        private static float armsPriority = 0.6f;
        private static float legsPriority = 0.4f;

        // Advanced Settings
        private static bool enablePrediction = true;
        private static float predictionTime = 0.1f;
        private static float smoothingFactor = 0.1f;
        private static bool requireLineOfSight = true;
        private static float maxAimAngle = 45f;
        private static bool showDebugVisuals = false;
        private static Color debugColor = new Color(1f, 0f, 0f, 0.5f);

        // Enhanced Settings
        private static float targetLockMultiplier = 2.0f;
        private static float velocityCompensation = 1.0f;
        private static float snapThreshold = 0.5f;
        private static bool enableLeadTarget = true;

        #region Mod Options

        [ModOptionCategory("AimAssist Settings", 1)]
        [ModOptionOrder(1)]
        [ModOption("Enable Aim Assist", "Toggle the aim assist functionality")]
        public static bool AimAssistEnabled
        {
            get => aimAssistEnabled;
            set => aimAssistEnabled = value;
        }

        [ModOptionCategory("AimAssist Settings", 1)]
        [ModOptionOrder(2)]
        [ModOptionSlider]
        [ModOption("Target Range", "Maximum range for aim assist to activate", nameof(RangeOptions))]
        public static float TargetRange
        {
            get => targetRange;
            set => targetRange = value;
        }

        [ModOptionCategory("AimAssist Settings", 1)]
        [ModOptionOrder(3)]
        [ModOptionSlider]
        [ModOption("Update Interval", "How often the aim assist updates", nameof(UpdateIntervalOptions))]
        public static float UpdateInterval
        {
            get => updateInterval;
            set => updateInterval = value;
        }

        [ModOptionCategory("AimAssist Settings", 1)]
        [ModOptionOrder(4)]
        [ModOptionSlider]
        [ModOption("Base Assist Strength", "Base strength of the aim assist", nameof(EnhancedStrengthOptions))]
        public static float AimAssistStrength
        {
            get => aimAssistStrength;
            set => aimAssistStrength = value;
        }

        [ModOptionCategory("AimAssist Settings", 2)]
        [ModOptionOrder(1)]
        [ModOptionSlider]
        [ModOption("Head Priority", "Priority for head targeting", nameof(PriorityOptions))]
        public static float HeadPriority
        {
            get => headPriority;
            set => headPriority = value;
        }

        [ModOptionCategory("AimAssist Settings", 2)]
        [ModOptionOrder(2)]
        [ModOptionSlider]
        [ModOption("Torso Priority", "Priority for torso targeting", nameof(PriorityOptions))]
        public static float TorsoPriority
        {
            get => torsoPriority;
            set => torsoPriority = value;
        }

        [ModOptionCategory("AimAssist Settings", 2)]
        [ModOptionOrder(3)]
        [ModOptionSlider]
        [ModOption("Arms Priority", "Priority for arms targeting", nameof(PriorityOptions))]
        public static float ArmsPriority
        {
            get => armsPriority;
            set => armsPriority = value;
        }

        [ModOptionCategory("AimAssist Settings", 2)]
        [ModOptionOrder(4)]
        [ModOptionSlider]
        [ModOption("Legs Priority", "Priority for legs targeting", nameof(PriorityOptions))]
        public static float LegsPriority
        {
            get => legsPriority;
            set => legsPriority = value;
        }

        [ModOptionCategory("AimAssist Settings", 3)]
        [ModOptionOrder(1)]
        [ModOption("Enable Prediction", "Enable target movement prediction")]
        public static bool EnablePrediction
        {
            get => enablePrediction;
            set => enablePrediction = value;
        }

        [ModOptionCategory("AimAssist Settings", 3)]
        [ModOptionOrder(2)]
        [ModOptionSlider]
        [ModOption("Prediction Time", "How far ahead to predict movement", nameof(PredictionTimeOptions))]
        public static float PredictionTime
        {
            get => predictionTime;
            set => predictionTime = value;
        }

        [ModOptionCategory("AimAssist Settings", 3)]
        [ModOptionOrder(3)]
        [ModOptionSlider]
        [ModOption("Smoothing Factor", "Smoothness of aim assist", nameof(SmoothingOptions))]
        public static float SmoothingFactor
        {
            get => smoothingFactor;
            set => smoothingFactor = value;
        }

        [ModOptionCategory("AimAssist Settings", 3)]
        [ModOptionOrder(4)]
        [ModOption("Require Line of Sight", "Only target visible enemies")]
        public static bool RequireLineOfSight
        {
            get => requireLineOfSight;
            set => requireLineOfSight = value;
        }

        [ModOptionCategory("AimAssist Settings", 3)]
        [ModOptionOrder(5)]
        [ModOptionSlider]
        [ModOption("Max Aim Angle", "Maximum angle for activation", nameof(AngleOptions))]
        public static float MaxAimAngle
        {
            get => maxAimAngle;
            set => maxAimAngle = value;
        }

        [ModOptionCategory("AimAssist Settings", 4)]
        [ModOptionOrder(1)]
        [ModOptionSlider]
        [ModOption("Target Lock Multiplier", "Strength multiplier for locked targets", nameof(EnhancedStrengthOptions))]
        public static float TargetLockMultiplier
        {
            get => targetLockMultiplier;
            set => targetLockMultiplier = value;
        }

        [ModOptionCategory("AimAssist Settings", 4)]
        [ModOptionOrder(2)]
        [ModOptionSlider]
        [ModOption("Velocity Compensation", "How much to compensate for target speed", nameof(EnhancedStrengthOptions))]
        public static float VelocityCompensation
        {
            get => velocityCompensation;
            set => velocityCompensation = value;
        }

        [ModOptionCategory("AimAssist Settings", 4)]
        [ModOptionOrder(3)]
        [ModOptionSlider]
        [ModOption("Snap Threshold", "Distance to start snapping to target", nameof(SnapThresholdOptions))]
        public static float SnapThreshold
        {
            get => snapThreshold;
            set => snapThreshold = value;
        }

        [ModOptionCategory("AimAssist Settings", 4)]
        [ModOptionOrder(4)]
        [ModOption("Enable Lead Target", "Automatically lead moving targets")]
        public static bool EnableLeadTarget
        {
            get => enableLeadTarget;
            set => enableLeadTarget = value;
        }

        [ModOptionCategory("AimAssist Settings", 5)]
        [ModOptionOrder(1)]
        [ModOption("Show Debug Visuals", "Display visual debugging aids")]
        public static bool ShowDebugVisuals
        {
            get => showDebugVisuals;
            set => showDebugVisuals = value;
        }

        #endregion

        #region Option Arrays

        public static ModOptionFloat[] RangeOptions()
        {
            ModOptionFloat[] options = new ModOptionFloat[21];
            for (int i = 0; i <= 20; i++)
            {
                float value = 5f + (i * 2.5f);
                options[i] = new ModOptionFloat(value.ToString("F1") + "m", value);
            }
            return options;
        }

        public static ModOptionFloat[] UpdateIntervalOptions()
        {
            ModOptionFloat[] options = new ModOptionFloat[10];
            for (int i = 0; i < 10; i++)
            {
                float value = 0.05f + (i * 0.05f);
                options[i] = new ModOptionFloat(value.ToString("F2") + "s", value);
            }
            return options;
        }

        public static ModOptionFloat[] EnhancedStrengthOptions()
        {
            ModOptionFloat[] options = new ModOptionFloat[21];
            for (int i = 0; i <= 20; i++)
            {
                float value = i * 0.25f;
                options[i] = new ModOptionFloat((value * 100f).ToString("F0") + "%", value);
            }
            return options;
        }

        public static ModOptionFloat[] PriorityOptions()
        {
            ModOptionFloat[] options = new ModOptionFloat[11];
            for (int i = 0; i <= 10; i++)
            {
                float value = i * 0.2f;
                options[i] = new ModOptionFloat(value.ToString("F1"), value);
            }
            return options;
        }

        public static ModOptionFloat[] PredictionTimeOptions()
        {
            ModOptionFloat[] options = new ModOptionFloat[11];
            for (int i = 0; i <= 10; i++)
            {
                float value = i * 0.05f;
                options[i] = new ModOptionFloat(value.ToString("F2") + "s", value);
            }
            return options;
        }

        public static ModOptionFloat[] SmoothingOptions()
        {
            ModOptionFloat[] options = new ModOptionFloat[11];
            for (int i = 0; i <= 10; i++)
            {
                float value = i * 0.1f;
                options[i] = new ModOptionFloat((value * 100f).ToString("F0") + "%", value);
            }
            return options;
        }

        public static ModOptionFloat[] AngleOptions()
        {
            ModOptionFloat[] options = new ModOptionFloat[19];
            for (int i = 0; i < 19; i++)
            {
                float value = 5f + (i * 5f);
                options[i] = new ModOptionFloat(value.ToString("F0") + "°", value);
            }
            return options;
        }

        public static ModOptionFloat[] SnapThresholdOptions()
        {
            ModOptionFloat[] options = new ModOptionFloat[11];
            for (int i = 0; i <= 10; i++)
            {
                float value = 0.1f + (i * 0.1f);
                options[i] = new ModOptionFloat(value.ToString("F1") + "m", value);
            }
            return options;
        }

        #endregion
    }
}