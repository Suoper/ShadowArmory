using ThunderRoad;
using UnityEngine;
using System.Collections;

namespace ShadowArmory
{
    public class ShadowStep : MonoBehaviour
    {
        private readonly string currentUser = "Suoper";
        private readonly string currentDateTime = "2025-04-07 00:10:41";

        private Creature creature;
        private bool canTeleport = true;
        private float cooldownTime = 2f;
        private float teleportDistance = 3f;
        private float teleportSpeed = 50f;

        // Effect references
        private string teleportStartEffectId = "TeleportStart";
        private string teleportEndEffectId = "TeleportEnd";

        public void Initialize(Creature owner)
        {
            creature = owner;
            Debug.Log($"[{currentDateTime}] {currentUser} - Shadow Step initialized");
        }

        private void Update()
        {
            if (!canTeleport) return;

            bool alternateUsePressed = Player.currentCreature.handLeft.playerHand.controlHand.alternateUsePressed
                                  || Player.currentCreature.handRight.playerHand.controlHand.alternateUsePressed;

            if (alternateUsePressed)
            {
                // Check if player is blocking with a shield
                bool hasShield = Player.currentCreature.handLeft.grabbedHandle?.item?.data.type == ItemData.Type.Shield
                            || Player.currentCreature.handRight.grabbedHandle?.item?.data.type == ItemData.Type.Shield;

                if (hasShield)
                {
                    // Find closest enemy
                    Creature closestEnemy = null;
                    float closestDistance = float.MaxValue;

                    foreach (Creature otherCreature in Creature.allActive)
                    {
                        if (otherCreature != Player.currentCreature && !otherCreature.isPlayer)
                        {
                            float distance = Vector3.Distance(Player.currentCreature.transform.position, otherCreature.transform.position);
                            if (distance < closestDistance)
                            {
                                closestDistance = distance;
                                closestEnemy = otherCreature;
                            }
                        }
                    }

                    if (closestEnemy != null)
                    {
                        StartCoroutine(PerformShadowStep(closestEnemy));
                    }
                }
            }
        }

        private IEnumerator PerformShadowStep(Creature targetCreature)
        {
            canTeleport = false;

            Vector3 targetPosition = targetCreature.transform.position;
            Vector3 direction = (targetPosition - Player.currentCreature.transform.position).normalized;
            Vector3 behindPosition = targetPosition - (direction * teleportDistance);

            // Spawn start effect
            EffectInstance startEffect = Catalog.GetData<EffectData>(teleportStartEffectId)?.Spawn(Player.currentCreature.transform.position, Player.currentCreature.transform.rotation);
            startEffect?.Play();

            // Perform teleport
            Vector3 startPos = Player.currentCreature.transform.position;
            float startTime = Time.time;
            float journeyLength = Vector3.Distance(startPos, behindPosition);
            float speed = teleportSpeed;

            while (Vector3.Distance(Player.currentCreature.transform.position, behindPosition) > 0.1f)
            {
                float distCovered = (Time.time - startTime) * speed;
                float fractionOfJourney = distCovered / journeyLength;

                Player.currentCreature.transform.position = Vector3.Lerp(startPos, behindPosition, fractionOfJourney);
                yield return null;
            }

            // Spawn end effect
            EffectInstance endEffect = Catalog.GetData<EffectData>(teleportEndEffectId)?.Spawn(behindPosition, Player.currentCreature.transform.rotation);
            endEffect?.Play();

            Debug.Log($"[{currentDateTime}] {currentUser} - Shadow Step executed to position {behindPosition}");

            // Start cooldown
            yield return new WaitForSeconds(cooldownTime);
            canTeleport = true;
        }

        private void OnDestroy()
        {
            Debug.Log($"[{currentDateTime}] {currentUser} - Shadow Step destroyed");
        }
    }
}