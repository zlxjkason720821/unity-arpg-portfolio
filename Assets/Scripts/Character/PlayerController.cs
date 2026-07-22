using ARPG.Combat;
using ARPG.Skills;
using UnityEngine;

namespace ARPG.Character
{
    /// <summary>
    /// 阶段一可玩：WASD 移动；左键/J 普攻；1 或 Q / 2 或 E 放技能。
    /// 数字键在中文输入法下常被吞，所以提供字母键备用。
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(SkillExecutor))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] float moveSpeed = 6f;
        [SerializeField] float rotateSpeed = 720f;
        [SerializeField] Camera worldCamera;
        [SerializeField] int basicAttackIndex;
        [SerializeField] int skill1Index = 1;
        [SerializeField] int skill2Index = 2;

        CharacterController _controller;
        SkillExecutor _skills;
        Vector3 _aimPoint;

        void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _skills = GetComponent<SkillExecutor>();
            if (worldCamera == null)
                worldCamera = Camera.main;
        }

        public void BindCamera(Camera cam) => worldCamera = cam;

        void Update()
        {
            UpdateAimPoint();
            HandleMove();
            HandleCombatInput();
        }

        void HandleMove()
        {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            Vector3 input = new Vector3(h, 0f, v);
            if (input.sqrMagnitude > 1f)
                input.Normalize();

            Vector3 velocity = input * moveSpeed;
            _controller.SimpleMove(velocity);

            if (input.sqrMagnitude > 0.01f)
            {
                Quaternion target = Quaternion.LookRotation(input, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation, target, rotateSpeed * Time.deltaTime);
            }
        }

        void HandleCombatInput()
        {
            // 普攻：鼠标左键 或 J（避免只点到 Scene 视图/被 UI 吃掉时完全没反馈）
            if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.J))
                CastTowardAimOrForward(basicAttackIndex);

            // 技能1：Alpha1 / Keypad1 / Q
            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1) || Input.GetKeyDown(KeyCode.Q))
                CastTowardAimOrForward(skill1Index);

            // 技能2：Alpha2 / Keypad2 / E
            if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2) || Input.GetKeyDown(KeyCode.E))
                CastTowardAimOrForward(skill2Index);
        }

        void CastTowardAimOrForward(int index)
        {
            // 若附近有敌人，自动朝最近敌人出手，降低“鼠标没指到就打空”的验收成本
            Vector3 aim = _aimPoint;
            var nearest = FindNearestEnemy(6f);
            if (nearest != null)
                aim = nearest.position;
            else
                aim = transform.position + transform.forward * 2f;

            _skills.TryCast(index, aim);
        }

        Transform FindNearestEnemy(float maxRange)
        {
            Combatant[] all = FindObjectsByType<Combatant>(FindObjectsSortMode.None);
            Transform best = null;
            float bestDist = maxRange;
            for (int i = 0; i < all.Length; i++)
            {
                var c = all[i];
                if (c == null || !c.IsAlive || c.CompareTag("Player"))
                    continue;
                // 玩家自身
                if (c.transform == transform)
                    continue;

                float d = Vector3.Distance(transform.position, c.transform.position);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = c.transform;
                }
            }
            return best;
        }

        void UpdateAimPoint()
        {
            _aimPoint = transform.position + transform.forward * 2f;
            if (worldCamera == null)
                return;

            Ray ray = worldCamera.ScreenPointToRay(Input.mousePosition);
            var plane = new Plane(Vector3.up, transform.position);
            if (plane.Raycast(ray, out float enter))
                _aimPoint = ray.GetPoint(enter);
        }
    }
}
