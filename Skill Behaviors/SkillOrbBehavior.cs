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
        
        // Orbital motion settings
        private float orbitRadius = 1.5f;
        private float orbitSpeed = 60f; // degrees per second
        private float weaponDistanceFromOrb = 0.8f;

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
            
            SetupOrbs();
            SetupTriggers();
            StartSpinning();
        }

        private void SetupOrbs()
        {
            // Create orb GameObjects positioned around the player
            Vector3 playerPosition = creature.transform.position;
            Vector3 playerForward = creature.transform.forward;
            Vector3 playerRight = creature.transform.right;
            Vector3 playerUp = creature.transform.up;

            // Create orbs at fixed positions relative to player
            CreateOrb(OrbDirection.Up, playerPosition + playerUp * 2f);
            CreateOrb(OrbDirection.Down, playerPosition - playerUp * 1f);
            CreateOrb(OrbDirection.Left, playerPosition - playerRight * 2f);
            CreateOrb(OrbDirection.Right, playerPosition + playerRight * 2f);
            CreateOrb(OrbDirection.Forward, playerPosition + playerForward * 2f);
            CreateOrb(OrbDirection.Backward, playerPosition - playerForward * 2f);

            Debug.Log($"[{currentDateTime}] {currentUser} - Created {orbs.Count} orbs");
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
            sphere.transform.localScale = Vector3.one * 0.3f;

            // Make orb slightly transparent
            Renderer renderer = sphere.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material material = new Material(Shader.Find("Standard"));
                material.color = new Color(0.5f, 0.8f, 1f, 0.6f);
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
                triggerCollider.radius = 1.2f; // Larger than visual sphere for easier detection

                // Add OrbTriggerHandler component
                OrbTriggerHandler triggerHandler = orb.gameObject.AddComponent<OrbTriggerHandler>();
                triggerHandler.Initialize(this, direction);

                orbTriggers[direction] = triggerCollider;

                Debug.Log($"[{currentDateTime}] {currentUser} - Setup trigger for {direction} orb");
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

            // Make weapon kinematic and position it
            if (weapon.physicBody != null)
            {
                weapon.physicBody.isKinematic = true;
                weapon.physicBody.useGravity = false;
                weapon.physicBody.velocity = Vector3.zero;
                weapon.physicBody.angularVelocity = Vector3.zero;
            }

            // Create assignment
            WeaponAssignment assignment = new WeaponAssignment(weapon, direction, orb);
            assignment.weaponHolder = weaponHolder;
            assignedWeapons.Add(assignment);

            Debug.Log($"[{currentDateTime}] {currentUser} - Weapon {weapon.itemId} successfully assigned to {direction}");
        }

        private void StartSpinning()
        {
            if (spinCoroutine != null)
            {
                StopCoroutine(spinCoroutine);
            }
            spinCoroutine = StartCoroutine(SpinAssignedWeapons());
        }

        private IEnumerator SpinAssignedWeapons()
        {
            Debug.Log($"[{currentDateTime}] {currentUser} - Started SpinAssignedWeapons coroutine");

            while (true)
            {
                for (int i = assignedWeapons.Count - 1; i >= 0; i--)
                {
                    WeaponAssignment assignment = assignedWeapons[i];
                    
                    // Clean up invalid assignments
                    if (assignment.weapon == null || assignment.orb == null || assignment.weaponHolder == null)
                    {
                        if (assignment.weaponHolder != null)
                        {
                            Destroy(assignment.weaponHolder);
                        }
                        assignedWeapons.RemoveAt(i);
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

                    // Update weapon holder position
                    assignment.weaponHolder.transform.localPosition = offset;
                    assignment.weaponHolder.transform.localRotation = Quaternion.LookRotation(offset.normalized);

                    // Update weapon position to match holder
                    if (assignment.weapon.physicBody != null)
                    {
                        assignment.weapon.transform.position = assignment.weaponHolder.transform.position;
                        assignment.weapon.transform.rotation = assignment.weaponHolder.transform.rotation;
                    }
                }

                yield return new WaitForFixedUpdate();
            }
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

            if (spinCoroutine != null)
            {
                StopCoroutine(spinCoroutine);
                spinCoroutine = null;
            }

            // Clean up assigned weapons
            foreach (var assignment in assignedWeapons)
            {
                if (assignment.weapon != null && assignment.weapon.physicBody != null)
                {
                    assignment.weapon.physicBody.isKinematic = false;
                    assignment.weapon.physicBody.useGravity = true;
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
                Debug.Log($"Weapon {weapon.itemId} touched {direction} orb");
                orbModule.OnWeaponTouchOrb(weapon, direction);
            }
        }
    }
}