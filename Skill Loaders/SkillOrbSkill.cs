using ThunderRoad;
using UnityEngine;

namespace ShadowArmory
{
    public class SkillOrbSkill : SkillData
    {
        private readonly string currentUser = "Suoper";
        private readonly string currentDateTime = "2025-01-01 00:00:00";

        public override void OnSkillLoaded(SkillData skillData, Creature creature)
        {
            base.OnSkillLoaded(skillData, creature);

            SkillOrbModule skillOrbModule = creature.gameObject.AddComponent<SkillOrbModule>();
            skillOrbModule.Initialize(creature);

            Debug.Log($"[{currentDateTime}] {currentUser} - Skill Orb Module loaded for {creature.name}");
        }

        public override void OnSkillUnloaded(SkillData skillData, Creature creature)
        {
            base.OnSkillUnloaded(skillData, creature);

            SkillOrbModule skillOrbModule = creature.gameObject.GetComponent<SkillOrbModule>();
            if (skillOrbModule != null)
            {
                Object.Destroy(skillOrbModule);
                Debug.Log($"[{currentDateTime}] {currentUser} - Skill Orb Module unloaded for {creature.name}");
            }
            else
            {
                Debug.LogWarning($"[{currentDateTime}] {currentUser} - Could not find Skill Orb Module component to unload from {creature.name}");
            }
        }
    }
}