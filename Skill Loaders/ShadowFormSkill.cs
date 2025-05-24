using ThunderRoad;
using UnityEngine;

namespace ShadowArmory
{
    public class ShadowFormSkill : SkillData
    {
        private readonly string currentUser = "Suoper";
        private readonly string currentDateTime = "2025-04-07 00:56:35";

        public override void OnSkillLoaded(SkillData skillData, Creature creature)
        {
            base.OnSkillLoaded(skillData, creature);

            ShadowForm shadowForm = creature.gameObject.AddComponent<ShadowForm>();
            shadowForm.Initialize(creature);

            Debug.Log($"[{currentDateTime}] {currentUser} - Shadow Form skill loaded for {creature.name}");
        }

        public override void OnSkillUnloaded(SkillData skillData, Creature creature)
        {
            base.OnSkillUnloaded(skillData, creature);

            ShadowForm shadowForm = creature.gameObject.GetComponent<ShadowForm>();
            if (shadowForm != null)
            {
                Object.Destroy(shadowForm);
            }

            Debug.Log($"[{currentDateTime}] {currentUser} - Shadow Form skill unloaded for {creature.name}");
        }
    }
}
