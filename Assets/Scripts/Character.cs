using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("2DPlatformer/Character")]
public class Character : MonoBehaviour
{
    [Header("Configuration (Dropdown)")]
    [SerializeField] private CharacterConfigurationSO configuration;

    [Header("Stats (Dropdown)")]
    [SerializeField] private CharacterStatsSO stats;

    public enum MovementSource { Character, Configuration }

    [Header("Movement Source")]
    [Tooltip("Choose where to read Walk/Run/Jump from.")]
    public MovementSource movementSource = MovementSource.Configuration;

    [Header("Movement (when source = Character)")]
    public float walkSpeed = 3.5f;
    public float runSpeed = 6.5f;
    public float jumpForce = 11f;

    [Header("Run Key")]
    public KeyCode runKeyPrimary = KeyCode.LeftShift;
    public KeyCode runKeyAlternate = KeyCode.RightShift;

    [Header("Grounding")]
    public LayerMask groundLayers;
    public float groundCheckExtra = 0.1f;

    [Header("References")]
    [SerializeField] private Transform body;
    [SerializeField] private Collider2D feetCollider;
    [SerializeField] private Rigidbody2D rb;

    [Header("Editor Hints")]
    [SerializeField] private bool autoFindChildren = true;

    // ---- runtime ----
    float inputX;
    bool jumpPressed;
    bool runHeld;
    bool grounded;

    // AI/Anim coordination
    private bool aiRunning;       // set by CharacterAI
    private bool preventRest;     // set by CharacterAI during AttackHold

    // ---- expose for other scripts/anim ----
    public CharacterConfigurationSO Config => configuration;
    public CharacterStatsSO Stats => stats;

    public float EffectiveWalkSpeed =>
        (movementSource == MovementSource.Configuration && configuration != null) ? configuration.walkSpeed : walkSpeed;
    public float EffectiveRunSpeed =>
        (movementSource == MovementSource.Configuration && configuration != null) ? configuration.runSpeed : runSpeed;
    public float EffectiveJumpForce =>
        (movementSource == MovementSource.Configuration && configuration != null) ? configuration.jumpForce : jumpForce;

    public bool IsGrounded => grounded;
    public float AbsVelX => rb ? Mathf.Abs(rb.velocity.x) : 0f;
    public bool HasMoveInput => Mathf.Abs(inputX) > 0.01f;

    public bool IsRunHeld => runHeld;          // player intent
    public bool AIRunning => aiRunning;        // AI intent
    public void SetAIRunning(bool v) => aiRunning = v;

    public bool PreventRest => preventRest;    // suppress Rest while true
    public void SetPreventRest(bool v) => preventRest = v;

    void Reset()
    {
        rb = GetComponent<Rigidbody2D>();
        TryAutoFindChildren();
    }

    void OnValidate()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (autoFindChildren) TryAutoFindChildren();
        EnsureAnimationComponent();
        EnsureCombatAndAIComponents();
        if (rb) rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    void Start()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        EnsureAnimationComponent();
        EnsureCombatAndAIComponents();
        if (rb) rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    void Update()
    {
        if (configuration == null) return;

        if (configuration.isPlayer)
        {
            inputX = Input.GetAxisRaw("Horizontal");
            runHeld = Input.GetKey(runKeyPrimary) || Input.GetKey(runKeyAlternate);
            if (Input.GetButtonDown("Jump")) jumpPressed = true;
        }
    }

    void FixedUpdate()
    {
        grounded = CheckGrounded();

        if (configuration != null && configuration.isPlayer)
        {
            HandlePlayerMovement();
        }
        // AI movement is in CharacterAI
    }

    void HandlePlayerMovement()
    {
        float targetX = 0f;

        if (Mathf.Abs(inputX) > 0.01f)
        {
            float speed = runHeld ? EffectiveRunSpeed : EffectiveWalkSpeed;
            targetX = speed * inputX;

            if (body != null)
            {
                var s = body.localScale;
                s.x = Mathf.Sign(inputX) * Mathf.Abs(s.x);
                body.localScale = s;
            }
        }

        Vector2 v = rb.velocity;
        v.x = targetX;
        rb.velocity = v;

        if (jumpPressed && grounded)
            rb.velocity = new Vector2(rb.velocity.x, EffectiveJumpForce);

        jumpPressed = false;
    }

    bool CheckGrounded()
    {
        if (feetCollider == null) return false;
        var b = feetCollider.bounds;
        var hit = Physics2D.BoxCast(b.center, b.size, 0f, Vector2.down, groundCheckExtra, groundLayers);
        return hit.collider != null;
    }

    void TryAutoFindChildren()
    {
        if (body == null) body = transform.Find("Body");
        if (body == null)
        {
            foreach (var t in GetComponentsInChildren<Transform>(true))
                if (t.name == "Body") { body = t; break; }
        }
        if (feetCollider == null)
        {
            var feet = transform.Find("Feet Collider") ?? transform.Find("FeetCollider");
            if (feet != null) feetCollider = feet.GetComponent<Collider2D>();
            if (feetCollider == null) feetCollider = GetComponentInChildren<Collider2D>();
        }
    }

    void EnsureAnimationComponent()
    {
        if (body == null) return;
        var c = body.GetComponent<CharacterAnimations>();
        if (c == null) c = body.gameObject.AddComponent<CharacterAnimations>();
        c.BindCharacter(this);
    }

    void EnsureCombatAndAIComponents()
    {
        if (configuration == null) return;
        var ai = GetComponent<CharacterAI>();
        var aic = GetComponent<CharacterAICombat>();
        var pc = GetComponent<CharacterCombat>();

        if (configuration.isPlayer)
        {
            if (pc == null) pc = gameObject.AddComponent<CharacterCombat>();
            if (ai != null) DestroyImmediate(ai);
            if (aic != null) DestroyImmediate(aic);
        }
        else
        {
            if (ai == null) ai = gameObject.AddComponent<CharacterAI>();
            if (aic == null) aic = gameObject.AddComponent<CharacterAICombat>();
            if (pc != null) DestroyImmediate(pc);
        }
    }

    // API for inspector dropdowns
    public void SetConfiguration(CharacterConfigurationSO newConfig) => configuration = newConfig;
    public void SetStats(CharacterStatsSO newStats) => stats = newStats;
}
