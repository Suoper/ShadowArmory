using ThunderRoad;
using UnityEngine;

namespace ShadowArmory
{
    public class WarriorStrengthSkill : SkillData
    {
        private WarriorStrength warriorStrength;
        private readonly string currentUser = "Suoper";
        private readonly string currentDateTime = "2025-04-07 00:54:06";

        public override void OnSkillLoaded(SkillData skillData, Creature creature)
        {
            base.OnSkillLoaded(skillData, creature);

            // Add WarriorStrength component and initialize it with the creature
            warriorStrength = creature.gameObject.AddComponent<WarriorStrength>();
            warriorStrength.Initialize(creature);

            Debug.Log($"[{currentDateTime}] {currentUser} - Warrior's Strength skill loaded for {creature.name}");
        }

        public override void OnSkillUnloaded(SkillData skillData, Creature creature)
        {
            base.OnSkillUnloaded(skillData, creature);

            // Destroy WarriorStrength component
            if (warriorStrength != null)
            {
                Object.Destroy(warriorStrength);
                warriorStrength = null;
            }

            Debug.Log($"[{currentDateTime}] {currentUser} - Warrior's Strength skill unloaded for {creature.name}");
        }
    }
}
