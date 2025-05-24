using ThunderRoad;
using UnityEngine;

namespace ShadowArmory
{
    public class StrongerThrowsSkill : SkillData
    {
        private readonly string currentUser = "Suoper";
        private readonly string currentDateTime = "2025-04-09 02:48:17";

        public override void OnSkillLoaded(SkillData skillData, Creature creature)
        {
            base.OnSkillLoaded(skillData, creature);

            StrongerThrows strongerThrows = creature.gameObject.AddComponent<StrongerThrows>();
            strongerThrows.Initialize(creature);

            Debug.Log($"[{currentDateTime}] {currentUser} - Stronger Throws skill loaded for {creature.name}");
        }

        public override void OnSkillUnloaded(SkillData skillData, Creature creature)
        {
            base.OnSkillUnloaded(skillData, creature);

            StrongerThrows strongerThrows = creature.gameObject.GetComponent<StrongerThrows>();
            if (strongerThrows != null)
            {
                Object.Destroy(strongerThrows);
                Debug.Log($"[{currentDateTime}] {currentUser} - Stronger Throws skill unloaded for {creature.name}");
            }
            else
            {
                Debug.LogWarning($"[{currentDateTime}] {currentUser} - Could not find Stronger Throws component to unload from {creature.name}");
            }
        }
    }
}