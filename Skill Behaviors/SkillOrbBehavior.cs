using ThunderRoad;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace ShadowArmory
{
    public class SkillOrbModule : MonoBehaviour
    {
        private readonly string currentUser = "Suoper";
        private readonly string currentDateTime = "2025-01-01 00:00:00";

        [System.Serializable]
        public enum OrbDirection
        {
            Up,
            Down,
            Left,
            Right,
            Forward,
            Backward
        }

        [System.Serializable]
        private class WeaponAssignment
        {
            public Item weapon;
            public OrbDirection direction;
            public Transform orb;
            public GameObject weaponHolder;
            public bool isAssigned;
            
            public WeaponAssignment(Item weapon, OrbDirection direction, Transform orb)
            {
                this.weapon = weapon;
                this.direction = direction;
                this.orb = orb;
                this.isAssigned = true;
            }
        }

        private Creature creature;
        private Dictionary<OrbDirection, Transform> orbs = new Dictionary<OrbDirection, Transform>();
        private List<WeaponAssignment> assignedWeapons = new List<WeaponAssignment>();
        private Dictionary<OrbDirection, Collider> orbTriggers = new Dictionary<OrbDirection, Collider>();
        
        // Orbital motion settings - now configurable
        private float orbitSpeed => SkillOrbConfig.OrbitSpeed;
        private float weaponDistanceFromOrb => SkillOrbConfig.WeaponDistanceFromOrb;
        private float orbDistanceFromPlayer => SkillOrbConfig.OrbDistanceFromPlayer;
        private float orbHeightOffset => SkillOrbConfig.OrbHeightOffset;

        // Active coroutines
        private Coroutine spinCoroutine = null;

        public void Initialize(Creature owner)
        {
            creature = owner;
            
            if (creature == null)
            {
                Debug.LogError($"[{currentDateTime}] {currentUser} - Cannot initialize SkillOrbModule: creature is null!");
                return;
            }

            Debug.Log($"[{currentDateTime}] {currentUser} - Initializing SkillOrbModule for {creature.name}");
            
            // Run validation checks
            if (SkillOrbConfig.EnableDebugLogs)
            {
                SkillOrbValidation.ValidateConfiguration();
                SkillOrbValidation.ValidateOrbDirectionsEnum();
            }
            
            SetupOrbs();
            SetupTriggers();
            StartSpinning();
        }

        private void SetupOrbs()
        {
            // Create orbs positioned around the player at shoulder height
            Vector3 playerPosition = creature.transform.position;
            Vector3 shoulderPosition = playerPosition + Vector3.up * orbHeightOffset;

            // Create orbs in a circle around the player at shoulder level
            CreateOrb(OrbDirection.Up, shoulderPosition + Vector3.up * 1f);
            CreateOrb(OrbDirection.Down, shoulderPosition + Vector3.down * 0.5f);
            CreateOrb(OrbDirection.Left, shoulderPosition + creature.transform.TransformDirection(-orbDistanceFromPlayer, 0, 0));
            CreateOrb(OrbDirection.Right, shoulderPosition + creature.transform.TransformDirection(orbDistanceFromPlayer, 0, 0));
            CreateOrb(OrbDirection.Forward, shoulderPosition + creature.transform.TransformDirection(0, 0, orbDistanceFromPlayer));
            CreateOrb(OrbDirection.Backward, shoulderPosition + creature.transform.TransformDirection(0, 0, -orbDistanceFromPlayer));

            Debug.Log($"[{currentDateTime}] {currentUser} - Created {orbs.Count} orbs around player");
        }

        private void CreateOrb(OrbDirection direction, Vector3 position)
        {
            GameObject orbObj = new GameObject($"SkillOrb_{direction}");
            orbObj.transform.position = position;
            orbObj.transform.SetParent(creature.transform);

            // Add visual representation (sphere)
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.SetParent(orbObj.transform);
            sphere.transform.localPosition = Vector3.zero;
            sphere.transform.localScale = Vector3.one * SkillOrbConfig.OrbScale;

            // Make orb slightly transparent
            Renderer renderer = sphere.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material material = new Material(Shader.Find("Standard"));
                material.color = SkillOrbConfig.OrbColor;
                material.SetFloat("_Mode", 3); // Transparent mode
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.DisableKeyword("_ALPHATEST_ON");
                material.EnableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = 3000;
                renderer.material = material;
            }

            orbs[direction] = orbObj.transform;
        }

        private void SetupTriggers()
        {
            foreach (var orbPair in orbs)
            {
                OrbDirection direction = orbPair.Key;
                Transform orb = orbPair.Value;

                // Add trigger collider to orb
                SphereCollider triggerCollider = orb.gameObject.AddComponent<SphereCollider>();
                triggerCollider.isTrigger = true;
                triggerCollider.radius = SkillOrbConfig.TriggerRadius;

                // Add OrbTriggerHandler component
                OrbTriggerHandler triggerHandler = orb.gameObject.AddComponent<OrbTriggerHandler>();
                triggerHandler.Initialize(this, direction);

                orbTriggers[direction] = triggerCollider;

                if (SkillOrbConfig.EnableDebugLogs)
                {
                    Debug.Log($"[{currentDateTime}] {currentUser} - Setup trigger for {direction} orb with radius {SkillOrbConfig.TriggerRadius}");
                }
            }
        }

        public void OnWeaponTouchOrb(Item weapon, OrbDirection direction)
        {
            if (weapon == null || !orbs.ContainsKey(direction))
            {
                Debug.LogWarning($"[{currentDateTime}] {currentUser} - Invalid weapon or direction for orb assignment");
                return;
            }

            // Check if weapon is already assigned
            if (IsWeaponAssigned(weapon))
            {
                Debug.Log($"[{currentDateTime}] {currentUser} - Weapon {weapon.itemId} is already assigned");
                return;
            }

            // Check if this orb already has a weapon assigned
            if (GetAssignedWeapon(direction) != null)
            {
                Debug.Log($"[{currentDateTime}] {currentUser} - Orb {direction} already has a weapon assigned");
                return;
            }

            AssignWeaponToOrb(weapon, direction);
        }

        private void AssignWeaponToOrb(Item weapon, OrbDirection direction)
        {
            Transform orb = orbs[direction];
            
            Debug.Log($"[{currentDateTime}] {currentUser} - Assigning weapon {weapon.itemId} to {direction} orb");

            // Create weapon holder for smooth positioning
            GameObject weaponHolder = new GameObject($"WeaponHolder_{direction}");
            weaponHolder.transform.SetParent(orb);
            weaponHolder.transform.localPosition = Vector3.forward * weaponDistanceFromOrb;
            weaponHolder.transform.localRotation = Quaternion.identity;

            // Make weapon kinematic and handle all rigidbodies
            ForceWeaponKinematic(weapon);

            // Create assignment
            WeaponAssignment assignment = new WeaponAssignment(weapon, direction, orb);
            assignment.weaponHolder = weaponHolder;
            assignedWeapons.Add(assignment);

            Debug.Log($"[{currentDateTime}] {currentUser} - Weapon {weapon.itemId} successfully assigned to {direction}");
        }

        private void ForceWeaponKinematic(Item weapon)
        {
            if (weapon == null) return;

            try
            {
                // Make sure the weapon's physics body is kinematic and has no velocity
                if (weapon.physicBody != null)
                {
                    weapon.physicBody.isKinematic = true;
                    weapon.physicBody.useGravity = false;
                    weapon.physicBody.velocity = Vector3.zero;
                    weapon.physicBody.angularVelocity = Vector3.zero;
                    weapon.physicBody.WakeUp();
                }

                // Also handle any child rigidbodies to be extra safe
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
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[{currentDateTime}] {currentUser} - Error forcing weapon kinematic: {e.Message}");
            }
        }

        private void StartSpinning()
        {
            if (spinCoroutine != null)
            {
                StopCoroutine(spinCoroutine);
            }
            spinCoroutine = StartCoroutine(SpinAssignedWeapons());
            
            // Start orb position updating if enabled
            if (SkillOrbConfig.EnableDynamicOrbPositioning)
            {
                StartCoroutine(UpdateOrbPositions());
            }
        }

        private IEnumerator UpdateOrbPositions()
        {
            if (SkillOrbConfig.EnableDebugLogs)
            {
                Debug.Log($"[{currentDateTime}] {currentUser} - Started UpdateOrbPositions coroutine");
            }

            while (SkillOrbConfig.EnableDynamicOrbPositioning)
            {
                if (creature != null)
                {
                    Vector3 playerPosition = creature.transform.position;
                    Vector3 shoulderPosition = playerPosition + Vector3.up * orbHeightOffset;

                    // Update orb positions to follow player
                    if (orbs.ContainsKey(OrbDirection.Up))
                        orbs[OrbDirection.Up].position = shoulderPosition + Vector3.up * 1f;
                    if (orbs.ContainsKey(OrbDirection.Down))
                        orbs[OrbDirection.Down].position = shoulderPosition + Vector3.down * 0.5f;
                    if (orbs.ContainsKey(OrbDirection.Left))
                        orbs[OrbDirection.Left].position = shoulderPosition + creature.transform.TransformDirection(-orbDistanceFromPlayer, 0, 0);
                    if (orbs.ContainsKey(OrbDirection.Right))
                        orbs[OrbDirection.Right].position = shoulderPosition + creature.transform.TransformDirection(orbDistanceFromPlayer, 0, 0);
                    if (orbs.ContainsKey(OrbDirection.Forward))
                        orbs[OrbDirection.Forward].position = shoulderPosition + creature.transform.TransformDirection(0, 0, orbDistanceFromPlayer);
                    if (orbs.ContainsKey(OrbDirection.Backward))
                        orbs[OrbDirection.Backward].position = shoulderPosition + creature.transform.TransformDirection(0, 0, -orbDistanceFromPlayer);
                }

                yield return new WaitForSeconds(SkillOrbConfig.OrbUpdateInterval);
            }
        }

        private IEnumerator SpinAssignedWeapons()
        {
            Debug.Log($"[{currentDateTime}] {currentUser} - Started SpinAssignedWeapons coroutine");

            while (true)
            {
                float deltaTime = Time.deltaTime;

                for (int i = assignedWeapons.Count - 1; i >= 0; i--)
                {
                    WeaponAssignment assignment = assignedWeapons[i];
                    
                    // Clean up invalid assignments
                    if (assignment.weapon == null || assignment.orb == null || assignment.weaponHolder == null)
                    {
                        CleanupAssignment(assignment, i);
                        continue;
                    }

                    // Check if weapon was grabbed by player - release it
                    if (SkillOrbConfig.EnableAutoWeaponRelease && assignment.weapon.IsHanded())
                    {
                        if (SkillOrbConfig.EnableDebugLogs)
                        {
                            Debug.Log($"[{currentDateTime}] {currentUser} - Weapon {assignment.weapon.itemId} was grabbed, releasing from orb");
                        }
                        ReleaseWeaponFromOrb(assignment, i);
                        continue;
                    }

                    // Calculate orbital position
                    float currentTime = Time.time;
                    float angle = (currentTime * orbitSpeed) % 360f;
                    float angleRad = angle * Mathf.Deg2Rad;

                    // Calculate position relative to orb
                    Vector3 offset = new Vector3(
                        Mathf.Cos(angleRad) * weaponDistanceFromOrb,
                        0f,
                        Mathf.Sin(angleRad) * weaponDistanceFromOrb
                    );

                    // Update weapon holder position and rotation
                    assignment.weaponHolder.transform.localPosition = offset;
                    assignment.weaponHolder.transform.localRotation = Quaternion.LookRotation(offset.normalized);

                    // Update weapon position to match holder
                    try
                    {
                        assignment.weapon.transform.position = assignment.weaponHolder.transform.position;
                        assignment.weapon.transform.rotation = assignment.weaponHolder.transform.rotation;

                        // Ensure weapon stays kinematic
                        if (assignment.weapon.physicBody != null)
                        {
                            assignment.weapon.physicBody.WakeUp();
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"[{currentDateTime}] {currentUser} - Error updating weapon position: {e.Message}");
                    }
                }

                yield return new WaitForFixedUpdate();
            }
        }

        private void CleanupAssignment(WeaponAssignment assignment, int index)
        {
            if (assignment.weaponHolder != null)
            {
                Destroy(assignment.weaponHolder);
            }
            assignedWeapons.RemoveAt(index);
            Debug.Log($"[{currentDateTime}] {currentUser} - Cleaned up invalid weapon assignment at index {index}");
        }

        private void ReleaseWeaponFromOrb(WeaponAssignment assignment, int index)
        {
            // Restore weapon physics
            if (assignment.weapon != null && assignment.weapon.physicBody != null)
            {
                assignment.weapon.physicBody.isKinematic = false;
                assignment.weapon.physicBody.useGravity = true;

                // Also restore child rigidbodies
                Rigidbody[] childRigidbodies = assignment.weapon.GetComponentsInChildren<Rigidbody>();
                foreach (Rigidbody rb in childRigidbodies)
                {
                    if (rb != null && rb != assignment.weapon.physicBody)
                    {
                        rb.isKinematic = false;
                        rb.useGravity = true;
                    }
                }
            }

            // Clean up holder
            if (assignment.weaponHolder != null)
            {
                Destroy(assignment.weaponHolder);
            }

            assignedWeapons.RemoveAt(index);
            Debug.Log($"[{currentDateTime}] {currentUser} - Released weapon {assignment.weapon?.itemId} from {assignment.direction} orb");
        }

        private bool IsWeaponAssigned(Item weapon)
        {
            foreach (var assignment in assignedWeapons)
            {
                if (assignment.weapon == weapon)
                    return true;
            }
            return false;
        }

        private WeaponAssignment GetAssignedWeapon(OrbDirection direction)
        {
            foreach (var assignment in assignedWeapons)
            {
                if (assignment.direction == direction)
                    return assignment;
            }
            return null;
        }

        private void OnDestroy()
        {
            Debug.Log($"[{currentDateTime}] {currentUser} - SkillOrbModule being destroyed");

            // Stop all coroutines
            if (spinCoroutine != null)
            {
                StopCoroutine(spinCoroutine);
                spinCoroutine = null;
            }
            StopAllCoroutines();

            // Clean up assigned weapons - restore their physics
            foreach (var assignment in assignedWeapons)
            {
                if (assignment.weapon != null && assignment.weapon.physicBody != null)
                {
                    assignment.weapon.physicBody.isKinematic = false;
                    assignment.weapon.physicBody.useGravity = true;

                    // Also restore child rigidbodies
                    Rigidbody[] childRigidbodies = assignment.weapon.GetComponentsInChildren<Rigidbody>();
                    foreach (Rigidbody rb in childRigidbodies)
                    {
                        if (rb != null && rb != assignment.weapon.physicBody)
                        {
                            rb.isKinematic = false;
                            rb.useGravity = true;
                        }
                    }
                }

                if (assignment.weaponHolder != null)
                {
                    Destroy(assignment.weaponHolder);
                }
            }

            assignedWeapons.Clear();

            // Clean up orbs
            foreach (var orb in orbs.Values)
            {
                if (orb != null)
                {
                    Destroy(orb.gameObject);
                }
            }

            orbs.Clear();
            orbTriggers.Clear();

            Debug.Log($"[{currentDateTime}] {currentUser} - SkillOrbModule destroyed");
        }
    }

    // Helper class for handling trigger events
    public class OrbTriggerHandler : MonoBehaviour
    {
        private SkillOrbModule orbModule;
        private SkillOrbModule.OrbDirection direction;

        public void Initialize(SkillOrbModule module, SkillOrbModule.OrbDirection dir)
        {
            orbModule = module;
            direction = dir;
        }

        private void OnTriggerEnter(Collider other)
        {
            Item weapon = other.GetComponentInParent<Item>();
            if (weapon != null && orbModule != null)
            {
                if (SkillOrbConfig.EnableDebugLogs)
                {
                    Debug.Log($"Weapon {weapon.itemId} touched {direction} orb");
                }
                orbModule.OnWeaponTouchOrb(weapon, direction);
            }
        }
    }
}