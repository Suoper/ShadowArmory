using ThunderRoad;
using UnityEngine;

namespace ShadowArmory
{
    /// <summary>
    /// Skill loader for SkillOrbModule - integrates with the game's skill system
    /// </summary>
    public class SkillOrbSkill : SkillData
    {
        private readonly string currentUser = "Suoper";
        private readonly string currentDateTime = "2025-06-29 03:30:38";

        [Header("Skill Configuration")]
        public string orbItemId = "SkillOrb"; // The item ID that will have the SkillOrbModule

        public override void OnSkillLoaded(SkillData skillData, Creature creature)
        {
            base.OnSkillLoaded(skillData, creature);

            Debug.Log($"[{currentDateTime}] {currentUser} - SkillOrb skill loaded for {creature.name}");

            // The SkillOrbModule will be added to items automatically via item module system
            // This skill just enables the functionality for the player
        }

        public override void OnSkillUnloaded(SkillData skillData, Creature creature)
        {
            base.OnSkillUnloaded(skillData, creature);

            Debug.Log($"[{currentDateTime}] {currentUser} - SkillOrb skill unloaded for {creature.name}");
        }
    }
}