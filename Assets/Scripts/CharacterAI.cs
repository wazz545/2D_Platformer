using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("2DPlatformer/Character AI")]
public class CharacterAI : MonoBehaviour
{
    [Header("Patrol (Optional)")]
    public Transform[] patrolPoints;
    public float waypointSnapDistance = 0.1f;

    [Header("Attack Hold")]
    [Tooltip("Extra distance beyond attackRange before resuming chase (prevents jitter).")]
    public float attackHoldBuffer = 0.25f;
    [Tooltip("How fast horizontal velocity is braked to zero while holding.")]
    public float holdBrake = 50f;

    private int patrolIndex;
    private Character ch;
    private Rigidbody2D rb;
    private Transform player;

    enum Brain { Idle, Patrol, Chase, AttackHold }
    Brain brain;

    void Awake()
    {
        ch = GetComponent<Character>();
        rb = GetComponent<Rigidbody2D>();
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p) player = p.transform;
        brain = (patrolPoints != null && patrolPoints.Length > 1) ? Brain.Patrol : Brain.Idle;
    }

    void FixedUpdate()
    {
        if (ch == null || ch.Config == null || ch.Config.isPlayer) return;

        float chaseR = ch.Config.chaseRange;
        float atkR = ch.Config.attackRange;
        float giveUpR = ch.Config.giveUpRange;

        float dist = player ? Vector2.Distance(transform.position, player.position) : Mathf.Infinity;

        if (player)
        {
            if (dist <= atkR) brain = Brain.AttackHold;
            else if (dist <= chaseR) brain = Brain.Chase;
            else if (dist >= giveUpR) brain = (patrolPoints != null && patrolPoints.Length > 1) ? Brain.Patrol : Brain.Idle;
        }

        switch (brain)
        {
            case Brain.Idle: Idle(); break;
            case Brain.Patrol: Patrol(); break;
            case Brain.Chase: Chase(); break;
            case Brain.AttackHold: AttackHold(); break;
        }
    }

    void Idle()
    {
        ch.SetAIRunning(false);
        ch.SetPreventRest(false);
        rb.velocity = new Vector2(0, rb.velocity.y);
        Face(0f);
    }

    void Patrol()
    {
        ch.SetAIRunning(false); // WALK anim
        ch.SetPreventRest(false);

        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            Idle();
            return;
        }

        float speed = ch.EffectiveWalkSpeed;
        Transform target = patrolPoints[patrolIndex];
        float dir = Mathf.Sign(target.position.x - transform.position.x);
        float dx = Mathf.Abs(target.position.x - transform.position.x);

        rb.velocity = new Vector2(dir * speed, rb.velocity.y);
        Face(dir);

        if (dx <= waypointSnapDistance)
            patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
    }

    void Chase()
    {
        ch.SetAIRunning(true); // RUN anim
        ch.SetPreventRest(false);

        if (!player) { Idle(); return; }

        float dir = Mathf.Sign(player.position.x - transform.position.x);
        rb.velocity = new Vector2(dir * ch.EffectiveRunSpeed, rb.velocity.y);
        Face(dir);
    }

    void AttackHold()
    {
        ch.SetAIRunning(false); // not running while striking
        ch.SetPreventRest(true); // keep Idle, do not drift into Rest

        if (!player) { Idle(); return; }

        // Smoothly brake horizontal movement and face the player
        float newX = Mathf.MoveTowards(rb.velocity.x, 0f, holdBrake * Time.fixedDeltaTime);
        rb.velocity = new Vector2(newX, rb.velocity.y);
        Face(Mathf.Sign(player.position.x - transform.position.x));

        // Exit AttackHold if player moves out of range + buffer
        float d = Vector2.Distance(transform.position, player.position);
        if (d > ch.Config.attackRange + attackHoldBuffer)
        {
            brain = Brain.Chase;
        }
        else if (d >= ch.Config.giveUpRange)
        {
            brain = (patrolPoints != null && patrolPoints.Length > 1) ? Brain.Patrol : Brain.Idle;
        }
    }

    void Face(float dir)
    {
        var body = transform.Find("Body");
        if (!body) return;
        if (Mathf.Abs(dir) > 0.01f)
        {
            var s = body.localScale;
            s.x = Mathf.Sign(dir) * Mathf.Abs(s.x);
            body.localScale = s;
        }
    }
}
