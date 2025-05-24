using ThunderRoad;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;

namespace ShadowArmory
{
    public class RiftForm : MonoBehaviour
    {
        // Updated with values from user input
        private readonly string currentUser = "Suoperadd debuging on everything and give the full code";
        private readonly string currentDateTime = "2025-04-13 00:36:28";

        // Core state
        private Creature creature;
        private bool isInRiftForm = false;
        private float currentDuration;
        private float currentCooldownTime = 0f;
        private float gestureTimer = 0f;
        private float currentGracePeriod = 0f;
        private Dictionary<RiftAbility, float> abilityCooldowns = new Dictionary<RiftAbility, float>();
        private RiftAbility currentAbility = RiftAbility.None;
        private float riftEnergy = 100f;

        // Visuals
        private EffectInstance riftEffect;
        private List<Renderer> characterRenderers = new List<Renderer>();
        private List<Material[]> originalMaterials = new List<Material[]>();
        private Material riftMaterial;
        private Dictionary<Collider, float> phasedColliders = new Dictionary<Collider, float>();
        private List<Creature> shadowClones = new List<Creature>();

        // Flight & physics
        private bool isFlying = false;
        private Vector3 flyDirection = Vector3.zero;
        private float currentFlySpeed = 0f;
        private bool wasUsingGravity = true;

        private enum RiftAbility { None, TimeSlow, RiftPull, RiftPush, DimensionShift, ShadowClone }

        private enum GestureType { None, Clap, Cross, Push, Pull, Circle }
        private GestureType detectedGesture = GestureType.None;
        private List<Vector3> leftHandPositions = new List<Vector3>();
        private List<Vector3> rightHandPositions = new List<Vector3>();
        private float gestureStartTime = 0f;

        private void Start()
        {
            Debug.Log($"[{currentDateTime}] {currentUser} - RiftForm component created.");
        }

        public void Initialize(Creature owner)
        {
            Debug.Log($"[{currentDateTime}] {currentUser} - Initializing RiftForm with owner: {owner?.name ?? "null"}");
            creature = owner;
            if (creature == null)
            {
                Debug.LogError($"[{currentDateTime}] {currentUser} - Cannot initialize: creature is null!");
                return;
            }

            // Get renderers and store original materials
            Debug.Log($"[{currentDateTime}] {currentUser} - Getting character renderers");
            characterRenderers.AddRange(creature.GetComponentsInChildren<Renderer>(true));
            Debug.Log($"[{currentDateTime}] {currentUser} - Found {characterRenderers.Count} renderers");

            foreach (Renderer r in characterRenderers)
            {
                if (r != null && r.materials != null)
                {
                    originalMaterials.Add(r.sharedMaterials);
                    Debug.Log($"[{currentDateTime}] {currentUser} - Stored materials for renderer: {r.name}");
                }
            }

            // Create rift material
            Debug.Log($"[{currentDateTime}] {currentUser} - Creating rift material");
            riftMaterial = new Material(Shader.Find("Standard"))
            {
                color = RiftFormConfig.GetRiftColor(),
                renderQueue = 3000
            };
            SetupTransparency(riftMaterial);

            // Initialize ability cooldowns
            Debug.Log($"[{currentDateTime}] {currentUser} - Initializing ability cooldowns");
            foreach (RiftAbility ability in System.Enum.GetValues(typeof(RiftAbility)))
            {
                if (ability != RiftAbility.None)
                {
                    abilityCooldowns[ability] = 0f;
                    Debug.Log($"[{currentDateTime}] {currentUser} - Added cooldown for: {ability}");
                }
            }

            // Subscribe to events
            Debug.Log($"[{currentDateTime}] {currentUser} - Subscribing to damage events");
            creature.OnDamageEvent += OnCreatureDamageEvent;

            Debug.Log($"[{currentDateTime}] {currentUser} - Rift Form initialized");
        }

        private void SetupTransparency(Material mat)
        {
            Debug.Log($"[{currentDateTime}] {currentUser} - Setting up transparency for material");
            mat.SetFloat("_Mode", 3);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.EnableKeyword("_ALPHABLEND_ON");
            Debug.Log($"[{currentDateTime}] {currentUser} - Transparency setup complete");
        }

        private bool IsRequiredSpellEquipped()
        {
            Debug.Log($"[{currentDateTime}] {currentUser} - Checking for required spell");
            if (Player.local == null)
            {
                Debug.LogWarning($"[{currentDateTime}] {currentUser} - Player.local is null");
                return false;
            }

            // Check both hands for the required spell
            bool leftHasSpell = CheckHandForSpell(Player.local.handLeft.ragdollHand);
            bool rightHasSpell = CheckHandForSpell(Player.local.handRight.ragdollHand);
            Debug.Log($"[{currentDateTime}] {currentUser} - Required spell check - Left: {leftHasSpell}, Right: {rightHasSpell}");
            return leftHasSpell || rightHasSpell;
        }

        private bool CheckHandForSpell(RagdollHand hand)
        {
            if (hand == null)
            {
                Debug.LogWarning($"[{currentDateTime}] {currentUser} - Hand is null during spell check");
                return false;
            }

            try
            {
                // Check spell caster
                SpellCaster spellCaster = hand.gameObject.GetComponent<SpellCaster>();
                if (spellCaster?.spellInstance?.id?.Contains(RiftFormConfig.RequiredSpellId) == true)
                {
                    Debug.Log($"[{currentDateTime}] {currentUser} - Found required spell in SpellCaster: {spellCaster.spellInstance.id}");
                    return true;
                }

                // Check grabbed item
                if (hand.grabbedHandle?.item?.data?.id?.Contains(RiftFormConfig.RequiredSpellId) == true)
                {
                    Debug.Log($"[{currentDateTime}] {currentUser} - Found required spell in grabbed item: {hand.grabbedHandle.item.data.id}");
                    return true;
                }

                Debug.Log($"[{currentDateTime}] {currentUser} - No required spell found in hand");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[{currentDateTime}] {currentUser} - Error checking for spell: {ex.Message}");
            }

            return false;
        }

        private void Update()
        {
            Debug.Log($"[{currentDateTime}] {currentUser} - Update frame {Time.frameCount}, isInRiftForm: {isInRiftForm}, energy: {riftEnergy:F1}");

            // Report grip states for debugging
            if (Player.local != null)
            {
                bool leftGrip = Player.local.handLeft.controlHand.gripPressed;
                bool rightGrip = Player.local.handRight.controlHand.gripPressed;
                Debug.Log($"[{currentDateTime}] {currentUser} - Grip states: Left={leftGrip}, Right={rightGrip}");
            }

            if (gestureTimer > 0)
            {
                gestureTimer -= Time.deltaTime;
                Debug.Log($"[{currentDateTime}] {currentUser} - Gesture timer: {gestureTimer:F1}");
            }

            if (isInRiftForm)
            {
                Debug.Log($"[{currentDateTime}] {currentUser} - In Rift Form - Duration: {currentDuration:F1}");
                HandleFlying();
                UpdateAbilityCooldowns();
                DetectGestures();

                // Check spell requirements after grace period
                if (currentGracePeriod > 0)
                {
                    currentGracePeriod -= Time.deltaTime;
                    Debug.Log($"[{currentDateTime}] {currentUser} - Grace period: {currentGracePeriod:F1}");
                }
                else if (!IsRequiredSpellEquipped())
                {
                    Debug.LogWarning($"[{currentDateTime}] {currentUser} - Required spell no longer equipped!");
                    ShowMessage("Spell unequipped - Rift Form canceled");
                    ExitRiftForm();
                    return;
                }

                // Update duration and energy
                currentDuration -= Time.deltaTime;
                riftEnergy = Mathf.Min(100f, riftEnergy + Time.deltaTime * 5f);
                Debug.Log($"[{currentDateTime}] {currentUser} - Updated duration: {currentDuration:F1}, energy: {riftEnergy:F1}");

                if (currentDuration <= 0)
                {
                    Debug.Log($"[{currentDateTime}] {currentUser} - Duration expired");
                    ExitRiftForm();
                    return;
                }

                // Countdown warnings
                if (currentDuration < 10f && Mathf.Floor(currentDuration) != Mathf.Floor(currentDuration + Time.deltaTime))
                    ShowMessage($"Ending in {Mathf.FloorToInt(currentDuration)}...");

            }
            else
            {
                // Update cooldown
                if (currentCooldownTime > 0)
                {
                    currentCooldownTime -= Time.deltaTime;
                    Debug.Log($"[{currentDateTime}] {currentUser} - Cooldown: {currentCooldownTime:F1}");
                    if (currentCooldownTime <= 0)
                        ShowMessage("Rift Form ready!");
                }

                CheckClappingGesture();
            }

            // Reset any phased colliders whose time is up
            CleanupPhasedColliders();
        }

        private void HandleFlying()
        {
            Debug.Log($"[{currentDateTime}] {currentUser} - HandleFlying called, isFlying: {isFlying}");
            if (!isFlying || Player.currentCreature == null)
            {
                Debug.LogWarning($"[{currentDateTime}] {currentUser} - Flying skipped - isFlying: {isFlying}, Player.currentCreature: {(Player.currentCreature != null)}");
                return;
            }

            RagdollPart chest = Player.currentCreature.ragdoll.GetPart(RagdollPart.Type.Torso);
            Rigidbody rb = chest?.physicBody?.rigidBody;
            if (rb == null)
            {
                Debug.LogError($"[{currentDateTime}] {currentUser} - Flying failed - No rigidbody found!");
                return;
            }

            // Calculate movement vectors
            Vector3 headForward = Player.local.head.transform.forward;
            headForward.y = 0; headForward.Normalize();
            Vector2 moveInput = Player.local.locomotion.moveDirection;
            Vector3 moveDir = Player.local.transform.right * moveInput.x + headForward * moveInput.y;
            moveDir.Normalize();

            Debug.Log($"[{currentDateTime}] {currentUser} - Move input: {moveInput}, resulting dir: {moveDir}");

            // Vertical movement using button presses
            float upForce = 0f;
            if (Player.local.handRight.controlHand.usePressed) upForce += 1f;
            if (Player.local.handLeft.controlHand.usePressed) upForce += 1f;

            Debug.Log($"[{currentDateTime}] {currentUser} - Up force: {upForce} (Left: {Player.local.handLeft.controlHand.usePressed}, Right: {Player.local.handRight.controlHand.usePressed})");

            // Smooth movement and apply velocity
            float targetSpeed = moveDir.magnitude * RiftFormConfig.FlySpeed;
            currentFlySpeed = Mathf.Lerp(currentFlySpeed, targetSpeed, Time.deltaTime * 3f);
            flyDirection = Vector3.Lerp(flyDirection, moveDir, Time.deltaTime * 3f);

            // Apply movement with vertical component
            rb.velocity = flyDirection * currentFlySpeed + Vector3.up * upForce * 5f;
            Debug.Log($"[{currentDateTime}] {currentUser} - Flying velocity: {rb.velocity}, speed: {currentFlySpeed:F1}");

            // Phase through objects at high speeds
            if (currentFlySpeed > RiftFormConfig.FlySpeed * 0.7f)
            {
                Debug.Log($"[{currentDateTime}] {currentUser} - Phase-through active at speed: {currentFlySpeed:F1}");
                RaycastHit[] hits = Physics.RaycastAll(rb.position, flyDirection, 0.5f);
                Debug.Log($"[{currentDateTime}] {currentUser} - Phase check - Found {hits.Length} potential objects");

                foreach (RaycastHit hit in hits)
                {
                    if (!hit.collider.isTrigger && !phasedColliders.ContainsKey(hit.collider) &&
                        hit.collider.GetComponentInParent<Player>() == null)
                    {
                        hit.collider.isTrigger = true;
                        phasedColliders[hit.collider] = Time.time + 0.5f;
                        riftEnergy -= 2f;
                        Debug.Log($"[{currentDateTime}] {currentUser} - Phased through: {hit.collider.gameObject.name}, remaining energy: {riftEnergy:F1}");
                    }
                }
            }

            // Auto-stabilization when not actively moving
            if (moveDir.magnitude < 0.1f && Mathf.Abs(upForce) < 0.1f)
            {
                Debug.Log($"[{currentDateTime}] {currentUser} - Auto-stabilizing");
                rb.velocity = Vector3.Lerp(rb.velocity, Vector3.zero, Time.deltaTime * 2f);
                rb.angularVelocity = Vector3.Lerp(rb.angularVelocity, Vector3.zero, Time.deltaTime * 3f);

                // Auto-level rotation
                if (Player.currentCreature)
                {
                    Quaternion targetRot = Quaternion.Euler(0, Player.currentCreature.transform.rotation.eulerAngles.y, 0);
                    Player.currentCreature.transform.rotation = Quaternion.Slerp(
                        Player.currentCreature.transform.rotation, targetRot, Time.deltaTime * 2f);
                    Debug.Log($"[{currentDateTime}] {currentUser} - Auto-leveling rotation");
                }
            }
        }

        private void CleanupPhasedColliders()
        {
            if (phasedColliders.Count > 0)
                Debug.Log($"[{currentDateTime}] {currentUser} - Checking {phasedColliders.Count} phased colliders");

            List<Collider> toReset = new List<Collider>();
            foreach (var pair in phasedColliders)
            {
                if (Time.time > pair.Value)
                {
                    toReset.Add(pair.Key);
                    Debug.Log($"[{currentDateTime}] {currentUser} - Collider reset time reached: {pair.Key?.gameObject?.name ?? "null"}");
                }
            }

            foreach (Collider col in toReset)
            {
                if (col)
                {
                    col.isTrigger = false;
                    Debug.Log($"[{currentDateTime}] {currentUser} - Reset collider: {col.gameObject.name}");
                }
                phasedColliders.Remove(col);
            }
        }

        private void DetectGestures()
        {
            Debug.Log($"[{currentDateTime}] {currentUser} - DetectGestures called, current ability: {currentAbility}");
            if (currentAbility != RiftAbility.None)
            {
                Debug.Log($"[{currentDateTime}] {currentUser} - Skipping gesture detection, ability in progress: {currentAbility}");
                return;
            }

            if (Player.local == null)
            {
                Debug.LogError($"[{currentDateTime}] {currentUser} - Player.local is null in DetectGestures");
                return;
            }

            Vector3 leftPos = Player.local.handLeft.transform.position;
            Vector3 rightPos = Player.local.handRight.transform.position;
            bool leftGrip = Player.local.handLeft.controlHand.gripPressed;
            bool rightGrip = Player.local.handRight.controlHand.gripPressed;

            Debug.Log($"[{currentDateTime}] {currentUser} - Gesture detection - Grips: L={leftGrip}, R={rightGrip}, Distance: {Vector3.Distance(leftPos, rightPos):F2}");

            // Use grips for gesture tracking
            if (leftGrip && rightGrip)
            {
                if (leftHandPositions.Count == 0)
                {
                    gestureStartTime = Time.time;
                    leftHandPositions.Clear();
                    rightHandPositions.Clear();
                    Debug.Log($"[{currentDateTime}] {currentUser} - Started new gesture recording");
                }

                // Record every 5th frame to avoid too much data
                if (Time.frameCount % 5 == 0)
                {
                    leftHandPositions.Add(leftPos);
                    rightHandPositions.Add(rightPos);
                    Debug.Log($"[{currentDateTime}] {currentUser} - Recorded hand positions, count: {leftHandPositions.Count}");
                }

                // Analyze after collecting enough points
                if (leftHandPositions.Count > 10 && Time.time - gestureStartTime > 0.5f)
                {
                    Debug.Log($"[{currentDateTime}] {currentUser} - Analyzing gesture with {leftHandPositions.Count} points, time: {Time.time - gestureStartTime:F1}s");
                    GestureType gesture = AnalyzeGesture();
                    Debug.Log($"[{currentDateTime}] {currentUser} - Gesture analysis result: {gesture}");

                    if (gesture != GestureType.None)
                    {
                        ActivateAbilityFromGesture(gesture);
                        leftHandPositions.Clear();
                        rightHandPositions.Clear();
                    }
                }
            }
            else
            {
                if (leftHandPositions.Count > 0)
                {
                    Debug.Log($"[{currentDateTime}] {currentUser} - Grip released, clearing gesture data");
                }
                leftHandPositions.Clear();
                rightHandPositions.Clear();
            }
        }

        private GestureType AnalyzeGesture()
        {
            Debug.Log($"[{currentDateTime}] {currentUser} - AnalyzeGesture called with {leftHandPositions.Count} positions");

            // Calculate hand movement directions
            Vector3 leftStart = leftHandPositions[0];
            Vector3 rightStart = rightHandPositions[0];
            Vector3 leftEnd = leftHandPositions[leftHandPositions.Count - 1];
            Vector3 rightEnd = rightHandPositions[rightHandPositions.Count - 1];

            Vector3 leftDir = (leftEnd - leftStart).normalized;
            Vector3 rightDir = (rightEnd - rightStart).normalized;
            float dotProduct = Vector3.Dot(leftDir, rightDir);
            float handDistance = Vector3.Distance(leftEnd, rightEnd);

            Debug.Log($"[{currentDateTime}] {currentUser} - Gesture analysis - Left movement: {leftDir}, Right movement: {rightDir}");
            Debug.Log($"[{currentDateTime}] {currentUser} - Gesture metrics - Dot product: {dotProduct:F2}, Hand distance: {handDistance:F2}");

            // Check for Push gesture (both hands pushing forward)
            float forwardDot = Vector3.Dot(leftDir, Player.local.head.transform.forward);
            Debug.Log($"[{currentDateTime}] {currentUser} - Forward dot: {forwardDot:F2}");

            if (dotProduct > 0.7f && forwardDot > 0.7f)
            {
                Debug.Log($"[{currentDateTime}] {currentUser} - Push gesture detected!");
                return GestureType.Push;
            }

            // Check for Pull gesture (both hands pulling back)
            if (dotProduct > 0.7f && Vector3.Dot(leftDir, -Player.local.head.transform.forward) > 0.7f)
            {
                Debug.Log($"[{currentDateTime}] {currentUser} - Pull gesture detected!");
                return GestureType.Pull;
            }

            // Check for Cross gesture (hands crossing each other)
            if (dotProduct < -0.5f && handDistance < 0.3f)
            {
                Debug.Log($"[{currentDateTime}] {currentUser} - Cross gesture detected!");
                return GestureType.Cross;
            }

            // Check for Circle gesture (circular motion)
            bool leftCircular = IsCircularMotion(leftHandPositions);
            bool rightCircular = IsCircularMotion(rightHandPositions);
            Debug.Log($"[{currentDateTime}] {currentUser} - Circular motion check - Left: {leftCircular}, Right: {rightCircular}");

            if (leftCircular || rightCircular)
            {
                Debug.Log($"[{currentDateTime}] {currentUser} - Circle gesture detected!");
                return GestureType.Circle;
            }

            Debug.Log($"[{currentDateTime}] {currentUser} - No gesture detected");
            return GestureType.None;
        }

        private bool IsCircularMotion(List<Vector3> positions)
        {
            if (positions.Count < 10)
            {
                Debug.Log($"[{currentDateTime}] {currentUser} - Not enough points for circle detection: {positions.Count}");
                return false;
            }

            // Simplified circle detection - check if path returns near starting point
            Vector3 start = positions[0];
            Vector3 end = positions[positions.Count - 1];
            float startEndDist = Vector3.Distance(start, end);

            // Find maximum distance from start point
            float maxDist = 0f;
            foreach (Vector3 pos in positions)
            {
                float dist = Vector3.Distance(pos, start);
                if (dist > maxDist) maxDist = dist;
            }

            Debug.Log($"[{currentDateTime}] {currentUser} - Circle detection - Start-end distance: {startEndDist:F2}, Max displacement: {maxDist:F2}");

            // If path returns near start and had significant displacement, likely circular
            bool isCircular = startEndDist < 0.2f && maxDist > 0.3f;
            Debug.Log($"[{currentDateTime}] {currentUser} - Circle detection result: {isCircular}");
            return isCircular;
        }

        private void ActivateAbilityFromGesture(GestureType gesture)
        {
            Debug.Log($"[{currentDateTime}] {currentUser} - ActivateAbilityFromGesture called with gesture: {gesture}");

            switch (gesture)
            {
                case GestureType.Push:
                    if (abilityCooldowns[RiftAbility.RiftPush] <= 0 && riftEnergy >= 30)
                    {
                        Debug.Log($"[{currentDateTime}] {currentUser} - Activating Rift Push");
                        ActivateRiftPush();
                        abilityCooldowns[RiftAbility.RiftPush] = 8f;
                        riftEnergy -= 30f;
                        Debug.Log($"[{currentDateTime}] {currentUser} - Rift Push activated, remaining energy: {riftEnergy:F1}");
                    }
                    else
                    {
                        Debug.LogWarning($"[{currentDateTime}] {currentUser} - Cannot activate Rift Push - Cooldown: {abilityCooldowns[RiftAbility.RiftPush]:F1}, Energy: {riftEnergy:F1}");
                    }
                    break;

                case GestureType.Pull:
                    if (abilityCooldowns[RiftAbility.RiftPull] <= 0 && riftEnergy >= 25)
                    {
                        Debug.Log($"[{currentDateTime}] {currentUser} - Activating Rift Pull");
                        ActivateRiftPull();
                        abilityCooldowns[RiftAbility.RiftPull] = 6f;
                        riftEnergy -= 25f;
                        Debug.Log($"[{currentDateTime}] {currentUser} - Rift Pull activated, remaining energy: {riftEnergy:F1}");
                    }
                    else
                    {
                        Debug.LogWarning($"[{currentDateTime}] {currentUser} - Cannot activate Rift Pull - Cooldown: {abilityCooldowns[RiftAbility.RiftPull]:F1}, Energy: {riftEnergy:F1}");
                    }
                    break;

                case GestureType.Cross:
                    if (abilityCooldowns[RiftAbility.ShadowClone] <= 0 && riftEnergy >= 40)
                    {
                        Debug.Log($"[{currentDateTime}] {currentUser} - Activating Shadow Clone");
                        ActivateShadowClone();
                        abilityCooldowns[RiftAbility.ShadowClone] = 20f;
                        riftEnergy -= 40f;
                        Debug.Log($"[{currentDateTime}] {currentUser} - Shadow Clone activated, remaining energy: {riftEnergy:F1}");
                    }
                    else
                    {
                        Debug.LogWarning($"[{currentDateTime}] {currentUser} - Cannot activate Shadow Clone - Cooldown: {abilityCooldowns[RiftAbility.ShadowClone]:F1}, Energy: {riftEnergy:F1}");
                    }
                    break;

                case GestureType.Circle:
                    if (abilityCooldowns[RiftAbility.TimeSlow] <= 0 && riftEnergy >= 50)
                    {
                        Debug.Log($"[{currentDateTime}] {currentUser} - Activating Time Slow");
                        ActivateTimeSlow();
                        abilityCooldowns[RiftAbility.TimeSlow] = 15f;
                        riftEnergy -= 50f;
                        Debug.Log($"[{currentDateTime}] {currentUser} - Time Slow activated, remaining energy: {riftEnergy:F1}");
                    }
                    else
                    {
                        Debug.LogWarning($"[{currentDateTime}] {currentUser} - Cannot activate Time Slow - Cooldown: {abilityCooldowns[RiftAbility.TimeSlow]:F1}, Energy: {riftEnergy:F1}");
                    }
                    break;
            }
        }

        private void UpdateAbilityCooldowns()
        {
            bool hasCooldowns = false;
            foreach (RiftAbility ability in abilityCooldowns.Keys)
            {
                if (abilityCooldowns[ability] > 0)
                {
                    abilityCooldowns[ability] -= Time.deltaTime;
                    hasCooldowns = true;
                    Debug.Log($"[{currentDateTime}] {currentUser} - Ability cooldown - {ability}: {abilityCooldowns[ability]:F1}s remaining");
                }
            }

            if (!hasCooldowns)
                Debug.Log($"[{currentDateTime}] {currentUser} - No ability cooldowns active");
        }

        private void ToggleRiftForm()
        {
            Debug.Log($"[{currentDateTime}] {currentUser} - ToggleRiftForm called, current state: {(isInRiftForm ? "Active" : "Inactive")}");

            if (isInRiftForm)
            {
                Debug.Log($"[{currentDateTime}] {currentUser} - Exiting Rift Form");
                ExitRiftForm();
            }
            else
            {
                if (!IsRequiredSpellEquipped())
                {
                    Debug.LogWarning($"[{currentDateTime}] {currentUser} - Required spell not equipped: {RiftFormConfig.RequiredSpellId}");
                    ShowMessage($"Rift Form requires {RiftFormConfig.RequiredSpellId} spell");
                    return;
                }

                if (currentCooldownTime > 0)
                {
                    float remainingMinutes = Mathf.Ceil(currentCooldownTime / 60f);
                    Debug.LogWarning($"[{currentDateTime}] {currentUser} - Rift Form on cooldown: {currentCooldownTime:F1}s remaining");
                    ShowMessage($"Cooldown: {remainingMinutes} minutes remaining");
                    return;
                }

                Debug.Log($"[{currentDateTime}] {currentUser} - Entering Rift Form");
                EnterRiftForm();
                currentCooldownTime = RiftFormConfig.CooldownTime;
                currentGracePeriod = RiftFormConfig.SpellCheckGracePeriod;
                Debug.Log($"[{currentDateTime}] {currentUser} - Cooldown set to {RiftFormConfig.CooldownTime}, grace period: {RiftFormConfig.SpellCheckGracePeriod}");
            }
        }

        private void EnterRiftForm()
        {
            Debug.Log($"[{currentDateTime}] {currentUser} - EnterRiftForm called");
            isInRiftForm = true;
            currentDuration = RiftFormConfig.Duration;
            Debug.Log($"[{currentDateTime}] {currentUser} - Duration set to {currentDuration}");

            // Apply rift materials
            Debug.Log($"[{currentDateTime}] {currentUser} - Applying rift materials to {characterRenderers.Count} renderers");
            foreach (Renderer renderer in characterRenderers)
            {
                if (renderer != null)
                {
                    Material[] newMats = new Material[renderer.materials.Length];
                    for (int i = 0; i < newMats.Length; i++)
                        newMats[i] = new Material(riftMaterial);
                    renderer.materials = newMats;
                    Debug.Log($"[{currentDateTime}] {currentUser} - Applied rift material to {renderer.name}");
                }
            }

            // Play effects
            Debug.Log($"[{currentDateTime}] {currentUser} - Spawning effect: {RiftFormConfig.EffectId}");
            EffectData effectData = Catalog.GetData<EffectData>(RiftFormConfig.EffectId);
            if (effectData != null && creature != null)
            {
                riftEffect = effectData.Spawn(creature.transform);
                if (riftEffect != null)
                {
                    riftEffect.Play();
                    Debug.Log($"[{currentDateTime}] {currentUser} - Effect played successfully");
                }
                else
                {
                    Debug.LogError($"[{currentDateTime}] {currentUser} - Failed to spawn effect instance");
                }
            }
            else
            {
                Debug.LogError($"[{currentDateTime}] {currentUser} - Failed to get effect data: {RiftFormConfig.EffectId} or creature is null");
            }

            EnableGodPowers();
            ShowMessage("RIFT FORM ACTIVATED");
        }

        private void ExitRiftForm()
        {
            Debug.Log($"[{currentDateTime}] {currentUser} - ExitRiftForm called");
            isInRiftForm = false;
            isFlying = false;

            // Restore original materials
            Debug.Log($"[{currentDateTime}] {currentUser} - Restoring original materials");
            for (int i = 0; i < characterRenderers.Count; i++)
            {
                if (characterRenderers[i] != null && i < originalMaterials.Count)
                {
                    characterRenderers[i].materials = originalMaterials[i];
                    Debug.Log($"[{currentDateTime}] {currentUser} - Restored materials for {characterRenderers[i].name}");
                }
            }

            // Stop effect
            Debug.Log($"[{currentDateTime}] {currentUser} - Stopping effects");
            if (riftEffect != null)
            {
                riftEffect.Stop();
                riftEffect = null;
            }

            DisableGodPowers();
            CleanupShadowClones();
            ShowMessage("RIFT FORM DEACTIVATED");
        }

        private void EnableGodPowers()
        {
            Debug.Log($"[{currentDateTime}] {currentUser} - EnableGodPowers called");

            if (Player.currentCreature == null)
            {
                Debug.LogError($"[{currentDateTime}] {currentUser} - Player.currentCreature is null!");
                return;
            }

            wasUsingGravity = true; // Store default state
            Debug.Log($"[{currentDateTime}] {currentUser} - Setting physics modifiers");
            Player.currentCreature.SetPhysicModifier(this, 0f, 0f);
            Player.currentCreature.AddJointForceMultiplier(this,
                RiftFormConfig.PositionMultiplier,
                RiftFormConfig.RotationMultiplier);

            Debug.Log($"[{currentDateTime}] {currentUser} - Force multipliers set - Position: {RiftFormConfig.PositionMultiplier}, Rotation: {RiftFormConfig.RotationMultiplier}");
            isFlying = true;
            Debug.Log($"[{currentDateTime}] {currentUser} - Flying enabled");
        }

        private void DisableGodPowers()
        {
            Debug.Log($"[{currentDateTime}] {currentUser} - DisableGodPowers called");

            if (Player.currentCreature == null)
            {
                Debug.LogError($"[{currentDateTime}] {currentUser} - Player.currentCreature is null!");
                return;
            }

            Debug.Log($"[{currentDateTime}] {currentUser} - Resetting physics modifiers");
            Player.currentCreature.SetPhysicModifier(this, 1.0f, 1.0f);
            Player.currentCreature.RemoveJointForceMultiplier(this);

            // Restore phased colliders
            Debug.Log($"[{currentDateTime}] {currentUser} - Restoring {phasedColliders.Count} phased colliders");
            foreach (var pair in phasedColliders)
            {
                if (pair.Key != null)
                {
                    pair.Key.isTrigger = false;
                    Debug.Log($"[{currentDateTime}] {currentUser} - Reset collider: {pair.Key.gameObject.name}");
                }
            }
            phasedColliders.Clear();
        }

        private void CleanupShadowClones()
        {
            Debug.Log($"[{currentDateTime}] {currentUser} - CleanupShadowClones called, {shadowClones.Count} clones");

            // Cleanup shadow clones
            foreach (Creature clone in shadowClones)
            {
                if (clone != null)
                {
                    clone.Kill();
                    Destroy(clone.gameObject, 3f);
                    Debug.Log($"[{currentDateTime}] {currentUser} - Killed and scheduled destruction of clone");
                }
            }
            shadowClones.Clear();
        }

        private void CheckClappingGesture()
        {
            if (gestureTimer > 0) return;

            if (Player.local == null)
            {
                Debug.LogWarning($"[{currentDateTime}] {currentUser} - Player.local is null in CheckClappingGesture");
                return;
            }

            bool leftGrip = Player.local.handLeft.controlHand.gripPressed;
            bool rightGrip = Player.local.handRight.controlHand.gripPressed;

            if (leftGrip && rightGrip)
            {
                Vector3 leftPos = Player.local.handLeft.transform.position;
                Vector3 rightPos = Player.local.handRight.transform.position;
                float handDistance = Vector3.Distance(leftPos, rightPos);

                Debug.Log($"[{currentDateTime}] {currentUser} - Clap check - Hand distance: {handDistance:F3}, limit: {RiftFormConfig.GestureDistance:F3}");

                if (handDistance < RiftFormConfig.GestureDistance)
                {
                    float leftVelocity = Player.local.handLeft.ragdollHand.physicBody.velocity.magnitude;
                    float rightVelocity = Player.local.handRight.ragdollHand.physicBody.velocity.magnitude;

                    Debug.Log($"[{currentDateTime}] {currentUser} - Clap velocities - Left: {leftVelocity:F1}, Right: {rightVelocity:F1}, required: {RiftFormConfig.GestureVelocity:F1}");

                    if (leftVelocity > RiftFormConfig.GestureVelocity &&
                        rightVelocity > RiftFormConfig.GestureVelocity)
                    {
                        gestureTimer = RiftFormConfig.GestureCooldown;
                        Debug.Log($"[{currentDateTime}] {currentUser} - Clap gesture detected! Setting cooldown: {RiftFormConfig.GestureCooldown:F1}s");
                        ToggleRiftForm();
                    }
                }
            }
        }

        private void OnCreatureDamageEvent(CollisionInstance collisionInstance, EventTime eventTime)
        {
            Debug.Log($"[{currentDateTime}] {currentUser} - OnCreatureDamageEvent - isInRiftForm: {isInRiftForm}, eventTime: {eventTime}");

            if (!isInRiftForm || eventTime != EventTime.OnStart || !collisionInstance.active)
            {
                Debug.Log($"[{currentDateTime}] {currentUser} - Skipping damage event processing");
                return;
            }

            if (collisionInstance.sourceCollider != null &&
               (collisionInstance.sourceCollider.name.Contains("Hand") ||
                collisionInstance.sourceCollider.name.Contains("Fist")))
            {

                // Enhance punch damage
                float baseDamage = collisionInstance.damageStruct.baseDamage;
                float newDamage = baseDamage * RiftFormConfig.PunchMultiplier;
                Debug.Log($"[{currentDateTime}] {currentUser} - Enhancing punch damage: {baseDamage:F1} → {newDamage:F1} (multiplier: {RiftFormConfig.PunchMultiplier:F1})");
                collisionInstance.damageStruct.baseDamage = newDamage;

                // Spawn effect at punch point
                SpawnPunchEffect(collisionInstance);
            }
        }

        private void SpawnPunchEffect(CollisionInstance collision)
        {
            Debug.Log($"[{currentDateTime}] {currentUser} - SpawnPunchEffect called at: {collision.contactPoint}");

            // Create temporary object at contact point
            GameObject tempObj = new GameObject("PunchEffect");
            tempObj.transform.position = collision.contactPoint;
            tempObj.transform.rotation = Quaternion.LookRotation(collision.contactNormal);
            Debug.Log($"[{currentDateTime}] {currentUser} - Created temp object at {collision.contactPoint}");

            // Play effect
            EffectData effectData = Catalog.GetData<EffectData>("RiftPunchEffect");
            if (effectData != null)
            {
                Debug.Log($"[{currentDateTime}] {currentUser} - Found effect data: RiftPunchEffect");
                EffectInstance effect = effectData.Spawn(tempObj.transform);
                effect.Play();
                Destroy(tempObj, 3.0f);
                Debug.Log($"[{currentDateTime}] {currentUser} - Punch effect played, temp object will be destroyed in 3s");

                // Add force to hit object
                if (collision.targetCollider?.attachedRigidbody != null)
                {
                    Vector3 force = collision.contactNormal * -30f;
                    collision.targetCollider.attachedRigidbody.AddForce(force, ForceMode.Impulse);
                    Debug.Log($"[{currentDateTime}] {currentUser} - Added force: {force} to hit object");
                }
                else
                {
                    Debug.Log($"[{currentDateTime}] {currentUser} - No rigidbody found to apply force");
                }
            }
            else
            {
                Debug.LogError($"[{currentDateTime}] {currentUser} - Could not find effect: RiftPunchEffect");
                Destroy(tempObj);
            }
        }

        // Ability implementation
        private void ActivateRiftPush()
        {
            Debug.Log($"[{currentDateTime}] {currentUser} - ActivateRiftPush called");
            ShowMessage("RIFT PUSH");

            if (Player.local == null)
            {
                Debug.LogError($"[{currentDateTime}] {currentUser} - Player.local is null in ActivateRiftPush");
                return;
            }

            Vector3 pushDir = Player.local.head.transform.forward;
            Debug.Log($"[{currentDateTime}] {currentUser} - Push direction: {pushDir}");

            // Find objects in front of player
            RaycastHit[] hits = Physics.SphereCastAll(
                Player.local.head.transform.position, 2f, pushDir, 10f);
            Debug.Log($"[{currentDateTime}] {currentUser} - Found {hits.Length} objects to potentially push");

            int pushedCreatures = 0;
            int pushedItems = 0;

            foreach (RaycastHit hit in hits)
            {
                // Apply force to creatures
                Creature hitCreature = hit.collider.GetComponentInParent<Creature>();
                if (hitCreature != null && hitCreature != creature)
                {
                    Debug.Log($"[{currentDateTime}] {currentUser} - Pushing creature: {hitCreature.name}");
                    foreach (RagdollPart part in hitCreature.ragdoll.parts)
                    {
                        if (part.physicBody != null)
                        {
                            part.physicBody.rigidBody.AddForce(pushDir * 20f, ForceMode.Impulse);
                            Debug.Log($"[{currentDateTime}] {currentUser} - Applied force to {part.name}: {pushDir * 20f}");
                        }
                    }
                    pushedCreatures++;
                }

                // Apply force to items
                Item hitItem = hit.collider.GetComponentInParent<Item>();
                if (hitItem != null && hitItem.physicBody != null)
                {
                    hitItem.physicBody.rigidBody.AddForce(pushDir * 15f, ForceMode.Impulse);
                    Debug.Log($"[{currentDateTime}] {currentUser} - Pushed item: {hitItem.name} with force: {pushDir * 15f}");
                    pushedItems++;
                }
            }

            Debug.Log($"[{currentDateTime}] {currentUser} - Rift Push results - Creatures: {pushedCreatures}, Items: {pushedItems}");

            // Play effect
            EffectData pushEffect = Catalog.GetData<EffectData>("RiftPushEffect");
            if (pushEffect != null)
            {
                Vector3 spawnPos = Player.local.transform.position + pushDir * 1f;
                EffectInstance effect = pushEffect.Spawn(spawnPos, Quaternion.LookRotation(pushDir));
                effect.Play();
                Debug.Log($"[{currentDateTime}] {currentUser} - Played push effect");
            }
            else
            {
                Debug.LogError($"[{currentDateTime}] {currentUser} - Could not find effect: RiftPushEffect");
            }
        }

        private void ActivateRiftPull()
        {
            Debug.Log($"[{currentDateTime}] {currentUser} - ActivateRiftPull called");
            ShowMessage("RIFT PULL");

            if (Player.local == null)
            {
                Debug.LogError($"[{currentDateTime}] {currentUser} - Player.local is null in ActivateRiftPull");
                return;
            }

            Vector3 pullDir = Player.local.head.transform.forward;
            Vector3 playerPos = Player.local.transform.position;
            Debug.Log($"[{currentDateTime}] {currentUser} - Pull direction: {pullDir}, player position: {playerPos}");

            Collider[] colliders = Physics.OverlapSphere(playerPos + pullDir * 5f, 5f);
            Debug.Log($"[{currentDateTime}] {currentUser} - Found {colliders.Length} objects in pull radius");

            int pulledCreatures = 0;
            int pulledItems = 0;

            foreach (Collider col in colliders)
            {
                // Pull creatures
                Creature hitCreature = col.GetComponentInParent<Creature>();
                if (hitCreature != null && hitCreature != creature)
                {
                    Debug.Log($"[{currentDateTime}] {currentUser} - Starting pull coroutine for creature: {hitCreature.name}");
                    StartCoroutine(PullCreature(hitCreature, playerPos));
                    pulledCreatures++;
                }

                // Pull items
                Item hitItem = col.GetComponentInParent<Item>();
                if (hitItem != null && hitItem.physicBody != null && !hitItem.IsHanded())
                {
                    Vector3 pullVector = (playerPos - hitItem.transform.position).normalized * 10f;
                    hitItem.physicBody.rigidBody.AddForce(pullVector, ForceMode.Impulse);
                    Debug.Log($"[{currentDateTime}] {currentUser} - Pulled item: {hitItem.name} with force: {pullVector}");
                    pulledItems++;
                }
            }

            Debug.Log($"[{currentDateTime}] {currentUser} - Rift Pull results - Creatures: {pulledCreatures}, Items: {pulledItems}");

            // Play effect
            EffectData pullEffect = Catalog.GetData<EffectData>("RiftPullEffect");
            if (pullEffect != null)
            {
                Vector3 spawnPos = playerPos + pullDir * 2f;
                EffectInstance effect = pullEffect.Spawn(spawnPos, Quaternion.LookRotation(-pullDir));
                effect.Play();
                Debug.Log($"[{currentDateTime}] {currentUser} - Played pull effect");
            }
            else
            {
                Debug.LogError($"[{currentDateTime}] {currentUser} - Could not find effect: RiftPullEffect");
            }
        }

        private IEnumerator PullCreature(Creature creature, Vector3 targetPos)
        {
            Debug.Log($"[{currentDateTime}] {currentUser} - PullCreature coroutine started for: {creature?.name ?? "null"}");

            if (creature == null)
            {
                Debug.LogError($"[{currentDateTime}] {currentUser} - Creature is null in PullCreature");
                yield break;
            }

            float pullDuration = 1.0f;
            float elapsed = 0f;
            Vector3 pullDirection = (targetPos - creature.transform.position).normalized;
            Debug.Log($"[{currentDateTime}] {currentUser} - Pull direction: {pullDirection}, duration: {pullDuration}s");

            while (elapsed < pullDuration && creature != null && !creature.isKilled)
            {
                if (creature.ragdoll.GetPart(RagdollPart.Type.Torso)?.physicBody != null)
                {
                    Vector3 force = pullDirection * 8f;
                    creature.ragdoll.GetPart(RagdollPart.Type.Torso).physicBody.rigidBody.AddForce(force, ForceMode.Impulse);
                    Debug.Log($"[{currentDateTime}] {currentUser} - Applied pull force: {force}, elapsed: {elapsed:F2}s");
                }
                else
                {
                    Debug.LogWarning($"[{currentDateTime}] {currentUser} - Could not find torso part for creature");
                }

                elapsed += Time.deltaTime;
                yield return new WaitForFixedUpdate();
            }

            Debug.Log($"[{currentDateTime}] {currentUser} - PullCreature coroutine complete after {elapsed:F2}s");
        }

        private void ActivateTimeSlow()
        {
            Debug.Log($"[{currentDateTime}] {currentUser} - ActivateTimeSlow called");
            ShowMessage("TIME SLOW");

            // Store original time scale
            float originalTimeScale = Time.timeScale;
            Debug.Log($"[{currentDateTime}] {currentUser} - Original time scale: {originalTimeScale}, setting to 0.3");
            Time.timeScale = 0.3f;
            Time.fixedDeltaTime = 0.02f * 0.3f;

            // Play effect
            EffectData timeEffect = Catalog.GetData<EffectData>("TimeSlowEffect");
            if (timeEffect != null)
            {
                EffectInstance effect = timeEffect.Spawn(creature.transform);
                effect.Play();
                Debug.Log($"[{currentDateTime}] {currentUser} - Played time slow effect");
            }
            else
            {
                Debug.LogError($"[{currentDateTime}] {currentUser} - Could not find effect: TimeSlowEffect");
            }

            // Restore time after 5 seconds (unscaled)
            Debug.Log($"[{currentDateTime}] {currentUser} - Starting restore time coroutine for 5s (realtime)");
            StartCoroutine(RestoreTimeScale(5f, originalTimeScale));
        }

        private IEnumerator RestoreTimeScale(float duration, float originalScale)
        {
            Debug.Log($"[{currentDateTime}] {currentUser} - RestoreTimeScale coroutine started, waiting {duration}s");
            yield return new WaitForSecondsRealtime(duration);

            Time.timeScale = originalScale;
            Time.fixedDeltaTime = 0.02f;
            Debug.Log($"[{currentDateTime}] {currentUser} - Time scale restored to {originalScale}");
            ShowMessage("Time restored");
        }

        private void ActivateShadowClone()
        {
            Debug.Log($"[{currentDateTime}] {currentUser} - ActivateShadowClone called");
            ShowMessage("SHADOW CLONE");

            if (creature == null)
            {
                Debug.LogError($"[{currentDateTime}] {currentUser} - Creature is null in ActivateShadowClone");
                return;
            }

            // Create clone on each side of the player
            Vector3 playerPos = creature.transform.position;
            Vector3 playerFwd = creature.transform.forward;

            Vector3 leftPos = playerPos - playerFwd * 1f + creature.transform.right * -1.5f;
            Vector3 rightPos = playerPos - playerFwd * 1f + creature.transform.right * 1.5f;

            Debug.Log($"[{currentDateTime}] {currentUser} - Creating clones at Left: {leftPos}, Right: {rightPos}");

            CreateShadowClone(leftPos, creature.transform.rotation);
            CreateShadowClone(rightPos, creature.transform.rotation);

            // Play effect
            EffectData cloneEffect = Catalog.GetData<EffectData>("ShadowCloneEffect");
            if (cloneEffect != null)
            {
                EffectInstance effect = cloneEffect.Spawn(creature.transform);
                effect.Play();
                Debug.Log($"[{currentDateTime}] {currentUser} - Played shadow clone effect");
            }
            else
            {
                Debug.LogError($"[{currentDateTime}] {currentUser} - Could not find effect: ShadowCloneEffect");
            }
        }

        private void CreateShadowClone(Vector3 position, Quaternion rotation)
        {
            Debug.Log($"[{currentDateTime}] {currentUser} - CreateShadowClone called at position: {position}");

            try
            {
                // Try to create a clone by spawning a new creature from prefab
                Debug.Log($"[{currentDateTime}] {currentUser} - Getting creature data for ID: {creature.data.id}");
                CreatureData creatureData = Catalog.GetData<CreatureData>(creature.data.id);

                // Get the creature prefab from data
                if (creatureData != null)
                {
                    Debug.Log($"[{currentDateTime}] {currentUser} - Creating clone GameObject");
                    // This is a workaround since we can't use InstantiateCreature directly
                    // We'll create our own clone from the creature components
                    GameObject cloneObj = new GameObject("ShadowClone");
                    cloneObj.transform.position = position;
                    cloneObj.transform.rotation = rotation;

                    // Copy the player's ragdoll to make a basic clone
                    Debug.Log($"[{currentDateTime}] {currentUser} - Adding Creature component");
                    Creature clone = cloneObj.AddComponent<Creature>();
                    clone.data = creatureData;

                    // Add to tracking
                    shadowClones.Add(clone);
                    Debug.Log($"[{currentDateTime}] {currentUser} - Shadow clone created and added to tracking list (total: {shadowClones.Count})");

                    // Apply shadow material
                    ApplyShadowMaterial(clone);

                    // Set behavior
                    Debug.Log($"[{currentDateTime}] {currentUser} - Starting clone management coroutine");
                    StartCoroutine(ManageShadowClone(clone));
                }
                else
                {
                    Debug.LogError($"[{currentDateTime}] {currentUser} - Could not get CreatureData for ID: {creature.data.id}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[{currentDateTime}] {currentUser} - Clone creation error: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private void ApplyShadowMaterial(Creature clone)
        {
            Debug.Log($"[{currentDateTime}] {currentUser} - ApplyShadowMaterial called for clone");

            Renderer[] renderers = clone.GetComponentsInChildren<Renderer>();
            Debug.Log($"[{currentDateTime}] {currentUser} - Found {renderers.Length} renderers on clone");

            foreach (Renderer r in renderers)
            {
                if (r != null)
                {
                    Material[] newMats = new Material[r.materials.Length];
                    for (int i = 0; i < newMats.Length; i++)
                        newMats[i] = new Material(riftMaterial);
                    r.materials = newMats;
                    Debug.Log($"[{currentDateTime}] {currentUser} - Applied shadow material to renderer: {r.name}");
                }
            }
        }

        private IEnumerator ManageShadowClone(Creature clone)
        {
            Debug.Log($"[{currentDateTime}] {currentUser} - ManageShadowClone coroutine started");

            float lifetime = 15f;
            float elapsed = 0f;

            while (elapsed < lifetime && clone != null && !clone.isKilled)
            {
                // Find nearby enemies
                Creature target = null;
                float closestDist = 10f;

                Collider[] nearby = Physics.OverlapSphere(clone.transform.position, 10f);
                Debug.Log($"[{currentDateTime}] {currentUser} - Clone scanning {nearby.Length} nearby colliders for targets");

                foreach (Collider col in nearby)
                {
                    Creature c = col.GetComponentInParent<Creature>();
                    if (c != null && c != creature && c != clone && !c.isPlayer)
                    {
                        float dist = Vector3.Distance(clone.transform.position, c.transform.position);
                        if (dist < closestDist)
                        {
                            closestDist = dist;
                            target = c;
                            Debug.Log($"[{currentDateTime}] {currentUser} - Clone found potential target: {c.name} at distance {dist:F2}");
                        }
                    }
                }

                // Attack target or follow player
                if (target != null)
                {
                    Debug.Log($"[{currentDateTime}] {currentUser} - Clone targeting creature: {target.name} at distance {closestDist:F2}");

                    // Move toward target
                    Vector3 direction = (target.transform.position - clone.transform.position).normalized;
                    clone.transform.rotation = Quaternion.Slerp(
                        clone.transform.rotation,
                        Quaternion.LookRotation(direction),
                        Time.deltaTime * 5f);

                    if (closestDist > 2f)
                    {
                        clone.transform.position += direction * Time.deltaTime * 3f;
                        Debug.Log($"[{currentDateTime}] {currentUser} - Clone moving toward target, distance: {closestDist:F2}");
                    }
                    else
                    {
                        Debug.Log($"[{currentDateTime}] {currentUser} - Clone attacking target");
                        // Attack - fixed to use float damage value
                        float damage = 10f;
                        target.Damage(damage);
                        Debug.Log($"[{currentDateTime}] {currentUser} - Clone dealt {damage} damage to target");

                        yield return new WaitForSeconds(1f);
                    }
                }
                else
                {
                    Debug.Log($"[{currentDateTime}] {currentUser} - No target found, clone following player");

                    // Follow player
                    Vector3 targetPos = creature.transform.position -
                                      creature.transform.forward * 2f +
                                      UnityEngine.Random.insideUnitSphere * 1.5f;
                    targetPos.y = clone.transform.position.y;

                    clone.transform.position = Vector3.Lerp(
                        clone.transform.position,
                        targetPos,
                        Time.deltaTime * 2f);

                    clone.transform.rotation = Quaternion.Slerp(
                        clone.transform.rotation,
                        creature.transform.rotation,
                        Time.deltaTime * 2f);
                }

                elapsed += Time.deltaTime;
                Debug.Log($"[{currentDateTime}] {currentUser} - Clone lifetime: {elapsed:F1}/{lifetime:F1}");
                yield return null;
            }

            // Dissolve and destroy
            Debug.Log($"[{currentDateTime}] {currentUser} - Clone lifecycle ending after {elapsed:F1}s");

            if (clone != null)
            {
                shadowClones.Remove(clone);
                clone.Kill();
                Destroy(clone.gameObject, 3f);
                Debug.Log($"[{currentDateTime}] {currentUser} - Clone killed and scheduled for destruction");
            }
        }

        private void ShowMessage(string message)
        {
            Debug.Log($"[{currentDateTime}] {currentUser} - MESSAGE: {message}");
            if (Player.local != null)
                Player.local.StartCoroutine(DisplayMessageCoroutine(message, 3.0f));
            else
                Debug.LogError($"[{currentDateTime}] {currentUser} - Cannot display message, Player.local is null");
        }

        private IEnumerator DisplayMessageCoroutine(string message, float duration)
        {
            Debug.Log($"[{currentDateTime}] {currentUser} - DisplayMessageCoroutine: \"{message}\" for {duration}s");

            if (Player.currentCreature != null && Player.currentCreature == Player.local.creature)
            {
                GameObject messageObj = new GameObject("MessageCanvas");
                Canvas canvas = messageObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                Debug.Log($"[{currentDateTime}] {currentUser} - Created message canvas");

                GameObject textObj = new GameObject("MessageText");
                textObj.transform.SetParent(messageObj.transform, false);
                TMPro.TextMeshProUGUI messageText = textObj.AddComponent<TMPro.TextMeshProUGUI>();

                messageText.text = message;
                messageText.fontSize = 36;
                messageText.alignment = TMPro.TextAlignmentOptions.Center;
                Debug.Log($"[{currentDateTime}] {currentUser} - Set up message text");

                messageObj.transform.SetParent(Player.local.head.cam.transform, false);
                messageObj.transform.localPosition = new Vector3(0, -0.3f, 2f);
                messageObj.transform.localRotation = Quaternion.identity;
                messageObj.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);

                RectTransform rectTransform = textObj.GetComponent<RectTransform>();
                rectTransform.sizeDelta = new Vector2(200, 50);
                Debug.Log($"[{currentDateTime}] {currentUser} - Message positioned in front of player");

                yield return new WaitForSeconds(duration);
                if (messageObj != null)
                {
                    Destroy(messageObj);
                    Debug.Log($"[{currentDateTime}] {currentUser} - Message destroyed after {duration}s");
                }
            }
            else
            {
                Debug.LogError($"[{currentDateTime}] {currentUser} - Cannot display message - Player.currentCreature is null or not local player");
            }
        }

        private void OnDestroy()
        {
            Debug.Log($"[{currentDateTime}] {currentUser} - RiftForm component being destroyed");

            if (isInRiftForm)
            {
                Debug.Log($"[{currentDateTime}] {currentUser} - Still in Rift Form, exiting first");
                ExitRiftForm();
            }

            if (creature != null)
            {
                Debug.Log($"[{currentDateTime}] {currentUser} - Unsubscribing from damage events");
                creature.OnDamageEvent -= OnCreatureDamageEvent;
            }

            Debug.Log($"[{currentDateTime}] {currentUser} - RiftForm destroyed");
        }
    }
}