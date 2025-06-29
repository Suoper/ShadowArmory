using ThunderRoad;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

namespace ShadowArmory
{
    /// <summary>
    /// SkillOrbModule - Creates direction orbs that weapons can collide with and orbit around
    /// Implements collision detection and orbital motion for weapon assignment
    /// </summary>
    public class SkillOrbModule : ItemModule
    {
        private readonly string currentUser = "Suoper";
        private readonly string currentDateTime = "2025-06-29 03:30:38";

        [Header("Orb Configuration")]
        public float orbDistance = 0.8f;          // Distance from center to orbs (will be overridden by config)
        public float orbRadius = 0.15f;           // Size of each orb (will be overridden by config)
        public float orbitSpeed = 30f;            // Speed of weapon orbiting (will be overridden by config)
        public float orbitRadius = 0.2f;          // Radius of weapon orbit around orb (will be overridden by config)

        // Direction orbs and assigned weapons
        private Dictionary<DirectionTrigger.WeaponDirection, GameObject> directionOrbs;
        private Dictionary<DirectionTrigger.WeaponDirection, Item> assignedWeapons;
        private Dictionary<Item, DirectionTrigger.WeaponDirection> weaponToDirection;

        // Orbital motion tracking
        private Dictionary<Item, float> weaponOrbitAngles;
        private Dictionary<Item, Vector3> weaponOrbitOffsets;

        // Visual orbs for debugging
        private List<GameObject> orbVisuals;

        // Update interval for smooth orbital motion
        private WaitForSeconds updateInterval = new WaitForSeconds(0.02f); // 50 FPS
        private Coroutine orbitalMotionCoroutine;

        public override void OnItemLoaded(Item item)
        {
            base.OnItemLoaded(item);

            SkillOrbConfig.DebugLog($"[{currentDateTime}] {currentUser} - SkillOrbModule loaded for item: {item.itemId}");

            // Initialize collections
            directionOrbs = new Dictionary<DirectionTrigger.WeaponDirection, GameObject>();
            assignedWeapons = new Dictionary<DirectionTrigger.WeaponDirection, Item>();
            weaponToDirection = new Dictionary<Item, DirectionTrigger.WeaponDirection>();
            weaponOrbitAngles = new Dictionary<Item, float>();
            weaponOrbitOffsets = new Dictionary<Item, Vector3>();
            orbVisuals = new List<GameObject>();

            // Create direction orbs
            CreateDirectionOrbs();

            // Start orbital motion system
            StartOrbitalMotion();
        }

        private void CreateDirectionOrbs()
        {
            SkillOrbConfig.DebugLog($"[{currentDateTime}] {currentUser} - Creating direction orbs");

            // Create orbs for each direction
            DirectionTrigger.WeaponDirection[] directions = {
                DirectionTrigger.WeaponDirection.Up,
                DirectionTrigger.WeaponDirection.Down,
                DirectionTrigger.WeaponDirection.Left,
                DirectionTrigger.WeaponDirection.Right,
                DirectionTrigger.WeaponDirection.Forward,
                DirectionTrigger.WeaponDirection.Backward
            };

            foreach (var direction in directions)
            {
                CreateDirectionOrb(direction);
            }

            SkillOrbConfig.DebugLog($"[{currentDateTime}] {currentUser} - Created {directionOrbs.Count} direction orbs");
        }

        private void CreateDirectionOrb(DirectionTrigger.WeaponDirection direction)
        {
            // Calculate position based on direction (use config values)
            Vector3 orbPosition = GetOrbPosition(direction);

            // Create orb GameObject
            GameObject orbObject = new GameObject($"DirectionOrb_{direction}");
            orbObject.transform.SetParent(item.transform);
            orbObject.transform.localPosition = orbPosition;

            // Add DirectionTrigger component
            DirectionTrigger trigger = orbObject.AddComponent<DirectionTrigger>();
            trigger.Initialize(this, direction);

            // Create visual representation if enabled
            if (SkillOrbConfig.ShowOrbVisuals)
            {
                CreateOrbVisual(orbObject, direction);
            }

            // Store reference
            directionOrbs[direction] = orbObject;

            SkillOrbConfig.DebugLog($"[{currentDateTime}] {currentUser} - Created orb for direction {direction} at position {orbPosition}");
        }

        private void CreateOrbVisual(GameObject orbObject, DirectionTrigger.WeaponDirection direction)
        {
            // Create a sphere primitive for visual feedback
            GameObject visualSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visualSphere.name = $"OrbVisual_{direction}";
            visualSphere.transform.SetParent(orbObject.transform);
            visualSphere.transform.localPosition = Vector3.zero;
            visualSphere.transform.localScale = Vector3.one * (SkillOrbConfig.OrbRadius * 2);

            // Remove the default collider (we use DirectionTrigger's collider)
            Collider defaultCollider = visualSphere.GetComponent<Collider>();
            if (defaultCollider != null)
            {
                Object.Destroy(defaultCollider);
            }

            // Make it semi-transparent and colored based on direction
            Renderer renderer = visualSphere.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material material = new Material(Shader.Find("Standard"));
                material.color = GetDirectionColor(direction);
                material.SetFloat("_Mode", 3); // Transparent mode
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.DisableKeyword("_ALPHATEST_ON");
                material.EnableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = 3000;
                
                Color color = material.color;
                color.a = SkillOrbConfig.OrbTransparency; // Use config transparency
                material.color = color;
                
                renderer.material = material;
            }

            orbVisuals.Add(visualSphere);
        }

        private Vector3 GetOrbPosition(DirectionTrigger.WeaponDirection direction)
        {
            float distance = SkillOrbConfig.OrbDistance; // Use config value
            
            switch (direction)
            {
                case DirectionTrigger.WeaponDirection.Up:
                    return Vector3.up * distance;
                case DirectionTrigger.WeaponDirection.Down:
                    return Vector3.down * distance;
                case DirectionTrigger.WeaponDirection.Left:
                    return Vector3.left * distance;
                case DirectionTrigger.WeaponDirection.Right:
                    return Vector3.right * distance;
                case DirectionTrigger.WeaponDirection.Forward:
                    return Vector3.forward * distance;
                case DirectionTrigger.WeaponDirection.Backward:
                    return Vector3.back * distance;
                default:
                    return Vector3.zero;
            }
        }

        private Color GetDirectionColor(DirectionTrigger.WeaponDirection direction)
        {
            switch (direction)
            {
                case DirectionTrigger.WeaponDirection.Up:
                    return Color.cyan;
                case DirectionTrigger.WeaponDirection.Down:
                    return Color.magenta;
                case DirectionTrigger.WeaponDirection.Left:
                    return Color.red;
                case DirectionTrigger.WeaponDirection.Right:
                    return Color.green;
                case DirectionTrigger.WeaponDirection.Forward:
                    return Color.blue;
                case DirectionTrigger.WeaponDirection.Backward:
                    return Color.yellow;
                default:
                    return Color.white;
            }
        }

        /// <summary>
        /// Called by DirectionTrigger when a weapon collides with an orb
        /// </summary>
        public void OnWeaponCollision(Item weapon, DirectionTrigger.WeaponDirection direction)
        {
            SkillOrbConfig.DebugLog($"[{currentDateTime}] {currentUser} - OnWeaponCollision: Weapon {weapon.itemId} hit {direction} orb");

            // Check if weapon is already assigned to a direction
            if (weaponToDirection.ContainsKey(weapon))
            {
                DirectionTrigger.WeaponDirection oldDirection = weaponToDirection[weapon];
                SkillOrbConfig.DebugLog($"[{currentDateTime}] {currentUser} - Weapon {weapon.itemId} was assigned to {oldDirection}, reassigning to {direction}");
                
                // Remove from old assignment
                UnassignWeapon(weapon);
            }

            // Check if this direction already has a weapon assigned
            if (assignedWeapons.ContainsKey(direction))
            {
                Item oldWeapon = assignedWeapons[direction];
                SkillOrbConfig.DebugLog($"[{currentDateTime}] {currentUser} - Direction {direction} already has weapon {oldWeapon.itemId}, replacing");
                
                // Remove old weapon assignment
                UnassignWeapon(oldWeapon);
            }

            // Assign weapon to this direction
            AssignWeapon(weapon, direction);
        }

        /// <summary>
        /// Manually retrieve a weapon from a specific direction
        /// </summary>
        public Item RetrieveWeapon(DirectionTrigger.WeaponDirection direction)
        {
            if (assignedWeapons.ContainsKey(direction))
            {
                Item weapon = assignedWeapons[direction];
                SkillOrbConfig.DebugLog($"[{currentDateTime}] {currentUser} - Manually retrieving weapon {weapon.itemId} from {direction} orb");
                
                // Unassign the weapon (this will restore its physics)
                UnassignWeapon(weapon);
                
                return weapon;
            }
            
            return null;
        }

        /// <summary>
        /// Get all currently assigned weapons
        /// </summary>
        public Dictionary<DirectionTrigger.WeaponDirection, Item> GetAssignedWeapons()
        {
            return new Dictionary<DirectionTrigger.WeaponDirection, Item>(assignedWeapons);
        }

        /// <summary>
        /// Event handler for when a weapon is grabbed - automatically unassigns it
        /// </summary>
        private void OnWeaponGrabbed(RagdollHand ragdollHand, Handle handle, EventTime eventTime)
        {
            // Find which weapon was grabbed and unassign it
            Item grabbedWeapon = handle?.item;
            if (grabbedWeapon != null && weaponToDirection.ContainsKey(grabbedWeapon))
            {
                SkillOrbConfig.DebugLog($"[{currentDateTime}] {currentUser} - Weapon {grabbedWeapon.itemId} was grabbed, auto-unassigning");
                UnassignWeapon(grabbedWeapon);
            }
        }

        private void AssignWeapon(Item weapon, DirectionTrigger.WeaponDirection direction)
        {
            SkillOrbConfig.DebugLog($"[{currentDateTime}] {currentUser} - Assigning weapon {weapon.itemId} to direction {direction}");

            // Store assignments
            assignedWeapons[direction] = weapon;
            weaponToDirection[weapon] = direction;

            // Subscribe to weapon grab events to auto-unassign when grabbed
            weapon.OnGrabEvent += OnWeaponGrabbed;

            // Initialize orbital motion for this weapon only if enabled
            if (SkillOrbConfig.EnableOrbitalMotion)
            {
                weaponOrbitAngles[weapon] = Random.Range(0f, 360f); // Random starting angle
                weaponOrbitOffsets[weapon] = Random.insideUnitSphere * 0.05f; // Small random offset

                // Make weapon kinematic for orbital control
                SetupWeaponForOrbiting(weapon);
            }

            SkillOrbConfig.DebugLog($"[{currentDateTime}] {currentUser} - Weapon {weapon.itemId} successfully assigned to {direction} orb");
        }

        private void UnassignWeapon(Item weapon)
        {
            if (!weaponToDirection.ContainsKey(weapon)) return;

            DirectionTrigger.WeaponDirection direction = weaponToDirection[weapon];
            SkillOrbConfig.DebugLog($"[{currentDateTime}] {currentUser} - Unassigning weapon {weapon.itemId} from direction {direction}");

            // Unsubscribe from events
            weapon.OnGrabEvent -= OnWeaponGrabbed;

            // Remove from assignments
            assignedWeapons.Remove(direction);
            weaponToDirection.Remove(weapon);
            weaponOrbitAngles.Remove(weapon);
            weaponOrbitOffsets.Remove(weapon);

            // Restore weapon physics
            RestoreWeaponPhysics(weapon);
        }

        private void SetupWeaponForOrbiting(Item weapon)
        {
            if (weapon == null || weapon.physicBody == null) return;

            try
            {
                // Make weapon kinematic to control its movement
                weapon.physicBody.isKinematic = true;
                weapon.physicBody.useGravity = false;
                weapon.physicBody.velocity = Vector3.zero;
                weapon.physicBody.angularVelocity = Vector3.zero;

                // Handle child rigidbodies
                Rigidbody[] childRigidbodies = weapon.GetComponentsInChildren<Rigidbody>();
                foreach (Rigidbody rb in childRigidbodies)
                {
                    if (rb != null && rb != weapon.physicBody)
                    {
                        rb.isKinematic = true;
                        rb.useGravity = false;
                        rb.velocity = Vector3.zero;
                        rb.angularVelocity = Vector3.zero;
                    }
                }

                SkillOrbConfig.DebugLog($"[{currentDateTime}] {currentUser} - Weapon {weapon.itemId} physics setup for orbiting");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[{currentDateTime}] {currentUser} - Error setting up weapon physics: {e.Message}");
            }
        }

        private void RestoreWeaponPhysics(Item weapon)
        {
            if (weapon == null || weapon.physicBody == null) return;

            try
            {
                // Restore normal physics
                weapon.physicBody.isKinematic = false;
                weapon.physicBody.useGravity = true;

                // Restore child rigidbodies
                Rigidbody[] childRigidbodies = weapon.GetComponentsInChildren<Rigidbody>();
                foreach (Rigidbody rb in childRigidbodies)
                {
                    if (rb != null && rb != weapon.physicBody)
                    {
                        rb.isKinematic = false;
                        rb.useGravity = true;
                    }
                }

                SkillOrbConfig.DebugLog($"[{currentDateTime}] {currentUser} - Weapon {weapon.itemId} physics restored");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[{currentDateTime}] {currentUser} - Error restoring weapon physics: {e.Message}");
            }
        }

        private void StartOrbitalMotion()
        {
            if (orbitalMotionCoroutine != null)
            {
                item.StopCoroutine(orbitalMotionCoroutine);
            }

            orbitalMotionCoroutine = item.StartCoroutine(OrbitalMotionUpdate());
            SkillOrbConfig.DebugLog($"[{currentDateTime}] {currentUser} - Orbital motion system started");
        }

        private IEnumerator OrbitalMotionUpdate()
        {
            while (true)
            {
                // Only update if orbital motion is enabled
                if (SkillOrbConfig.EnableOrbitalMotion)
                {
                    // Update position of each assigned weapon
                    foreach (var kvp in assignedWeapons)
                    {
                        DirectionTrigger.WeaponDirection direction = kvp.Key;
                        Item weapon = kvp.Value;

                        if (weapon != null && directionOrbs.ContainsKey(direction))
                        {
                            UpdateWeaponOrbitalPosition(weapon, direction);
                        }
                    }
                }

                yield return updateInterval;
            }
        }

        private void UpdateWeaponOrbitalPosition(Item weapon, DirectionTrigger.WeaponDirection direction)
        {
            if (!weaponOrbitAngles.ContainsKey(weapon) || !directionOrbs.ContainsKey(direction))
                return;

            try
            {
                // Get orb position
                GameObject orb = directionOrbs[direction];
                Vector3 orbCenter = orb.transform.position;

                // Update orbit angle using config speed
                float currentAngle = weaponOrbitAngles[weapon];
                currentAngle += SkillOrbConfig.OrbitSpeed * Time.deltaTime;
                weaponOrbitAngles[weapon] = currentAngle % 360f;

                // Calculate orbital position using config radius
                float radians = currentAngle * Mathf.Deg2Rad;
                float radius = SkillOrbConfig.OrbitRadius;
                Vector3 orbitOffset = new Vector3(
                    Mathf.Cos(radians) * radius,
                    Mathf.Sin(radians) * radius * 0.5f, // Flatter orbit
                    Mathf.Sin(radians * 2f) * radius * 0.3f // Figure-8 motion
                );

                // Add small random offset for variety
                Vector3 randomOffset = weaponOrbitOffsets[weapon];
                Vector3 targetPosition = orbCenter + orbitOffset + randomOffset;

                // Smoothly move weapon to target position
                weapon.transform.position = Vector3.Lerp(weapon.transform.position, targetPosition, Time.deltaTime * 5f);

                // Add slight rotation for visual appeal
                weapon.transform.Rotate(Vector3.up, SkillOrbConfig.OrbitSpeed * 0.5f * Time.deltaTime);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[{currentDateTime}] {currentUser} - Error updating orbital position for {weapon.itemId}: {e.Message}");
            }
        }

        public override void OnItemDespawn(EventTime eventTime)
        {
            SkillOrbConfig.DebugLog($"[{currentDateTime}] {currentUser} - SkillOrbModule despawning");

            // Stop orbital motion
            if (orbitalMotionCoroutine != null)
            {
                item.StopCoroutine(orbitalMotionCoroutine);
                orbitalMotionCoroutine = null;
            }

            // Restore all weapon physics
            foreach (var weapon in weaponToDirection.Keys)
            {
                RestoreWeaponPhysics(weapon);
            }

            // Clear collections
            assignedWeapons?.Clear();
            weaponToDirection?.Clear();
            weaponOrbitAngles?.Clear();
            weaponOrbitOffsets?.Clear();

            // Destroy orb visuals
            foreach (var visual in orbVisuals)
            {
                if (visual != null)
                {
                    Object.Destroy(visual);
                }
            }
            orbVisuals?.Clear();

            // Destroy direction orbs
            foreach (var orb in directionOrbs.Values)
            {
                if (orb != null)
                {
                    Object.Destroy(orb);
                }
            }
            directionOrbs?.Clear();

            base.OnItemDespawn(eventTime);
        }
    }
}