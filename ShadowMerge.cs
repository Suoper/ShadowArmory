using ThunderRoad;
using UnityEngine;
using System.Collections;
using System.Threading.Tasks;
using System;

namespace ShadowArmory
{
    // Updated by Suoper on 2025-04-12 02:23:52
    public class MergeShadowRift : SpellMergeData
    {
        private bool isActive = false;
        private const int riftCount = 6; // Number of rifts to spawn
        private const float riftRadius = 5f; // Radius of the rifts around the enemy
        private const float daggerForce = 50f; // Force applied to daggers
        private const float riftLifetime = 3f; // Lifetime of the rift
        private const float daggerLifetime = 5f; // Lifetime of daggers
        private const float cooldownTime = 10f; // Cooldown for the merge
        private float lastMergeTime = -cooldownTime; // Tracks the last merge trigger time
        private string daggerItemID = "DaggerCommon"; // ID of the dagger to spawn
        private string portalMaterialID = "WeaponPortalMaterial"; // Shader material for daggers
        private string riftPrefabID = "Rift"; // ID of the rift prefab

        public override void Merge(bool active)
        {
            base.Merge(active);

            if (active)
            {
                if (Time.time - lastMergeTime < cooldownTime)
                {
                    Debug.Log("[ShadowRift] Merge ability is on cooldown.");
                    ShowCooldownEffect();
                    return;
                }

                isActive = true;
                lastMergeTime = Time.time;
                Debug.Log("[ShadowRift] Activating Shadow Rift Merge.");
                ActivateRiftMerge();
            }
            else
            {
                isActive = false;
            }
        }

        private void ActivateRiftMerge()
        {
            Creature target = FindClosestEnemy();
            if (target == null)
            {
                Debug.Log("[ShadowRift] No target found for Shadow Rift Merge.");
                return;
            }

            Vector3 targetPosition = target.transform.position;
            Debug.Log($"[ShadowRift] Target found at position {targetPosition}, spawning {riftCount} rifts");

            for (int i = 0; i < riftCount; i++)
            {
                float angle = i * (360f / riftCount);
                Vector3 riftPosition = targetPosition + Quaternion.Euler(0, angle, 0) * (Vector3.forward * riftRadius);

                // Debug position of each rift
                Debug.Log($"[ShadowRift] Spawning rift {i} at position {riftPosition}");

                // Using the direct GameObject loading approach first (matches your example code)
                SpawnRiftDirectly(riftPosition, target);
            }
        }

        private void SpawnRiftDirectly(Vector3 position, Creature target)
        {
            try
            {
                Debug.Log($"[ShadowRift] Loading Rift prefab as GameObject...");

                // This uses the same approach as in your Shadow.cs example
                Catalog.LoadAssetAsync<GameObject>(riftPrefabID, (riftPrefab) =>
                {
                    if (riftPrefab == null)
                    {
                        Debug.LogError($"[ShadowRift] Failed to load Rift prefab! Trying fallback method...");
                        SpawnRiftAsFallback(position, target);
                        return;
                    }

                    Debug.Log($"[ShadowRift] Successfully loaded Rift prefab, instantiating...");

                    // Instantiate the rift at the position
                    GameObject rift = GameObject.Instantiate(riftPrefab, position, Quaternion.identity);
                    Debug.Log($"[ShadowRift] Rift instantiated at {position}");

                    // Apply visual effects to rift
                    ApplyRiftEffects(rift);

                    // Play effect at the rift position
                    EffectData effectData = Catalog.GetData<EffectData>("DarkMagicExplosion");
                    if (effectData != null)
                    {
                        Debug.Log($"[ShadowRift] Spawning rift effect");
                        effectData.Spawn(position, rift.transform.rotation).Play();
                    }

                    // Launch dagger from the rift
                    SpawnAndLaunchDagger(position, target);

                    // Destroy the rift after its lifetime
                    CoroutineHelper.Instance.StartCoroutine(DestroyRiftAfterDelay(rift, riftLifetime));

                }, riftPrefabID);
            }
            catch (Exception e)
            {
                Debug.LogError($"[ShadowRift] Error spawning rift: {e.Message}");
                SpawnRiftAsFallback(position, target);
            }
        }

        private void SpawnRiftAsFallback(Vector3 position, Creature target)
        {
            Debug.Log($"[ShadowRift] Using fallback method for rift spawning");

            // Create a simple visible object as a fallback "rift"
            GameObject fallbackRift = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            fallbackRift.transform.position = position;
            fallbackRift.transform.localScale = Vector3.one * 0.5f;

            // Make it look interesting
            Renderer renderer = fallbackRift.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = Color.blue;
                renderer.material.SetColor("_EmissionColor", Color.blue * 2f);
                renderer.material.EnableKeyword("_EMISSION");
            }

            // Play an effect at the rift position
            EffectData effectData = Catalog.GetData<EffectData>("DarkMagicExplosion");
            if (effectData != null)
            {
                effectData.Spawn(position, Quaternion.identity).Play();
            }

            // Launch dagger from the rift
            SpawnAndLaunchDagger(position, target);

            // Destroy the fallback rift after its lifetime
            CoroutineHelper.Instance.StartCoroutine(DestroyRiftAfterDelay(fallbackRift, riftLifetime));
        }

        private void ApplyRiftEffects(GameObject rift)
        {
            Debug.Log($"[ShadowRift] Applying visual effects to rift");

            try
            {
                // Get all renderers on the rift
                Renderer[] renderers = rift.GetComponentsInChildren<Renderer>(true);
                Debug.Log($"[ShadowRift] Found {renderers.Length} renderers on rift");

                if (renderers.Length == 0)
                {
                    Debug.LogWarning("[ShadowRift] No renderers found on rift!");
                    return;
                }

                // Apply materials to all renderers
                Catalog.LoadAssetAsync<Material>(portalMaterialID, (portalMaterial) =>
                {
                    if (portalMaterial == null)
                    {
                        Debug.LogError($"[ShadowRift] Failed to load portal material!");
                        return;
                    }

                    Debug.Log($"[ShadowRift] Successfully loaded portal material");

                    foreach (Renderer renderer in renderers)
                    {
                        Material newMaterial = new Material(portalMaterial);

                        // Set shader properties
                        newMaterial.SetColor("_GlowColor", Color.blue);
                        newMaterial.SetFloat("_GlowIntensity", 2.0f);
                        newMaterial.SetFloat("_Brightness", 2.0f);

                        renderer.material = newMaterial;
                        Debug.Log($"[ShadowRift] Applied material to renderer: {renderer.name}");
                    }

                }, portalMaterialID);
            }
            catch (Exception e)
            {
                Debug.LogError($"[ShadowRift] Error applying rift effects: {e.Message}");
            }
        }

        private IEnumerator DestroyRiftAfterDelay(GameObject rift, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (rift != null)
            {
                Debug.Log($"[ShadowRift] Destroying rift after {delay} seconds");
                GameObject.Destroy(rift);
            }
        }

        private void SpawnAndLaunchDagger(Vector3 position, Creature target)
        {
            ItemData daggerData = Catalog.GetData<ItemData>(daggerItemID);
            if (daggerData == null)
            {
                Debug.LogError($"[ShadowRift] Dagger item data not found for ID: {daggerItemID}");
                return;
            }

            Debug.Log($"[ShadowRift] Spawning dagger at {position}, targeting creature at {target.transform.position}");

            daggerData.SpawnAsync(dagger =>
            {
                if (dagger == null)
                {
                    Debug.LogError("[ShadowRift] Failed to spawn dagger.");
                    return;
                }

                // Position and setup the dagger
                dagger.transform.position = position;
                Vector3 direction = (target.transform.position - position).normalized;
                Rigidbody daggerBody = dagger.physicBody.rigidBody;

                if (daggerBody != null)
                {
                    Debug.Log($"[ShadowRift] Launching dagger with force {daggerForce} in direction {direction}");
                    daggerBody.AddForce(direction * daggerForce, ForceMode.Impulse);
                }

                // Apply shader effects to the dagger
                ApplyShaderEffectsToWeapon(dagger);

                // Despawn the dagger after its lifetime
                CoroutineHelper.Instance.StartCoroutine(DestroyDaggerAfterDelay(dagger, daggerLifetime));
            });
        }

        private IEnumerator DestroyDaggerAfterDelay(Item dagger, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (dagger != null)
            {
                Debug.Log($"[ShadowRift] Despawning dagger after {delay} seconds");
                dagger.Despawn();
            }
        }

        private void ApplyShaderEffectsToWeapon(Item weapon)
        {
            Debug.Log($"[ShadowRift] Applying shader effects to weapon {weapon.name}");

            if (weapon == null)
            {
                Debug.LogError("[ShadowRift] Weapon is null! Cannot apply shader effects.");
                return;
            }

            // Load the portal material with correct catalog path and delegate syntax
            Catalog.LoadAssetAsync<Material>(portalMaterialID, delegate (Material portalMaterial)
            {
                if (portalMaterial == null)
                {
                    Debug.LogError($"[ShadowRift] Failed to load {portalMaterialID}");
                    return;
                }

                Debug.Log($"[ShadowRift] Successfully loaded portal material for weapon");

                // Apply to all renderers
                foreach (Renderer renderer in weapon.GetComponentsInChildren<Renderer>())
                {
                    if (renderer != null)
                    {
                        // Create new material instance
                        Material newMaterial = new Material(portalMaterial);

                        // Set basic properties that will work without textures
                        newMaterial.SetColor("_GlowColor", Color.black);
                        newMaterial.SetFloat("_GlowIntensity", 1.5f);
                        newMaterial.SetFloat("_Brightness", 1.2f);

                        // Apply the material
                        renderer.material = newMaterial;
                        Debug.Log($"[ShadowRift] Applied portal shader to renderer: {renderer.name}");
                    }
                }

                // Try loading textures, but continue even if they fail
                Catalog.LoadAssetAsync<Texture2D>("NoiseTexture", delegate (Texture2D noiseTexture)
                {
                    if (noiseTexture != null)
                    {
                        Debug.Log($"[ShadowRift] Successfully loaded noise texture");
                        foreach (Renderer renderer in weapon.GetComponentsInChildren<Renderer>())
                        {
                            if (renderer != null && renderer.material != null)
                            {
                                renderer.material.SetTexture("_NoiseTex", noiseTexture);
                            }
                        }
                    }
                }, "NoiseTexture");

                Catalog.LoadAssetAsync<Texture2D>("StarsTexture", delegate (Texture2D starsTexture)
                {
                    if (starsTexture != null)
                    {
                        Debug.Log($"[ShadowRift] Successfully loaded stars texture");
                        foreach (Renderer renderer in weapon.GetComponentsInChildren<Renderer>())
                        {
                            if (renderer != null && renderer.material != null)
                            {
                                renderer.material.SetTexture("_StarsTex", starsTexture);
                            }
                        }
                    }
                }, "StarsTexture");

            }, portalMaterialID);
        }

        private Creature FindClosestEnemy()
        {
            Creature closest = null;
            float closestDistance = float.MaxValue;

            foreach (Creature creature in Creature.allActive)
            {
                if (creature != Player.currentCreature && creature.state == Creature.State.Alive)
                {
                    float distance = Vector3.Distance(Player.currentCreature.transform.position, creature.transform.position);
                    if (distance < closestDistance)
                    {
                        closest = creature;
                        closestDistance = distance;
                    }
                }
            }

            return closest;
        }

        private void ShowCooldownEffect()
        {
            EffectData effect = Catalog.GetData<EffectData>("SpellFail");
            if (effect != null)
            {
                effect.Spawn(Player.currentCreature.transform.position, Player.currentCreature.transform.rotation).Play();
            }
        }
    }
}