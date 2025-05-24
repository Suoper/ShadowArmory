using ThunderRoad;
using UnityEngine;

namespace ShadowArmory
{
    public class RiftFormSkill : SkillData
    {
        private readonly string currentUser = "Suoper";
        private readonly string currentDateTime = "2025-04-08 21:41:15";

        public override void OnSkillLoaded(SkillData skillData, Creature creature)
        {
            base.OnSkillLoaded(skillData, creature);

            RiftForm riftForm = creature.gameObject.AddComponent<RiftForm>();

            riftForm.Initialize(creature);

            Debug.Log($"[{currentDateTime}] {currentUser} - Rift Form skill loaded for {creature.name}");
        }

        public override void OnSkillUnloaded(SkillData skillData, Creature creature)
        {
            base.OnSkillUnloaded(skillData, creature);

            RiftForm riftForm = creature.gameObject.GetComponent<RiftForm>();
            if (riftForm != null)
            {
                Object.Destroy(riftForm);
                Debug.Log($"[{currentDateTime}] {currentUser} - Rift Form skill unloaded for {creature.name}");
            }
            else
            {
                Debug.LogWarning($"[{currentDateTime}] {currentUser} - Could not find Rift Form component to unload from {creature.name}");
            }
        }
    }
}