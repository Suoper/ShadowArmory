using ThunderRoad;
using UnityEngine;
using System.Collections.Generic;

namespace ShadowArmory
{
    public class Gravepiercer : MonoBehaviour
    {
        private readonly string currentUser = "Suoper";
        private readonly string currentDateTime = "2025-04-09 20:52:09";

        private Creature creature;
        private bool isStabbing = false;
        private float lastTriggerTime = 0f;
        private List<EffectInstance> activeSpikes = new List<EffectInstance>();
        private const float MIN_STAB_VELOCITY = 5.0f;
        private const float SPIKE_SPACING = 1.5f;
        private const float SPIKE_FORWARD_OFFSET = 1.0f;
        private const int MAX_SPIKES = 5;

        public void Initialize(Creature owner)
        {
            creature = owner;

            if (creature == null)
            {
                Debug.LogError($"[{currentDateTime}] {currentUser} - Cannot initialize Gravepiercer: creature is null!");
                return;
            }

            creature.handLeft.OnGrabEvent += OnHandGrab;
            creature.handRight.OnGrabEvent += OnHandGrab;

            Debug.Log($"[{currentDateTime}] {currentUser} - Gravepiercer initialized for {creature.name}");
        }

        private void OnHandGrab(Side side, Handle handle, float axisPosition, HandlePose handlePose, EventTime eventTime)
        {
            if (!GravepiecerConfig.EnabledGravepiercer || handle?.item == null) return;

            if (eventTime == EventTime.OnStart)
            {
                if (HasShadowMaterial(handle.item))
                {
                    handle.item.gameObject.AddComponent<CollisionHandler>().Initialize(this);
                    Debug.Log($"[{currentDateTime}] {currentUser} - Shadow weapon equipped, enabling Gravepiercer");
                }
            }
            else if (eventTime == EventTime.OnEnd)
            {
                if (HasShadowMaterial(handle.item))
                {
                    CollisionHandler handler = handle.item.gameObject.GetComponent<CollisionHandler>();
                    if (handler != null)
                    {
                        Destroy(handler);
                    }
                }
            }
        }

        private class CollisionHandler : MonoBehaviour
        {
            private Gravepiercer gravepiercer;

            public void Initialize(Gravepiercer owner)
            {
                gravepiercer = owner;
            }

            private void OnCollisionEnter(Collision collision)
            {
                if (gravepiercer != null)
                {
                    CollisionInstance collisionInstance = new CollisionInstance(new DamageStruct(DamageType.Pierce, 0f))
                    {
                        contactPoint = collision.contacts[0].point,
                        contactNormal = collision.contacts[0].normal,
                        intensity = collision.impulse.magnitude,
                        sourceCollider = collision.collider
                    };
                    gravepiercer.OnWeaponCollision(collisionInstance);
                }
            }
        }

        private bool HasShadowMaterial(Item item)
        {
            foreach (Renderer renderer in item.GetComponentsInChildren<Renderer>())
            {
                foreach (Material material in renderer.materials)
                {
                    if (material.name.Contains("WeaponMaterial"))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private void OnWeaponCollision(CollisionInstance collision)
        {
            if (!GravepiecerConfig.EnabledGravepiercer ||
                Time.time - lastTriggerTime < GravepiecerConfig.Cooldown ||
                isStabbing) return;

            // Check if we're stabbing the ground
            if (Vector3.Dot(collision.contactNormal, Vector3.up) > 0.7f &&
                collision.intensity > MIN_STAB_VELOCITY)
            {
                isStabbing = true;
                lastTriggerTime = Time.time;
                SpawnSpikeLine(collision.contactPoint, collision.sourceCollider.transform.forward);
                StartCoroutine(ResetStabCooldown());
            }
        }

        private System.Collections.IEnumerator ResetStabCooldown()
        {
            yield return new WaitForSeconds(0.5f);
            isStabbing = false;
        }

        private void SpawnSpikeLine(Vector3 startPoint, Vector3 direction)
        {
            Vector3 currentPoint = startPoint;
            direction.y = 0f;
            direction = direction.normalized;

            for (int i = 0; i < MAX_SPIKES; i++)
            {
                Vector3 spikePosition = currentPoint + (direction * (SPIKE_FORWARD_OFFSET + (i * SPIKE_SPACING)));

                RaycastHit hit;
                if (Physics.Raycast(spikePosition + Vector3.up * 2f, Vector3.down, out hit, 4f, LayerMask.GetMask("Default")))
                {
                    spikePosition.y = hit.point.y;
                }

                SpawnSpike(spikePosition, i * GravepiecerConfig.SpikeDelay);
                CheckForEnemies(spikePosition);
            }

            PlayGroundEffect(startPoint);
        }

        private void SpawnSpike(Vector3 position, float delay)
        {
            StartCoroutine(SpawnSpikeDelayed(position, delay));
        }

        private System.Collections.IEnumerator SpawnSpikeDelayed(Vector3 position, float delay)
        {
            yield return new WaitForSeconds(delay);

            EffectData spikeEffect = Catalog.GetData<EffectData>(GravepiecerConfig.SpikeEffectId);
            if (spikeEffect != null)
            {
                EffectInstance spike = spikeEffect.Spawn(position, Quaternion.identity);
                spike.Play();
                activeSpikes.Add(spike);

                StartCoroutine(DespawnSpikeDelayed(spike, GravepiecerConfig.SpikeDuration));
            }

            EffectData impactEffect = Catalog.GetData<EffectData>(GravepiecerConfig.ImpactEffectId);
            if (impactEffect != null)
            {
                EffectInstance impact = impactEffect.Spawn(position, Quaternion.identity);
                impact.Play();
                StartCoroutine(DespawnEffectDelayed(impact, 2f));
            }
        }


        private void CheckForEnemies(Vector3 position)
        {
            Collider[] hitColliders = Physics.OverlapSphere(position, GravepiecerConfig.SpikeRadius);
            foreach (Collider collider in hitColliders)
            {
                if (collider.TryGetComponent(out RagdollPart ragdollPart))
                {
                    Creature targetCreature = ragdollPart.ragdoll?.creature;
                    if (targetCreature != null && targetCreature != creature && !targetCreature.isKilled)
                    {
                        // Apply damage - fixed to use float instead of DamageStruct
                        targetCreature.Damage(GravepiecerConfig.SpikeDamage);

                        // Apply forces
                        if (ragdollPart.physicBody != null)
                        {
                            Vector3 pushDirection = (ragdollPart.transform.position - position).normalized;
                            ragdollPart.physicBody.AddForce(pushDirection * GravepiecerConfig.SpikeForce, ForceMode.Impulse);
                            ragdollPart.physicBody.AddForce(Vector3.up * GravepiecerConfig.SpikeUpwardForce, ForceMode.Impulse);
                        }

                        // Apply effect to enemy
                        EffectData enemyEffect = Catalog.GetData<EffectData>(GravepiecerConfig.EnemyEffectId);
                        if (enemyEffect != null)
                        {
                            EffectInstance effect = enemyEffect.Spawn(ragdollPart.transform);
                            effect.Play();
                            StartCoroutine(DespawnEffectDelayed(effect, 2f));
                        }
                    }
                }
            }
        }

        private void PlayGroundEffect(Vector3 position)
        {
            EffectData groundEffect = Catalog.GetData<EffectData>(GravepiecerConfig.GroundEffectId);
            if (groundEffect != null)
            {
                EffectInstance effect = groundEffect.Spawn(position, Quaternion.identity);
                effect.Play();
                StartCoroutine(DespawnEffectDelayed(effect, 2f));
            }
        }

        private System.Collections.IEnumerator DespawnSpikeDelayed(EffectInstance spike, float duration)
        {
            yield return new WaitForSeconds(duration);
            if (spike != null)
            {
                spike.Stop();
                spike.Despawn();
                activeSpikes.Remove(spike);
            }
        }

        private System.Collections.IEnumerator DespawnEffectDelayed(EffectInstance effect, float duration)
        {
            yield return new WaitForSeconds(duration);
            if (effect != null)
            {
                effect.Stop();
                effect.Despawn();
            }
        }

        private void OnDestroy()
        {
            if (creature != null)
            {
                creature.handLeft.OnGrabEvent -= OnHandGrab;
                creature.handRight.OnGrabEvent -= OnHandGrab;
            }

            foreach (var spike in activeSpikes)
            {
                if (spike != null)
                {
                    spike.Stop();
                    spike.Despawn();
                }
            }
            activeSpikes.Clear();

            Debug.Log($"[{currentDateTime}] {currentUser} - Gravepiercer destroyed");
        }
    }
}