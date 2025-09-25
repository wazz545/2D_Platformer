using UnityEngine;

[RequireComponent(typeof(CharacterMovement))]
public class EnemyFollow : MonoBehaviour
{
    public enum EnemyAttackType { Throw, Push, Both }

    [Header("References")]
    [Tooltip("Who to chase/attack (usually the Player transform).")]
    public Transform target;

    [Header("Detection")]
    [Tooltip("How far away the enemy can notice the player.")]
    public float detectionRange = 10f;
    [Tooltip("At or inside this distance, the enemy starts attacking.")]
    public float attackRange = 2f;
    [Tooltip("Stop moving toward the player when this close (prevents jitter).")]
    public float stopDistance = 1f;

    [Header("Patrol")]
    [Tooltip("Waypoints to patrol in a loop. Leave empty to disable patrol.")]
    public Transform[] patrolPoints;
    [Tooltip("Seconds to wait (idle) when arriving at a patrol point.")]
    public float waitAtPatrolPoint = 2f;
    [Tooltip("Within this radius, the enemy considers the patrol point reached.")]
    public float patrolStopDistance = 0.5f;

    [Header("Attack")]
    [Tooltip("Choose the enemy's attack behavior.")]
    public EnemyAttackType attackType = EnemyAttackType.Both;
    [Tooltip("Cooldown between attack attempts (seconds).")]
    public float attackCooldown = 1.5f;

    private CharacterMovement movement;
    private int currentPatrolIndex = 0;
    private float patrolWaitTimer;
    private float lastAttackTime;

    private enum State { Patrol, Pursue, Attack }
    private State currentState = State.Patrol;

    void Start()
    {
        movement = GetComponent<CharacterMovement>();
        movement.isPlayer = false;

        if (patrolPoints != null && patrolPoints.Length > 0)
            currentPatrolIndex = 0;
        else
            currentState = State.Pursue; // no patrol points → idle until detecting player
    }

    void Update()
    {
        if (target == null) return;

        float distanceToPlayer = Vector2.Distance(transform.position, target.position);

        switch (currentState)
        {
            case State.Patrol:
                Patrol();
                if (distanceToPlayer <= detectionRange)
                    currentState = State.Pursue;
                break;

            case State.Pursue:
                PursuePlayer(distanceToPlayer);
                if (distanceToPlayer <= attackRange)
                    currentState = State.Attack;
                else if (patrolPoints != null && patrolPoints.Length > 0 && distanceToPlayer > detectionRange * 1.5f)
                    currentState = State.Patrol; // lost player → back to patrol
                break;

            case State.Attack:
                AttackPlayer(distanceToPlayer);
                if (distanceToPlayer > attackRange)
                    currentState = State.Pursue;
                break;
        }
    }

    // ---------- Patrol ----------
    private void Patrol()
    {
        if (patrolPoints == null || patrolPoints.Length == 0) return;

        Transform patrolTarget = patrolPoints[currentPatrolIndex];
        float distanceToTarget = Vector2.Distance(transform.position, patrolTarget.position);

        if (distanceToTarget > patrolStopDistance && patrolWaitTimer <= 0f)
        {
            float dir = patrolTarget.position.x > transform.position.x ? 1 : -1;
            movement.MoveAI(dir, false);
        }
        else
        {
            // At patrol point → idle + wait
            GetComponent<AnimationStates>().ChangeState(AnimationStates.State.Idle);

            patrolWaitTimer += Time.deltaTime;
            if (patrolWaitTimer >= waitAtPatrolPoint)
            {
                currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
                patrolWaitTimer = 0f;
            }
        }
    }

    // ---------- Pursue ----------
    private void PursuePlayer(float distance)
    {
        if (distance > stopDistance)
        {
            float direction = target.position.x > transform.position.x ? 1 : -1;
            movement.MoveAI(direction, false);
        }
        else
        {
            GetComponent<AnimationStates>().ChangeState(AnimationStates.State.Idle);
        }
    }

    // ---------- Attack ----------
    private void AttackPlayer(float distance)
    {
        if (Time.time - lastAttackTime < attackCooldown) return;

        // Face player first
        float dir = target.position.x > transform.position.x ? 1 : -1;
        transform.localScale = new Vector3(dir > 0 ? 1 : -1, 1, 1);

        if (attackType == EnemyAttackType.Throw || attackType == EnemyAttackType.Both)
            movement.AIThrow(); // ground-throw loops; air-throw is one-shot then fall → handled by AnimationStates

        if (attackType == EnemyAttackType.Push || attackType == EnemyAttackType.Both)
            movement.AIPush(dir);

        lastAttackTime = Time.time;
    }

    // ---------- Debug Gizmos ----------
    void OnDrawGizmosSelected()
    {
        // Patrol path
        if (patrolPoints != null && patrolPoints.Length > 1)
        {
            Gizmos.color = Color.yellow;
            for (int i = 0; i < patrolPoints.Length; i++)
            {
                Vector3 a = patrolPoints[i].position;
                Vector3 b = patrolPoints[(i + 1) % patrolPoints.Length].position;
                Gizmos.DrawLine(a, b);
                Gizmos.DrawSphere(a, 0.12f);
            }
        }

        // Detection & attack radii
        Gizmos.color = new Color(0f, 0.6f, 1f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
