using ThunderRoad;
using UnityEngine;
using System.Collections.Generic;

namespace ShadowArmory
{
    public class ShadowBindEffect
    {
        private readonly string currentUser = "Suoper";
        private readonly string currentDateTime = "2025-04-10 00:12:52";

        public class ChainPoint
        {
            public Transform source;
            public Transform target;
            public EffectInstance chainEffect;
            public EffectInstance bindEffect;
            public Vector3 offset;

            public ChainPoint(Transform source, Transform target, Vector3 offset)
            {
                this.source = source;
                this.target = target;
                this.offset = offset;
            }
        }

        private List<ChainPoint> chains = new List<ChainPoint>();
        private List<Transform> chainAnchors = new List<Transform>();
        private List<EffectInstance> boundEffects = new List<EffectInstance>();
        private float effectEndTime;
        private const int CHAINS_PER_LIMB = 2;

        private void CreateChainAnchors(Creature target)
        {
            Debug.Log($"[{currentDateTime}] {currentUser} - Creating chain anchors for {target.name}");
            float radius = 2f;
            int numPoints = 12;
            // Add extra height to the center point
            Vector3 centerPoint = target.transform.position + Vector3.up * (ShadowBindConfig.ChainHeight + 2f);

            for (int i = 0; i < numPoints; i++)
            {
                float angle = i * (360f / numPoints);
                Vector3 pos = centerPoint + Quaternion.Euler(0, angle, 0) * (Vector3.forward * radius);

                GameObject anchor = new GameObject($"ChainAnchor_{i}");
                anchor.transform.position = pos;
                chainAnchors.Add(anchor.transform);
            }

            Debug.Log($"[{currentDateTime}] {currentUser} - Created {numPoints} chain anchors at height {ShadowBindConfig.ChainHeight}");
        }

        public void CreateBindEffect(Creature target)
        {
            Debug.Log($"[{currentDateTime}] {currentUser} - Creating bind effect for {target.name}");
            CleanupChains();
            effectEndTime = Time.time + ShadowBindConfig.BindDuration;
            CreateChainAnchors(target);

            if (target.brain != null)
            {
                Debug.Log($"[{currentDateTime}] {currentUser} - Stopping brain for {target.name}");
                target.brain.Stop();
            }
            if (target.animator != null)
            {
                Debug.Log($"[{currentDateTime}] {currentUser} - Disabling animator for {target.name}");
                target.animator.enabled = false;
            }

            target.ragdoll.SetState(Ragdoll.State.Destabilized);

            var partsToChain = new[] {
                RagdollPart.Type.Head,
                RagdollPart.Type.Torso,
                RagdollPart.Type.LeftHand,
                RagdollPart.Type.RightHand,
                RagdollPart.Type.LeftArm,
                RagdollPart.Type.RightArm,
                RagdollPart.Type.LeftLeg,
                RagdollPart.Type.RightLeg
            };

            int anchorIndex = 0;
            foreach (var partType in partsToChain)
            {
                RagdollPart part = target.ragdoll.GetPart(partType);
                if (part != null && part.physicBody != null)
                {
                    Debug.Log($"[{currentDateTime}] {currentUser} - Creating chains and effects for {partType}");

                    // Create bind effect on the body part
                    EffectData bindData = Catalog.GetData<EffectData>(ShadowBindConfig.BindEffectId);
                    if (bindData != null)
                    {
                        EffectInstance bindEffect = bindData.Spawn(part.transform);
                        bindEffect.Play();
                        boundEffects.Add(bindEffect);
                    }

                    for (int i = 0; i < CHAINS_PER_LIMB; i++)
                    {
                        Transform anchor = chainAnchors[anchorIndex % chainAnchors.Count];
                        Vector3 offset = Random.insideUnitSphere * 0.2f;

                        ChainPoint chainPoint = new ChainPoint(anchor, part.transform, offset);
                        chains.Add(chainPoint);

                        // Create visual chain effect
                        EffectData chainData = Catalog.GetData<EffectData>(ShadowBindConfig.ChainEffectId);
                        if (chainData != null)
                        {
                            chainPoint.chainEffect = chainData.Spawn(anchor);
                            chainPoint.chainEffect.SetSource(anchor);
                            chainPoint.chainEffect.SetTarget(part.transform);
                            chainPoint.chainEffect.Play();
                        }

                        // Apply stronger upward force
                        Vector3 upForce = Vector3.up * (ShadowBindConfig.BindForce * 1.5f);
                        part.physicBody.AddForce(upForce, ForceMode.Impulse);

                        // Additional upward velocity
                        part.physicBody.velocity = Vector3.up * 5f;

                        ConfigurableJoint joint = part.gameObject.AddComponent<ConfigurableJoint>();
                        joint.connectedBody = null;
                        joint.anchor = Vector3.zero;
                        joint.axis = Vector3.up;

                        // Modified joint settings for better lifting
                        joint.xMotion = ConfigurableJointMotion.Limited;
                        joint.yMotion = ConfigurableJointMotion.Limited;
                        joint.zMotion = ConfigurableJointMotion.Limited;
                        joint.angularXMotion = ConfigurableJointMotion.Limited;
                        joint.angularYMotion = ConfigurableJointMotion.Limited;
                        joint.angularZMotion = ConfigurableJointMotion.Limited;

                        // Increased limit for more movement
                        SoftJointLimit limit = new SoftJointLimit();
                        limit.limit = 0.5f;
                        joint.linearLimit = limit;

                        // Stronger springs for better control
                        SoftJointLimitSpring spring = new SoftJointLimitSpring();
                        spring.spring = 2000f;
                        spring.damper = 100f;
                        joint.linearLimitSpring = spring;

                        joint.breakForce = 7500f;
                        joint.breakTorque = 7500f;

                        // Lower drag for more height
                        part.physicBody.drag = 5f;
                        part.physicBody.angularDrag = 5f;

                        anchorIndex++;
                    }
                }
            }

            Debug.Log($"[{currentDateTime}] {currentUser} - Created {chains.Count} chains and {boundEffects.Count} bind effects");
        }

        public void UpdateChainPositions()
        {
            if (Time.time >= effectEndTime)
            {
                Debug.Log($"[{currentDateTime}] {currentUser} - Effect duration ended, cleaning up");
                CleanupChains();
                return;
            }

            foreach (ChainPoint chain in chains)
            {
                if (chain.chainEffect != null)
                {
                    float time = Time.time;
                    // Increased oscillation for more dramatic effect
                    Vector3 sourceOffset = new Vector3(
                        Mathf.Sin(time * 2f) * 0.2f,
                        Mathf.Cos(time * 1.5f) * 0.3f,
                        Mathf.Sin(time * 1.7f) * 0.2f
                    );

                    chain.chainEffect.SetSource(chain.source);
                    chain.chainEffect.SetTarget(chain.target);
                    chain.source.position += sourceOffset + chain.offset;

                    // Add continuous upward force
                    if (chain.target != null)
                    {
                        Rigidbody rb = chain.target.GetComponent<Rigidbody>();
                        if (rb != null)
                        {
                            rb.AddForce(Vector3.up * (ShadowBindConfig.HoldForce * 0.5f), ForceMode.Force);
                        }
                    }
                }
            }
        }

        public void CleanupChains()
        {
            Debug.Log($"[{currentDateTime}] {currentUser} - Cleaning up all effects and chains");

            foreach (ChainPoint chain in chains)
            {
                if (chain.chainEffect != null)
                {
                    chain.chainEffect.Stop();
                    chain.chainEffect.Despawn();
                }
            }
            chains.Clear();

            foreach (Transform anchor in chainAnchors)
            {
                if (anchor != null)
                {
                    Object.Destroy(anchor.gameObject);
                }
            }
            chainAnchors.Clear();

            foreach (EffectInstance effect in boundEffects)
            {
                if (effect != null)
                {
                    effect.Stop();
                    effect.Despawn();
                }
            }
            boundEffects.Clear();
        }
    }
}