using ThunderRoad;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using static ThunderRoad.AudioPortal;
using System.Linq;
using System;
using System.Text;

namespace ShadowArmory
{
    public class Shadow : SpellCastCharge
    {
        private List<string> availableWeapons; // New name for our weapon list
        private bool isOnCooldown = false;
        private float cooldownTimer = 0f;
        private List<string> weaponIDs;
        private List<string> effectIDs;
        private GameObject riftMesh;
        private Material portalMaterial;
        private const float MIN_GESTURE_SPEED = 0.3f;
        private float gestureStartTime;
        private Dictionary<GameObject, float> activeRifts = new Dictionary<GameObject, float>();
        private List<Item> spawnedWeapons = new List<Item>(); // Track spawned weapons
        private const int MAX_ACTIVE_RIFTS = 5; // Limit number of rifts
        private const int MAX_SPAWNED_WEAPONS = 10;
        private const float RIFT_LIFETIME = 5f;
        private readonly string currentUser = "SuoperShow";
        private readonly string currentDateTime = "2025-04-13 20:23:50"; // Updated timestamp
        private Vector3 lastHandPosition;
        private float gestureAccumulator = 0f;
        private const float GESTURE_CHECK_INTERVAL = 0.1f; // Check gesture every 0.1 seconds
        private const float GESTURE_DISTANCE_THRESHOLD = 0.12f;
        private const float GESTURE_COOLDOWN = 0.5f;
        private float lastGestureTime = 0f;
        private Vector3 gestureStartPosition;

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

        private enum Direction
        {
            X,
            Y,
            Z
        }

        private Dictionary<WeaponDirection, string> directionToWeapon;
        private Vector3 handStartPosition;
        private bool isTracking = false;
        private float gestureThreshold = 0.12f; // Minimum distance for gesture detection

        private Direction GetDirection(Vector3 vector)
        {
            float absX = Mathf.Abs(vector.x);
            float absY = Mathf.Abs(vector.y);
            float absZ = Mathf.Abs(vector.z);

            if (absX >= absY && absX >= absZ)
                return Direction.X;
            else if (absY >= absX && absY >= absZ)
                return Direction.Y;
            else
                return Direction.Z;
        }

        private void CleanupAllRiftsAndWeapons()
        {
            Debug.Log($"[{currentDateTime}] {currentUser} - Cleaning up all rifts and weapons");

            // Clean up rifts
            foreach (var rift in activeRifts.Keys.ToList())
            {
                if (rift != null)
                {
                    CloseRift(rift);
                }
            }
            activeRifts.Clear();

            // Clean up weapons
            foreach (var weapon in spawnedWeapons.ToList())
            {
                if (weapon != null)
                {
                    if (!weapon.IsHanded()) // Only cleanup weapons that aren't being held
                    {
                        UnityEngine.Object.Destroy(weapon.gameObject);
                        spawnedWeapons.Remove(weapon);
                    }
                }
            }

            // Remove any null references from the list
            spawnedWeapons.RemoveAll(w => w == null);
        }

        private void CleanupOldestIfNeeded()
        {
            // Clean up oldest rift if we have too many
            if (activeRifts.Count >= MAX_ACTIVE_RIFTS)
            {
                var oldest = activeRifts.OrderBy(x => x.Value).FirstOrDefault();
                if (oldest.Key != null)
                {
                    CloseRift(oldest.Key);
                }
            }

            // Clean up oldest weapon if we have too many
            if (spawnedWeapons.Count >= MAX_SPAWNED_WEAPONS)
            {
                var oldestWeapon = spawnedWeapons
                    .Where(w => w != null && !w.IsHanded())
                    .FirstOrDefault();

                if (oldestWeapon != null)
                {
                    spawnedWeapons.Remove(oldestWeapon);
                    UnityEngine.Object.Destroy(oldestWeapon.gameObject);
                }
            }

            // Clean up null references
            spawnedWeapons.RemoveAll(w => w == null);
            foreach (var rift in activeRifts.Keys.ToList())
            {
                if (rift == null)
                {
                    activeRifts.Remove(rift);
                }
            }
        }

        public override void Init()
        {
            base.Init();
            Debug.Log($"[{currentDateTime}] {currentUser} - Shadow spell initialization started");

            // Initialize collections
            weaponIDs = new List<string>();
            availableWeapons = new List<string>();
            directionToWeapon = new Dictionary<WeaponDirection, string>();
            effectIDs = new List<string>();

            // Load weapons and effects
            LoadAllWeaponIDs();
            LoadAllEffectIDs();
            InitializeWeaponMappings();
            LoadWeaponAssignments();

            Debug.Log($"[{currentDateTime}] {currentUser} - Shadow spell initialization completed");
        }

        private class StoredWeapon
        {
            public string WeaponId;
            public float StorageTime;
            public Quaternion OriginalRotation;
            public Vector3 RelativePosition;
        }

        public override void OnCatalogRefresh()
        {
            base.OnCatalogRefresh();
            InitializeWeaponMappings();
        }

        // This method helps us interact with the T1ShadowCrystal module
        private string GetWeaponIDFromCrystal(WeaponDirection direction)
        {
            // Check if we're using a T1ShadowCrystal
            List<RagdollHand> hands = new List<RagdollHand>() {
            Player.local.creature.handLeft,
            Player.local.creature.handRight
            };
            foreach (RagdollHand hand in hands)
            {
                if (hand.grabbedHandle == null || hand.grabbedHandle.item == null) continue;

                Item item = hand.grabbedHandle.item;
                T1ShadowCrystal crystal = item.data.GetModule<T1ShadowCrystal>();
                if (crystal != null)
                {
                    // Convert our enum to string for the common interface
                    string directionStr = direction.ToString();
                    string weaponId = crystal.GetWeaponForDirection(directionStr);

                    if (!string.IsNullOrEmpty(weaponId))
                    {
                        Debug.Log($"[{currentDateTime}] {currentUser} - Found weapon {weaponId} for direction {direction} in crystal");
                        return weaponId;
                    }
                }
            }

            // Check PlayerPrefs as fallback
            string key = $"T1ShadowCrystal_Direction_{direction}";
            if (PlayerPrefs.HasKey(key))
            {
                string weaponId = PlayerPrefs.GetString(key);
                Debug.Log($"[{currentDateTime}] {currentUser} - Found weapon {weaponId} for direction {direction} in PlayerPrefs");
                return weaponId;
            }

            // Return default if nothing found
            return "SwordShortCommon";
        }

        // Helper method to convert the direction enum

        // Remove or comment out this method - it's causing errors
        /*
        private T1ShadowCrystal.WeaponDirection ConvertToModuleDirection(WeaponDirection direction)
        {
            switch (direction)
            {
                case WeaponDirection.Up:
                    return T1ShadowCrystal.WeaponDirection.Up;
                case WeaponDirection.Down:
                    return T1ShadowCrystal.WeaponDirection.Down;
                case WeaponDirection.Left:
                    return T1ShadowCrystal.WeaponDirection.Left;
                case WeaponDirection.Right:
                    return T1ShadowCrystal.WeaponDirection.Right;
                case WeaponDirection.Forward:
                    return T1ShadowCrystal.WeaponDirection.Forward;
                case WeaponDirection.Backward:
                    return T1ShadowCrystal.WeaponDirection.Backward;
                default:
                    return T1ShadowCrystal.WeaponDirection.None;
            }
        }
        */

        private void LoadAllWeaponIDs()
        {
            Debug.Log($"[{currentDateTime}] {currentUser} - Loading all available weapons...");
            weaponIDs.Clear();

            // Get all weapons from the catalog
            foreach (ItemData itemData in Catalog.GetDataList<ItemData>())
            {
                // Only add items that are weapons
                if (itemData.type == ItemData.Type.Weapon)
                {
                    weaponIDs.Add(itemData.id);
                    Debug.Log($"[{currentDateTime}] {currentUser} - Added weapon: {itemData.id}");
                }
            }

            Debug.Log($"[{currentDateTime}] {currentUser} - Total weapons loaded: {weaponIDs.Count}");
        }

        private void LoadAllEffectIDs()
        {
            effectIDs = new List<string>();
            Debug.Log("Loading effect IDs...");
            foreach (var effectData in Catalog.GetDataList<EffectData>())
            {
                effectIDs.Add(effectData.id);
                Debug.Log($"Added effect: {effectData.id}");
            }
        }

        public override void Fire(bool active)
        {
            base.Fire(active);

            if (active)
            {
                lastHandPosition = spellCaster.ragdollHand.transform.position;
                gestureAccumulator = 0f;
                gestureStartPosition = spellCaster.ragdollHand.transform.position;
                isTracking = true;
            }
            else
            {
                isTracking = false;
                // Only cleanup when spell stops AND we want to clean up
                if (ShadowConfig.CleanupOnSpellEnd)
                {
                    CleanupAllRiftsAndWeapons();
                }
            }
        }

        private WeaponDirection DetermineGestureDirection(Vector3 movement)
        {
            // Get the hand side
            Side handSide = spellCaster.ragdollHand.side;

            // Log current state for debugging
            Debug.Log($"[2025-05-10 20:20:13] SuoperSame - Processing gesture: Raw movement={movement}, Hand side={handSide}");

            // Convert to player's local space for consistent directions
            Vector3 localMovement = Player.local.transform.InverseTransformDirection(movement);
            float speed = movement.magnitude / GESTURE_CHECK_INTERVAL;

            // Early exit if movement is too slow
            if (speed < MIN_GESTURE_SPEED)
            {
                return WeaponDirection.None;
            }

            // Get absolute values to determine dominant axis
            float absX = Mathf.Abs(localMovement.x);
            float absY = Mathf.Abs(localMovement.y);
            float absZ = Mathf.Abs(localMovement.z);

            Debug.Log($"[2025-05-10 20:20:13] SuoperSame - Gesture components - X:{localMovement.x:F2}({absX:F2}), " +
                      $"Y:{localMovement.y:F2}({absY:F2}), Z:{localMovement.z:F2}({absZ:F2})");

            // CRITICAL FIX: LEFT HAND ADJUSTMENT
            // For left hand, we need to mirror the X and Z axes to maintain intuitive gestures
            if (handSide == Side.Left)
            {
                // We invert X and Z axes for left hand to make gestures feel natural
                localMovement.x = -localMovement.x;
                localMovement.z = -localMovement.z;

                Debug.Log($"[2025-05-10 20:20:13] SuoperSame - Left hand adjustment applied - " +
                          $"Modified movement: X:{localMovement.x:F2}, Y:{localMovement.y:F2}, Z:{localMovement.z:F2}");
            }

            // Determine direction based on the strongest component
            WeaponDirection direction;

            // Find dominant axis with sufficient threshold margin
            // We consider an axis dominant if it's at least 1.2x the magnitude of others
            const float dominanceThreshold = 1.2f;

            if (absY > absX * dominanceThreshold && absY > absZ * dominanceThreshold)
            {
                // Y-axis dominant (up/down)
                direction = localMovement.y > 0 ? WeaponDirection.Up : WeaponDirection.Down;
            }
            else if (absX > absY * dominanceThreshold && absX > absZ * dominanceThreshold)
            {
                // X-axis dominant (left/right)
                // We already adjusted for hand side above, so we don't need to invert here
                direction = localMovement.x > 0 ? WeaponDirection.Right : WeaponDirection.Left;
            }
            else if (absZ > absY * dominanceThreshold && absZ > absX * dominanceThreshold)
            {
                // Z-axis dominant (forward/backward)
                // We already adjusted for hand side above, so we don't need to invert here
                direction = localMovement.z > 0 ? WeaponDirection.Forward : WeaponDirection.Backward;
            }
            else
            {
                // No clear dominance, fallback to greatest component
                if (absY >= absX && absY >= absZ)
                {
                    direction = localMovement.y > 0 ? WeaponDirection.Up : WeaponDirection.Down;
                }
                else if (absX >= absY && absX >= absZ)
                {
                    direction = localMovement.x > 0 ? WeaponDirection.Right : WeaponDirection.Left;
                }
                else
                {
                    direction = localMovement.z > 0 ? WeaponDirection.Forward : WeaponDirection.Backward;
                }
            }

            Debug.Log($"[2025-05-10 20:20:13] SuoperSame - Final gesture detection: {direction} for {handSide} hand");
            return direction;
        }
        // In your Shadow.cs file
        private string GetWeaponForDirection(WeaponDirection direction)
        {
            // First check crystal in hand via our specialized method
            string weaponId = GetWeaponIDFromCrystal(direction);
            if (!string.IsNullOrEmpty(weaponId))
            {
                return weaponId;
            }

            // Fallback to default mapping if nothing found
            if (directionToWeapon != null && directionToWeapon.ContainsKey(direction))
            {
                return directionToWeapon[direction];
            }

            // Ultimate fallback
            return "SwordShortCommon";
        }

        private void UpdateWeaponShaderProperties(Material material)
        {
            if (material == null || material.shader == null) return;

            // Create colors from RGB components
            Color glowColor = new Color(ShadowConfig.GlowColorR, ShadowConfig.GlowColorG, ShadowConfig.GlowColorB, 1f);
            Color pulseColor1 = new Color(ShadowConfig.PulseColor1R, ShadowConfig.PulseColor1G, ShadowConfig.PulseColor1B, 1f);
            Color pulseColor2 = new Color(ShadowConfig.PulseColor2R, ShadowConfig.PulseColor2G, ShadowConfig.PulseColor2B, 1f);
            Color rimColor = new Color(ShadowConfig.RimColorR, ShadowConfig.RimColorG, ShadowConfig.RimColorB, 1f);

            // Set all shader properties
            material.SetColor("_GlowColor", glowColor);
            material.SetColor("_PulseColor1", pulseColor1);
            material.SetColor("_PulseColor2", pulseColor2);
            material.SetColor("_RimColor", rimColor);

            material.SetFloat("_GlowIntensity", ShadowConfig.GlowIntensity);
            material.SetFloat("_PulseSpeed", ShadowConfig.PulseSpeed);
            material.SetFloat("_Brightness", ShadowConfig.Brightness);
            material.SetFloat("_RimPower", ShadowConfig.RimPower);
            material.SetFloat("_DistortionStrength", ShadowConfig.DistortionStrength);
            material.SetFloat("_StarsBrightness", ShadowConfig.StarsBrightness);
        }

        private void ApplyShaderEffectsToWeapon(Item weapon)
        {
            if (!ShadowConfig.ApplyShaderToWeapons) return;

            Debug.Log($"[{currentDateTime}] {currentUser} - Attempting to apply weapon shader effects...");

            if (weapon == null)
            {
                Debug.LogError($"[{currentDateTime}] {currentUser} - Weapon is null!");
                return;
            }

            try
            {
                // Load the portal material with correct catalog path and delegate syntax
                Catalog.LoadAssetAsync<Material>("WeaponMaterial", delegate (Material portalMaterial)
                {
                    if (portalMaterial == null)
                    {
                        Debug.LogError($"[{currentDateTime}] {currentUser} - Failed to load WeaponPortalMaterial");
                        return;
                    }

                    // Load textures
                    Catalog.LoadAssetAsync<Texture2D>("NoiseTexture", delegate (Texture2D noiseTexture)
                    {
                        Catalog.LoadAssetAsync<Texture2D>("StarsTexture", delegate (Texture2D starsTexture)
                        {
                            // Apply to all renderers
                            foreach (Renderer renderer in weapon.GetComponentsInChildren<Renderer>())
                            {
                                if (renderer != null)
                                {
                                    // Create new material instance
                                    Material newMaterial = new Material(portalMaterial);

                                    // Set textures if available
                                    if (noiseTexture != null)
                                        newMaterial.SetTexture("_NoiseTex", noiseTexture);
                                    if (starsTexture != null)
                                        newMaterial.SetTexture("_StarsTex", starsTexture);

                                    // Set shader properties
                                    newMaterial.SetColor("_GlowColor", new Color(ShadowConfig.GlowColorR, ShadowConfig.GlowColorG, ShadowConfig.GlowColorB, 1f));
                                    newMaterial.SetColor("_PulseColor1", new Color(ShadowConfig.PulseColor1R, ShadowConfig.PulseColor1G, ShadowConfig.PulseColor1B, 1f));
                                    newMaterial.SetColor("_PulseColor2", new Color(ShadowConfig.PulseColor2R, ShadowConfig.PulseColor2G, ShadowConfig.PulseColor2B, 1f));
                                    newMaterial.SetColor("_RimColor", new Color(ShadowConfig.RimColorR, ShadowConfig.RimColorG, ShadowConfig.RimColorB, 1f));

                                    newMaterial.SetFloat("_GlowIntensity", ShadowConfig.GlowIntensity);
                                    newMaterial.SetFloat("_PulseSpeed", ShadowConfig.PulseSpeed);
                                    newMaterial.SetFloat("_Brightness", ShadowConfig.WeaponBrightness);
                                    newMaterial.SetFloat("_RimPower", ShadowConfig.RimPower);
                                    newMaterial.SetFloat("_DistortionStrength", ShadowConfig.WeaponDistortionStrength);
                                    newMaterial.SetFloat("_StarsBrightness", ShadowConfig.StarsBrightness);
                                    newMaterial.SetFloat("_OpeningSpeed", ShadowConfig.WeaponOpeningSpeed);

                                    // Apply the material
                                    renderer.material = newMaterial;
                                    Debug.Log($"[{currentDateTime}] {currentUser} - Applied portal shader to renderer: {renderer.name}");
                                }
                            }
                        }, "StarsTexture");
                    }, "NoiseTexture");
                }, "WeaponPortalMaterial");
            }
            catch (Exception e)
            {
                Debug.LogError($"[{currentDateTime}] {currentUser} - Error applying shader effects: {e.Message}");
            }
        }

        private void InitializeWeaponMappings()
        {
            Debug.Log($"[{currentDateTime}] {currentUser} - Initializing weapon mappings...");

            // Clear and recreate the direction mapping if needed
            if (directionToWeapon == null)
            {
                directionToWeapon = new Dictionary<WeaponDirection, string>();
            }
            else
            {
                directionToWeapon.Clear();
            }

            // Default mappings (these will be overridden by saved preferences if available)
            directionToWeapon[WeaponDirection.Up] = "SwordShortCommon";
            directionToWeapon[WeaponDirection.Down] = "DaggerCommon";
            directionToWeapon[WeaponDirection.Left] = "AxeShortHatchet";
            directionToWeapon[WeaponDirection.Right] = "ShieldPartisan";
            directionToWeapon[WeaponDirection.Forward] = "SpearFighter";
            directionToWeapon[WeaponDirection.Backward] = "BowRecurve";

            // Log the mappings
            foreach (var mapping in directionToWeapon)
            {
                Debug.Log($"[{currentDateTime}] {currentUser} - Initialized mapping: {mapping.Value} to direction: {mapping.Key}");
            }
        }

        private void LoadWeaponAssignments()
        {
            try
            {
                // Initialize default mapping if not loaded
                if (directionToWeapon == null)
                {
                    directionToWeapon = new Dictionary<WeaponDirection, string>();
                }
                else
                {
                    directionToWeapon.Clear();
                }

                if (PlayerPrefs.HasKey("ShadowArmory_WeaponAssignments"))
                {
                    string jsonStr = PlayerPrefs.GetString("ShadowArmory_WeaponAssignments");
                    Debug.Log($"[{currentDateTime}] {currentUser} - Loading weapon assignments: {jsonStr}");

                    // Basic parsing without JSONObject
                    if (jsonStr.StartsWith("{") && jsonStr.EndsWith("}"))
                    {
                        // Remove braces
                        jsonStr = jsonStr.Substring(1, jsonStr.Length - 2);

                        // Split pairs
                        string[] pairs = jsonStr.Split(',');

                        foreach (string pair in pairs)
                        {
                            string[] keyValue = pair.Split(':');
                            if (keyValue.Length == 2)
                            {
                                // Clean up the strings
                                string key = keyValue[0].Trim('"', ' ');
                                string value = keyValue[1].Trim('"', ' ');

                                if (Enum.TryParse(key, out WeaponDirection direction))
                                {
                                    directionToWeapon[direction] = value;
                                    Debug.Log($"[{currentDateTime}] {currentUser} - Loaded assignment - {direction}: {value}");
                                }
                            }
                        }
                    }
                }

                // Add default mappings for any missing directions
                if (!directionToWeapon.ContainsKey(WeaponDirection.Up))
                    directionToWeapon[WeaponDirection.Up] = "SwordShortCommon";

                if (!directionToWeapon.ContainsKey(WeaponDirection.Down))
                    directionToWeapon[WeaponDirection.Down] = "DaggerCommon";

                if (!directionToWeapon.ContainsKey(WeaponDirection.Left))
                    directionToWeapon[WeaponDirection.Left] = "AxeShortHatchet";

                if (!directionToWeapon.ContainsKey(WeaponDirection.Right))
                    directionToWeapon[WeaponDirection.Right] = "ShieldPartisan";

                if (!directionToWeapon.ContainsKey(WeaponDirection.Forward))
                    directionToWeapon[WeaponDirection.Forward] = "SpearFighter";

                if (!directionToWeapon.ContainsKey(WeaponDirection.Backward))
                    directionToWeapon[WeaponDirection.Backward] = "BowRecurve";
            }
            catch (Exception e)
            {
                Debug.LogError($"[{currentDateTime}] {currentUser} - Error loading weapon assignments: {e.Message}");
            }
        }

        private void SaveWeaponAssignments()
        {
            try
            {
                StringBuilder jsonBuilder = new StringBuilder();
                jsonBuilder.Append("{");

                bool isFirst = true;
                foreach (var kvp in directionToWeapon)
                {
                    if (!isFirst)
                        jsonBuilder.Append(",");

                    jsonBuilder.Append("\"").Append(kvp.Key.ToString()).Append("\":\"").Append(kvp.Value).Append("\"");
                    isFirst = false;
                }

                jsonBuilder.Append("}");
                string jsonString = jsonBuilder.ToString();

                Debug.Log($"[{currentDateTime}] {currentUser} - Saving weapon assignments: {jsonString}");

                // Save to player prefs
                PlayerPrefs.SetString("ShadowArmory_WeaponAssignments", jsonString);
                PlayerPrefs.Save();
            }
            catch (Exception e)
            {
                Debug.LogError($"[{currentDateTime}] {currentUser} - Error saving weapon assignments: {e.Message}");
            }
        }

        private void OpenRiftAndSummonWeapon(Vector3 spawnPosition)
        {
            Debug.Log($"[{currentDateTime}] {currentUser} - OpenRiftAndSummonWeapon called");

            if (weaponIDs == null || weaponIDs.Count == 0)
            {
                Debug.LogError($"[{currentDateTime}] {currentUser} - No weapons available to summon. WeaponIDs is null or empty.");
                return;
            }

            if (ShadowConfig.selectedWeaponIndex >= weaponIDs.Count)
            {
                Debug.LogError($"[{currentDateTime}] {currentUser} - Selected weapon index {ShadowConfig.selectedWeaponIndex} is out of range. Total weapons: {weaponIDs.Count}");
                return;
            }

            RagdollHand casterHand = spellCaster.ragdollHand;
            Quaternion riftRotation = Quaternion.LookRotation(casterHand.transform.forward);

            if (ShadowConfig.UseRift)
            {
                Debug.Log($"[{currentDateTime}] {currentUser} - Loading Rift asset...");
                Catalog.LoadAssetAsync<GameObject>("Rift", (loadedAsset) =>
                {
                    if (loadedAsset == null)
                    {
                        Debug.LogError($"[{currentDateTime}] {currentUser} - Failed to load Rift mesh - asset is null!");
                        return;
                    }

                    Debug.Log($"[{currentDateTime}] {currentUser} - Rift asset loaded successfully, creating instance...");
                    riftMesh = UnityEngine.Object.Instantiate(loadedAsset, spawnPosition, riftRotation);

                    // Add the rift to our tracking dictionary
                    activeRifts[riftMesh] = Time.time;

                    Rigidbody riftRb = riftMesh.GetComponent<Rigidbody>();
                    if (riftRb == null)
                    {
                        riftRb = riftMesh.AddComponent<Rigidbody>();
                        Debug.Log($"[{currentDateTime}] {currentUser} - Added Rigidbody to rift");
                    }
                    riftRb.isKinematic = true;

                    UpdateRiftMaterials(riftMesh);

                    string effectID = effectIDs[ShadowConfig.selectedEffectIndex];
                    if (!string.IsNullOrEmpty(effectID))
                    {
                        Debug.Log($"[{currentDateTime}] {currentUser} - Spawning effect: {effectID}");
                        Catalog.GetData<EffectData>(effectID)?.Spawn(spawnPosition, riftRotation);
                    }

                    Debug.Log($"[{currentDateTime}] {currentUser} - Spawning weapon...");
                    SpawnWeapon(spawnPosition, riftMesh);
                }, "Rift");
            }
            else
            {
                Debug.Log($"[{currentDateTime}] {currentUser} - Rift disabled, spawning weapon directly");
                string effectID = effectIDs[ShadowConfig.selectedEffectIndex];
                if (!string.IsNullOrEmpty(effectID))
                {
                    Catalog.GetData<EffectData>(effectID)?.Spawn(spawnPosition, riftRotation);
                }

                GameObject tempRift = new GameObject("TempRift");
                tempRift.transform.position = spawnPosition;
                tempRift.transform.rotation = riftRotation;

                // Add temporary rift to tracking
                activeRifts[tempRift] = Time.time;

                SpawnWeapon(spawnPosition, tempRift);
            }
        }

        private void CloseRift(GameObject rift)
        {
            if (rift != null)
            {
                // Spawn closing effect
                Catalog.GetData<EffectData>("DismissEffect")?.Spawn(rift.transform.position, rift.transform.rotation);
                activeRifts.Remove(rift);
                UnityEngine.Object.Destroy(rift);
            }
        }

        private IEnumerator ApplyEffectsWithDelay(Item item)
        {
            // Wait two frames to ensure everything is properly initialized
            yield return null;
            yield return null;

            Debug.Log("Applying weapon effects...");

            if (ShadowConfig.UseBlackWeaponEffect)
            {
                SetWeaponBlack(item);
                Debug.Log("Applied black weapon effect");
            }
            else if (ShadowConfig.ApplyShaderToWeapons)
            {
                ApplyShaderEffectsToWeapon(item);
                Debug.Log("Applied shader effects to weapon");
            }
        }

        private enum WeaponStyle
        {
            OneHanded,
            TwoHanded,
            Dagger,
            Polearm,
            Shield,
            Bow
        }

        private void SpawnWeapon(Vector3 position, GameObject riftObject)
        {
            if (weaponIDs == null || ShadowConfig.selectedWeaponIndex >= weaponIDs.Count)
            {
                Debug.Log($"[{currentDateTime}] {currentUser} - Invalid weapon configuration. WeaponIDs null? {weaponIDs == null}. Selected Index: {ShadowConfig.selectedWeaponIndex}");
                return;
            }

            try
            {
                string weaponId = weaponIDs[ShadowConfig.selectedWeaponIndex];
                Debug.Log($"[{currentDateTime}] {currentUser} - Attempting to spawn weapon: {weaponId}");

                ItemData itemData = Catalog.GetData<ItemData>(weaponId);
                if (itemData == null)
                {
                    Debug.LogError($"[{currentDateTime}] {currentUser} - ItemData not found for weapon ID: {weaponId}");
                    return;
                }

                bool hasSpawned = false; // Flag to prevent multiple spawns

                Debug.Log($"[{currentDateTime}] {currentUser} - Starting SpawnAsync for weapon: {weaponId}");
                itemData.SpawnAsync(spawnedItem =>
                {
                    // Prevent multiple spawn callbacks
                    if (hasSpawned) return;
                    hasSpawned = true;

                    Debug.Log($"[{currentDateTime}] {currentUser} - SpawnAsync callback started");

                    if (spawnedItem == null)
                    {
                        Debug.LogError($"[{currentDateTime}] {currentUser} - Spawned item is null!");
                        return;
                    }

                    try
                    {
                        // Add to tracking list
                        spawnedWeapons.Add(spawnedItem);
                        Debug.Log($"[{currentDateTime}] {currentUser} - Added weapon to tracking list");

                        // Set up weapon position and physics
                        spawnedItem.transform.position = position;
                        spawnedItem.transform.rotation = riftObject.transform.rotation;
                        spawnedItem.physicBody.isKinematic = false;
                        spawnedItem.physicBody.useGravity = true;
                        spawnedItem.gameObject.SetActive(true);
                        Debug.Log($"[{currentDateTime}] {currentUser} - Set up weapon physics and position");

                        // Apply shader effects if enabled
                        if (ShadowConfig.ApplyShaderToWeapons)
                        {
                            ApplyShaderEffectsToWeapon(spawnedItem);
                            Debug.Log($"[{currentDateTime}] {currentUser} - Applied shader effects");
                        }

                        // Get the side and attempt to grab
                        Side side = this.spellCaster.ragdollHand.side;
                        Debug.Log($"[{currentDateTime}] {currentUser} - Got hand side: {side}");

                        Handle handle = spawnedItem.GetMainHandle(side);
                        if (handle == null)
                        {
                            Debug.LogError($"[{currentDateTime}] {currentUser} - Could not find handle for weapon!");
                            return;
                        }

                        RagdollHand hand = Player.currentCreature.GetHand(side);
                        if (hand == null)
                        {
                            Debug.LogError($"[{currentDateTime}] {currentUser} - Could not get hand!");
                            return;
                        }

                        // Make sure we don't clean up during grab
                        hand.Grab(handle);
                        Debug.Log($"[{currentDateTime}] {currentUser} - Grabbed weapon successfully");

                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[{currentDateTime}] {currentUser} - Error in SpawnAsync callback: {e.Message}\n{e.StackTrace}");
                    }
                }, position, riftObject.transform.rotation);

                Debug.Log($"[{currentDateTime}] {currentUser} - SpawnAsync initiated");
            }
            catch (Exception e)
            {
                Debug.LogError($"[{currentDateTime}] {currentUser} - Error spawning weapon: {e.Message}\n{e.StackTrace}");
            }
        }

        private Color GetColorFromConfig(float r, float g, float b)
        {
            return new Color(r, g, b, 1f);
        }

        public class TrailController : MonoBehaviour
        {
            private LineRenderer lineRenderer;
            private Item item;
            private Queue<Vector3> positions;
            private float minSpeed = 3f;
            private int maxPositions = 25;
            private float fadeTime = 0.2f;
            private bool isActive = false;

            public void Initialize(LineRenderer lr, Item weaponItem)
            {
                lineRenderer = lr;
                item = weaponItem;
                positions = new Queue<Vector3>();
            }

            private void Update()
            {
                if (item == null || lineRenderer == null) return;

                // Check weapon speed
                float speed = item.physicBody.velocity.magnitude;
                isActive = speed >= minSpeed;

                // Update positions
                if (isActive)
                {
                    positions.Enqueue(item.transform.position);
                    if (positions.Count > maxPositions)
                    {
                        positions.Dequeue();
                    }
                }
                else if (positions.Count > 0)
                {
                    positions.Dequeue();
                }

                // Update line renderer
                lineRenderer.positionCount = positions.Count;
                lineRenderer.SetPositions(positions.ToArray());

                // Update opacity based on speed
                float opacity = Mathf.Lerp(0f, 0.5f, Mathf.InverseLerp(minSpeed, minSpeed * 2f, speed));
                Color startColor = new Color(0.3f, 0.3f, 0.3f, opacity);
                Color endColor = new Color(0.3f, 0.3f, 0.3f, 0f);
                lineRenderer.startColor = startColor;
                lineRenderer.endColor = endColor;
            }

            private void OnDestroy()
            {
                if (lineRenderer != null)
                {
                    Destroy(lineRenderer);
                }
            }
        }

        private IEnumerator HandleWeaponAshEffect(Item item)
        {
            float timer = 0f;
            bool startedCounting = false;
            bool hasBeenGrabbed = false;

            // Initial delay before starting any checks
            yield return new WaitForSeconds(5f);

            // Apply effects after the initial delay
            if (item != null)
            {
                CoroutineHelper.Instance.StartHelperCoroutine(ApplyEffectsWithDelay(item));
            }

            while (item != null && item.gameObject != null)
            {
                // If the item is currently held, mark it as having been grabbed
                if (item.IsHanded())
                {
                    hasBeenGrabbed = true;
                    startedCounting = false;
                    timer = 0f;
                }
                // Only start despawn timer if the item has been grabbed before and is now dropped
                else if (hasBeenGrabbed)
                {
                    if (!startedCounting)
                    {
                        startedCounting = true;
                        timer = 0f;
                        Debug.Log($"[{currentDateTime}] {currentUser} - Starting despawn timer for dropped weapon");
                    }
                    timer += Time.deltaTime;

                    if (timer >= 5f)
                    {
                        Debug.Log($"[{currentDateTime}] {currentUser} - Despawning dropped weapon");

                        // Spawn dissolution effect
                        Catalog.GetData<EffectData>("FireDissolution")?.Spawn(item.transform.position, item.transform.rotation);

                        // Handle rift effects if needed
                        if (riftMesh != null)
                        {
                            Catalog.GetData<EffectData>("FireDissolution")?.Spawn(riftMesh.transform.position, riftMesh.transform.rotation);
                            UnityEngine.Object.Destroy(riftMesh);
                        }

                        // Remove from tracking and destroy
                        spawnedWeapons.Remove(item);
                        UnityEngine.Object.Destroy(item.gameObject);
                        break;
                    }
                }
                // If it hasn't been grabbed yet, don't start the despawn timer
                else
                {
                    startedCounting = false;
                    timer = 0f;
                }

                yield return null;
            }
        }

        private void UpdateRiftMaterials(GameObject riftObject)
        {
            if (riftObject == null) return;

            foreach (Renderer renderer in riftObject.GetComponentsInChildren<Renderer>())
            {
                foreach (Material material in renderer.materials)
                {
                    if (material != null && material.shader != null)
                    {
                        // Basic properties
                        material.SetFloat("_Brightness", ShadowConfig.RiftBrightness);
                        material.SetFloat("_OpeningSpeed", ShadowConfig.RiftOpeningSpeed);
                        material.SetFloat("_DistortionStrength", ShadowConfig.RiftDistortionStrength);

                        // Advanced properties
                        material.SetFloat("_GlowIntensity", ShadowConfig.RiftGlowIntensity);
                        material.SetFloat("_PulseSpeed", ShadowConfig.RiftPulseSpeed);
                        material.SetFloat("_RimPower", ShadowConfig.RiftRimPower);
                        material.SetFloat("_StarsBrightness", ShadowConfig.RiftStarsBrightness);

                        // Colors
                        material.SetColor("_GlowColor", GetColorFromConfig(
                            ShadowConfig.RiftGlowColorR,
                            ShadowConfig.RiftGlowColorG,
                            ShadowConfig.RiftGlowColorB));
                        material.SetColor("_PulseColor1", GetColorFromConfig(
                            ShadowConfig.RiftPulseColor1R,
                            ShadowConfig.RiftPulseColor1G,
                            ShadowConfig.RiftPulseColor1B));
                        material.SetColor("_PulseColor2", GetColorFromConfig(
                            ShadowConfig.RiftPulseColor2R,
                            ShadowConfig.RiftPulseColor2G,
                            ShadowConfig.RiftPulseColor2B));
                        material.SetColor("_RimColor", GetColorFromConfig(
                            ShadowConfig.RiftRimColorR,
                            ShadowConfig.RiftRimColorG,
                            ShadowConfig.RiftRimColorB));
                    }
                }
            }
        }

        private void SetWeaponBlack(Item weapon)
        {
            foreach (var renderer in weapon.GetComponentsInChildren<Renderer>())
            {
                foreach (var material in renderer.materials)
                {
                    material.color = Color.black;
                }
            }
        }

        private void HandleXAxisGesture(Vector3 velocity)
        {
            // Minimum velocity threshold to detect a gesture
            float threshold = 2.0f;

            // Check if the gesture speed exceeds the minimum threshold
            if (Time.time - lastGestureTime < GESTURE_COOLDOWN)
                return;

            Side handSide = spellCaster.ragdollHand.side;

            if ((handSide == Side.Right && velocity.x < -threshold) ||
                (handSide == Side.Left && velocity.x < -threshold))
            {
                Debug.Log($"[{currentDateTime}] {currentUser} - Left gesture detected");
                SpawnWeaponByDirection(WeaponDirection.Left);
                lastGestureTime = Time.time;
            }
            else if ((handSide == Side.Right && velocity.x > threshold) ||
                     (handSide == Side.Left && velocity.x > threshold))
            {
                Debug.Log($"[{currentDateTime}] {currentUser} - Right gesture detected");
                SpawnWeaponByDirection(WeaponDirection.Right);
                lastGestureTime = Time.time;
            }
        }

        public void SpawnWeaponByDirection(WeaponDirection direction)
        {
            Debug.Log($"[2025-04-13 21:52:14] SuoperThe - Attempting to spawn weapon for direction: {direction}");

            // Get the weapon ID for this direction
            string weaponId = GetWeaponForDirection(direction);
            Debug.Log($"[2025-04-13 21:52:14] SuoperThe - Found weapon {weaponId} for direction {direction}");

            // Now spawn the weapon using your existing OpenRiftAndSummonWeapon logic
            if (!string.IsNullOrEmpty(weaponId))
            {
                // Find the index of this weapon in your weaponIDs list
                int weaponIndex = weaponIDs.IndexOf(weaponId);
                if (weaponIndex >= 0)
                {
                    ShadowConfig.selectedWeaponIndex = weaponIndex;
                    Vector3 spawnPosition = spellCaster.ragdollHand.transform.position +
                                           spellCaster.ragdollHand.transform.forward * 0.3f;
                    OpenRiftAndSummonWeapon(spawnPosition);
                }
                else
                {
                    // If weapon not found in list, try loading it directly
                    ItemData weaponData = Catalog.GetData<ItemData>(weaponId);
                    if (weaponData != null)
                    {
                        Vector3 spawnPosition = spellCaster.ragdollHand.transform.position +
                                               spellCaster.ragdollHand.transform.forward * 0.3f;

                        // Spawn effect
                        string effectID = effectIDs[ShadowConfig.selectedEffectIndex];
                        if (!string.IsNullOrEmpty(effectID))
                        {
                            Catalog.GetData<EffectData>(effectID)?.Spawn(spawnPosition, spellCaster.ragdollHand.transform.rotation);
                        }

                        // Spawn the weapon directly
                        weaponData.SpawnAsync(item =>
                        {
                            if (item != null)
                            {
                                spawnedWeapons.Add(item);
                                item.transform.position = spawnPosition;
                                item.transform.rotation = spellCaster.ragdollHand.transform.rotation;

                                // Apply effects
                                if (ShadowConfig.UseBlackWeaponEffect)
                                {
                                    SetWeaponBlack(item);
                                }
                                else if (ShadowConfig.ApplyShaderToWeapons)
                                {
                                    ApplyShaderEffectsToWeapon(item);
                                }

                                // Try grab with caster hand
                                spellCaster.ragdollHand.Grab(item.GetMainHandle(spellCaster.ragdollHand.side));
                            }
                        });
                    }
                }
            }
        }

        private void HandleYAxisGesture(Vector3 velocity)
        {
            float threshold = 2.0f;

            if (Time.time - lastGestureTime < GESTURE_COOLDOWN)
                return;

            Side handSide = spellCaster.ragdollHand.side;

            Debug.Log($"[2025-05-10 20:23:40] Suoper - Y-axis gesture: velocity.y={velocity.y}, handSide={handSide}");

            if (handSide == Side.Left)
            {
              
                if (velocity.y > threshold)
                {
                    Debug.Log($"[2025-05-10 20:23:40] Suoper - LEFT hand, detected DOWN gesture (inverted)");
                    SpawnWeaponByDirection(WeaponDirection.Down); 
                    lastGestureTime = Time.time;
                }
                else if (velocity.y < -threshold)
                {
                    Debug.Log($"[2025-05-10 20:23:40] Suoper - LEFT hand, detected UP gesture (inverted)");
                    SpawnWeaponByDirection(WeaponDirection.Up); 
                    lastGestureTime = Time.time;
                }
            }
            else 
            {
                if (velocity.y > threshold)
                {
                    Debug.Log($"[2025-05-10 20:23:40] Suoper - RIGHT hand, detected UP gesture");
                    SpawnWeaponByDirection(WeaponDirection.Up);
                    lastGestureTime = Time.time;
                }
                else if (velocity.y < -threshold)
                {
                    Debug.Log($"[2025-05-10 20:23:40] Suoper - RIGHT hand, detected DOWN gesture");
                    SpawnWeaponByDirection(WeaponDirection.Down);
                    lastGestureTime = Time.time;
                }
            }
        }

        private void HandleZAxisGesture(Vector3 velocity)
        {
            float threshold = 2.0f;

            if (Time.time - lastGestureTime < GESTURE_COOLDOWN)
                return;

            Side handSide = spellCaster.ragdollHand.side;

            Debug.Log($"[2025-05-10 20:31:05] Suopernow - Z-axis gesture: velocity.z={velocity.z}, handSide={handSide}");

            // FIX: Both hands, invert Z direction detection completely
            // Positive Z is now backward, negative Z is forward (opposite of before)
            if (velocity.z > threshold)
            {
                Debug.Log($"[2025-05-10 20:31:05] Suopernow - {handSide} hand, detected BACKWARD gesture (fixed)");
                SpawnWeaponByDirection(WeaponDirection.Backward); // Now positive Z is BACKWARD
                lastGestureTime = Time.time;
            }
            else if (velocity.z < -threshold)
            {
                Debug.Log($"[2025-05-10 20:31:05] Suopernow - {handSide} hand, detected FORWARD gesture (fixed)");
                SpawnWeaponByDirection(WeaponDirection.Forward); // Now negative Z is FORWARD
                lastGestureTime = Time.time;
            }
        }

        public override void UpdateCaster()
        {
            base.UpdateCaster();

            // Normal spell usage mode - check for gestures to spawn weapons
            if (spellCaster.isFiring && spellCaster.ragdollHand.grabbedHandle == null)
            {
                // Get velocity relative to head movement (prevents detecting gestures during whole body movement)
                Vector3 relativeVelocity = spellCaster.ragdollHand.transform.InverseTransformVector(
                    spellCaster.ragdollHand.Velocity() - Player.currentCreature.ragdoll.headPart.physicBody.velocity);

                // Determine which axis has the dominant movement
                Direction dominantAxis = GetDirection(relativeVelocity);

                Debug.Log($"[{currentDateTime}] {currentUser} - Dominant axis: {dominantAxis}, Velocity: {relativeVelocity}");

                switch (dominantAxis)
                {
                    case Direction.X:
                        HandleXAxisGesture(relativeVelocity);
                        break;
                    case Direction.Y:
                        HandleYAxisGesture(relativeVelocity);
                        break;
                    case Direction.Z:
                        HandleZAxisGesture(relativeVelocity);
                        break;
                }
            }

            // Rift cleanup code
            UpdateRifts();
            if (Time.frameCount % 300 == 0)
            {
                CleanupOldestIfNeeded();
            }
        }

        private void UpdateRifts()
        {
            try
            {
                List<GameObject> riftsToClose = new List<GameObject>();
                foreach (var riftEntry in activeRifts)
                {
                    if (riftEntry.Key == null) continue;

                    float timeAlive = Time.time - riftEntry.Value;
                    if (timeAlive >= RIFT_LIFETIME)
                    {
                        riftsToClose.Add(riftEntry.Key);
                    }
                }

                foreach (var rift in riftsToClose)
                {
                    CloseRift(rift);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[{currentDateTime}] {currentUser} - Error updating rifts: {e.Message}");
            }
        }

        private void ShowCooldownEffect()
        {
            if (spellCaster != null)
            {
                Catalog.GetData<EffectData>("SpellFail")?.Spawn(
                    spellCaster.transform.position,
                    spellCaster.transform.rotation);
            }
        }
    }
}