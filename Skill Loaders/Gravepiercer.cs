using ThunderRoad;
using UnityEngine;

namespace ShadowArmory
{
    public class GravepiecerSkill : SkillData
    {
        private readonly string currentUser = "Suoper";
        private readonly string currentDateTime = "2025-04-09 20:40:52";

        public override void OnSkillLoaded(SkillData skillData, Creature creature)
        {
            base.OnSkillLoaded(skillData, creature);

            Gravepiercer gravepiercer = creature.gameObject.AddComponent<Gravepiercer>();
            gravepiercer.Initialize(creature);

            Debug.Log($"[{currentDateTime}] {currentUser} - Gravepiercer skill loaded for {creature.name}");
        }

        public override void OnSkillUnloaded(SkillData skillData, Creature creature)
        {
            base.OnSkillUnloaded(skillData, creature);

            Gravepiercer gravepiercer = creature.gameObject.GetComponent<Gravepiercer>();
            if (gravepiercer != null)
            {
                Object.Destroy(gravepiercer);
                Debug.Log($"[{currentDateTime}] {currentUser} - Gravepiercer skill unloaded for {creature.name}");
            }
            else
            {
                Debug.LogWarning($"[{currentDateTime}] {currentUser} - Could not find Gravepiercer component to unload from {creature.name}");
            }
        }
    }
}