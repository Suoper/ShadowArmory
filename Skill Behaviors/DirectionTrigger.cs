using ThunderRoad;
using UnityEngine;

namespace ShadowArmory
{
    /// <summary>
    /// Component that detects weapon collisions with direction orbs
    /// Handles the OnTriggerEnter event for weapon assignment
    /// </summary>
    public class DirectionTrigger : MonoBehaviour
    {
        private readonly string currentUser = "Suoper";
        private readonly string currentDateTime = "2025-06-29 03:30:38";

        [Header("Direction Configuration")]
        public WeaponDirection direction;
        public float orbRadius = 0.15f;

        private SkillOrbModule parentModule;
        private SphereCollider triggerCollider;

        public void Initialize(SkillOrbModule module, WeaponDirection dir)
        {
            parentModule = module;
            direction = dir;

            // Setup the trigger collider
            SetupTriggerCollider();

            SkillOrbConfig.DebugLog($"[{currentDateTime}] {currentUser} - DirectionTrigger initialized for direction: {direction}");
        }

        private void SetupTriggerCollider()
        {
            // Add sphere collider if not present
            triggerCollider = GetComponent<SphereCollider>();
            if (triggerCollider == null)
            {
                triggerCollider = gameObject.AddComponent<SphereCollider>();
            }

            // Configure as trigger
            triggerCollider.isTrigger = true;
            triggerCollider.radius = SkillOrbConfig.OrbRadius;

            SkillOrbConfig.DebugLog($"[{currentDateTime}] {currentUser} - Trigger collider setup complete for {direction} orb - radius: {SkillOrbConfig.OrbRadius}");
        }

        private void OnTriggerEnter(Collider other)
        {
            SkillOrbConfig.DebugLog($"[{currentDateTime}] {currentUser} - OnTriggerEnter called! Direction: {direction}, Collider: {other.name}");

            // Check if the colliding object is a weapon
            Item item = other.GetComponentInParent<Item>();
            if (item == null)
            {
                SkillOrbConfig.DebugLog($"[{currentDateTime}] {currentUser} - No Item component found on {other.name}");
                return;
            }

            // Check if it's actually a weapon
            if (!IsWeapon(item))
            {
                SkillOrbConfig.DebugLog($"[{currentDateTime}] {currentUser} - Item {item.itemId} is not a weapon, ignoring");
                return;
            }

            // Check if weapon is currently held (shouldn't assign held weapons)
            if (item.IsHanded())
            {
                SkillOrbConfig.DebugLog($"[{currentDateTime}] {currentUser} - Weapon {item.itemId} is currently held, ignoring");
                return;
            }

            SkillOrbConfig.DebugLog($"[{currentDateTime}] {currentUser} - WEAPON COLLISION DETECTED! Weapon: {item.itemId}, Direction: {direction}");

            // Provide haptic feedback if enabled
            if (SkillOrbConfig.EnableCollisionFeedback)
            {
                // Try to find the player's hands for haptic feedback
                if (Player.local?.hands != null)
                {
                    foreach (RagdollHand hand in Player.local.hands)
                    {
                        hand?.HapticTick(0.5f);
                    }
                }
            }

            // Notify parent module about the weapon collision
            if (parentModule != null)
            {
                parentModule.OnWeaponCollision(item, direction);
            }
            else
            {
                Debug.LogError($"[{currentDateTime}] {currentUser} - Parent SkillOrbModule is null!");
            }
        }

        private bool IsWeapon(Item item)
        {
            // Check if item data indicates it's a weapon
            if (item.data != null && item.data.type == ItemData.Type.Weapon)
            {
                return true;
            }

            // Additional check for items that might be weapon-like
            if (item.itemId != null)
            {
                string itemId = item.itemId.ToLower();
                string[] weaponKeywords = { "sword", "dagger", "axe", "spear", "bow", "mace", "hammer", "staff", "blade", "knife" };
                
                foreach (string keyword in weaponKeywords)
                {
                    if (itemId.Contains(keyword))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private void OnDestroy()
        {
            SkillOrbConfig.DebugLog($"[{currentDateTime}] {currentUser} - DirectionTrigger for {direction} being destroyed");
        }

        // Enum definition (should match the one used throughout the codebase)
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
    }
}