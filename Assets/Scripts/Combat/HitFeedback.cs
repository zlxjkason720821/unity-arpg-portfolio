using UnityEngine;

namespace ARPG.Combat
{
    /// <summary>
    /// 受击反馈：MaterialPropertyBlock 闪色（避免 .material 拆实例增 Draw Call）+ 击退。
    /// </summary>
    [RequireComponent(typeof(Combatant))]
    public class HitFeedback : MonoBehaviour
    {
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int ColorId = Shader.PropertyToID("_Color");
        static readonly int OutlineWidthId = Shader.PropertyToID("_OutlineWidth");

        [SerializeField] Renderer targetRenderer;
        [SerializeField] Color flashColor = new Color(1f, 0.35f, 0.35f, 1f);
        [SerializeField] float flashDuration = 0.12f;
        [SerializeField] float knockbackForce = 2.5f;
        [SerializeField] float knockbackDuration = 0.12f;
        [SerializeField] float hitOutlineWidth = 0.02f;
        [SerializeField] Rigidbody optionalBody;

        Combatant _combatant;
        CharacterController _cc;
        MaterialPropertyBlock _mpb;
        Color _originalColor = Color.white;
        float _originalOutline;
        bool _hasBaseColor;
        bool _hasColor;
        bool _hasOutline;
        float _flashUntil;
        bool _flashing;
        Vector3 _knockVelocity;
        float _knockUntil;

        void Awake()
        {
            _combatant = GetComponent<Combatant>();
            _cc = GetComponent<CharacterController>();
            _mpb = new MaterialPropertyBlock();
            if (targetRenderer == null)
                targetRenderer = GetComponentInChildren<Renderer>();
            if (optionalBody == null)
                optionalBody = GetComponent<Rigidbody>();
            CacheProperties();
        }

        void OnEnable()
        {
            if (_combatant != null)
                _combatant.OnDamaged += HandleDamaged;
        }

        void OnDisable()
        {
            if (_combatant != null)
                _combatant.OnDamaged -= HandleDamaged;
        }

        void Update()
        {
            if (_flashing && targetRenderer != null && Time.time >= _flashUntil)
            {
                ApplyVisual(_originalColor, _originalOutline);
                _flashing = false;
            }

            if (Time.time < _knockUntil && _knockVelocity.sqrMagnitude > 0.0001f)
            {
                Vector3 step = _knockVelocity * Time.deltaTime;
                if (_cc != null)
                    _cc.Move(step);
                else if (optionalBody == null)
                    transform.position += step;
                _knockVelocity = Vector3.Lerp(_knockVelocity, Vector3.zero, Time.deltaTime * 12f);
            }
        }

        void HandleDamaged(float amount, Combatant source)
        {
            if (targetRenderer != null)
            {
                ApplyVisual(flashColor, hitOutlineWidth);
                _flashUntil = Time.time + flashDuration;
                _flashing = true;
            }

            if (source == null)
                return;

            Vector3 dir = transform.position - source.transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.001f)
                return;
            dir.Normalize();

            if (optionalBody != null && !optionalBody.isKinematic)
            {
                optionalBody.AddForce(dir * knockbackForce, ForceMode.VelocityChange);
            }
            else
            {
                _knockVelocity = dir * (knockbackForce / Mathf.Max(0.05f, knockbackDuration));
                _knockUntil = Time.time + knockbackDuration;
            }
        }

        void CacheProperties()
        {
            if (targetRenderer == null || targetRenderer.sharedMaterial == null)
                return;

            var mat = targetRenderer.sharedMaterial;
            _hasBaseColor = mat.HasProperty(BaseColorId);
            _hasColor = mat.HasProperty(ColorId);
            _hasOutline = mat.HasProperty(OutlineWidthId);

            if (_hasBaseColor)
                _originalColor = mat.GetColor(BaseColorId);
            else if (_hasColor)
                _originalColor = mat.GetColor(ColorId);

            if (_hasOutline)
                _originalOutline = mat.GetFloat(OutlineWidthId);
        }

        void ApplyVisual(Color color, float outlineWidth)
        {
            // 优化后：PropertyBlock 改色，不拆 sharedMaterial，利于合批
            targetRenderer.GetPropertyBlock(_mpb);
            if (_hasBaseColor)
                _mpb.SetColor(BaseColorId, color);
            if (_hasColor)
                _mpb.SetColor(ColorId, color);
            if (_hasOutline)
                _mpb.SetFloat(OutlineWidthId, outlineWidth);
            targetRenderer.SetPropertyBlock(_mpb);
        }
    }
}
