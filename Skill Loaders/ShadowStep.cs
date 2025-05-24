using ThunderRoad;
using UnityEngine;

namespace ShadowArmory
{
    public class ShadowStepSkill : SkillData
    {
        private ShadowStep shadowStep;
        private readonly string currentUser = "Suoper";
        private readonly string currentDateTime = "2025-04-07 00:56:35";

        public override void OnSkillLoaded(SkillData skillData, Creature creature)
        {
            base.OnSkillLoaded(skillData, creature);

            shadowStep = creature.gameObject.AddComponent<ShadowStep>();
            shadowStep.Initialize(creature);

            Debug.Log($"[{currentDateTime}] {currentUser} - Shadow Step skill loaded for {creature.name}");
        }

        public override void OnSkillUnloaded(SkillData skillData, Creature creature)
        {
            base.OnSkillUnloaded(skillData, creature);

            if (shadowStep != null)
            {
                Object.Destroy(shadowStep);
                shadowStep = null;
            }

            Debug.Log($"[{currentDateTime}] {currentUser} - Shadow Step skill unloaded for {creature.name}");
        }
    }
}