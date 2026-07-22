using ARPG.Combat;
using UnityEngine;
using UnityEngine.UI;

namespace ARPG.UI
{
    /// <summary>
    /// 世界空间或 Screen Space 血条：绑定 Combatant.OnHealthChanged。
    /// </summary>
    public class HealthBarUI : MonoBehaviour
    {
        [SerializeField] Combatant target;
        [SerializeField] Image fillImage;
        [SerializeField] bool billboardToCamera = true;
        [SerializeField] Camera worldCamera;

        void Awake()
        {
            if (worldCamera == null)
                worldCamera = Camera.main;
            if (target == null)
                target = GetComponentInParent<Combatant>();
        }

        public void Bind(Combatant combatant, Image fill)
        {
            if (target != null)
                target.OnHealthChanged -= Refresh;
            target = combatant;
            fillImage = fill;
            if (isActiveAndEnabled && target != null)
                target.OnHealthChanged += Refresh;
            Refresh();
        }

        void OnEnable()
        {
            if (target != null)
                target.OnHealthChanged += Refresh;
            Refresh();
        }

        void OnDisable()
        {
            if (target != null)
                target.OnHealthChanged -= Refresh;
        }

        void LateUpdate()
        {
            if (!billboardToCamera || worldCamera == null)
                return;
            transform.forward = worldCamera.transform.forward;
        }

        void Refresh()
        {
            if (fillImage == null || target == null)
                return;
            fillImage.fillAmount = target.HealthNormalized;
        }
    }
}
