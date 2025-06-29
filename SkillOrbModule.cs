using ThunderRoad;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace ShadowArmory
{
    /// <summary>
    /// Skill Orb Module - Handles weapon spinning/orbiting around directional orbs
    /// Implements weapon assignment through trigger system and smooth orbital motion
    /// </summary>
    public class SkillOrbModule : ItemModule
    {
        #region Orb Directions and Configuration
        public enum OrbDirection
        {
            None,
            Up,
            Down,
            Left,
            Right,
            Forward,
            Backward
        }

        [Serializable]
        public class OrbitSettings
        {
            public float orbitalRadius = 1.0f;
            public float orbitalSpeed = 45f; // degrees per second
            public float orbitalHeight = 0.5f;
            public bool enableShaking = true;
            public float shakingIntensity = 0.1f;
            public float shakingFrequency = 2.0f;
        }
        #endregion

        #region Private Variables
        // Orb and weapon tracking
        private Dictionary<OrbDirection, GameObject> directionOrbs = new Dictionary<OrbDirection, GameObject>();
        private Dictionary<OrbDirection, List<Item>> assignedWeapons = new Dictionary<OrbDirection, List<Item>>();
        private Dictionary<Item, OrbDirection> weaponToOrbMapping = new Dictionary<Item, OrbDirection>();
        
        // Orbital motion tracking
        private Dictionary<Item, float> weaponOrbitalAngles = new Dictionary<Item, float>();
        private Dictionary<Item, Vector3> weaponHolderPositions = new Dictionary<Item, Vector3>();
        
        // Coroutines
        private Coroutine orbitalMotionCoroutine = null;
        private Coroutine shakinCoroutine = null;
        
        // Configuration
        [SerializeField] private OrbitSettings orbitSettings = new OrbitSettings();
        
        // System state
        private bool isActive = false;
        private bool isDestroying = false;
        
        // Debug logging
        private readonly string currentUser = "Suoper";
        private readonly string currentDateTime = "2025-01-16 12:00:00";
        
        // Performance optimization
        private WaitForFixedUpdate fixedUpdateWait = new WaitForFixedUpdate();
        #endregion

        #region Unity Lifecycle
        public override void OnItemLoaded(Item item)
        {
            base.OnItemLoaded(item);
            
            Debug.Log($"[{currentDateTime}] {currentUser} - SkillOrbModule loaded for item: {item.itemId}");
            
            // Initialize orb directions
            InitializeOrbs();
            
            // Initialize weapon tracking dictionaries
            InitializeWeaponTracking();
            
            // Subscribe to item events
            item.OnCollisionEvent += OnItemCollision;
            item.OnDespawnEvent += OnItemDespawn;
            
            // Start the system
            ActivateOrbSystem();
        }

        private void OnItemDespawn(EventTime eventTime)
        {
            if (eventTime == EventTime.OnStart)
            {
                Debug.Log($"[{currentDateTime}] {currentUser} - SkillOrbModule despawning, cleaning up...");
                DeactivateOrbSystem();
                isDestroying = true;
            }
        }

        protected void OnDestroy()
        {
            Debug.Log($"[{currentDateTime}] {currentUser} - SkillOrbModule OnDestroy called");
            DeactivateOrbSystem();
        }
        #endregion

        #region Orb System Management
        private void InitializeOrbs()
        {
            // Create virtual orbs for each direction around the main item
            Vector3 basePosition = item.transform.position;
            float orbDistance = 2.0f;
            
            CreateOrb(OrbDirection.Up, basePosition + Vector3.up * orbDistance);
            CreateOrb(OrbDirection.Down, basePosition + Vector3.down * orbDistance);
            CreateOrb(OrbDirection.Left, basePosition + Vector3.left * orbDistance);
            CreateOrb(OrbDirection.Right, basePosition + Vector3.right * orbDistance);
            CreateOrb(OrbDirection.Forward, basePosition + Vector3.forward * orbDistance);
            CreateOrb(OrbDirection.Backward, basePosition + Vector3.back * orbDistance);
            
            Debug.Log($"[{currentDateTime}] {currentUser} - Initialized {directionOrbs.Count} directional orbs");
        }

        private void CreateOrb(OrbDirection direction, Vector3 position)
        {
            // Create a simple sphere to represent the orb
            GameObject orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            orb.name = $"SkillOrb_{direction}";
            orb.transform.position = position;
            orb.transform.localScale = Vector3.one * 0.3f;
            
            // Make it kinematic and add trigger
            Rigidbody orbRb = orb.GetComponent<Rigidbody>();
            if (orbRb != null)
            {
                orbRb.isKinematic = true;
                orbRb.useGravity = false;
            }
            
            Collider orbCollider = orb.GetComponent<Collider>();
            if (orbCollider != null)
            {
                orbCollider.isTrigger = true;
            }
            
            // Add orb identifier component
            OrbIdentifier identifier = orb.AddComponent<OrbIdentifier>();
            identifier.direction = direction;
            identifier.orbModule = this;
            
            directionOrbs[direction] = orb;
            
            Debug.Log($"[{currentDateTime}] {currentUser} - Created orb for direction {direction} at {position}");
        }

        private void InitializeWeaponTracking()
        {
            // Initialize dictionaries for each orb direction
            foreach (OrbDirection direction in Enum.GetValues(typeof(OrbDirection)))
            {
                if (direction != OrbDirection.None)
                {
                    assignedWeapons[direction] = new List<Item>();
                }
            }
        }

        public void ActivateOrbSystem()
        {
            if (isActive) return;
            
            isActive = true;
            Debug.Log($"[{currentDateTime}] {currentUser} - Activating orb system");
            
            // Start orbital motion coroutine
            if (orbitalMotionCoroutine == null)
            {
                orbitalMotionCoroutine = item.StartCoroutine(SpinAssignedWeapons());
            }
            
            // Start shaking coroutine if enabled
            if (orbitSettings.enableShaking && shakinCoroutine == null)
            {
                shakinCoroutine = item.StartCoroutine(ShakinCoroutine());
            }
        }

        public void DeactivateOrbSystem()
        {
            if (!isActive) return;
            
            isActive = false;
            Debug.Log($"[{currentDateTime}] {currentUser} - Deactivating orb system");
            
            // Stop coroutines
            if (orbitalMotionCoroutine != null)
            {
                item.StopCoroutine(orbitalMotionCoroutine);
                orbitalMotionCoroutine = null;
            }
            
            if (shakinCoroutine != null)
            {
                item.StopCoroutine(shakinCoroutine);
                shakinCoroutine = null;
            }
            
            // Release all weapons
            ReleaseAllWeapons();
            
            // Destroy orbs
            foreach (var orb in directionOrbs.Values)
            {
                if (orb != null)
                {
                    GameObject.Destroy(orb);
                }
            }
            directionOrbs.Clear();
        }
        #endregion

        #region Weapon Assignment and Orbital Motion
        /// <summary>
        /// Main coroutine that handles spinning/orbiting of assigned weapons around their orbs
        /// This fixes the positioning issues mentioned in the problem statement
        /// </summary>
        private IEnumerator SpinAssignedWeapons()
        {
            Debug.Log($"[{currentDateTime}] {currentUser} - Starting SpinAssignedWeapons coroutine");
            
            while (isActive && !isDestroying)
            {
                // Process each direction's assigned weapons
                foreach (var kvp in assignedWeapons)
                {
                    OrbDirection direction = kvp.Key;
                    List<Item> weapons = kvp.Value;
                    
                    if (weapons.Count == 0 || !directionOrbs.ContainsKey(direction))
                        continue;
                    
                    GameObject orb = directionOrbs[direction];
                    if (orb == null) continue;
                    
                    Vector3 orbCenter = orb.transform.position;
                    
                    // Process each weapon assigned to this orb
                    for (int i = 0; i < weapons.Count; i++)
                    {
                        Item weapon = weapons[i];
                        if (weapon == null || !weapon.gameObject.activeInHierarchy)
                        {
                            // Remove invalid weapons
                            weapons.RemoveAt(i);
                            i--;
                            continue;
                        }
                        
                        // Update orbital position for this weapon
                        UpdateWeaponOrbitalPosition(weapon, orbCenter, direction, i, weapons.Count);
                    }
                }
                
                yield return fixedUpdateWait;
            }
            
            Debug.Log($"[{currentDateTime}] {currentUser} - SpinAssignedWeapons coroutine ended");
        }

        /// <summary>
        /// Updates a weapon's position to orbit around its assigned orb
        /// This creates the smooth orbital motion mentioned in the problem statement
        /// </summary>
        private void UpdateWeaponOrbitalPosition(Item weapon, Vector3 orbCenter, OrbDirection direction, int weaponIndex, int totalWeapons)
        {
            if (weapon == null) return;
            
            try
            {
                // Ensure weapon is kinematic for smooth positioning
                SetWeaponKinematic(weapon);
                
                // Calculate orbital parameters
                float baseAngle = (360f / totalWeapons) * weaponIndex; // Distribute weapons evenly
                float timeAngle = Time.time * orbitSettings.orbitalSpeed;
                float totalAngle = baseAngle + timeAngle;
                
                // Store current angle for this weapon
                weaponOrbitalAngles[weapon] = totalAngle;
                
                // Calculate orbital position
                Vector3 orbitalOffset = new Vector3(
                    Mathf.Cos(totalAngle * Mathf.Deg2Rad) * orbitSettings.orbitalRadius,
                    orbitSettings.orbitalHeight,
                    Mathf.Sin(totalAngle * Mathf.Deg2Rad) * orbitSettings.orbitalRadius
                );
                
                Vector3 targetPosition = orbCenter + orbitalOffset;
                
                // Store holder position for reference
                weaponHolderPositions[weapon] = targetPosition;
                
                // Smoothly move weapon to target position
                weapon.transform.position = Vector3.Lerp(
                    weapon.transform.position,
                    targetPosition,
                    Time.fixedDeltaTime * 5f
                );
                
                // Make weapon face the orb center
                Vector3 lookDirection = (orbCenter - weapon.transform.position).normalized;
                if (lookDirection != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                    weapon.transform.rotation = Quaternion.Slerp(
                        weapon.transform.rotation,
                        targetRotation,
                        Time.fixedDeltaTime * 3f
                    );
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[{currentDateTime}] {currentUser} - Error updating weapon orbital position: {e.Message}");
            }
        }

        /// <summary>
        /// The shakin coroutine that was defined but never called (as mentioned in the problem statement)
        /// This adds subtle shaking to the orbital motion for more dynamic movement
        /// </summary>
        private IEnumerator ShakinCoroutine()
        {
            Debug.Log($"[{currentDateTime}] {currentUser} - Starting shakin coroutine");
            
            while (isActive && !isDestroying && orbitSettings.enableShaking)
            {
                // Apply shaking to all assigned weapons
                foreach (var weaponList in assignedWeapons.Values)
                {
                    foreach (Item weapon in weaponList)
                    {
                        if (weapon == null || !weapon.gameObject.activeInHierarchy)
                            continue;
                        
                        // Add subtle shaking to the weapon's position
                        Vector3 shakeOffset = new Vector3(
                            UnityEngine.Random.Range(-orbitSettings.shakingIntensity, orbitSettings.shakingIntensity),
                            UnityEngine.Random.Range(-orbitSettings.shakingIntensity, orbitSettings.shakingIntensity),
                            UnityEngine.Random.Range(-orbitSettings.shakingIntensity, orbitSettings.shakingIntensity)
                        );
                        
                        // Apply shake to weapon position
                        if (weaponHolderPositions.ContainsKey(weapon))
                        {
                            Vector3 basePosition = weaponHolderPositions[weapon];
                            weapon.transform.position = basePosition + shakeOffset;
                        }
                    }
                }
                
                // Wait for next shake update
                yield return new WaitForSeconds(1f / orbitSettings.shakingFrequency);
            }
            
            Debug.Log($"[{currentDateTime}] {currentUser} - Shakin coroutine ended");
        }

        private void SetWeaponKinematic(Item weapon)
        {
            if (weapon.physicBody != null)
            {
                weapon.physicBody.isKinematic = true;
                weapon.physicBody.useGravity = false;
                weapon.physicBody.velocity = Vector3.zero;
                weapon.physicBody.angularVelocity = Vector3.zero;
            }
        }
        #endregion

        #region Trigger System Integration
        private void OnItemCollision(ref CollisionInstance collisionInstance)
        {
            // Check if a weapon touched an orb through the trigger system
            if (collisionInstance.sourceCollider.isTrigger || collisionInstance.targetCollider.isTrigger)
            {
                HandleWeaponOrbTrigger(collisionInstance);
            }
        }

        private void HandleWeaponOrbTrigger(CollisionInstance collision)
        {
            // Determine which object is the weapon and which is the orb
            Item weapon = null;
            OrbIdentifier orb = null;
            
            // Check source
            weapon = collision.sourceCollider.GetComponentInParent<Item>();
            orb = collision.targetCollider.GetComponent<OrbIdentifier>();
            
            // Check target if not found
            if (weapon == null || orb == null)
            {
                weapon = collision.targetCollider.GetComponentInParent<Item>();
                orb = collision.sourceCollider.GetComponent<OrbIdentifier>();
            }
            
            if (weapon != null && orb != null && weapon != item)
            {
                AssignWeaponToOrb(weapon, orb.direction);
            }
        }

        public void AssignWeaponToOrb(Item weapon, OrbDirection direction)
        {
            if (weapon == null || direction == OrbDirection.None) return;
            
            Debug.Log($"[{currentDateTime}] {currentUser} - Assigning weapon {weapon.itemId} to orb direction {direction}");
            
            // Remove weapon from any previous assignments
            RemoveWeaponFromAllOrbs(weapon);
            
            // Add to new direction
            if (assignedWeapons.ContainsKey(direction))
            {
                assignedWeapons[direction].Add(weapon);
                weaponToOrbMapping[weapon] = direction;
                
                // Initialize orbital angle for this weapon
                weaponOrbitalAngles[weapon] = UnityEngine.Random.Range(0f, 360f);
                
                Debug.Log($"[{currentDateTime}] {currentUser} - Successfully assigned weapon to {direction}. Total weapons for this orb: {assignedWeapons[direction].Count}");
            }
        }

        private void RemoveWeaponFromAllOrbs(Item weapon)
        {
            foreach (var weaponList in assignedWeapons.Values)
            {
                weaponList.Remove(weapon);
            }
            
            weaponToOrbMapping.Remove(weapon);
            weaponOrbitalAngles.Remove(weapon);
            weaponHolderPositions.Remove(weapon);
        }

        private void ReleaseAllWeapons()
        {
            foreach (var weaponList in assignedWeapons.Values)
            {
                foreach (Item weapon in weaponList)
                {
                    if (weapon != null && weapon.physicBody != null)
                    {
                        weapon.physicBody.isKinematic = false;
                        weapon.physicBody.useGravity = true;
                    }
                }
                weaponList.Clear();
            }
            
            weaponToOrbMapping.Clear();
            weaponOrbitalAngles.Clear();
            weaponHolderPositions.Clear();
        }
        #endregion

        #region Helper Components
        /// <summary>
        /// Component attached to orbs to identify their direction
        /// </summary>
        public class OrbIdentifier : MonoBehaviour
        {
            public OrbDirection direction;
            public SkillOrbModule orbModule;
        }
        #endregion
    }
}