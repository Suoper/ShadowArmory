using ThunderRoad;
using UnityEngine;
using System.Collections.Generic;

namespace ShadowArmory
{
    public class WarriorStrength : MonoBehaviour
    {
        private Creature creature;
        private Dictionary<Item, float> originalMasses = new Dictionary<Item, float>();

        // Warrior's Strength settings
        private float massReductionMultiplier = 1.0f;
        private float positionMultiplier = 3f;
        private float rotationMultiplier = 3f;

        // Effect reference
        private string strengthEffectId = "WarriorStrengthEffect";
        private Dictionary<Item, EffectInstance> activeEffects = new Dictionary<Item, EffectInstance>();

        public void Initialize(Creature owner)
        {
            creature = owner;

            // Subscribe to grab events for items, both with and without handles
            creature.handLeft.OnGrabEvent += OnItemGrabbed;
            creature.handRight.OnGrabEvent += OnItemGrabbed;
            creature.handLeft.OnUnGrabEvent += OnItemUnGrabbed;
            creature.handRight.OnUnGrabEvent += OnItemUnGrabbed;

            // Apply joint force multiplier immediately
            if (creature != null)
            {
                creature.AddJointForceMultiplier(this, positionMultiplier, rotationMultiplier);
            }

            Debug.Log($"Warrior's Strength initialized for {creature.name}");
        }

        private void OnItemGrabbed(Side side, Handle handle, float axisPosition, HandlePose handlePose, EventTime eventTime)
        {
            // Handle both handle-based and non-handle-based items
            Item item = handle != null ? handle.item : null;
            if (item == null) return; // If the item is null, nothing to do

            if (eventTime == EventTime.OnEnd)
            {
                // Store original mass if it hasn't been stored already
                if (!originalMasses.ContainsKey(item))
                {
                    originalMasses[item] = item.physicBody.mass;
                }

                // Apply warrior strength enhancements
                ApplyStrengthEnhancements(item);

                // Spawn strength effect
                SpawnStrengthEffect(item);
            }
        }

        private void OnItemUnGrabbed(Side side, Handle handle, bool throwing, EventTime eventTime)
        {
            // Handle both handle-based and non-handle-based items
            Item item = handle != null ? handle.item : null;
            if (item == null) return;

            if (eventTime == EventTime.OnEnd)
            {
                RestoreOriginalValues(item);
                RemoveStrengthEffect(item);
            }
        }

        private void ApplyStrengthEnhancements(Item item)
        {
            if (item != null)
            {
                item.physicBody.mass *= massReductionMultiplier;
            }
        }

        private void RestoreOriginalValues(Item item)
        {
            if (item != null && originalMasses.ContainsKey(item))
            {
                // Restore original mass
                item.physicBody.mass = originalMasses[item];
                originalMasses.Remove(item);
            }
        }

        private void SpawnStrengthEffect(Item item)
        {
            if (!activeEffects.ContainsKey(item))
            {
                EffectInstance effect = Catalog.GetData<EffectData>(strengthEffectId)?.Spawn(item.transform.position, item.transform.rotation);
                if (effect != null)
                {
                    effect.Play();
                    effect.SetParent(item.transform);
                    activeEffects[item] = effect;
                }
            }
        }

        private void RemoveStrengthEffect(Item item)
        {
            if (activeEffects.ContainsKey(item))
            {
                activeEffects[item].Stop();
                activeEffects.Remove(item);
            }
        }

        private void OnDestroy()
        {
            // Remove joint force multiplier
            if (creature != null)
            {
                creature.RemoveJointForceMultiplier(this);
            }

            // Clean up all enhanced items
            foreach (var item in new List<Item>(originalMasses.Keys))
            {
                RestoreOriginalValues(item);
                RemoveStrengthEffect(item);
            }

            // Unsubscribe from events
            if (creature != null)
            {
                creature.handLeft.OnGrabEvent -= OnItemGrabbed;
                creature.handRight.OnGrabEvent -= OnItemGrabbed;
                creature.handLeft.OnUnGrabEvent -= OnItemUnGrabbed;
                creature.handRight.OnUnGrabEvent -= OnItemUnGrabbed;
            }
        }
    }
}
