using ThunderRoad;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace ShadowArmory
{
    /// <summary>
    /// Shadow Crystal - Allows storing weapons in directions for quick retrieval
    /// Updated with level transition support: 2025-05-11 02:26:55
    /// </summary>
    public class T1ShadowCrystal : ItemModule
    {
        // Keep the enum inside this class
        public enum WeaponDirection
        {
            None,
            Up,
            Down,
            Left,
            Right,
            Forward,
            Backward
        }

        #region Private Variables
        // Internal state
        private bool altUseActive = false;
        private Item hoveringWeapon = null;
        private EffectInstance hoverEffect = null;
        private RagdollHand activeHand = null;
        private bool isTrackingGesture = false;
        private Dictionary<string, string> rightHandWeapons = new Dictionary<string, string>();
        private Dictionary<string, string> leftHandWeapons = new Dictionary<string, string>();
        private bool isDestroying = false;
        private Coroutine activeCoroutine = null;
        private float lastPickupTime = 0f;
        private float gestureStartTime = 0f;
        private bool justAssignedWeapon = false;
        private Vector3 lastHandPosition;
        private float lastTime;
        private Vector3 hoverPositionOffset = Vector3.zero;
        private int weaponPickupAttempts = 0;
        private float lastEffectTime = 0f;
        private float defaultHoverHeight = 0.25f;
        private List<EffectInstance> activeEffects = new List<EffectInstance>(); // Track all active effects
        private float effectCleanupDelay = 5.0f; // Time before auto-cleanup

        // NEW: Level change detection
        private bool checkForLevelChanges = true;
        private string lastLoadedLevel = "";
        private float levelCheckInterval = 1.0f; // Check every second

        // Debug logging
        private readonly string currentUser = "SuoperFull";
        private readonly string currentDateTime = "2025-05-11 02:26:55";

        // Cache for performance
        private readonly List<Rigidbody> tempRigidbodyList = new List<Rigidbody>(20);

        // Configuration constants
        private const float GESTURE_DOMINANT_AXIS_MULTIPLIER = 1.2f;
        #endregion

        #region Unity Lifecycle
        public override void OnItemLoaded(Item item)
        {
            base.OnItemLoaded(item);

            Debug.Log($"[{currentDateTime}] {currentUser} - T1ShadowCrystal module loaded for item: {item.itemId}");

            // Load saved weapon assignments from PlayerPrefs
            LoadSavedWeapons();

            // Subscribe to events
            item.OnHeldActionEvent += Item_OnHeldActionEvent;
            item.OnGrabEvent += Item_OnGrabEvent;
            item.OnUngrabEvent += Item_OnUngrabEvent;
            item.OnDespawnEvent += Item_OnDespawnEvent;

            // NEW: Start level change detection
            Debug.Log($"[{currentDateTime}] {currentUser} - Starting level change detection");
            if (Level.current != null)
                lastLoadedLevel = Level.current.data.id;

            // Start the coroutine to check for level changes
            item.StartCoroutine(CheckForLevelChanges());
        }

        // NEW: Level change detection coroutine
        private IEnumerator CheckForLevelChanges()
        {
            Debug.Log($"[{currentDateTime}] {currentUser} - Level change detection started");

            while (checkForLevelChanges && !isDestroying)
            {
                // Check if level changed
                if (Level.current != null && lastLoadedLevel != Level.current.data.id)
                {
                    string prevLevel = lastLoadedLevel;
                    lastLoadedLevel = Level.current.data.id;

                    Debug.Log($"[{currentDateTime}] {currentUser} - Level changed from {prevLevel} to {lastLoadedLevel}, reinitializing crystal");

                    // Reset crystal state
                    DeactivateAltUse(true);
                    CleanupAllEffects();

                    // Reset state
                    activeHand = null;
                    hoveringWeapon = null;
                    altUseActive = false;
                    isTrackingGesture = false;
                    justAssignedWeapon = false;

                    // Stop running coroutines (except this one)
                    if (activeCoroutine != null)
                    {
                        try
                        {
                            item.StopCoroutine(activeCoroutine);
                            activeCoroutine = null;
                        }
                        catch (Exception e)
                        {
                            Debug.LogError($"[{currentDateTime}] {currentUser} - Error stopping coroutine: {e.Message}");
                        }
                    }

                    // Re-load saved weapons
                    LoadSavedWeapons();
                }

                // Wait before next check
                yield return new WaitForSeconds(levelCheckInterval);
            }
        }

        private void Item_OnDespawnEvent(EventTime eventTime)
        {
            if (eventTime == EventTime.OnStart)
            {
                Debug.Log($"[{currentDateTime}] {currentUser} - Crystal despawning, cleaning up...");
                CleanupEverything();
                isDestroying = true;
            }
        }

        private void Item_OnGrabEvent(Handle handle, RagdollHand hand)
        {
            activeHand = hand;
            Debug.Log($"[{currentDateTime}] {currentUser} - Crystal grabbed with {(hand.side == Side.Right ? "RIGHT" : "LEFT")} hand");

            // Reset cooldown timer when grabbing to ensure we can pick up weapons immediately
            lastPickupTime = 0f;
            lastTime = Time.time;
            lastHandPosition = hand.transform.position;

            // Send haptic feedback if enabled
            if (ShadowCrystalConfig.UseHapticFeedback && activeHand != null)
            {
                activeHand.HapticTick(0.5f);
            }
        }

        private void Item_OnUngrabEvent(Handle handle, RagdollHand hand, bool throwing)
        {
            DeactivateAltUse();
            activeHand = null;
            Debug.Log($"[{currentDateTime}] {currentUser} - Crystal released from {(hand.side == Side.Right ? "RIGHT" : "LEFT")} hand");
        }

        private void Item_OnHeldActionEvent(RagdollHand hand, Handle handle, Interactable.Action action)
        {
            // Handle alt use (grip button)
            if (action == Interactable.Action.AlternateUseStart)
            {
                ActivateAltUse();
            }
            else if (action == Interactable.Action.AlternateUseStop)
            {
                DeactivateAltUse();
            }
        }

        protected void OnDestroy()
        {
            Debug.Log($"[{currentDateTime}] {currentUser} - OnDestroy called, cleaning up...");
            CleanupEverything();
        }
        #endregion

        #region Weapon Storage and Retrieval
        private void LoadSavedWeapons()
        {
            // Reset dictionaries
            rightHandWeapons.Clear();
            leftHandWeapons.Clear();

            // Load saved weapon assignments from PlayerPrefs for both hands
            foreach (WeaponDirection dir in Enum.GetValues(typeof(WeaponDirection)))
            {
                if (dir == WeaponDirection.None) continue;

                // Load right hand weapons
                string rightKey = $"T1ShadowCrystal_RightHand_Direction_{dir}";
                if (PlayerPrefs.HasKey(rightKey))
                {
                    string weaponId = PlayerPrefs.GetString(rightKey);
                    rightHandWeapons[dir.ToString()] = weaponId;
                    Debug.Log($"[{currentDateTime}] {currentUser} - Loaded RIGHT hand weapon {weaponId} for direction {dir}");
                }

                // Load left hand weapons
                string leftKey = $"T1ShadowCrystal_LeftHand_Direction_{dir}";
                if (PlayerPrefs.HasKey(leftKey))
                {
                    string weaponId = PlayerPrefs.GetString(leftKey);
                    leftHandWeapons[dir.ToString()] = weaponId;
                    Debug.Log($"[{currentDateTime}] {currentUser} - Loaded LEFT hand weapon {weaponId} for direction {dir}");
                }

                // For backward compatibility, also check the old format keys
                string legacyKey = $"T1ShadowCrystal_Direction_{dir}";
                if (PlayerPrefs.HasKey(legacyKey))
                {
                    string weaponId = PlayerPrefs.GetString(legacyKey);
                    // Add to right hand (legacy default)
                    if (!rightHandWeapons.ContainsKey(dir.ToString()))
                    {
                        rightHandWeapons[dir.ToString()] = weaponId;
                        Debug.Log($"[{currentDateTime}] {currentUser} - Loaded legacy weapon {weaponId} for direction {dir}");
                    }
                }
            }
        }

        // Method accessible by scripts with string-based direction and side
        public string GetWeaponForDirection(string direction, Side handSide)
        {
            Dictionary<string, string> weaponMap = (handSide == Side.Right) ? rightHandWeapons : leftHandWeapons;

            if (weaponMap.TryGetValue(direction, out string weaponId))
            {
                return weaponId;
            }

            // Fallback to the other hand if this hand doesn't have an assignment
            // and we're not using separate hand assignments
            if (!ShadowCrystalConfig.SeparateHandAssignments)
            {
                Dictionary<string, string> fallbackMap = (handSide == Side.Right) ? leftHandWeapons : rightHandWeapons;
                if (fallbackMap.TryGetValue(direction, out string fallbackWeaponId))
                {
                    return fallbackWeaponId;
                }
            }

            return null;
        }

        // Legacy method for backward compatibility
        public string GetWeaponForDirection(string direction)
        {
            // Default to right hand for legacy calls
            return GetWeaponForDirection(direction, Side.Right);
        }

        private void AssignWeaponToDirection(Side handSide, WeaponDirection direction, string weaponId)
        {
            if (direction == WeaponDirection.None || isDestroying || string.IsNullOrEmpty(weaponId)) return;

            string handName = (handSide == Side.Right) ? "RIGHT" : "LEFT";

            // Store in the appropriate dictionary based on hand side
            if (handSide == Side.Right)
            {
                rightHandWeapons[direction.ToString()] = weaponId;
            }
            else
            {
                leftHandWeapons[direction.ToString()] = weaponId;
            }

            // Store in PlayerPrefs with hand-specific prefix
            string key = $"T1ShadowCrystal_{handName}Hand_Direction_{direction}";
            PlayerPrefs.SetString(key, weaponId);

            // For backward compatibility, also store in the original format if right hand
            if (handSide == Side.Right)
            {
                string legacyKey = $"T1ShadowCrystal_Direction_{direction}";
                PlayerPrefs.SetString(legacyKey, weaponId);
            }

            PlayerPrefs.Save(); // Force save immediately

            Debug.Log($"[{currentDateTime}] {currentUser} - *** ASSIGNED {handName} HAND WEAPON {weaponId} TO DIRECTION {direction} ***");
        }
        #endregion

        #region Alt Use and Activation
        private void ActivateAltUse()
        {
            // If we're already in alt use mode, clean up everything first for a clean state
            if (altUseActive)
            {
                DeactivateAltUse(true);

                // Brief cooldown to prevent accidental re-activation
                if (Time.time - lastPickupTime < ShadowCrystalConfig.PickupCooldown / 2)
                {
                    Debug.Log($"[{currentDateTime}] {currentUser} - Cooldown active, preventing immediate reactivation");
                    return;
                }
            }

            // Basic validation
            if (activeHand == null || isDestroying)
            {
                Debug.Log($"[{currentDateTime}] {currentUser} - Can't activate: activeHand null={activeHand == null}, destroying={isDestroying}");
                return;
            }

            // Set active state
            altUseActive = true;
            lastPickupTime = Time.time;
            weaponPickupAttempts = 0;

            Debug.Log($"[{currentDateTime}] {currentUser} - Alt use activated with {(activeHand.side == Side.Right ? "RIGHT" : "LEFT")} hand");

            // Make sure we're starting with a clean state
            if (hoveringWeapon != null)
            {
                Debug.Log($"[{currentDateTime}] {currentUser} - Found lingering hover weapon on activation, releasing");
                ReleaseHoveringWeapon();
            }

            // Immediate feedback
            PlayEffect("hover", item.transform.position, item.transform.rotation);

            // Haptic feedback
            if (ShadowCrystalConfig.UseHapticFeedback && activeHand != null)
            {
                activeHand.HapticTick(0.3f);
            }

            // Start the alt-use coroutine
            StopActiveCoroutines();
            activeCoroutine = item.StartCoroutine(AltUseCoroutine());

            Debug.Log($"[{currentDateTime}] {currentUser} - Alt-use coroutine started");
        }

        private void DeactivateAltUse(bool forced = false)
        {
            if (!altUseActive && !forced) return;

            Debug.Log($"[{currentDateTime}] {currentUser} - Alt use deactivated (forced={forced})");

            // Stop all coroutines first before state changes
            StopActiveCoroutines();

            // Release hovering weapon if any
            if (hoveringWeapon != null)
            {
                ReleaseHoveringWeapon();
            }

            // Reset state
            altUseActive = false;
            isTrackingGesture = false;
            justAssignedWeapon = false;
        }

        private void StopActiveCoroutines()
        {
            if (activeCoroutine != null)
            {
                try
                {
                    item.StopCoroutine(activeCoroutine);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[{currentDateTime}] {currentUser} - Error stopping coroutine: {e.Message}");
                }
                activeCoroutine = null;
            }
        }
        #endregion

        #region Core Coroutines
        private IEnumerator AltUseCoroutine()
        {
            // Reduced interval for more responsive updating
            WaitForSeconds updateInterval = new WaitForSeconds(0.05f);
            float startTime = Time.time;
            bool foundWeapon = false;

            // First wait a short time to make sure crystal is stable
            yield return new WaitForSeconds(0.1f);

            Debug.Log($"[{currentDateTime}] {currentUser} - Starting to look for weapons");

            // Continue looking as long as we're active
            while (altUseActive && activeHand != null && !isDestroying)
            {
                // Safety timeout
                if (Time.time - startTime > 15f)
                {
                    Debug.Log($"[{currentDateTime}] {currentUser} - Alt-use timeout after 15 seconds");
                    break;
                }

                // First priority: get a weapon if don't have one
                if (hoveringWeapon == null && !justAssignedWeapon)
                {
                    // Try to find a weapon
                    FindNearestWeapon();

                    // If we found a weapon, set it up
                    if (hoveringWeapon != null && !foundWeapon)
                    {
                        foundWeapon = true;
                        Debug.Log($"[{currentDateTime}] {currentUser} - FOUND WEAPON: {hoveringWeapon.itemId}");

                        // Generate a small random offset for hovering
                        hoverPositionOffset = new Vector3(
                            UnityEngine.Random.Range(-0.05f, 0.05f),
                            UnityEngine.Random.Range(0.05f, 0.1f), // Slightly higher Y offset
                            UnityEngine.Random.Range(-0.05f, 0.05f)
                        );

                        // Play weapon found effect
                        PlayEffect("hover", hoveringWeapon.transform.position, Quaternion.identity);

                        // IMPORTANT: Force the first position update immediately
                        ForceUpdateHoverPosition();

                        // Haptic feedback for found weapon
                        if (ShadowCrystalConfig.UseHapticFeedback && activeHand != null)
                        {
                            activeHand.HapticTick(0.5f);
                        }
                    }
                    else if (hoveringWeapon == null)
                    {
                        // Try less times, check more often
                        weaponPickupAttempts++;
                        if (weaponPickupAttempts > 40)
                        {
                            Debug.Log($"[{currentDateTime}] {currentUser} - Failed to find weapons after {weaponPickupAttempts} attempts");
                            break;
                        }
                    }
                }

                // If we have a weapon, check if valid and update position
                if (hoveringWeapon != null)
                {
                    // Basic validity checks
                    if (!hoveringWeapon.gameObject.activeInHierarchy)
                    {
                        Debug.Log($"[{currentDateTime}] {currentUser} - Weapon became inactive");
                        ReleaseHoveringWeapon();
                    }
                    else if (hoveringWeapon.IsHanded())
                    {
                        Debug.Log($"[{currentDateTime}] {currentUser} - Weapon was grabbed by player");
                        ReleaseHoveringWeapon();
                    }
                    else
                    {
                        // Forced physics and position update every frame for responsive hovering
                        ForceWeaponKinematic();
                        UpdateHoverPosition();

                        // Handle gesture tracking
                        UpdateGestureTracking();
                    }
                }

                // Use a short update interval to make positioning more responsive
                yield return updateInterval;
            }

            Debug.Log($"[{currentDateTime}] {currentUser} - Alt use coroutine ended");
            DeactivateAltUse(true);
        }

        private void UpdateGestureTracking()
        {
            // If we're not tracking the gesture yet, start tracking
            if (!isTrackingGesture)
            {
                gestureStartTime = Time.time;
                isTrackingGesture = true;
                lastHandPosition = activeHand.transform.position;
                Debug.Log($"[{currentDateTime}] {currentUser} - Started tracking gesture with {(activeHand.side == Side.Right ? "RIGHT" : "LEFT")} hand");
            }
            // Otherwise check for gestures
            else
            {
                // Calculate movement since gesture start
                Vector3 movement = activeHand.transform.position - lastHandPosition;
                float movementMagnitude = movement.magnitude;

                // If we've moved enough to be considered a gesture
                if (movementMagnitude > ShadowCrystalConfig.GestureThreshold)
                {
                    ProcessGesture(movement);
                }
                else if (ShadowCrystalConfig.AutoReleaseOnTimeout &&
                         Time.time - gestureStartTime > ShadowCrystalConfig.GestureTimeout)
                {
                    Debug.Log($"[{currentDateTime}] {currentUser} - Gesture timeout reached, resetting");
                    isTrackingGesture = false;
                    lastHandPosition = activeHand.transform.position;
                    gestureStartTime = Time.time;
                    PlayEffect("hover", activeHand.transform.position, Quaternion.identity);
                }
            }
        }
        #endregion

        #region Weapon Handling and Physics
        private void ForceWeaponKinematic()
        {
            if (hoveringWeapon == null) return;

            try
            {
                // Make sure the weapon's physics body is kinematic and has no velocity
                if (hoveringWeapon.physicBody != null)
                {
                    hoveringWeapon.physicBody.isKinematic = true;
                    hoveringWeapon.physicBody.useGravity = false;
                    hoveringWeapon.physicBody.velocity = Vector3.zero;
                    hoveringWeapon.physicBody.angularVelocity = Vector3.zero;

                    // Wake physics for each frame to ensure it responds to position changes
                    hoveringWeapon.physicBody.WakeUp();
                }

                // Also handle any child rigidbodies to be extra safe
                hoveringWeapon.GetComponentsInChildren(tempRigidbodyList);
                foreach (Rigidbody rb in tempRigidbodyList)
                {
                    if (rb != null && rb != hoveringWeapon.physicBody)
                    {
                        rb.isKinematic = true;
                        rb.useGravity = false;
                        rb.velocity = Vector3.zero;
                        rb.angularVelocity = Vector3.zero;
                    }
                }
                tempRigidbodyList.Clear();
            }
            catch (Exception e)
            {
                Debug.LogError($"[{currentDateTime}] {currentUser} - Error forcing weapon kinematic: {e.Message}");
            }
        }

        private void ForceUpdateHoverPosition()
        {
            if (hoveringWeapon == null || item == null) return;

            try
            {
                // First ensure rigidbody is properly set to kinematic
                ForceWeaponKinematic();

                // Calculate the target position EXPLICITLY ABOVE the crystal
                Vector3 crystalPos = item.transform.position;
                // Get hover height from config or use default
                float hoverHeight = defaultHoverHeight;
                if (ShadowCrystalConfig.HoverHeight > 0)
                    hoverHeight = ShadowCrystalConfig.HoverHeight;

                // Position weapon directly above crystal
                Vector3 targetPos = crystalPos + (Vector3.up * hoverHeight) + hoverPositionOffset;

                // Apply rotation around Y axis
                float angle = (Time.time * 40f) % 360f;
                Quaternion targetRot = Quaternion.Euler(0, angle, 0);

                // DIRECTLY SET position and rotation (no lerping for first update)
                hoveringWeapon.transform.position = targetPos;
                hoveringWeapon.transform.rotation = targetRot;

                Debug.Log($"[{currentDateTime}] {currentUser} - Forced weapon {hoveringWeapon.itemId} to position {targetPos} above crystal at {crystalPos}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[{currentDateTime}] {currentUser} - Error force updating position: {e.Message}");
            }
        }

        private void UpdateHoverPosition()
        {
            if (hoveringWeapon == null || item == null) return;

            try
            {
                // Get the crystal position as our anchor point
                Vector3 crystalPosition = item.transform.position;

                // Get hover height from config or use default
                float hoverHeight = defaultHoverHeight;
                if (ShadowCrystalConfig.HoverHeight > 0)
                    hoverHeight = ShadowCrystalConfig.HoverHeight;

                // Calculate target position: above the crystal
                Vector3 targetPosition = crystalPosition +
                                       (Vector3.up * hoverHeight) +
                                       hoverPositionOffset;

                // Calculate rotation around the up axis
                float rotationSpeed = 40f;
                float currentAngle = (Time.time * rotationSpeed) % 360f;
                Quaternion targetRotation = Quaternion.Euler(0, currentAngle, 0);

                // Use faster lerp speeds for more responsive movement
                float posSpeed = ShadowCrystalConfig.PositionLerpSpeed;
                if (posSpeed < 10f) posSpeed = 15f; // Minimum acceptable speed

                float rotSpeed = ShadowCrystalConfig.RotationLerpSpeed;
                if (rotSpeed < 5f) rotSpeed = 7f; // Minimum acceptable speed

                // Move weapon to target position with faster interpolation
                hoveringWeapon.transform.position = Vector3.Lerp(
                    hoveringWeapon.transform.position,
                    targetPosition,
                    Time.deltaTime * posSpeed);

                // Update rotation
                hoveringWeapon.transform.rotation = Quaternion.Slerp(
                    hoveringWeapon.transform.rotation,
                    targetRotation,
                    Time.deltaTime * rotSpeed);
            }
            catch (Exception e)
            {
                Debug.LogError($"[{currentDateTime}] {currentUser} - Error updating hover position: {e.Message}");
            }
        }

        private void FindNearestWeapon()
        {
            if (activeHand == null || isDestroying || hoveringWeapon != null) return;

            try
            {
                // Use larger search radius for better results
                float searchRadius = 2.0f;
                if (ShadowCrystalConfig.SearchRadius > 0)
                    searchRadius = ShadowCrystalConfig.SearchRadius;

                // Use the crystal position as search center (not the hand)
                Vector3 searchCenter = item.transform.position;

                // Log the search parameters
                Debug.Log($"[{currentDateTime}] {currentUser} - Searching for weapons from crystal at {searchCenter} with radius {searchRadius}");

                // Find all colliders nearby
                Collider[] colliders = Physics.OverlapSphere(
                    searchCenter,
                    searchRadius);

                float closestDistance = float.MaxValue;
                Item closestItem = null;
                int weaponsFound = 0;

                foreach (Collider collider in colliders)
                {
                    // Skip invalid colliders
                    if (collider == null || !collider.gameObject.activeInHierarchy) continue;

                    // Get the item from this collider
                    Item itemComponent = collider.GetComponentInParent<Item>();

                    // Process valid items (must not be this crystal, must not be held)
                    if (itemComponent != null && itemComponent != item && !itemComponent.IsHanded())
                    {
                        weaponsFound++;

                        // Calculate distance from crystal (not hand)
                        float distance = Vector3.Distance(searchCenter, itemComponent.transform.position);
                        if (distance < closestDistance)
                        {
                            closestDistance = distance;
                            closestItem = itemComponent;
                        }
                    }
                }

                // If we found a weapon, start hovering it
                if (closestItem != null)
                {
                    Debug.Log($"[{currentDateTime}] {currentUser} - Found {weaponsFound} weapons, selecting: {closestItem.itemId} at {closestDistance:F2}m");
                    StartHoveringWeapon(closestItem);
                }
                else
                {
                    Debug.Log($"[{currentDateTime}] {currentUser} - No weapons found in search radius");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[{currentDateTime}] {currentUser} - Error finding weapon: {e.Message}");
            }
        }

        private void StartHoveringWeapon(Item weapon)
        {
            if (weapon == null || isDestroying) return;

            try
            {
                // Safety check - release any existing weapon first
                if (hoveringWeapon != null)
                {
                    ReleaseHoveringWeapon();
                }

                Debug.Log($"[{currentDateTime}] {currentUser} - Starting hover for weapon: {weapon.itemId}");
                hoveringWeapon = weapon;

                // Display debug message with weapon info
                Debug.Log($"[{currentDateTime}] {currentUser} - Weapon info: Position={weapon.transform.position}, " +
                          $"Physics={weapon.physicBody != null}, " +
                          $"Handles={weapon.handles?.Count ?? 0}");

                // IMPORTANT: Force kinematic mode immediately
                if (weapon.physicBody != null)
                {
                    weapon.physicBody.isKinematic = true;
                    weapon.physicBody.useGravity = false;
                    weapon.physicBody.velocity = Vector3.zero;
                    weapon.physicBody.angularVelocity = Vector3.zero;
                }

                // Also get all child rigidbodies and set them to kinematic
                weapon.GetComponentsInChildren(true, tempRigidbodyList);
                foreach (Rigidbody rb in tempRigidbodyList)
                {
                    if (rb != null)
                    {
                        rb.isKinematic = true;
                        rb.useGravity = false;
                        rb.velocity = Vector3.zero;
                        rb.angularVelocity = Vector3.zero;
                    }
                }
                tempRigidbodyList.Clear();

                // Create hover effect if configured
                if (ShadowCrystalConfig.UseVisualFeedback)
                {
                    string effectId = "effectShadowHover"; // Default effect ID
                    EffectData effect = Catalog.GetData<EffectData>(effectId);
                    if (effect != null)
                    {
                        hoverEffect = effect.Spawn(weapon.transform);
                        if (hoverEffect != null)
                        {
                            hoverEffect.Play();
                            // Add to tracked effects
                            activeEffects.Add(hoverEffect);
                            Debug.Log($"[{currentDateTime}] {currentUser} - Started hover effect");
                        }
                    }
                }

                // IMPORTANT: Force update position immediately
                ForceUpdateHoverPosition();
            }
            catch (Exception e)
            {
                Debug.LogError($"[{currentDateTime}] {currentUser} - Error starting hover: {e.Message}");
                hoveringWeapon = null;
            }
        }

        private void ReleaseHoveringWeapon()
        {
            if (hoveringWeapon == null) return;

            Debug.Log($"[{currentDateTime}] {currentUser} - Releasing hover weapon: {hoveringWeapon.itemId}");

            try
            {
                // Stop and clean up effects first
                StopHoverEffect();

                // Re-enable physics on all rigidbodies if the weapon still exists
                if (hoveringWeapon.gameObject != null && hoveringWeapon.gameObject.activeInHierarchy)
                {
                    // Get all rigidbodies in the item and its children
                    hoveringWeapon.GetComponentsInChildren(true, tempRigidbodyList);

                    foreach (Rigidbody rb in tempRigidbodyList)
                    {
                        if (rb != null)
                        {
                            // First, prepare velocities while still kinematic
                            rb.velocity = new Vector3(
                                UnityEngine.Random.Range(-0.2f, 0.2f),
                                UnityEngine.Random.Range(-0.3f, -0.1f),
                                UnityEngine.Random.Range(-0.2f, 0.2f));

                            // Then enable physics
                            rb.useGravity = true;
                            rb.isKinematic = false;
                            rb.WakeUp();
                        }
                    }
                    tempRigidbodyList.Clear();
                }

                // Store reference then clear our reference
                Item releasedWeapon = hoveringWeapon;
                hoveringWeapon = null;

                Debug.Log($"[{currentDateTime}] {currentUser} - Weapon released successfully");
            }
            catch (Exception e)
            {
                Debug.LogError($"[{currentDateTime}] {currentUser} - Error releasing weapon: {e.Message}");
                hoveringWeapon = null;
            }
        }

        private void StopHoverEffect()
        {
            if (hoverEffect != null)
            {
                try
                {
                    hoverEffect.Stop();
                    hoverEffect.Despawn();

                    // Remove from tracked effects
                    if (activeEffects.Contains(hoverEffect))
                    {
                        activeEffects.Remove(hoverEffect);
                    }

                    Debug.Log($"[{currentDateTime}] {currentUser} - Stopped hover effect");
                }
                catch (Exception e)
                {
                    Debug.LogError($"[{currentDateTime}] {currentUser} - Error stopping effect: {e.Message}");
                }
                hoverEffect = null;
            }
        }
        #endregion

        #region Gesture Detection and Processing
        private WeaponDirection DetermineGestureDirection(Vector3 movement)
        {
            if (activeHand == null) return WeaponDirection.None;

            // Convert to player's local space for consistent directions  
            Vector3 localDir = Player.local.transform.InverseTransformDirection(movement.normalized);

            float absX = Mathf.Abs(localDir.x);
            float absY = Mathf.Abs(localDir.y);
            float absZ = Mathf.Abs(localDir.z);
            Side handSide = activeHand.side;

            // Determine dominant axis with simpler logic
            if (absY > absX && absY > absZ)
            {
                // Y-axis dominant (up/down)
                return localDir.y > 0 ? WeaponDirection.Up : WeaponDirection.Down;
            }
            else if (absX > absY && absX > absZ)
            {
                // X-axis dominant (left/right) - handle differently based on hand
                if (handSide == Side.Right)
                {
                    return localDir.x > 0 ? WeaponDirection.Right : WeaponDirection.Left;
                }
                else
                {
                    // For left hand, invert for natural feeling
                    return localDir.x > 0 ? WeaponDirection.Left : WeaponDirection.Right;
                }
            }
            else
            {
                // Z-axis dominant or no clear dominance (forward/backward)
                return localDir.z > 0 ? WeaponDirection.Forward : WeaponDirection.Backward;
            }
        }

        private void ProcessGesture(Vector3 movement)
        {
            // Determine gesture direction
            WeaponDirection gestureDirection = DetermineGestureDirection(movement);

            Debug.Log($"[{currentDateTime}] {currentUser} - {(activeHand.side == Side.Right ? "RIGHT" : "LEFT")} hand detected gesture: {gestureDirection}");

            // Assign weapon to the detected direction
            AssignWeaponToDirection(activeHand.side, gestureDirection, hoveringWeapon.itemId);

            // Visual and haptic feedback
            PlayEffect("assign", hoveringWeapon.transform.position, Quaternion.identity);

            if (ShadowCrystalConfig.UseHapticFeedback && activeHand != null)
            {
                activeHand.HapticTick(1.0f);
            }

            // Reset tracking
            isTrackingGesture = false;
            justAssignedWeapon = true;

            // Start the auto-cleanup timer
            item.StartCoroutine(AutoCleanupEffectsAfterDelay());

            // What to do next depends on configuration
            if (ShadowCrystalConfig.KeepHoveringAfterAssign)
            {
                // Reset gesture tracking but keep hovering
                lastHandPosition = activeHand.transform.position;
                gestureStartTime = Time.time;

                // Only look for another weapon if cycle weapons is enabled
                if (ShadowCrystalConfig.CycleWeapons)
                {
                    item.StartCoroutine(FindNextWeaponDelayed());
                }
            }
            else
            {
                // Release weapon and deactivate alt-use
                ReleaseHoveringWeapon();
                DeactivateAltUse(true);

                // Re-enable after delay
                item.StartCoroutine(ReactivateAfterDelay());
            }
        }

        private IEnumerator ReactivateAfterDelay()
        {
            yield return new WaitForSeconds(ShadowCrystalConfig.PickupCooldown);
            lastPickupTime = 0f;
            justAssignedWeapon = false;
            Debug.Log($"[{currentDateTime}] {currentUser} - Ready for next pickup");
        }

        private IEnumerator FindNextWeaponDelayed()
        {
            yield return new WaitForSeconds(0.5f);

            if (altUseActive && hoveringWeapon != null)
            {
                ReleaseHoveringWeapon();
                justAssignedWeapon = false;
            }
        }
        #endregion

        #region Utility Methods
        // Auto-cleanup coroutine that destroys effects after the specified delay
        private IEnumerator AutoCleanupEffectsAfterDelay()
        {
            Debug.Log($"[{currentDateTime}] {currentUser} - Starting auto-cleanup timer for effects ({effectCleanupDelay} seconds)");

            // Wait for the specified delay
            yield return new WaitForSeconds(effectCleanupDelay);

            // Clean up all tracked effects
            CleanupAllEffects();

            // Also stop alt-use mode if it's still active
            if (altUseActive)
            {
                Debug.Log($"[{currentDateTime}] {currentUser} - Auto-deactivating alt-use mode after assignment");
                DeactivateAltUse(true);
            }
        }

        // Method to clean up all active effects
        private void CleanupAllEffects()
        {
            Debug.Log($"[{currentDateTime}] {currentUser} - Cleaning up all active effects: {activeEffects.Count}");

            // Stop hover effect
            StopHoverEffect();

            // Stop and dispose all tracked effects
            foreach (EffectInstance effect in activeEffects.ToArray())
            {
                if (effect != null)
                {
                    try
                    {
                        effect.Stop();
                        effect.Despawn();
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[{currentDateTime}] {currentUser} - Error cleaning up effect: {e.Message}");
                    }
                }
            }

            // Clear the list
            activeEffects.Clear();

            // Release hovering weapon if still active
            if (hoveringWeapon != null)
            {
                ReleaseHoveringWeapon();
            }
        }

        private void PlayEffect(string effectType, Vector3 position, Quaternion rotation)
        {
            if (Time.time - lastEffectTime < 0.5f) return;
            lastEffectTime = Time.time;

            try
            {
                string effectId = null;

                // Determine which effect to use based on type
                if (effectType == "hover")
                    effectId = "effectShadowHover";
                else if (effectType == "assign")
                    effectId = "effectShadowAssign";

                if (!ShadowCrystalConfig.UseVisualFeedback || string.IsNullOrEmpty(effectId))
                    return;

                EffectData effect = Catalog.GetData<EffectData>(effectId);
                if (effect != null)
                {
                    EffectInstance instance = effect.Spawn(position, rotation);
                    if (instance != null)
                    {
                        instance.Play();

                        // Add to tracked effects list
                        activeEffects.Add(instance);

                        // Still maintain auto-dispose for individual effects
                        item.StartCoroutine(AutoDisposeEffect(instance, 2.0f));
                    }
                }
                else
                {
                    Debug.LogError($"[{currentDateTime}] {currentUser} - Effect not found: {effectId}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[{currentDateTime}] {currentUser} - Error playing effect: {e.Message}");
            }
        }

        private IEnumerator AutoDisposeEffect(EffectInstance effect, float delay)
        {
            if (effect == null) yield break;
            yield return new WaitForSeconds(delay);

            if (effect != null)
            {
                try
                {
                    effect.Stop();
                    effect.Despawn();

                    // Remove from tracked effects if still in list
                    if (activeEffects.Contains(effect))
                    {
                        activeEffects.Remove(effect);
                    }
                }
                catch { /* Ignore errors during cleanup */ }
            }
        }

        private void CleanupEverything()
        {
            Debug.Log($"[{currentDateTime}] {currentUser} - Performing complete cleanup");

            // Mark as destroying to prevent new operations
            isDestroying = true;

            // NEW: Stop level change detection
            checkForLevelChanges = false;

            // Stop active coroutines
            StopActiveCoroutines();

            // Clean up all effects
            CleanupAllEffects();

            // Reset flags
            altUseActive = false;
            isTrackingGesture = false;
            justAssignedWeapon = false;

            // Unsubscribe from events
            if (item != null)
            {
                item.OnHeldActionEvent -= Item_OnHeldActionEvent;
                item.OnGrabEvent -= Item_OnGrabEvent;
                item.OnUngrabEvent -= Item_OnUngrabEvent;
                item.OnDespawnEvent -= Item_OnDespawnEvent;
            }

            Debug.Log($"[{currentDateTime}] {currentUser} - Cleanup completed");
        }
        #endregion
    }
}