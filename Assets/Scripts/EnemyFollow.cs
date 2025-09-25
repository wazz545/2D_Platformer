using UnityEngine;

[RequireComponent(typeof(CharacterMovement))]
public class EnemyFollow : MonoBehaviour
{
    public enum EnemyAttackType { Throw, Push, Both }

    [Header("References")]
    public Transform target; // Player

    [Header("Detection Settings")]
    public float detectionRange = 10f;   // distance to notice player
    public float attackRange = 2f;       // distance to attack
    public float stopDistance = 1f;      // distance to stop pursuing

    [Header("Patrol Settings")]
    public Transform[] patrolPoints;     // multiple patrol points
    public float patrolSpeed = 2f;
    public float waitAtPatrolPoint = 2f; // wait at each point
    public float patrolStopDistance = 0.5f; // distance considered "arrived"

    [Header("Attack Settings")]
    public EnemyAttackType attackType = EnemyAttackType.Both;
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
                else if (distanceToPlayer > detectionRange * 1.5f) // lost player
                    currentState = State.Patrol;
                break;

            case State.Attack:
                AttackPlayer(distanceToPlayer);
                if (distanceToPlayer > attackRange)
                    currentState = State.Pursue;
                break;
        }
    }

    // -------------------
    // Patrol logic
    // -------------------
    private void Patrol()
    {
        if (patrolPoints == null || patrolPoints.Length == 0) return;

        Transform patrolTarget = patrolPoints[currentPatrolIndex];
        float distanceToTarget = Vector2.Distance(transform.position, patrolTarget.position);

        // Walk toward patrol point
        if (distanceToTarget > patrolStopDistance && patrolWaitTimer <= 0f)
        {
            float dir = patrolTarget.position.x > transform.position.x ? 1 : -1;
            movement.MoveAI(dir, false);
        }
        else
        {
            // At patrol point → idle + wait
            movement.GetComponent<AnimationStates>().ChangeState(AnimationStates.State.Idle);

            patrolWaitTimer += Time.deltaTime;

            if (patrolWaitTimer >= waitAtPatrolPoint)
            {
                // Next point (loop around)
                currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
                patrolWaitTimer = 0f;
            }
        }
    }

    // -------------------
    // Pursue logic
    // -------------------
    private void PursuePlayer(float distance)
    {
        if (distance > stopDistance)
        {
            float direction = target.position.x > transform.position.x ? 1 : -1;
            movement.MoveAI(direction, false);
        }
        else
        {
            // idle near player
            movement.GetComponent<AnimationStates>().ChangeState(AnimationStates.State.Idle);
        }
    }

    // -------------------
    // Attack logic
    // -------------------
    private void AttackPlayer(float distance)
    {
        if (Time.time - lastAttackTime < attackCooldown)
            return; // cooldown active

        bool grounded = Mathf.Abs(movement.GetComponent<Rigidbody2D>().velocity.y) < 0.01f;

        if (attackType == EnemyAttackType.Throw || attackType == EnemyAttackType.Both)
        {
            movement.AIThrow();
            // NOTE: AnimationStates already decides idle-loop vs air-throw
        }

        if (attackType == EnemyAttackType.Push || attackType == EnemyAttackType.Both)
        {
            float direction = target.position.x > transform.position.x ? 1 : -1;
            movement.AIPush(direction);
        }

        lastAttackTime = Time.time;
    }

    // -------------------
    // Draw patrol path in editor (debug)
    // -------------------
    void OnDrawGizmosSelected()
    {
        if (patrolPoints != null && patrolPoints.Length > 1)
        {
            Gizmos.color = Color.yellow;
            for (int i = 0; i < patrolPoints.Length; i++)
            {
                Vector3 current = patrolPoints[i].position;
                Vector3 next = patrolPoints[(i + 1) % patrolPoints.Length].position;
                Gizmos.DrawLine(current, next);
                Gizmos.DrawSphere(current, 0.2f);
            }
        }
    }
}
