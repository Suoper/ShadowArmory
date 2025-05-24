using ThunderRoad;
using UnityEngine;
using System.Collections.Generic;

namespace ShadowArmory
{
    public class StrongerThrows : MonoBehaviour
    {
        private readonly string currentUser = "Suoper";
        private readonly string currentDateTime = "2025-04-09 02:44:28";

        private Creature creature;
        private Dictionary<Item, float> throwVelocities = new Dictionary<Item, float>();
        private Dictionary<Item, Vector3> lastPositions = new Dictionary<Item, Vector3>();
        private Dictionary<Item, float> chargeTime = new Dictionary<Item, float>();
        private Dictionary<Item, EffectInstance> chargeEffects = new Dictionary<Item, EffectInstance>();

        public void Initialize(Creature owner)
        {
            creature = owner;

            if (creature == null)
            {
                Debug.LogError($"[{currentDateTime}] {currentUser} - Cannot initialize Stronger Throws: creature is null!");
                return;
            }

            creature.handLeft.OnGrabEvent += OnHandGrab;
            creature.handRight.OnGrabEvent += OnHandGrab;
            creature.handLeft.OnUnGrabEvent += OnHandUnGrab;
            creature.handRight.OnUnGrabEvent += OnHandUnGrab;

            Debug.Log($"[{currentDateTime}] {currentUser} - Stronger Throws initialized for {creature.name}");
        }

        private void OnHandGrab(Side side, Handle handle, float axisPosition, HandlePose handlePose, EventTime eventTime)
        {
            if (!StrongerThrowsConfig.EnabledThrows || handle?.item == null) return;

            if (eventTime == EventTime.OnStart)
            {
                Item item = handle.item;
                if (!throwVelocities.ContainsKey(item))
                {
                    throwVelocities[item] = 0f;
                    lastPositions[item] = item.transform.position;
                    chargeTime[item] = 0f;
                    StartChargeEffect(item);
                }
            }
        }

        private void StartChargeEffect(Item item)
        {
            if (!StrongerThrowsConfig.ShowChargeEffect) return;

            EffectData effectData = Catalog.GetData<EffectData>(StrongerThrowsConfig.ChargeEffectId);
            if (effectData != null)
            {
                EffectInstance effect = effectData.Spawn(item.transform, null, true);
                if (effect != null)
                {
                    effect.Play();
                    chargeEffects[item] = effect;
                    Debug.Log($"[{currentDateTime}] {currentUser} - Started charge effect for {item.name}");
                }
            }
        }

        private void OnHandUnGrab(Side side, Handle handle, bool throwing, EventTime eventTime)
        {
            if (!StrongerThrowsConfig.EnabledThrows || handle?.item == null) return;

            if (eventTime == EventTime.OnStart && throwing)
            {
                Item item = handle.item;
                if (throwVelocities.ContainsKey(item))
                {
                    float charge = Mathf.Clamp01(chargeTime[item] / StrongerThrowsConfig.MaxChargeTime);
                    float velocityMultiplier = Mathf.Lerp(
                        StrongerThrowsConfig.MinVelocityMultiplier,
                        StrongerThrowsConfig.MaxVelocityMultiplier,
                        charge
                    );

                    // Calculate enhanced throw velocity
                    Vector3 throwVelocity = item.physicBody.velocity;
                    Vector3 enhancedVelocity = throwVelocity * velocityMultiplier;

                    // Add upward arc for better trajectories
                    float arcStrength = StrongerThrowsConfig.ThrowArcStrength * charge;
                    enhancedVelocity += Vector3.up * arcStrength;

                    // Apply spin for more stable flight
                    Vector3 spinAxis = Vector3.Cross(throwVelocity.normalized, Vector3.up);
                    float spinForce = StrongerThrowsConfig.SpinForce * charge;
                    item.physicBody.AddTorque(spinAxis * spinForce, ForceMode.Impulse);

                    // Apply the enhanced velocity
                    item.physicBody.velocity = enhancedVelocity;

                    Debug.Log($"[{currentDateTime}] {currentUser} - Enhanced throw: Velocity {enhancedVelocity.magnitude:F2}, Charge {charge:P0}");

                    // Create throw effect
                    if (StrongerThrowsConfig.ShowThrowEffect)
                    {
                        CreateThrowEffect(item, enhancedVelocity.magnitude);
                    }

                    // Cleanup
                    CleanupItemEffects(item);
                    throwVelocities.Remove(item);
                    lastPositions.Remove(item);
                    chargeTime.Remove(item);
                }
            }
        }

        private void CreateThrowEffect(Item item, float velocity)
        {
            EffectData effectData = Catalog.GetData<EffectData>(StrongerThrowsConfig.ThrowEffectId);
            if (effectData != null)
            {
                EffectInstance effect = effectData.Spawn(item.transform, null, true);
                if (effect != null)
                {
                    effect.Play();
                    Debug.Log($"[{currentDateTime}] {currentUser} - Created throw effect for {item.name}");
                }
            }
        }

        private void CleanupItemEffects(Item item)
        {
            if (chargeEffects.TryGetValue(item, out EffectInstance effect))
            {
                if (effect != null)
                {
                    effect.Stop();
                    effect.Despawn();
                }
                chargeEffects.Remove(item);
            }
        }

        private void Update()
        {
            if (!StrongerThrowsConfig.EnabledThrows) return;

            foreach (var item in new List<Item>(throwVelocities.Keys))
            {
                if (item == null) continue;

                // Update velocity tracking
                Vector3 currentPosition = item.transform.position;
                Vector3 movement = currentPosition - lastPositions[item];
                float currentVelocity = movement.magnitude / Time.deltaTime;
                throwVelocities[item] = Mathf.Lerp(throwVelocities[item], currentVelocity, Time.deltaTime * 10f);
                lastPositions[item] = currentPosition;

                // Update charge time while gripped
                if (item.IsHanded())
                {
                    bool isCharging = item.mainHandler.playerHand.controlHand.gripPressed;
                    if (isCharging)
                    {
                        chargeTime[item] = Mathf.Min(
                            chargeTime[item] + Time.deltaTime,
                            StrongerThrowsConfig.MaxChargeTime
                        );

                        // Update charge effect
                        if (chargeEffects.TryGetValue(item, out EffectInstance effect))
                        {
                            float chargePercent = chargeTime[item] / StrongerThrowsConfig.MaxChargeTime;
                            UpdateChargeEffect(effect, chargePercent);
                        }
                    }
                    else
                    {
                        chargeTime[item] = 0f;
                    }
                }
            }

            // Cleanup any null items
            CleanupNullItems();
        }

        private void UpdateChargeEffect(EffectInstance effect, float chargePercent)
        {
            if (effect == null) return;

            // Scale effect with charge
            effect.SetIntensity(chargePercent);

            // Change color based on charge
            Color chargeColor = Color.Lerp(
                StrongerThrowsConfig.MinChargeColor,
                StrongerThrowsConfig.MaxChargeColor,
                chargePercent
            );        }

        private void CleanupNullItems()
        {
            List<Item> itemsToRemove = new List<Item>();
            foreach (var item in throwVelocities.Keys)
            {
                if (item == null)
                {
                    itemsToRemove.Add(item);
                }
            }

            foreach (var item in itemsToRemove)
            {
                CleanupItemEffects(item);
                throwVelocities.Remove(item);
                lastPositions.Remove(item);
                chargeTime.Remove(item);
            }
        }

        private void OnDestroy()
        {
            if (creature != null)
            {
                creature.handLeft.OnGrabEvent -= OnHandGrab;
                creature.handRight.OnGrabEvent -= OnHandGrab;
                creature.handLeft.OnUnGrabEvent -= OnHandUnGrab;
                creature.handRight.OnUnGrabEvent -= OnHandUnGrab;
            }

            // Cleanup all effects
            foreach (var effect in chargeEffects.Values)
            {
                if (effect != null)
                {
                    effect.Stop();
                    effect.Despawn();
                }
            }
            chargeEffects.Clear();

            Debug.Log($"[{currentDateTime}] {currentUser} - Stronger Throws destroyed");
        }
    }
}