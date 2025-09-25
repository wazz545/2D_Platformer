using UnityEngine;

[RequireComponent(typeof(AnimationStates), typeof(Rigidbody2D))]
public class CharacterMovement : MonoBehaviour
{
    [Header("Config")]
    public CharacterConfig config;
    public bool isPlayer = true;   // Toggle: Player uses input, Enemy uses AI

    private AnimationStates anim;
    private Rigidbody2D rb;
    private bool isGrounded;
    private bool canDoubleJump;
    private float coyoteTimer;

    void Start()
    {
        anim = GetComponent<AnimationStates>();
        rb = GetComponent<Rigidbody2D>();

        rb.gravityScale = config.gravityScale;
        anim.SetRigidbody(rb);
    }

    void Update()
    {
        GroundCheck();

        if (isPlayer)
            HandlePlayerInput();
    }

    private void GroundCheck()
    {
        bool hitGround = Physics2D.Raycast(transform.position, Vector2.down, config.groundCheckDistance, config.groundMask);
        if (hitGround)
        {
            isGrounded = true;
            coyoteTimer = config.coyoteTime;
        }
        else
        {
            if (coyoteTimer > 0f) coyoteTimer -= Time.deltaTime;
            else isGrounded = false;
        }

        anim.SetGrounded(isGrounded);
    }

    // ======================
    // Player Input
    // ======================
    private void HandlePlayerInput()
    {
        float move = Input.GetAxisRaw("Horizontal");
        bool run = Input.GetKey(KeyCode.LeftShift);
        bool push = Input.GetKey(KeyCode.E);

        // Throw
        if (Input.GetKey(KeyCode.F))
        {
            DoThrow();
        }
        else if (anim.CurrentState == AnimationStates.State.Throw && isGrounded)
        {
            ReturnToGrounded(move, run);
        }

        if (anim.IsThrowing)
        {
            HandleMove(move, run, true);
            return;
        }

        // Push
        if (push && isGrounded)
        {
            DoPush(move);
            return;
        }

        // Walk/Run
        HandleMove(move, run, false);

        // Jump
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (isGrounded || coyoteTimer > 0f)
            {
                DoJump(run, move);
            }
            else if (config.allowDoubleJump && canDoubleJump)
            {
                DoDoubleJump(move);
            }
        }

        // Knockback test
        if (Input.GetKeyDown(KeyCode.K))
            anim.ChangeState(AnimationStates.State.HitBack);

        // Fall off ledge
        if (!isGrounded && rb.velocity.y < 0 &&
            anim.CurrentState != AnimationStates.State.Jump &&
            anim.CurrentState != AnimationStates.State.Throw)
        {
            anim.StartFall();
        }
    }

    // ======================
    // Shared Movement
    // ======================
    private void HandleMove(float move, bool run, bool isThrowing)
    {
        if (anim.CurrentState != AnimationStates.State.Jump &&
            anim.CurrentState != AnimationStates.State.Stunned &&
            anim.CurrentState != AnimationStates.State.HitBack)
        {
            if (move != 0)
            {
                float speed = run ? config.runSpeed : config.walkSpeed;
                if (isThrowing && !isGrounded) speed = config.airControl;

                transform.Translate(new Vector2(move * speed * Time.deltaTime, 0f));
                transform.localScale = new Vector3(move > 0 ? 1 : -1, 1, 1);

                if (!isThrowing)
                    anim.ChangeState(run ? AnimationStates.State.Run : AnimationStates.State.Walk);
            }
            else if (isGrounded && !isThrowing)
            {
                anim.ChangeState(AnimationStates.State.Idle);
            }
        }
        else if (anim.CurrentState == AnimationStates.State.Jump && move != 0)
        {
            float airSpeed = config.airControl * Time.deltaTime;
            transform.Translate(new Vector2(move * airSpeed, 0f));
            transform.localScale = new Vector3(move > 0 ? 1 : -1, 1, 1);
        }
    }

    private void ReturnToGrounded(float move, bool run)
    {
        if (!isGrounded) anim.ChangeState(AnimationStates.State.Jump);
        else if (Mathf.Abs(move) > 0)
            anim.ChangeState(run ? AnimationStates.State.Run : AnimationStates.State.Walk);
        else
            anim.ChangeState(AnimationStates.State.Idle);
    }

    private void DoThrow()
    {
        if (isGrounded && anim.CurrentState == AnimationStates.State.Idle)
            anim.ChangeState(AnimationStates.State.Throw, false); // loop
        else if (!isGrounded && anim.CurrentState == AnimationStates.State.Jump)
            anim.ChangeState(AnimationStates.State.Throw, true);  // once
    }

    private void DoPush(float move)
    {
        anim.ChangeState(AnimationStates.State.Push);
        if (move != 0)
        {
            transform.Translate(new Vector2(move * config.pushSpeed * Time.deltaTime, 0f));
            transform.localScale = new Vector3(move > 0 ? 1 : -1, 1, 1);
        }
    }

    private void DoJump(bool running, float move)
    {
        float jumpForce = running ? config.runJumpForce : config.walkJumpForce;
        float jumpTime = running ? config.runJumpTime : config.walkJumpTime;
        float speed = running ? config.runSpeed : config.walkSpeed;

        rb.velocity = new Vector2(move * speed, jumpForce);

        isGrounded = false;
        canDoubleJump = config.allowDoubleJump;

        float distance = CalculateJumpDistance(jumpForce, speed);
        config.jumpDistance = distance;

        anim.StartJump(jumpTime, distance);
    }

    private void DoDoubleJump(float move)
    {
        rb.velocity = new Vector2(move * config.walkSpeed, config.doubleJumpForce);
        canDoubleJump = false;

        float distance = CalculateJumpDistance(config.doubleJumpForce, config.walkSpeed);
        config.jumpDistance = distance;

        anim.StartJump(config.walkJumpTime, distance);
    }

    private float CalculateJumpDistance(float jumpForce, float horizSpeed)
    {
        float g = -Physics2D.gravity.y * config.gravityScale;
        float totalAirTime = (2f * jumpForce) / g;
        return horizSpeed * totalAirTime;
    }

    // ======================
    // AI Control Methods
    // ======================
    public void MoveAI(float direction, bool run)
    {
        HandleMove(direction, run, false);
    }

    public void AIThrow()
    {
        DoThrow();
    }

    public void AIPush(float direction)
    {
        DoPush(direction);
    }

    public void AIJump(bool running, float direction)
    {
        DoJump(running, direction);
    }

    public void AIDoubleJump(float direction)
    {
        if (config.allowDoubleJump && canDoubleJump)
            DoDoubleJump(direction);
    }
}
