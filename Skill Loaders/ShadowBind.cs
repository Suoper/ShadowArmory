using ThunderRoad;
using UnityEngine;

namespace ShadowArmory
{
    public class ShadowBindSkill : SkillData
    {
        private readonly string currentUser = "Suoper";
        private readonly string currentDateTime = "2025-04-09 21:02:17";

        public override void OnSkillLoaded(SkillData skillData, Creature creature)
        {
            base.OnSkillLoaded(skillData, creature);

            ShadowBind shadowBind = creature.gameObject.AddComponent<ShadowBind>();
            shadowBind.Initialize(creature);

            Debug.Log($"[{currentDateTime}] {currentUser} - Shadow Bind skill loaded for {creature.name}");
        }

        public override void OnSkillUnloaded(SkillData skillData, Creature creature)
        {
            base.OnSkillUnloaded(skillData, creature);

            ShadowBind shadowBind = creature.gameObject.GetComponent<ShadowBind>();
            if (shadowBind != null)
            {
                Object.Destroy(shadowBind);
                Debug.Log($"[{currentDateTime}] {currentUser} - Shadow Bind skill unloaded for {creature.name}");
            }
            else
            {
                Debug.LogWarning($"[{currentDateTime}] {currentUser} - Could not find Shadow Bind component to unload from {creature.name}");
            }
        }
    }
}