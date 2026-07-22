using ARPG.Combat;
using ARPG.Data;
using ARPG.Skills;
using UnityEngine;
using UnityEngine.AI;

namespace ARPG.AI
{
    /// <summary>
    /// Explicit finite-state AI: Patrol → Chase → Attack (and back when target leaves range).
    /// Ranges come from <see cref="EnemyData"/> (data-driven). Scene Gizmos visualize detect/attack radii for demos.
    /// </summary>
    [RequireComponent(typeof(Combatant))]
    [RequireComponent(typeof(SkillExecutor))]
    public class SimpleEnemyAI : MonoBehaviour
    {
        public enum AiState
        {
            Patrol,
            Chase,
            Attack
        }

        [SerializeField] EnemyData enemyData;
        [SerializeField] Transform player;
        [SerializeField] float detectRange = 12f;
        [SerializeField] float attackRange = 2.2f;
        [SerializeField] float loseTargetRange = 16f;
        [SerializeField] float moveSpeed = 3.5f;
        [SerializeField] float patrolRadius = 3.5f;
        [SerializeField] float patrolArriveDistance = 0.4f;
        [SerializeField] int skillIndex;
        [SerializeField] bool useNavMesh;
        [SerializeField] bool drawGizmos = true;

        Combatant _self;
        SkillExecutor _skills;
        NavMeshAgent _agent;
        CharacterController _cc;
        Vector3 _home;
        Vector3 _patrolTarget;
        AiState _state = AiState.Patrol;

        /// <summary>Current FSM state (useful for debug UI / interview demos).</summary>
        public AiState CurrentState => _state;

        void Awake()
        {
            _self = GetComponent<Combatant>();
            _skills = GetComponent<SkillExecutor>();
            _agent = GetComponent<NavMeshAgent>();
            _cc = GetComponent<CharacterController>();
            _home = transform.position;
            PickNewPatrolPoint();
            ApplyData(enemyData);
        }

        void Start()
        {
            if (player == null)
            {
                var p = GameObject.FindGameObjectWithTag("Player");
                if (p != null)
                    player = p.transform;
            }
        }

        /// <summary>Inject data + player reference from bootstrap / level loader.</summary>
        public void Configure(EnemyData data, Transform playerTarget)
        {
            player = playerTarget;
            _home = transform.position;
            PickNewPatrolPoint();
            ApplyData(data);
            SetState(AiState.Patrol);
        }

        void ApplyData(EnemyData data)
        {
            enemyData = data;
            if (data == null)
            {
                if (_agent != null)
                    _agent.speed = moveSpeed;
                return;
            }

            detectRange = data.detectRange;
            attackRange = data.attackRange;
            loseTargetRange = Mathf.Max(data.detectRange + 4f, detectRange);
            moveSpeed = data.moveSpeed;
            if (_self != null)
                _self.ApplyEnemyData(data, destroyOnDeath: true);
            if (_agent != null)
                _agent.speed = moveSpeed;
        }

        void Update()
        {
            if (!_self.IsAlive)
                return;

            float distToPlayer = player != null
                ? Vector3.Distance(transform.position, player.position)
                : float.MaxValue;

            // --- State transitions (explicit FSM, easy to explain in interview) ---
            switch (_state)
            {
                case AiState.Patrol:
                    if (player != null && distToPlayer <= detectRange)
                        SetState(AiState.Chase);
                    break;

                case AiState.Chase:
                    if (player == null || distToPlayer > loseTargetRange)
                        SetState(AiState.Patrol);
                    else if (distToPlayer <= attackRange)
                        SetState(AiState.Attack);
                    break;

                case AiState.Attack:
                    if (player == null || distToPlayer > loseTargetRange)
                        SetState(AiState.Patrol);
                    else if (distToPlayer > attackRange * 1.15f)
                        SetState(AiState.Chase);
                    break;
            }

            // --- State behaviours ---
            switch (_state)
            {
                case AiState.Patrol:
                    TickPatrol();
                    break;
                case AiState.Chase:
                    FacePlayer();
                    MoveToward(player.position);
                    break;
                case AiState.Attack:
                    FacePlayer();
                    StopMove();
                    if (player != null)
                        _skills.TryCast(skillIndex, player.position);
                    break;
            }
        }

        void SetState(AiState next)
        {
            if (_state == next)
                return;
            _state = next;
            if (next == AiState.Patrol)
                PickNewPatrolPoint();
        }

        void TickPatrol()
        {
            Vector3 flat = _patrolTarget;
            flat.y = transform.position.y;
            float dist = Vector3.Distance(transform.position, flat);
            if (dist <= patrolArriveDistance)
            {
                PickNewPatrolPoint();
                return;
            }

            Vector3 dir = flat - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(dir.normalized);
            MoveToward(_patrolTarget);
        }

        void PickNewPatrolPoint()
        {
            Vector2 rnd = Random.insideUnitCircle * patrolRadius;
            _patrolTarget = _home + new Vector3(rnd.x, 0f, rnd.y);
            _patrolTarget.y = transform.position.y;
        }

        void FacePlayer()
        {
            if (player == null)
                return;
            Vector3 dir = player.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(dir);
        }

        void MoveToward(Vector3 destination)
        {
            if (useNavMesh && _agent != null && _agent.isOnNavMesh)
            {
                _agent.isStopped = false;
                _agent.SetDestination(destination);
                return;
            }

            Vector3 dir = destination - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f)
                return;
            dir.Normalize();
            Vector3 delta = dir * moveSpeed * Time.deltaTime;
            if (_cc != null)
                _cc.Move(delta);
            else
                transform.position += delta;
        }

        void StopMove()
        {
            if (useNavMesh && _agent != null && _agent.isOnNavMesh)
                _agent.isStopped = true;
        }

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            if (!drawGizmos)
                return;

            // Detect range (yellow) + attack range (red) — record these in Scene view for the AI demo shot.
            Gizmos.color = new Color(1f, 0.92f, 0.2f, 0.35f);
            Gizmos.DrawWireSphere(transform.position, detectRange);
            Gizmos.color = new Color(1f, 0.2f, 0.15f, 0.55f);
            Gizmos.DrawWireSphere(transform.position, attackRange);

            Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.5f);
            Gizmos.DrawWireSphere(Application.isPlaying ? _home : transform.position, patrolRadius);

            if (Application.isPlaying)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(transform.position + Vector3.up, _patrolTarget + Vector3.up);
                UnityEditor.Handles.Label(
                    transform.position + Vector3.up * 2.4f,
                    $"AI: {_state}");
            }
        }
#endif
    }
}
