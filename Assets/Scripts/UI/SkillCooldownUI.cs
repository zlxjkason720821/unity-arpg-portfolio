using ARPG.Skills;
using UnityEngine;
using UnityEngine.UI;

namespace ARPG.UI
{
    /// <summary>
    /// 显示单个技能的冷却遮罩（Image.fillAmount）。
    /// </summary>
    public class SkillCooldownUI : MonoBehaviour
    {
        [SerializeField] SkillExecutor executor;
        [SerializeField] int skillIndex;
        [SerializeField] Image cooldownOverlay;
        [SerializeField] Text cooldownText;
        [SerializeField] string idleLabel = string.Empty;

        public void Bind(SkillExecutor skillExecutor, int index, Image overlay, Text label = null)
        {
            executor = skillExecutor;
            skillIndex = index;
            cooldownOverlay = overlay;
            cooldownText = label;
            if (label != null)
                idleLabel = label.text;
        }

        void Update()
        {
            if (executor == null || cooldownOverlay == null)
                return;
            if (skillIndex < 0 || skillIndex >= executor.EquippedSkills.Count)
                return;

            var skill = executor.EquippedSkills[skillIndex];
            if (skill == null)
                return;

            float norm = executor.GetCooldownNormalized(skill);
            cooldownOverlay.fillAmount = norm;

            if (cooldownText != null)
            {
                float remain = executor.GetCooldownRemaining(skill);
                cooldownText.text = remain > 0.05f ? remain.ToString("0.0") : idleLabel;
            }
        }
    }
}
