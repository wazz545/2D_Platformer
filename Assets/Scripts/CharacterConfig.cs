using UnityEngine;

[CreateAssetMenu(fileName = "CharacterConfig", menuName = "Configs/Character Config", order = 1)]
public class CharacterConfig : ScriptableObject
{
    [Header("Movement Speeds")]
    [Tooltip("Walking speed in units per second.")]
    public float walkSpeed = 3f;
    [Tooltip("Running speed in units per second.")]
    public float runSpeed = 6f;
    [Tooltip("Pushing speed in units per second.")]
    public float pushSpeed = 2f;

    [Header("Jump Settings")]
    [Tooltip("Jump force while walking.")]
    public float walkJumpForce = 10f;
    [Tooltip("Jump force while running.")]
    public float runJumpForce = 14f;
    [Tooltip("Force applied for double jump.")]
    public float doubleJumpForce = 8f;
    [Tooltip("Can the character perform a double jump?")]
    public bool allowDoubleJump = true;

    [Header("Physics")]
    [Tooltip("Gravity multiplier applied to Rigidbody2D.")]
    public float gravityScale = 3f;
    [Tooltip("Horizontal control power while in air (used for small nudges while throwing/jumping).")]
    public float airControl = 3f;
    [Tooltip("Layers considered as ground for raycast checks.")]
    public LayerMask groundMask;
    [Tooltip("Raycast distance for ground detection.")]
    public float groundCheckDistance = 0.1f;
    [Tooltip("Extra time (seconds) allowed to still jump after leaving ground.")]
    public float coyoteTime = 0.1f;

    [Header("Animation Rates")]
    [Tooltip("Idle animation cycle speed.")]
    public float idleRate = 0.30f;
    [Tooltip("Walk animation cycle speed.")]
    public float walkRate = 0.20f;
    [Tooltip("Run animation cycle speed.")]
    public float runRate = 0.10f;
    [Tooltip("Jump animation cycle speed for rise1→rise2→rise3→apex.")]
    public float jumpAnimRate = 0.15f;
    [Tooltip("Resting animation speed.")]
    public float restRate = 0.40f;
    [Tooltip("Push animation speed.")]
    public float pushRate = 0.18f;
    [Tooltip("Stunned animation speed.")]
    public float stunnedRate = 0.25f;
    [Tooltip("Hit-back animation speed.")]
    public float hitBackRate = 0.20f;

    [Header("Throw")]
    [Tooltip("Time between throw frames.")]
    public float throwRate = 0.15f;

    [Header("Delays")]
    [Tooltip("Delay before switching to Rest state while idle.")]
    public float restDelay = 5f;
    [Tooltip("Delay after showing the Land frame.")]
    public float landingDelay = 0.10f;

    [Header("Runtime Info (Read-Only)")]
    [HideInInspector] public float jumpDistance;
}
