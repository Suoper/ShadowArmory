using ShadowArmory;
using ThunderRoad;
using UnityEngine;

namespace ShadowArmory
{
    public class AimAssistSkill : SkillData
    {
        private readonly string currentUser = "Suoper";
        private readonly string currentDateTime = "2025-04-09 01:28:49";

        public override void OnSkillLoaded(SkillData skillData, Creature creature)
        {
            base.OnSkillLoaded(skillData, creature);

            AimAssist aimAssist = creature.gameObject.AddComponent<AimAssist>();

            aimAssist.Initialize(creature);

            Debug.Log($"[{currentDateTime}] {currentUser} - Aim Assist skill loaded for {creature.name}");
        }

        public override void OnSkillUnloaded(SkillData skillData, Creature creature)
        {
            base.OnSkillUnloaded(skillData, creature);

            AimAssist aimAssist = creature.gameObject.GetComponent<AimAssist>();
            if (aimAssist != null)
            {
                Object.Destroy(aimAssist);
                Debug.Log($"[{currentDateTime}] {currentUser} - Aim Assist skill unloaded for {creature.name}");
            }
            else
            {
                Debug.LogWarning($"[{currentDateTime}] {currentUser} - Could not find Aim Assist component to unload from {creature.name}");
            }
        }
    }
}