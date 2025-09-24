using UnityEngine;

[RequireComponent(typeof(PlayerAnimation), typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Ground Movement")]
    public float walkSpeed = 3f;
    public float runSpeed = 6f;

    [Header("Jump Settings")]
    public float walkJumpForce = 10f;
    public float runJumpForce = 14f;

    public float walkJumpDistance = 1.5f;
    public float runJumpDistance = 3.0f;

    public float walkJumpTime = 0.6f;  // seconds from jump → land
    public float runJumpTime = 0.9f;

    public bool allowDoubleJump = true;
    public float doubleJumpForce = 8f;

    [Header("Physics")]
    public float gravityScale = 3f;
    public float airControl = 3f;  // how strongly player can move in air

    [Header("Debug (read-only)")]
    [SerializeField] private bool isGrounded = true;

    private RectTransform rt;
    private PlayerAnimation anim;
    private Rigidbody2D rb;

    private bool canDoubleJump = false;
    private bool isRunning = false;
    private float jumpStartTime;

    void Start()
    {
        rt = GetComponent<RectTransform>();
        anim = GetComponent<PlayerAnimation>();
        rb = GetComponent<Rigidbody2D>();

        rb.gravityScale = gravityScale;
        anim.SetRigidbody(rb);
        anim.SetGrounded(isGrounded);
    }

    void Update()
    {
        if (rb.gravityScale != gravityScale) rb.gravityScale = gravityScale;

        float move = Input.GetAxisRaw("Horizontal");
        bool run = Input.GetKey(KeyCode.LeftShift);
        isRunning = run;

        // ----- Grounded movement -----
        if (anim.CurrentState != PlayerAnimation.State.Jump)
        {
            if (move != 0)
            {
                float speed = run ? runSpeed : walkSpeed;
                rt.anchoredPosition += new Vector2(move * speed * Time.deltaTime, 0f);
                rt.localScale = new Vector3(move > 0 ? 1 : -1, 1, 1);

                anim.ChangeState(run ? PlayerAnimation.State.Run : PlayerAnimation.State.Walk);
            }
            else if (isGrounded)
            {
                if (anim.CurrentState == PlayerAnimation.State.Walk) anim.WalkToIdle();
                else if (anim.CurrentState == PlayerAnimation.State.Run) anim.RunToIdle();
                else if (anim.CurrentState != PlayerAnimation.State.Rest) anim.ChangeState(PlayerAnimation.State.Idle);
            }
        }
        else
        {
            // ----- Air control -----
            if (move != 0)
            {
                float airSpeed = airControl * Time.deltaTime;
                rt.anchoredPosition += new Vector2(move * airSpeed, 0f);
                rt.localScale = new Vector3(move > 0 ? 1 : -1, 1, 1);
            }
        }

        // ----- Jump -----
        if (Input.GetKeyDown(KeyCode.Space) && anim.CurrentState != PlayerAnimation.State.Stunned)
        {
            if (isGrounded)
            {
                DoJump(run, move);
            }
            else if (allowDoubleJump && canDoubleJump)
            {
                rb.velocity = new Vector2(move * walkJumpDistance, doubleJumpForce);
                canDoubleJump = false;
                anim.DoJump();
            }
        }
    }

    private void DoJump(bool running, float move)
    {
        float jumpForce = running ? runJumpForce : walkJumpForce;
        float jumpDist = running ? runJumpDistance : walkJumpDistance;
        float jumpTime = running ? runJumpTime : walkJumpTime;

        // vertical velocity
        rb.velocity = new Vector2(move * jumpDist, jumpForce);

        // adjust gravity for jump duration (shorter/longer hang)
        rb.gravityScale = gravityScale * (0.6f / jumpTime);

        isGrounded = false;
        canDoubleJump = allowDoubleJump;
        jumpStartTime = Time.time;

        anim.SetGrounded(false);
        anim.DoJump();
    }

    void OnCollisionEnter2D(Collision2D c)
    {
        if (c.contacts.Length > 0 && c.contacts[0].normal.y > 0.5f)
        {
            isGrounded = true;
            canDoubleJump = false;
            anim.SetGrounded(true);

            rb.gravityScale = gravityScale; // reset gravity after landing
        }
    }

    void OnCollisionExit2D(Collision2D c)
    {
        isGrounded = false;
        anim.SetGrounded(false);
    }
}
