using UnityEngine;

[CreateAssetMenu(fileName = "CharacterConfig", menuName = "Configs/Character Config", order = 1)]
public class CharacterConfig : ScriptableObject
{
    [Header("Movement")]
    public float walkSpeed = 3f;
    public float runSpeed = 6f;
    public float pushSpeed = 2f;

    [Header("Jump")]
    public float walkJumpForce = 10f;
    public float runJumpForce = 14f;
    public float doubleJumpForce = 8f;
    public bool allowDoubleJump = true;

    public float walkJumpTime = 0.6f;
    public float runJumpTime = 0.9f;

    [Header("Coyote Time")]
    public float coyoteTime = 0.1f;

    [Header("Physics")]
    public float gravityScale = 3f;
    public float airControl = 3f;
    public LayerMask groundMask;
    public float groundCheckDistance = 0.1f;

    [Header("Throw Settings")]
    public float throwRate = 0.15f;

    [Header("Animation Rates")]
    public float idleRate = 0.3f;
    public float walkRate = 0.2f;
    public float runRate = 0.1f;
    public float restRate = 0.4f;
    public float pushRate = 0.18f;
    public float stunnedRate = 0.25f;
    public float hitBackRate = 0.2f;

    [Header("Delays")]
    public float restDelay = 5f;
    public float landingDelay = 0.1f;

    [Header("Runtime Info (Read-Only)")]
    [HideInInspector] public float jumpDistance;
}
