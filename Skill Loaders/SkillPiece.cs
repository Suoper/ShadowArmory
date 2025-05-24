using ThunderRoad;
using UnityEngine;

namespace ShadowArmory
{
    public class ShadowDamperSkill : SkillData
    {
        private ShadowDamper damper;
        private readonly string currentUser = "Suoper";
        private readonly string currentDateTime = "2025-04-07 00:54:42";

        public override void OnSkillLoaded(SkillData skillData, Creature creature)
        {
            base.OnSkillLoaded(skillData, creature);

            // Add the damper component to the creature
            damper = creature.gameObject.AddComponent<ShadowDamper>();
            damper.Initialize(creature);

            Debug.Log($"[{currentDateTime}] {currentUser} - Shadow Damper skill loaded for {creature.name}");
        }

        public override void OnSkillUnloaded(SkillData skillData, Creature creature)
        {
            base.OnSkillUnloaded(skillData, creature);

            if (damper != null)
            {
                Object.Destroy(damper);
                damper = null;
            }

            Debug.Log($"[{currentDateTime}] {currentUser} - Shadow Damper skill unloaded for {creature.name}");
        }
    }
}