using UnityEngine;
using System.Collections.Generic;
using ThunderRoad;

namespace ShadowArmory
{
    /// <summary>
    /// Enhanced aim assist that makes weapon targeting nearly perfect
    /// </summary>
    public class AimAssist : MonoBehaviour
    {
        private readonly string currentUser = "Suoper";
        private readonly string currentDateTime = "2025-04-12 23:04:13";

        // Core references
        private Creature creature;
        private Dictionary<Creature, EffectData> targetEffectData = new Dictionary<Creature, EffectData>();

        // Targeting state
        private float lastUpdateTime = 0f;
        private bool hasWeaponEquipped = false;
        private List<Item> equippedWeapons = new List<Item>();
        private Dictionary<Item, bool> lastUseState = new Dictionary<Item, bool>();

        // Advanced target tracking
        private Dictionary<Creature, Vector3> lastTargetPositions = new Dictionary<Creature, Vector3>();
        private Dictionary<Creature, Vector3> targetVelocities = new Dictionary<Creature, Vector3>();
        private Dictionary<Creature, float> targetLockTimes = new Dictionary<Creature, float>();
        private RagdollPart currentTarget = null;
        private float targetLockStrength = 0f;
        private const float TARGET_LOCK_RATE = 2.5f;
        private Vector3 predictedImpactPoint;
        private float perfectAimCooldown = 0f;

        // Visual indicators
        private List<EffectInstance> targetEffectInstances = new List<EffectInstance>();

        #region Initialization

        public void Initialize(Creature owner)
        {
            creature = owner;

            if (creature == null)
            {
                Debug.LogError($"[{currentDateTime}] {currentUser} - Cannot initialize Aim Assist: creature is null!");
                return;
            }

            // Subscribe to events
            creature.OnDespawnEvent += OnCreatureDespawn;
            creature.handLeft.OnGrabEvent += OnHandGrab;
            creature.handRight.OnGrabEvent += OnHandGrab;
            creature.handLeft.OnUnGrabEvent += OnHandUnGrab;
            creature.handRight.OnUnGrabEvent += OnHandUnGrab;

            // Instead of using HeldActionDelegate, we'll check bow drawing in Update method
            EventManager.onCreatureSpawn += OnCreatureSpawn;
            EventManager.onCreatureKill += OnCreatureKill;

            Debug.Log($"[{currentDateTime}] {currentUser} - Aim Assist initialized for {creature.name}");
        }

        private void OnCreatureDespawn(EventTime eventTime)
        {
            if (eventTime == EventTime.OnEnd)
            {
                // Clean up event subscriptions
                if (creature != null)
                {
                    creature.handLeft.OnGrabEvent -= OnHandGrab;
                    creature.handRight.OnGrabEvent -= OnHandGrab;
                    creature.handLeft.OnUnGrabEvent -= OnHandUnGrab;
                    creature.handRight.OnUnGrabEvent -= OnHandUnGrab;
                    creature.OnDespawnEvent -= OnCreatureDespawn;
                }

                EventManager.onCreatureSpawn -= OnCreatureSpawn;
                EventManager.onCreatureKill -= OnCreatureKill;

                ClearTargetEffects();
            }
        }

        private void OnCreatureSpawn(Creature creature)
        {
            // Reset tracking data for new creatures
            if (!creature.isPlayer && !lastTargetPositions.ContainsKey(creature))
            {
                lastTargetPositions[creature] = creature.transform.position;
                targetVelocities[creature] = Vector3.zero;
                targetLockTimes[creature] = 0f;
            }
        }

        private void OnCreatureKill(Creature creature, Player player, CollisionInstance collisionInstance, EventTime eventTime)
        {
            if (eventTime == EventTime.OnEnd)
            {
                // Remove tracking data for dead creatures
                lastTargetPositions.Remove(creature);
                targetVelocities.Remove(creature);
                targetLockTimes.Remove(creature);

                if (currentTarget?.ragdoll?.creature == creature)
                {
                    currentTarget = null;
                    targetLockStrength = 0f;
                }
            }
        }

        #endregion

        #region Weapon Tracking

        private void OnHandGrab(Side side, Handle handle, float axisPosition, HandlePose pose, EventTime eventTime)
        {
            if (!AimAssistConfig.AimAssistEnabled || eventTime != EventTime.OnStart) return;

            if (handle?.item != null)
            {
                Item item = handle.item;

                // Track weapons by type
                if (IsWeapon(item) && !equippedWeapons.Contains(item))
                {
                    equippedWeapons.Add(item);
                    lastUseState[item] = false; // Initialize use state tracking
                    hasWeaponEquipped = true;
                    Debug.Log($"[{currentDateTime}] {currentUser} - {item.itemId} grabbed, aim assist active");
                }
            }
        }

        private void OnHandUnGrab(Side side, Handle handle, bool throwing, EventTime eventTime)
        {
            if (eventTime != EventTime.OnStart || handle?.item == null) return;

            Item item = handle.item;

            if (equippedWeapons.Contains(item))
            {
                equippedWeapons.Remove(item);
                lastUseState.Remove(item);

                // Check if any weapons remain equipped
                if (equippedWeapons.Count == 0)
                {
                    hasWeaponEquipped = false;
                    ClearTargetEffects();
                }
            }
        }

        private bool IsWeapon(Item item)
        {
            if (item == null) return false;

            // Check if this is a weapon (based only on existing ThunderRoad API)
            return item.data.type == ItemData.Type.Weapon ||
                   IsBowLike(item) ||
                   IsThrownLike(item);
        }

        private bool IsBowLike(Item item)
        {
            // Check if this item behaves like a bow (based on item properties)
            return item.data.category?.Contains("Bow") == true ||
                   item.data.id.ToLower().Contains("bow") ||
                   item.itemId.ToLower().Contains("bow");
        }

        private bool IsThrownLike(Item item)
        {
            if (item == null || item.data == null) return false;

            // Get lowercase versions for case-insensitive comparison
            string itemId = item.itemId?.ToLower() ?? "";
            string dataId = item.data.id?.ToLower() ?? "";
            string category = item.data.category?.ToLower() ?? "";
            string displayName = item.data.displayName?.ToLower() ?? "";

            // Check various throw-related keywords (case insensitive)
            string[] throwKeywords = new[] {
        "throw", "thrown", "dart", "projectile", "ranged"
    };

            // Check various throwable weapon types (will match regardless of capitalization)
            string[] weaponTypes = new[] {
        "dagger", "knife", "kunai", "shuriken", "star", "axe", "hatchet",
        "tomahawk", "javelin", "spear", "dart", "disc", "chakram", "boomerang",
        // Additional capitalization variants just to be extra safe
        "Dagger", "Knife", "Axe", "Shuriken", "Dart", "Spear", "Javelin"
    };

            // Check category first (case insensitive)
            if (category.IndexOf("throw", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                category.IndexOf("dagger", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                category.IndexOf("thrown", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            // Check for throw keywords in any field (case insensitive)
            foreach (string keyword in throwKeywords)
            {
                if (itemId.IndexOf(keyword, System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    dataId.IndexOf(keyword, System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    displayName.IndexOf(keyword, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }

            // Check for throwable weapon types in any field (case insensitive)
            foreach (string weaponType in weaponTypes)
            {
                if (itemId.IndexOf(weaponType, System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    dataId.IndexOf(weaponType, System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    displayName.IndexOf(weaponType, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }

            // Check specific cases - lightweight/small weapons
            if (item.physicBody != null && item.physicBody.mass < 3.0f && item.data.type == ItemData.Type.Weapon)
            {
                // Small, light weapons are often throwable
                if (item.physicBody.mass < 1.5f)
                    return true;

                // Additional check - weapons with certain dimensions
                Collider[] colliders = item.GetComponentsInChildren<Collider>();
                if (colliders.Length > 0)
                {
                    float maxSize = 0f;
                    foreach (Collider col in colliders)
                    {
                        float size = Mathf.Max(col.bounds.size.x, col.bounds.size.y, col.bounds.size.z);
                        maxSize = Mathf.Max(maxSize, size);
                    }

                    // Small items are often throwable
                    if (maxSize < 0.5f)
                        return true;
                }
            }

            return false;
        }

        #endregion

        #region Target Tracking

        private void Update()
        {
            if (!AimAssistConfig.AimAssistEnabled ||
                !hasWeaponEquipped ||
                Time.time - lastUpdateTime < AimAssistConfig.UpdateInterval)
                return;

            lastUpdateTime = Time.time;

            // Handle perfect aim cooldown
            if (perfectAimCooldown > 0)
            {
                perfectAimCooldown -= Time.deltaTime;
            }

            // Check for bow drawing/release (fixed to avoid usePressedPrev)
            CheckBowActions();

            UpdateTargeting();
            UpdateTargetEffects();
        }

        private void CheckBowActions()
        {
            foreach (Item item in equippedWeapons)
            {
                if (IsBowLike(item))
                {
                    // Check if bow is drawn
                    RagdollHand[] hands = new[] { creature.handLeft, creature.handRight };
                    foreach (RagdollHand hand in hands)
                    {
                        if (hand.grabbedHandle?.item == item && hand.playerHand != null && hand.playerHand.controlHand != null)
                        {
                            // Check current use state
                            bool currentUsePressed = hand.playerHand.controlHand.usePressed;

                            // Get previous use state from our own tracking
                            bool previousUsePressed = lastUseState.ContainsKey(item) ? lastUseState[item] : false;

                            // If aiming with bow (button held down), give perfect aim
                            if (currentUsePressed && currentTarget != null)
                            {
                                perfectAimCooldown = 0.2f;
                            }

                            // Check if button just released (fired bow)
                            if (previousUsePressed && !currentUsePressed && currentTarget != null)
                            {
                                perfectAimCooldown = 0.5f;
                                PerfectBowAim(item);
                            }

                            // Update stored state
                            lastUseState[item] = currentUsePressed;
                        }
                    }
                }
            }
        }

        private void UpdateTargeting()
        {
            RagdollPart bestTarget = null;
            float bestScore = float.MinValue;
            Vector3 playerPosition = creature.transform.position;
            Vector3 playerEyePosition = creature.isPlayer ?
                Player.local.head.transform.position :
                creature.ragdoll.GetPart(RagdollPart.Type.Head).transform.position;

            foreach (Creature target in Creature.allActive)
            {
                if (target == creature || target.isKilled || target.isPlayer) continue;

                // Check if target is in range
                float distance = Vector3.Distance(playerPosition, target.transform.position);
                if (distance > AimAssistConfig.TargetRange) continue;

                // Track target data for prediction
                if (!lastTargetPositions.ContainsKey(target))
                {
                    lastTargetPositions[target] = target.transform.position;
                    targetVelocities[target] = Vector3.zero;
                    targetLockTimes[target] = 0f;
                }
                else
                {
                    Vector3 newPosition = target.transform.position;
                    Vector3 movement = newPosition - lastTargetPositions[target];
                    Vector3 velocity = movement / Time.deltaTime;

                    // Smooth velocity tracking
                    targetVelocities[target] = Vector3.Lerp(
                        targetVelocities[target],
                        velocity,
                        AimAssistConfig.SmoothingFactor
                    );

                    lastTargetPositions[target] = newPosition;
                }

                // Evaluate each body part
                foreach (RagdollPart.Type partType in AimAssistConfig.TargetPriority)
                {
                    RagdollPart targetPart = target.ragdoll.GetPart(partType);
                    if (targetPart == null) continue;

                    // Skip if line of sight is required and part isn't visible
                    if (AimAssistConfig.RequireLineOfSight && !IsPartVisible(targetPart, playerEyePosition))
                        continue;

                    // Calculate scoring components
                    float priorityWeight = GetPartPriorityWeight(partType);
                    float distanceScore = 1.0f - (distance / AimAssistConfig.TargetRange);
                    float angleScore = GetAngleScore(targetPart);
                    float velocityScore = 1.0f - Mathf.Min(1.0f, targetVelocities[target].magnitude / 10.0f);
                    float lockBonus = target.brain.currentTarget == creature ? 0.5f : 0f; // Bonus for enemies targeting us

                    // Calculate total score with different weights
                    float totalScore = (priorityWeight * 0.4f) +
                                      (distanceScore * 0.3f) +
                                      (angleScore * 0.3f) +
                                      (velocityScore * 0.2f) +
                                      lockBonus;

                    // Keep track of highest scoring part
                    if (totalScore > bestScore)
                    {
                        bestScore = totalScore;
                        bestTarget = targetPart;
                    }
                }
            }

            // Handle target transition
            if (bestTarget != null)
            {
                Creature targetCreature = bestTarget.ragdoll.creature;

                // Update target lock time
                if (!targetLockTimes.ContainsKey(targetCreature))
                {
                    targetLockTimes[targetCreature] = 0f;
                }

                if (bestTarget == currentTarget)
                {
                    // Increase lock strength when targeting the same part
                    targetLockStrength = Mathf.Min(1.0f, targetLockStrength + TARGET_LOCK_RATE * Time.deltaTime);
                    targetLockTimes[targetCreature] += Time.deltaTime;
                }
                else
                {
                    // Reset lock strength on target change
                    targetLockStrength = 0.2f; // Small initial value for smoother transitions
                    currentTarget = bestTarget;
                }

                // Apply aim assist to all equipped weapons
                ApplyAimAssist(bestTarget, bestScore);
            }
            else
            {
                // Clear targeting when no suitable target found
                targetLockStrength = 0f;
                currentTarget = null;
            }
        }

        private bool IsPartVisible(RagdollPart part, Vector3 viewerPosition)
        {
            Vector3 direction = part.transform.position - viewerPosition;
            float distance = direction.magnitude;

            if (distance <= AimAssistConfig.TargetRange)
            {
                RaycastHit hit;
                if (Physics.Raycast(
                    viewerPosition,
                    direction.normalized,
                    out hit,
                    distance,
                    Physics.DefaultRaycastLayers,
                    QueryTriggerInteraction.Ignore))
                {
                    // Check if the hit object belongs to the target part or its creature
                    Creature hitCreature = hit.collider.GetComponentInParent<Creature>();
                    if (hitCreature == part.ragdoll.creature)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private float GetPartPriorityWeight(RagdollPart.Type partType)
        {
            switch (partType)
            {
                case RagdollPart.Type.Head: return AimAssistConfig.HeadPriority;
                case RagdollPart.Type.Torso: return AimAssistConfig.TorsoPriority;
                case RagdollPart.Type.LeftArm:
                case RagdollPart.Type.RightArm: return AimAssistConfig.ArmsPriority;
                case RagdollPart.Type.LeftLeg:
                case RagdollPart.Type.RightLeg: return AimAssistConfig.LegsPriority;
                default: return 0.1f;
            }
        }

        private float GetAngleScore(RagdollPart targetPart)
        {
            float bestAngleScore = 0f;

            // Check all equipped weapons and return the best angle score
            foreach (Item weapon in equippedWeapons)
            {
                Vector3 toTarget = targetPart.transform.position - weapon.transform.position;
                float angleToTarget = Vector3.Angle(weapon.transform.forward, toTarget);
                float normalizedAngle = 1.0f - Mathf.Clamp01(angleToTarget / AimAssistConfig.MaxAimAngle);

                if (normalizedAngle > bestAngleScore)
                {
                    bestAngleScore = normalizedAngle;
                }
            }

            return bestAngleScore;
        }

        #endregion

        #region Aim Assistance

        private void ApplyAimAssist(RagdollPart targetPart, float targetScore)
        {
            foreach (Item weapon in equippedWeapons)
            {
                if (weapon == null) continue;

                // Skip if the weapon is currently being telekinesis'd
                if (weapon.isTelekinesisGrabbed) continue;

                // Calculate prediction data
                Vector3 predictedPosition = GetPredictedPosition(targetPart);
                Vector3 weaponPosition = weapon.transform.position;
                Vector3 toTarget = predictedPosition - weaponPosition;
                float distance = toTarget.magnitude;
                float angleToTarget = Vector3.Angle(weapon.transform.forward, toTarget);

                // Store globally for visual effects
                predictedImpactPoint = predictedPosition;

                // Only apply aim assist if target is within our angular threshold
                if (angleToTarget <= AimAssistConfig.MaxAimAngle)
                {
                    // Calculate optimal targeting direction
                    Vector3 targetDirection = toTarget.normalized;
                    Quaternion targetRotation = Quaternion.LookRotation(targetDirection);

                    // Enhanced strength calculations
                    float baseStrength = AimAssistConfig.AimAssistStrength;
                    float distanceMultiplier = 1.0f - (distance / AimAssistConfig.TargetRange);
                    float angleMultiplier = 1.0f - (angleToTarget / AimAssistConfig.MaxAimAngle);
                    float lockMultiplier = 1.0f + (targetLockStrength * AimAssistConfig.TargetLockMultiplier);

                    // Check for perfect aim mode
                    bool perfectMode = perfectAimCooldown > 0;

                    // Special handling for different weapon types
                    if (IsBowLike(weapon))
                    {
                        // Enhanced bow aiming
                        if (perfectMode)
                        {
                            // Nearly instant aim for perfect shots (when releasing arrow)
                            baseStrength = 5.0f;
                        }
                        else
                        {
                            // Enhanced aiming for bows in general
                            baseStrength *= 1.5f;
                        }
                    }
                    // Enhance thrown weapon aiming
                    else if (IsThrownLike(weapon) && perfectMode)
                    {
                        baseStrength = 3.0f;
                    }

                    // Snap effect when close to target
                    float snapEffect = 0f;
                    if (distance < AimAssistConfig.SnapThreshold)
                    {
                        snapEffect = 1.0f - (distance / AimAssistConfig.SnapThreshold);
                        snapEffect *= snapEffect; // Square for more aggressive snap
                    }

                    // Calculate final rotation strength 
                    float finalStrength = baseStrength *
                                       distanceMultiplier *
                                       angleMultiplier *
                                       lockMultiplier *
                                       (1.0f + snapEffect) * // Add snap effect
                                       Time.deltaTime *
                                       (perfectMode ? 5.0f : 1.0f); // Multiplier for perfect aim mode

                    // Enhanced rotation with velocity compensation
                    if (AimAssistConfig.EnableLeadTarget)
                    {
                        Vector3 targetVelocity = targetVelocities[targetPart.ragdoll.creature];
                        float velocityMagnitude = targetVelocity.magnitude;

                        if (velocityMagnitude > 0.5f) // Only lead if target is moving significantly
                        {
                            // Calculate lead amount based on target speed, distance and compensation factor
                            Vector3 leadOffset = targetVelocity *
                                              (distance * 0.05f) *
                                              AimAssistConfig.VelocityCompensation;

                            // Limit maximum lead distance
                            leadOffset = Vector3.ClampMagnitude(leadOffset, distance * 0.5f);

                            Vector3 leadPosition = predictedPosition + leadOffset;
                            Vector3 leadDirection = (leadPosition - weaponPosition).normalized;
                            targetRotation = Quaternion.LookRotation(leadDirection);

                            // Draw debug visualization for lead targeting
                            if (AimAssistConfig.ShowDebugVisuals)
                            {
                                Debug.DrawLine(predictedPosition, leadPosition, Color.yellow, AimAssistConfig.UpdateInterval);
                            }
                        }
                    }

                    // Apply smoother, stronger rotation
                    weapon.transform.rotation = Quaternion.Slerp(
                        weapon.transform.rotation,
                        targetRotation,
                        finalStrength
                    );

                    // Apply position adjustment only for specific cases
                    if (perfectMode || snapEffect > 0.5f)
                    {
                        float positionStrength = finalStrength * 0.3f * (perfectMode ? 2.0f : snapEffect);

                        // Calculate an optimal position adjustment that maintains the weapon's relative position to hands
                        Vector3 optimalPosition = weaponPosition + (targetDirection * positionStrength);

                        // Limit how much we can move the weapon
                        float maxPositionDelta = perfectMode ? 0.5f : 0.2f;

                        weapon.transform.position = Vector3.MoveTowards(
                            weaponPosition,
                            optimalPosition,
                            maxPositionDelta * positionStrength
                        );
                    }

                    // Draw debug visualization
                    if (AimAssistConfig.ShowDebugVisuals)
                    {
                        Debug.DrawLine(weaponPosition, predictedPosition, Color.red, AimAssistConfig.UpdateInterval);
                        Debug.DrawRay(weaponPosition, weapon.transform.forward * 2f, Color.green, AimAssistConfig.UpdateInterval);
                    }
                }
            }
        }

        private void PerfectBowAim(Item bowItem)
        {
            if (currentTarget == null) return;

            // Find the arrow's spawn point and direction (simplified for compatibility)
            Transform bowTransform = bowItem.transform;

            // Calculate the perfect trajectory to hit the target
            Vector3 targetPos = GetPredictedPosition(currentTarget);
            Vector3 toTarget = targetPos - bowTransform.position;
            float distance = toTarget.magnitude;

            // Calculate gravity compensation based on distance
            float heightCompensation = 0.05f * distance;
            Vector3 adjustedTarget = targetPos + new Vector3(0, heightCompensation, 0);

            // Apply the perfect direction to the bow
            Vector3 perfectDirection = (adjustedTarget - bowTransform.position).normalized;
            bowItem.transform.rotation = Quaternion.LookRotation(perfectDirection);
        }

        private Vector3 GetPredictedPosition(RagdollPart targetPart)
        {
            if (!AimAssistConfig.EnablePrediction)
                return targetPart.transform.position;

            Creature target = targetPart.ragdoll.creature;
            if (!lastTargetPositions.ContainsKey(target) || !targetVelocities.ContainsKey(target))
            {
                return targetPart.transform.position;
            }

            Vector3 currentPosition = targetPart.transform.position;
            Vector3 velocity = targetVelocities[target];

            // Enhanced prediction based on target state
            float predictionTime = AimAssistConfig.PredictionTime;

            // Calculate gravity component for longer shots
            float gravityCompensation = 0;

            bool usingRangedWeapon = false;
            foreach (Item weapon in equippedWeapons)
            {
                if (IsBowLike(weapon))
                {
                    usingRangedWeapon = true;
                    break;
                }
            }

            if (usingRangedWeapon)
            {
                // For ranged weapons, we need more prediction
                predictionTime *= 1.5f;

                // Add gravity compensation for ballistic trajectories
                float distance = Vector3.Distance(currentPosition, creature.transform.position);
                gravityCompensation = distance * 0.01f * AimAssistConfig.VelocityCompensation;
            }

            // Calculate predicted position
            Vector3 prediction = currentPosition +
                               (velocity * predictionTime) +
                               (Vector3.up * gravityCompensation);

            return prediction;
        }

        #endregion

        #region Visual Effects

        private void UpdateTargetEffects()
        {
            if (currentTarget == null)
            {
                ClearTargetEffects();
                return;
            }

            // Draw debug visuals if enabled
            if (AimAssistConfig.ShowDebugVisuals)
            {
                foreach (Item weapon in equippedWeapons)
                {
                    Debug.DrawLine(
                        weapon.transform.position,
                        predictedImpactPoint,
                        Color.red,
                        AimAssistConfig.UpdateInterval
                    );
                }
            }
        }

        private void ClearTargetEffects()
        {
            foreach (var effect in targetEffectInstances)
            {
                if (effect != null)
                {
                    effect.End();
                }
            }
            targetEffectInstances.Clear();
        }

        #endregion

        private void OnDestroy()
        {
            if (creature != null)
            {
                creature.handLeft.OnGrabEvent -= OnHandGrab;
                creature.handRight.OnGrabEvent -= OnHandGrab;
                creature.handLeft.OnUnGrabEvent -= OnHandUnGrab;
                creature.handRight.OnUnGrabEvent -= OnHandUnGrab;
                creature.OnDespawnEvent -= OnCreatureDespawn;
            }

            EventManager.onCreatureSpawn -= OnCreatureSpawn;
            EventManager.onCreatureKill -= OnCreatureKill;

            ClearTargetEffects();
            Debug.Log($"[{currentDateTime}] {currentUser} - Aim Assist destroyed");
        }
    }
}