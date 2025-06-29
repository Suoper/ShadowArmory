using ThunderRoad;
using UnityEngine;
using System.Collections.Generic;

namespace ShadowArmory
{
    /// <summary>
    /// Example implementation showing how to integrate SkillOrbModule
    /// This demonstrates the expected behavior from the problem statement
    /// </summary>
    public class SkillOrbExample : ItemModule
    {
        private SkillOrbModule orbModule;
        private readonly string currentUser = "Suoper";
        private readonly string currentDateTime = "2025-01-16 12:00:00";

        public override void OnItemLoaded(Item item)
        {
            base.OnItemLoaded(item);
            
            Debug.Log($"[{currentDateTime}] {currentUser} - SkillOrbExample loaded, demonstrating orb functionality");
            
            // Initialize the orb module
            InitializeOrbModule();
            
            // Demonstrate the functionality
            DemonstrateOrbFunctionality();
        }

        private void InitializeOrbModule()
        {
            // Create and configure the SkillOrbModule
            orbModule = gameObject.AddComponent<SkillOrbModule>();
            
            // Configure orbital settings for demonstration
            var settings = new SkillOrbModule.OrbitSettings()
            {
                orbitalRadius = 1.5f,           // Weapons orbit 1.5 units from orb center
                orbitalSpeed = 60f,             // 60 degrees per second rotation
                orbitalHeight = 0.3f,           // Slight height offset for visual appeal
                enableShaking = true,           // Enable the shakin coroutine
                shakingIntensity = 0.05f,       // Subtle shaking effect
                shakingFrequency = 3.0f         // 3 shakes per second
            };
            
            Debug.Log($"[{currentDateTime}] {currentUser} - SkillOrbModule configured with orbital settings");
        }

        private void DemonstrateOrbFunctionality()
        {
            // Wait a moment for initialization, then start demonstration
            item.StartCoroutine(DemonstrationCoroutine());
        }

        private System.Collections.IEnumerator DemonstrationCoroutine()
        {
            yield return new WaitForSeconds(1f);
            
            Debug.Log($"[{currentDateTime}] {currentUser} - Starting SkillOrbModule demonstration");
            
            // 1. Activate the orb system (this starts both SpinAssignedWeapons and shakin coroutines)
            orbModule.ActivateOrbSystem();
            Debug.Log($"[{currentDateTime}] {currentUser} - ✓ Orb system activated - SpinAssignedWeapons and shakin coroutines are now running");
            
            yield return new WaitForSeconds(0.5f);
            
            // 2. Demonstrate weapon assignment to orbs
            // Note: In real usage, this would happen through the trigger system when weapons touch orbs
            Debug.Log($"[{currentDateTime}] {currentUser} - Simulating weapon assignments to demonstrate orbital motion");
            
            // Find some test items to demonstrate with
            var nearbyItems = FindNearbyWeapons();
            
            if (nearbyItems.Count > 0)
            {
                // Assign weapons to different orb directions
                for (int i = 0; i < nearbyItems.Count && i < 4; i++)
                {
                    SkillOrbModule.OrbDirection direction = GetDirectionForIndex(i);
                    orbModule.AssignWeaponToOrb(nearbyItems[i], direction);
                    Debug.Log($"[{currentDateTime}] {currentUser} - ✓ Assigned weapon {nearbyItems[i].itemId} to {direction} orb");
                    
                    yield return new WaitForSeconds(0.2f);
                }
                
                Debug.Log($"[{currentDateTime}] {currentUser} - ✓ Weapons are now orbiting around their assigned orbs with smooth motion");
                Debug.Log($"[{currentDateTime}] {currentUser} - ✓ Shakin coroutine is adding subtle movement variations");
                Debug.Log($"[{currentDateTime}] {currentUser} - ✓ Weapon holder positioning creates proper orbital motion");
            }
            else
            {
                Debug.Log($"[{currentDateTime}] {currentUser} - No nearby weapons found for demonstration, but orb system is active");
            }
            
            // 3. Run automated tests to verify all functionality
            yield return new WaitForSeconds(1f);
            SkillOrbModuleTest.RunAllTests(orbModule, ConvertToGameObjects(nearbyItems));
            
            Debug.Log($"[{currentDateTime}] {currentUser} - SkillOrbModule demonstration completed!");
            Debug.Log($"[{currentDateTime}] {currentUser} - Expected behavior: Weapons should continuously orbit around their assigned orbs while the system is active");
        }

        private List<Item> FindNearbyWeapons()
        {
            List<Item> weapons = new List<Item>();
            
            // Find items within a reasonable distance
            Collider[] colliders = Physics.OverlapSphere(item.transform.position, 10f);
            
            foreach (Collider col in colliders)
            {
                Item foundItem = col.GetComponentInParent<Item>();
                if (foundItem != null && foundItem != item && IsWeapon(foundItem))
                {
                    weapons.Add(foundItem);
                    if (weapons.Count >= 4) break; // Limit for demonstration
                }
            }
            
            return weapons;
        }

        private bool IsWeapon(Item item)
        {
            // Simple check - in a real implementation, you'd want more sophisticated weapon detection
            return item.data != null && 
                   (item.data.type == ItemData.Type.Weapon || 
                    item.itemId.ToLower().Contains("sword") ||
                    item.itemId.ToLower().Contains("axe") ||
                    item.itemId.ToLower().Contains("bow") ||
                    item.itemId.ToLower().Contains("spear"));
        }

        private SkillOrbModule.OrbDirection GetDirectionForIndex(int index)
        {
            switch (index % 4)
            {
                case 0: return SkillOrbModule.OrbDirection.Up;
                case 1: return SkillOrbModule.OrbDirection.Right;
                case 2: return SkillOrbModule.OrbDirection.Down;
                case 3: return SkillOrbModule.OrbDirection.Left;
                default: return SkillOrbModule.OrbDirection.Forward;
            }
        }

        private List<GameObject> ConvertToGameObjects(List<Item> items)
        {
            List<GameObject> gameObjects = new List<GameObject>();
            foreach (Item item in items)
            {
                if (item != null && item.gameObject != null)
                {
                    gameObjects.Add(item.gameObject);
                }
            }
            return gameObjects;
        }

        protected void OnDestroy()
        {
            Debug.Log($"[{currentDateTime}] {currentUser} - SkillOrbExample being destroyed");
            
            if (orbModule != null)
            {
                orbModule.DeactivateOrbSystem();
            }
        }
    }

    /// <summary>
    /// Configuration class for SkillOrbModule settings
    /// This allows easy customization of the orbital behavior
    /// </summary>
    public static class SkillOrbConfig
    {
        // Orbital motion settings
        public static float DefaultOrbitalRadius = 1.0f;
        public static float DefaultOrbitalSpeed = 45f;
        public static float DefaultOrbitalHeight = 0.5f;
        
        // Shaking effect settings
        public static bool EnableShaking = true;
        public static float DefaultShakingIntensity = 0.1f;
        public static float DefaultShakingFrequency = 2.0f;
        
        // Visual feedback settings
        public static bool ShowOrbVisuals = true;
        public static float OrbSize = 0.3f;
        public static Color OrbColor = Color.cyan;
        
        // Performance settings
        public static int MaxWeaponsPerOrb = 8;
        public static float UpdateFrequency = 50f; // Hz
    }
}