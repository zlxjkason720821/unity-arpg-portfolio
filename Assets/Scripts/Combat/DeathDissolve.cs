using System.Collections;
using UnityEngine;

namespace ARPG.Combat
{
    /// <summary>
    /// 死亡溶解：播放完再 Destroy。配合 ARPG/SimpleDissolve。
    /// </summary>
    [RequireComponent(typeof(Combatant))]
    public class DeathDissolve : MonoBehaviour
    {
        static readonly int CutoffId = Shader.PropertyToID("_Cutoff");

        [SerializeField] Renderer targetRenderer;
        [SerializeField] float duration = 0.55f;
        [SerializeField] Material dissolveMaterialTemplate;

        Combatant _combatant;
        bool _playing;
        Material _runtimeMat;

        void Awake()
        {
            _combatant = GetComponent<Combatant>();
            if (targetRenderer == null)
                targetRenderer = GetComponentInChildren<Renderer>();
        }

        void OnEnable()
        {
            if (_combatant != null)
                _combatant.OnDied += HandleDied;
        }

        void OnDisable()
        {
            if (_combatant != null)
                _combatant.OnDied -= HandleDied;
        }

        public void SetDissolveTemplate(Material template) => dissolveMaterialTemplate = template;

        void HandleDied(Combatant _)
        {
            if (_playing)
                return;
            StartCoroutine(PlayRoutine());
        }

        IEnumerator PlayRoutine()
        {
            _playing = true;

            foreach (var col in GetComponentsInChildren<Collider>())
                col.enabled = false;

            var enemyAi = GetComponent<ARPG.AI.SimpleEnemyAI>();
            if (enemyAi != null)
                enemyAi.enabled = false;
            var skills = GetComponent<ARPG.Skills.SkillExecutor>();
            if (skills != null)
                skills.enabled = false;

            if (targetRenderer != null && dissolveMaterialTemplate != null)
            {
                _runtimeMat = new Material(dissolveMaterialTemplate);
                if (targetRenderer.sharedMaterial != null && targetRenderer.sharedMaterial.HasProperty("_BaseColor"))
                    _runtimeMat.SetColor("_BaseColor", targetRenderer.sharedMaterial.GetColor("_BaseColor"));
                targetRenderer.material = _runtimeMat;
            }
            else
            {
                yield return ScaleOut();
                Destroy(gameObject);
                yield break;
            }

            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float a = Mathf.Clamp01(t / duration);
                if (_runtimeMat != null)
                    _runtimeMat.SetFloat(CutoffId, a);
                yield return null;
            }

            Destroy(gameObject);
        }

        IEnumerator ScaleOut()
        {
            Vector3 start = transform.localScale;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                transform.localScale = Vector3.Lerp(start, Vector3.zero, t / duration);
                yield return null;
            }
        }
    }
}
