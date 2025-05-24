using ThunderRoad;
using UnityEngine;
using System.Collections.Generic;

namespace ShadowArmory
{
    public class ShadowForm : MonoBehaviour
    {
        private readonly string currentUser = "Suoper";
        private readonly string currentDateTime = "2025-04-09 02:06:59";

        private Creature creature;
        private bool isInShadowForm = false;
        private float currentDuration;
        private float currentCooldownTime = 0f;
        private float gestureTimer = 0f;

        // Visual effects
        private EffectInstance shadowEffect;
        private List<Renderer> characterRenderers = new List<Renderer>();
        private List<Material[]> originalMaterials = new List<Material[]>();
        private Material shadowMaterial;

        // Stealth settings
        private List<Creature> nearbyCreatures = new List<Creature>();
        private float lastCreatureCheckTime = 0f;

        // Store original detection values for each creature
        private Dictionary<Creature, float> originalSightFovH = new Dictionary<Creature, float>();
        private Dictionary<Creature, float> originalSightFovV = new Dictionary<Creature, float>();
        private Dictionary<Creature, bool> originalCanHear = new Dictionary<Creature, bool>();
        private Dictionary<Item, EffectInstance> activeEffects = new Dictionary<Item, EffectInstance>();

        public void Initialize(Creature owner)
        {
            creature = owner;

            if (creature == null)
            {
                Debug.LogError($"[{currentDateTime}] {currentUser} - Cannot initialize Shadow Form: creature is null!");
                return;
            }

            Debug.Log($"[{currentDateTime}] {currentUser} - Initializing Shadow Form for {creature.name}");

            Renderer[] renderers = creature.GetComponentsInChildren<Renderer>(true);
            characterRenderers.AddRange(renderers);
            Debug.Log($"[{currentDateTime}] {currentUser} - Found {characterRenderers.Count} renderers");

            foreach (Renderer renderer in characterRenderers)
            {
                if (renderer != null && renderer.materials != null)
                {
                    Material[] newMaterials = new Material[renderer.materials.Length];
                    for (int i = 0; i < renderer.materials.Length; i++)
                    {
                        newMaterials[i] = renderer.materials[i];
                    }
                    originalMaterials.Add(newMaterials);
                }
            }

            Catalog.LoadAssetAsync<Material>(ShadowFormConfig.ShadowMaterialId, (material) =>
            {
                if (material != null)
                {
                    shadowMaterial = material;
                    Debug.Log($"[{currentDateTime}] {currentUser} - Successfully loaded {ShadowFormConfig.ShadowMaterialId}");
                }
                else
                {
                    Debug.LogWarning($"[{currentDateTime}] {currentUser} - Failed to load {ShadowFormConfig.ShadowMaterialId}, will use fallback");
                }
            }, "ShadowForm");

            creature.OnDamageEvent += OnCreatureDamage;

            foreach (Creature c in Creature.allActive)
            {
                if (!c.isPlayer)
                {
                    nearbyCreatures.Add(c);
                }
            }

            EventManager.onCreatureSpawn += OnCreatureSpawn;

            Debug.Log($"[{currentDateTime}] {currentUser} - Shadow Form initialized");
        }

        private void OnCreatureSpawn(Creature creature)
        {
            if (!creature.isPlayer && !nearbyCreatures.Contains(creature))
            {
                nearbyCreatures.Add(creature);

                if (isInShadowForm)
                {
                    MakeInvisibleToCreature(creature);
                }
            }
        }

        private void ToggleShadowForm()
        {
            if (isInShadowForm)
            {
                ExitShadowForm();
            }
            else
            {
                if (currentCooldownTime > 0)
                {
                    Debug.Log($"[{currentDateTime}] {currentUser} - Can't use Shadow Form yet, cooldown active.");
                    return;
                }

                EnterShadowForm();
                currentCooldownTime = ShadowFormConfig.CooldownTime;
            }
        }

        private void EnterShadowForm()
        {
            isInShadowForm = true;
            currentDuration = ShadowFormConfig.Duration;
            Debug.Log($"[{currentDateTime}] {currentUser} - Entered Shadow Form");

            foreach (Renderer renderer in characterRenderers)
            {
                if (renderer != null && renderer.materials != null)
                {
                    Material[] newMaterials = new Material[renderer.materials.Length];
                    for (int i = 0; i < renderer.materials.Length; i++)
                    {
                        if (shadowMaterial != null)
                        {
                            Material tempMaterial = new Material(shadowMaterial);
                            tempMaterial.color = new Color(
                                tempMaterial.color.r,
                                tempMaterial.color.g,
                                tempMaterial.color.b,
                                ShadowFormConfig.TransparencyLevel
                            );
                            newMaterials[i] = tempMaterial;
                        }
                    }
                    renderer.materials = newMaterials;
                }
            }

            MakeInvisibleToEnemies();
            IncreaseStrength();
        }

        private void ExitShadowForm()
        {
            isInShadowForm = false;
            Debug.Log($"[{currentDateTime}] {currentUser} - Exited Shadow Form");

            for (int i = 0; i < characterRenderers.Count; i++)
            {
                if (characterRenderers[i] != null && i < originalMaterials.Count)
                {
                    characterRenderers[i].materials = originalMaterials[i];
                }
            }

            RestoreEnemyDetection();
            ResetStrength();
        }

        private void MakeInvisibleToEnemies()
        {
            foreach (Creature npc in nearbyCreatures.ToArray())
            {
                if (npc != null && !npc.isKilled)
                {
                    MakeInvisibleToCreature(npc);
                }
            }

            nearbyCreatures.RemoveAll(c => c == null || c.isKilled);
            Debug.Log($"[{currentDateTime}] {currentUser} - Player is now invisible to {nearbyCreatures.Count} enemies");
        }

        private void MakeInvisibleToCreature(Creature npc)
        {
            if (npc == null || npc.isPlayer || npc.isKilled)
                return;

            try
            {
                if (npc.brain?.instance != null)
                {
                    var detectionModule = npc.brain.instance.GetModule<BrainModuleDetection>();
                    if (detectionModule != null)
                    {
                        if (!originalSightFovH.ContainsKey(npc))
                        {
                            originalSightFovH[npc] = detectionModule.sightDetectionHorizontalFov;
                            originalSightFovV[npc] = detectionModule.sightDetectionVerticalFov;
                            originalCanHear[npc] = detectionModule.canHear;
                        }

                        detectionModule.sightDetectionHorizontalFov = 0f;
                        detectionModule.sightDetectionVerticalFov = 0f;
                        detectionModule.alertednessLevel = 0f;
                        detectionModule.canHear = false;
                    }

                    if (npc.brain != null && npc.brain.currentTarget != null)
                    {
                        npc.brain.SetState(Brain.State.Idle);
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[{currentDateTime}] {currentUser} - Failed to make invisible to creature: {ex.Message}");
            }
        }

        private void RestoreEnemyDetection()
        {
            foreach (KeyValuePair<Creature, float> entry in originalSightFovH)
            {
                if (entry.Key != null && !entry.Key.isKilled && entry.Key.brain?.instance != null)
                {
                    var detectionModule = entry.Key.brain.instance.GetModule<BrainModuleDetection>();
                    if (detectionModule != null)
                    {
                        detectionModule.sightDetectionHorizontalFov = entry.Value;
                        detectionModule.sightDetectionVerticalFov = originalSightFovV[entry.Key];
                        detectionModule.canHear = originalCanHear[entry.Key];
                    }
                }
            }

            originalSightFovH.Clear();
            originalSightFovV.Clear();
            originalCanHear.Clear();

            Debug.Log($"[{currentDateTime}] {currentUser} - Restored normal enemy detection");
        }

        private void CheckNearbyCreatures()
        {
            if (Time.time - lastCreatureCheckTime < ShadowFormConfig.CreatureCheckInterval)
                return;

            lastCreatureCheckTime = Time.time;

            foreach (Creature c in Creature.allActive)
            {
                if (!c.isPlayer && !c.isKilled && !nearbyCreatures.Contains(c))
                {
                    nearbyCreatures.Add(c);

                    if (isInShadowForm)
                    {
                        MakeInvisibleToCreature(c);
                    }
                }
            }

            nearbyCreatures.RemoveAll(c => c == null || c.isKilled);
        }

        private void IncreaseStrength()
        {
            if (Player.currentCreature == null)
                return;

            Player.currentCreature.AddJointForceMultiplier(this,
                ShadowFormConfig.PositionMultiplier,
                ShadowFormConfig.RotationMultiplier);
        }

        private void ResetStrength()
        {
            if (Player.currentCreature == null)
                return;

            Player.currentCreature.RemoveJointForceMultiplier(this);
        }

        private void Update()
        {
            if (gestureTimer > 0)
                gestureTimer -= Time.deltaTime;

            if (isInShadowForm)
            {
                CheckNearbyCreatures();

                currentDuration -= Time.deltaTime;
                if (currentDuration <= 0)
                {
                    Debug.Log($"[{currentDateTime}] {currentUser} - Shadow Form expired, exiting...");
                    ExitShadowForm();
                }
            }
            else
            {
                if (currentCooldownTime > 0)
                {
                    currentCooldownTime -= Time.deltaTime;
                    if (currentCooldownTime <= 0)
                    {
                        Debug.Log($"[{currentDateTime}] {currentUser} - Cooldown expired, ready to use Shadow Form again.");
                    }
                }

                bool leftGrip = Player.local.handLeft.controlHand.gripPressed;
                bool rightGrip = Player.local.handRight.controlHand.gripPressed;

                if (leftGrip && rightGrip)
                {
                    Vector3 leftPos = Player.local.handLeft.transform.position;
                    Vector3 rightPos = Player.local.handRight.transform.position;
                    float handDistance = Vector3.Distance(leftPos, rightPos);
                    bool armsSpread = handDistance > ShadowFormConfig.ArmSpread;

                    if (armsSpread && gestureTimer <= 0f)
                    {
                        gestureTimer = ShadowFormConfig.GestureCooldown;
                        Debug.Log($"[{currentDateTime}] {currentUser} - Detected wide arm spread with grips. Activating Shadow Form...");
                        ToggleShadowForm();
                    }
                }
            }
        }

        private void OnCreatureDamage(CollisionInstance collisionInstance, EventTime eventTime)
        {
            if (isInShadowForm && collisionInstance.damageStruct.damage > 0)
            {
                Debug.Log($"[{currentDateTime}] {currentUser} - Took damage while in Shadow Form: {collisionInstance.damageStruct.damage}");
            }
        }

        private void OnDestroy()
        {
            Debug.Log($"[{currentDateTime}] {currentUser} - Shadow Form being destroyed");

            if (isInShadowForm)
            {
                ExitShadowForm();
            }

            if (creature != null)
            {
                creature.OnDamageEvent -= OnCreatureDamage;
            }

            EventManager.onCreatureSpawn -= OnCreatureSpawn;

            Debug.Log($"[{currentDateTime}] {currentUser} - Shadow Form destroyed");
        }
    }
}