using ThunderRoad;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ShadowArmory
{
    /// <summary>
    /// Advanced shadow binding spell that immobilizes enemies with shadow chains
    /// </summary>
    public class ShadowBind : MonoBehaviour
    {
        private readonly string currentUser = "SuoperJust";
        private readonly string currentDateTime = "2025-04-12 23:27:04";

        // Core references
        private Creature creature;
        private SpellCaster spellCaster;
        private ShadowBindEffect bindEffect;

        // Targeting and charging state
        private bool isCharging = false;
        private float chargeStartTime = 0f;
        private float chargeLevel = 0f;
        private Side chargingSide;
        private Transform targetTransform;
        private Creature targetCreature;
        private Dictionary<Creature, float> targetScores = new Dictionary<Creature, float>();
        private List<Creature> boundCreatures = new List<Creature>();
        private float lastTargetUpdateTime = 0f;

        // Visual effects
        private EffectInstance handChargingEffect;
        private EffectInstance targetingEffect;
        private EffectInstance chainEffect;
        private List<EffectInstance> activeEffects = new List<EffectInstance>();
        private Dictionary<Creature, List<EffectInstance>> creatureEffects = new Dictionary<Creature, List<EffectInstance>>();

        // Enhanced targeting system
        private RaycastHit[] rayHits = new RaycastHit[20];
        private Collider[] sphereResults = new Collider[20];
        private List<Creature> nearbyCreatures = new List<Creature>();
        private readonly float TARGET_UPDATE_INTERVAL = 0.05f;

        public void Initialize(Creature owner)
        {
            creature = owner;
            bindEffect = new ShadowBindEffect();

            if (creature == null)
            {
                Debug.LogError($"[{currentDateTime}] {currentUser} - Cannot initialize ShadowBind: creature is null!");
                return;
            }

            // Setup spell caster component
            spellCaster = creature.GetComponent<SpellCaster>();
            if (spellCaster == null)
            {
                spellCaster = creature.gameObject.AddComponent<SpellCaster>();
            }

            // Add input handlers to both hands
            InputHandler leftHandler = creature.handLeft.gameObject.AddComponent<InputHandler>();
            InputHandler rightHandler = creature.handRight.gameObject.AddComponent<InputHandler>();
            leftHandler.Initialize(this, Side.Left);
            rightHandler.Initialize(this, Side.Right);

            // Pre-load effects
            PreloadEffects();

            Debug.Log($"[{currentDateTime}] {currentUser} - ShadowBind initialized for {creature.name}");
        }

        private void PreloadEffects()
        {
            // Preload effects to avoid hitches when first used
            EffectData chargingEffect = Catalog.GetData<EffectData>(ShadowBindConfig.ChargingEffectId);
            EffectData targetingEffect = Catalog.GetData<EffectData>(ShadowBindConfig.TargetingEffectId);
            EffectData chainEffect = Catalog.GetData<EffectData>(ShadowBindConfig.ChainEffectId);
            EffectData bindEffect = Catalog.GetData<EffectData>(ShadowBindConfig.BindEffectId);

            // Ensure effects exist
            if (chargingEffect == null) Debug.LogWarning($"[{currentDateTime}] {currentUser} - Missing effect: {ShadowBindConfig.ChargingEffectId}");
            if (targetingEffect == null) Debug.LogWarning($"[{currentDateTime}] {currentUser} - Missing effect: {ShadowBindConfig.TargetingEffectId}");
            if (chainEffect == null) Debug.LogWarning($"[{currentDateTime}] {currentUser} - Missing effect: {ShadowBindConfig.ChainEffectId}");
            if (bindEffect == null) Debug.LogWarning($"[{currentDateTime}] {currentUser} - Missing effect: {ShadowBindConfig.BindEffectId}");
        }

        /// <summary>
        /// Handles user input and trigger detection
        /// </summary>
        private class InputHandler : MonoBehaviour
        {
            private ShadowBind shadowBind;
            private Side side;
            private bool wasGripPressed = false;
            private bool wasAltUsePressed = false;
            private float buttonHoldStartTime = 0f;
            private bool isHolding = false;

            public void Initialize(ShadowBind owner, Side handSide)
            {
                shadowBind = owner;
                side = handSide;
            }

            private void Update()
            {
                if (shadowBind == null || PlayerControl.GetHand(side) == null) return;

                bool isGripPressed = PlayerControl.GetHand(side).gripPressed;
                bool isAltUsePressed = PlayerControl.GetHand(side).alternateUsePressed;

                // Detect button press combination
                if (isGripPressed && isAltUsePressed)
                {
                    // Start charging if not already
                    if (!isHolding)
                    {
                        buttonHoldStartTime = Time.time;
                        isHolding = true;
                    }

                    // After a small delay, activate the binding spell
                    if (isHolding && Time.time - buttonHoldStartTime > 0.2f && !shadowBind.isCharging)
                    {
                        shadowBind.DisableSpellWheel();
                        shadowBind.StartCharging(side);
                    }
                }
                else if (isHolding)
                {
                    // Release actions
                    isHolding = false;
                    if (shadowBind.isCharging && shadowBind.chargingSide == side)
                    {
                        shadowBind.ReleaseBind();
                        shadowBind.EnableSpellWheel();
                    }
                }

                // Store previous button states for next frame
                wasGripPressed = isGripPressed;
                wasAltUsePressed = isAltUsePressed;
            }
        }

        private void DisableSpellWheel()
        {
            if (spellCaster != null)
            {
                spellCaster.DisableSpellWheel(this);
            }
        }

        private void EnableSpellWheel()
        {
            if (spellCaster != null)
            {
                spellCaster.AllowSpellWheel(this);
            }
        }

        private void StartCharging(Side side)
        {
            isCharging = true;
            chargeStartTime = Time.time;
            chargingSide = side;
            chargeLevel = 0f;

            // Initial hand effect
            PlayChargingEffect(side);

            // Start haptic feedback
            if (creature.player != null)
            {
                ApplyHapticPulse(side, 0.2f, 1.0f, 0.2f);
            }
        }

        private void Update()
        {
            if (!isCharging) return;

            // Update charge level
            float chargeElapsed = Time.time - chargeStartTime;
            chargeLevel = Mathf.Clamp01(chargeElapsed / ShadowBindConfig.ChargeTime);

            // Update targeting at fixed intervals for performance
            if (Time.time - lastTargetUpdateTime > TARGET_UPDATE_INTERVAL)
            {
                UpdateTargeting();
                lastTargetUpdateTime = Time.time;

                // Provide charging feedback
                float intensity = Mathf.Lerp(0.2f, 1.0f, chargeLevel);
                float frequency = Mathf.Lerp(0.5f, 2.0f, chargeLevel);

                if (creature.player != null)
                {
                    ApplyHapticPulse(chargingSide, intensity, frequency, 0.1f);
                }
            }

            // Scale/update charging effect
            if (handChargingEffect != null)
            {
                Transform handTransform = GetHandTransform(chargingSide);
                handChargingEffect.SetIntensity(chargeLevel);

                // Add subtle hand movement for immersion
                if (chargeLevel > 0.5f && handTransform != null)
                {
                    float pulsation = Mathf.Sin(Time.time * 10f) * 0.02f * chargeLevel;
                    Vector3 pulseDirection = handTransform.up * pulsation;

                    // Apply small force to hand for feedback
                    RagdollHand hand = chargingSide == Side.Left ? creature.handLeft : creature.handRight;
                    if (hand.physicBody != null)
                    {
                        hand.physicBody.AddForce(pulseDirection, ForceMode.Impulse);
                    }
                }
            }

            // Update chain effect if we have a target and are fully charged
            if (targetCreature != null && chargeLevel >= 1.0f)
            {
                UpdateChainEffect();

                // Provide strong haptic feedback when target is acquired and charged
                if (creature.player != null && Time.frameCount % 10 == 0)
                {
                    ApplyHapticPulse(chargingSide, 0.7f, 3.0f, 0.1f);
                }
            }
        }

        private void ReleaseBind()
        {
            // Check for success conditions
            bool chargeSuccess = chargeLevel >= 1.0f;
            bool targetSuccess = targetCreature != null && !targetCreature.isKilled;

            if (chargeSuccess && targetSuccess)
            {
                // Successful bind
                ApplyBind(targetCreature);

                if (creature.player != null)
                {
                    // Strong success feedback
                    ApplyHapticPulse(chargingSide, 1.0f, 1.0f, 0.5f);

                    // Also provide feedback to other hand
                    Side otherSide = chargingSide == Side.Left ? Side.Right : Side.Left;
                    ApplyHapticPulse(otherSide, 0.7f, 1.0f, 0.3f);
                }
            }
            else
            {
                // Failed attempt - just provide haptic feedback
                if (creature.player != null)
                {
                    // Weak failure feedback
                    ApplyHapticPulse(chargingSide, 0.3f, 3.0f, 0.2f);
                }
            }

            // Clean up effects
            CleanupEffects();

            // Reset state
            isCharging = false;
            targetCreature = null;
            targetTransform = null;
            chargeLevel = 0f;
        }

        private void UpdateTargeting()
        {
            Transform handTransform = GetHandTransform(chargingSide);
            if (handTransform == null) return;

            // Clear previous scores
            targetScores.Clear();

            // Find nearby creatures first (optimization)
            FindNearbyTargets(handTransform);

            // No creatures found? Exit early
            if (nearbyCreatures.Count == 0)
            {
                if (targetCreature != null)
                {
                    // Lost previous target
                    targetCreature = null;
                    targetTransform = null;
                    UpdateTargetingEffect();
                }
                return;
            }

            // Score all potential targets
            float bestScore = -1f;
            Creature bestTarget = null;

            foreach (Creature target in nearbyCreatures)
            {
                // Skip invalid targets
                if (target == null || target == creature || target.isKilled) continue;

                // Calculate targeting score
                float score = CalculateTargetingScore(target, handTransform);

                // Track score
                targetScores[target] = score;

                // Keep track of best candidate
                if (score > bestScore)
                {
                    bestScore = score;
                    bestTarget = target;
                }
            }

            // Apply minimum threshold for targeting
            if (bestScore < 0.3f)
            {
                bestTarget = null;
            }

            // Check if target changed
            if (bestTarget != targetCreature)
            {
                targetCreature = bestTarget;
                targetTransform = bestTarget?.transform;

                // Update targeting effect
                UpdateTargetingEffect();

                // Provide haptic feedback for new target
                if (bestTarget != null && creature.player != null)
                {
                    // Target acquired haptic feedback
                    ApplyHapticPulse(chargingSide, 0.5f, 2.0f, 0.2f);
                }
            }
        }

        private void FindNearbyTargets(Transform handTransform)
        {
            nearbyCreatures.Clear();

            // First do a quick sphere check
            int hitCount = Physics.OverlapSphereNonAlloc(
                handTransform.position,
                ShadowBindConfig.BindRange,
                sphereResults,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore
            );

            HashSet<Creature> potentialCreatures = new HashSet<Creature>();

            // Process sphere results
            for (int i = 0; i < hitCount; i++)
            {
                Creature hitCreature = sphereResults[i].GetComponentInParent<Creature>();
                if (hitCreature != null && !hitCreature.isPlayer && !hitCreature.isKilled)
                {
                    potentialCreatures.Add(hitCreature);
                }
            }

            // Then check forward cone with rays
            Vector3 rayOrigin = handTransform.position;
            Vector3 rayForward = handTransform.forward;

            // Cast rays in a cone pattern
            for (int i = 0; i < 5; i++)
            {
                // Center ray
                if (i == 0)
                {
                    Physics.RaycastNonAlloc(rayOrigin, rayForward, rayHits, ShadowBindConfig.BindRange * 1.5f);
                }
                // Cone rays
                else
                {
                    float angle = i * 10f; // 10, 20, 30, 40 degrees
                    Vector3 direction = Quaternion.AngleAxis(angle, handTransform.up) * rayForward;
                    Physics.RaycastNonAlloc(rayOrigin, direction, rayHits, ShadowBindConfig.BindRange);

                    direction = Quaternion.AngleAxis(-angle, handTransform.up) * rayForward;
                    Physics.RaycastNonAlloc(rayOrigin, direction, rayHits, ShadowBindConfig.BindRange);

                    direction = Quaternion.AngleAxis(angle, handTransform.right) * rayForward;
                    Physics.RaycastNonAlloc(rayOrigin, direction, rayHits, ShadowBindConfig.BindRange);

                    direction = Quaternion.AngleAxis(-angle, handTransform.right) * rayForward;
                    Physics.RaycastNonAlloc(rayOrigin, direction, rayHits, ShadowBindConfig.BindRange);
                }

                // Process ray hits
                foreach (RaycastHit hit in rayHits)
                {
                    if (hit.collider == null) continue;

                    Creature hitCreature = hit.collider.GetComponentInParent<Creature>();
                    if (hitCreature != null && !hitCreature.isPlayer && !hitCreature.isKilled)
                    {
                        potentialCreatures.Add(hitCreature);
                    }
                }
            }

            // Add all found creatures to result list
            foreach (Creature c in potentialCreatures)
            {
                nearbyCreatures.Add(c);
            }
        }

        private float CalculateTargetingScore(Creature target, Transform handTransform)
        {
            // Get head for better targeting
            RagdollPart head = target.ragdoll.GetPart(RagdollPart.Type.Head);
            Transform targetPoint = head != null ? head.transform : target.transform;

            // Calculate base metrics
            Vector3 directionToTarget = (targetPoint.position - handTransform.position);
            float distance = directionToTarget.magnitude;
            float angle = Vector3.Angle(handTransform.forward, directionToTarget.normalized);

            // Calculate scores (higher is better)
            float distanceScore = 1.0f - Mathf.Clamp01(distance / ShadowBindConfig.BindRange);
            float angleScore = 1.0f - Mathf.Clamp01(angle / ShadowBindConfig.TargetingAngle);

            // Line of sight check with weight
            float visibilityScore = 0f;
            if (Physics.Raycast(handTransform.position, directionToTarget.normalized, out RaycastHit hit, distance))
            {
                Creature hitCreature = hit.collider.GetComponentInParent<Creature>();
                visibilityScore = (hitCreature == target) ? 1.0f : 0.0f;
            }

            // Additional factors
            float targetingWeight = 0f;

            // Bonus for enemies that are already targeting the player
            if (target.brain.currentTarget == creature)
            {
                targetingWeight = 0.3f;
            }

            // Bonus for enemies that are close to the player
            float proximityToPlayer = 1.0f - Mathf.Clamp01(
                Vector3.Distance(target.transform.position, creature.transform.position) / 5.0f);
            targetingWeight += proximityToPlayer * 0.2f;

            // Bonus for enemies in front of the player (not just the hand)
            Vector3 playerToTarget = (target.transform.position - creature.transform.position).normalized;
            float playerFacingScore = Vector3.Dot(creature.transform.forward, playerToTarget);
            playerFacingScore = Mathf.Clamp01(playerFacingScore);
            targetingWeight += playerFacingScore * 0.2f;

            // Combine all factors with appropriate weights
            float finalScore = (distanceScore * 0.4f) +
                              (angleScore * 0.4f) +
                              (visibilityScore * 0.4f) +
                              targetingWeight;

            // Penalty for already bound creatures
            if (boundCreatures.Contains(target))
            {
                finalScore *= 0.2f;
            }

            return finalScore;
        }

        private void ApplyBind(Creature target)
        {
            if (target == null) return;

            // Add to bound creatures list
            if (!boundCreatures.Contains(target))
            {
                boundCreatures.Add(target);
            }

            // Create bind effect
            bindEffect.CreateBindEffect(target);

            // Pause brain and handle ragdoll
            ManageTargetState(target, true);

            // Start coroutine to manage bind duration and effects
            StartCoroutine(ManageBindCoroutine(target, ShadowBindConfig.BindDuration));

            // Play animation effects
            PlayBindingEffects(target);
        }

        private void ManageTargetState(Creature target, bool binding)
        {
            if (target.brain != null)
            {
                if (binding)
                {
                    // Pause brain and animations
                    target.brain.StopAllCoroutines();

                    // Set ragdoll to destabilized for better binding effect
                    target.ragdoll.SetState(Ragdoll.State.Destabilized);

                    // Freeze creature's weapons to prevent them from being used
                    foreach (Handle handle in target.handLeft.GetComponentsInChildren<Handle>())
                    {
                        if (handle.item != null)
                        {
                            handle.item.physicBody.isKinematic = true;
                        }
                    }
                    foreach (Handle handle in target.handRight.GetComponentsInChildren<Handle>())
                    {
                        if (handle.item != null)
                        {
                            handle.item.physicBody.isKinematic = true;
                        }
                    }

                    // Disable locomotion
                    if (target.locomotion != null)
                    {
                        target.locomotion.enabled = false;
                    }
                }
                else
                {
                    // Re-enable brain and locomotion
                    target.brain.ResetBrain();

                    // Reset state
                    target.ragdoll.SetState(Ragdoll.State.Inert);

                    // Unfreeze weapons
                    foreach (Handle handle in target.handLeft.GetComponentsInChildren<Handle>())
                    {
                        if (handle.item != null)
                        {
                            handle.item.physicBody.isKinematic = false;
                        }
                    }
                    foreach (Handle handle in target.handRight.GetComponentsInChildren<Handle>())
                    {
                        if (handle.item != null)
                        {
                            handle.item.physicBody.isKinematic = false;
                        }
                    }

                    // Re-enable locomotion
                    if (target.locomotion != null)
                    {
                        target.locomotion.enabled = true;
                    }
                }
            }
        }

        private void PlayBindingEffects(Creature target)
        {
            // Create effect list if needed
            if (!creatureEffects.ContainsKey(target))
            {
                creatureEffects[target] = new List<EffectInstance>();
            }

            // Spawn binding effects at key points on the creature
            SpawnBindEffectAt(target, RagdollPart.Type.Head);
            SpawnBindEffectAt(target, RagdollPart.Type.Torso);
            SpawnBindEffectAt(target, RagdollPart.Type.LeftArm);
            SpawnBindEffectAt(target, RagdollPart.Type.RightArm);
            SpawnBindEffectAt(target, RagdollPart.Type.LeftLeg);
            SpawnBindEffectAt(target, RagdollPart.Type.RightLeg);
        }

        private void SpawnBindEffectAt(Creature target, RagdollPart.Type partType)
        {
            RagdollPart part = target.ragdoll.GetPart(partType);
            if (part == null) return;

            EffectData effectData = Catalog.GetData<EffectData>(ShadowBindConfig.BindEffectId);
            if (effectData != null)
            {
                EffectInstance effect = effectData.Spawn(part.transform);
                effect.Play();
                creatureEffects[target].Add(effect);
            }
        }

        private IEnumerator ManageBindCoroutine(Creature target, float duration)
        {
            if (target == null) yield break;

            float endTime = Time.time + duration;
            float lastPulseTime = 0f;

            while (Time.time < endTime && target != null && !target.isKilled)
            {
                bindEffect.UpdateChainPositions();

                // Apply upward force to keep target suspended
                foreach (RagdollPart part in target.ragdoll.parts)
                {
                    if (part.physicBody != null)
                    {
                        // Calculate force based on part type
                        float forceMultiplier = 1.0f;

                        // Apply higher force to torso and head
                        if (part.type == RagdollPart.Type.Torso) forceMultiplier = 2.0f;
                        else if (part.type == RagdollPart.Type.Head) forceMultiplier = 1.5f;

                        // Calculate upward force with some variation
                        Vector3 upForce = Vector3.up * ShadowBindConfig.HoldForce * forceMultiplier;

                        // Add small random wobble
                        upForce += new Vector3(
                            Random.Range(-0.1f, 0.1f),
                            0f,
                            Random.Range(-0.1f, 0.1f)
                        ) * ShadowBindConfig.HoldForce * 0.1f;

                        part.physicBody.AddForce(upForce, ForceMode.Force);

                        // Increase drag to make movement more restricted
                        part.physicBody.drag = 5f;
                        part.physicBody.angularDrag = 10f;
                    }
                }

                // Apply periodic stronger pulses
                if (Time.time - lastPulseTime > 0.75f)
                {
                    lastPulseTime = Time.time;

                    foreach (RagdollPart part in target.ragdoll.parts)
                    {
                        if (part.physicBody != null)
                        {
                            // Stronger pulse upward
                            Vector3 pulseDelta = Vector3.up * ShadowBindConfig.HoldForce * 0.5f;
                            part.physicBody.AddForce(pulseDelta, ForceMode.Impulse);

                            // Play pulse effect on torso
                            if (part.type == RagdollPart.Type.Torso)
                            {
                                EffectData pulseEffect = Catalog.GetData<EffectData>(ShadowBindConfig.ChainEffectId);
                                if (pulseEffect != null)
                                {
                                    EffectInstance effect = pulseEffect.Spawn(part.transform);
                                    effect.Play();
                                    creatureEffects[target].Add(effect);
                                }
                            }
                        }
                    }
                }

                // Warning flash near end of duration
                if (endTime - Time.time < 1.0f)
                {
                    float pulseFrequency = Mathf.Lerp(2f, 10f, 1.0f - (endTime - Time.time));
                    if (Time.time % (1f / pulseFrequency) < 0.1f)
                    {
                        foreach (EffectInstance effect in creatureEffects[target])
                        {
                            effect.SetIntensity(2.0f);
                        }
                    }
                    else
                    {
                        foreach (EffectInstance effect in creatureEffects[target])
                        {
                            effect.SetIntensity(0.7f);
                        }
                    }
                }

                yield return null;
            }

            // Cleanup when done
            CleanupTargetEffects(target);
            ManageTargetState(target, false);
            boundCreatures.Remove(target);

            // Give the enemy a brief recovery period
            if (target != null && target.brain != null)
            {
                target.brain.ResetBrain();
                target.brain.StopAllCoroutines();
                StartCoroutine(DelayBrainReset(target, 1.0f));
            }
        }

        private IEnumerator DelayBrainReset(Creature target, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (target != null && !target.isKilled && target.brain != null)
            {
                target.brain.ResetBrain();
            }
        }

        private void PlayChargingEffect(Side side)
        {
            Transform handTransform = GetHandTransform(side);

            EffectData chargingEffect = Catalog.GetData<EffectData>(ShadowBindConfig.ChargingEffectId);
            if (chargingEffect != null && handTransform != null)
            {
                handChargingEffect = chargingEffect.Spawn(handTransform);
                handChargingEffect.Play();
                activeEffects.Add(handChargingEffect);
            }
        }

        private void UpdateTargetingEffect()
        {
            if (targetingEffect != null)
            {
                targetingEffect.Stop();
                activeEffects.Remove(targetingEffect);
            }

            if (targetCreature != null)
            {
                EffectData targetEffect = Catalog.GetData<EffectData>(ShadowBindConfig.TargetingEffectId);
                if (targetEffect != null)
                {
                    RagdollPart torso = targetCreature.ragdoll.GetPart(RagdollPart.Type.Torso);
                    Transform effectParent = torso != null ? torso.transform : targetCreature.transform;

                    targetingEffect = targetEffect.Spawn(effectParent);
                    targetingEffect.Play();
                    activeEffects.Add(targetingEffect);
                }
            }
        }

        private void UpdateChainEffect()
        {
            Transform handTransform = GetHandTransform(chargingSide);

            if (chainEffect != null)
            {
                chainEffect.Stop();
                activeEffects.Remove(chainEffect);
            }

            if (targetCreature != null && handTransform != null)
            {
                EffectData chainData = Catalog.GetData<EffectData>(ShadowBindConfig.ChainEffectId);
                if (chainData != null)
                {
                    chainEffect = chainData.Spawn(handTransform);

                    // Try to set source and target - chain effect should support this
                    RagdollPart targetPart = targetCreature.ragdoll.GetPart(RagdollPart.Type.Torso) ??
                                            targetCreature.ragdoll.GetPart(RagdollPart.Type.Head);
                    Transform targetPoint = targetPart != null ? targetPart.transform : targetCreature.transform;

                    // Use reflection if needed to set source/target
                    var sourceMethod = chainEffect.GetType().GetMethod("SetSource");
                    var targetMethod = chainEffect.GetType().GetMethod("SetTarget");

                    if (sourceMethod != null && targetMethod != null)
                    {
                        sourceMethod.Invoke(chainEffect, new object[] { handTransform });
                        targetMethod.Invoke(chainEffect, new object[] { targetPoint });
                    }

                    chainEffect.Play();
                    activeEffects.Add(chainEffect);
                }
            }
        }

        private void CleanupEffects()
        {
            foreach (EffectInstance effect in activeEffects)
            {
                if (effect != null)
                {
                    effect.Stop();
                }
            }
            activeEffects.Clear();

            handChargingEffect = null;
            targetingEffect = null;
            chainEffect = null;
        }

        private void CleanupTargetEffects(Creature target)
        {
            if (creatureEffects.ContainsKey(target))
            {
                foreach (EffectInstance effect in creatureEffects[target])
                {
                    if (effect != null)
                    {
                        effect.Stop();
                    }
                }
                creatureEffects.Remove(target);
            }

            if (target != null)
            {
                // Remove any configurableJoints
                foreach (RagdollPart part in target.ragdoll.parts)
                {
                    if (part != null && part.gameObject != null)
                    {
                        foreach (ConfigurableJoint joint in part.gameObject.GetComponents<ConfigurableJoint>())
                        {
                            Destroy(joint);
                        }
                    }

                    // Reset physics properties
                    if (part != null && part.physicBody != null)
                    {
                        part.physicBody.drag = 0f;
                        part.physicBody.angularDrag = 0.05f;
                    }
                }
            }
        }

        private Transform GetHandTransform(Side side)
        {
            return side == Side.Left ? creature.handLeft.transform : creature.handRight.transform;
        }

        private void ApplyHapticPulse(Side side, float intensity, float frequency, float duration)
        {
            PlayerHand playerHand = side == Side.Left ?
                        creature.handLeft.playerHand :
                        creature.handRight.playerHand;

            if (playerHand?.controlHand != null)
            {
                AnimationCurve curve = AnimationCurve.EaseInOut(0f, intensity, 1f, 0f);
                GameData.HapticClip haptic = new GameData.HapticClip(curve, duration, frequency);
                playerHand.controlHand.HapticPlayClip(haptic);
            }
        }

        private void OnDestroy()
        {
            EnableSpellWheel();

            if (creature != null)
            {
                InputHandler leftHandler = creature.handLeft.gameObject.GetComponent<InputHandler>();
                InputHandler rightHandler = creature.handRight.gameObject.GetComponent<InputHandler>();

                if (leftHandler != null)
                    Destroy(leftHandler);
                if (rightHandler != null)
                    Destroy(rightHandler);
            }

            // Free all bound enemies
            foreach (Creature target in boundCreatures.ToArray())
            {
                if (target != null && !target.isKilled)
                {
                    ManageTargetState(target, false);
                    CleanupTargetEffects(target);
                }
            }
            boundCreatures.Clear();

            if (bindEffect != null)
            {
                bindEffect.CleanupChains();
            }

            CleanupEffects();

            foreach (var entry in creatureEffects)
            {
                foreach (EffectInstance effect in entry.Value)
                {
                    if (effect != null)
                    {
                        effect.Stop();
                    }
                }
            }
            creatureEffects.Clear();

            Debug.Log($"[{currentDateTime}] {currentUser} - ShadowBind destroyed");
        }
    }
}