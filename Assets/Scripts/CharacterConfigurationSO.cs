using UnityEngine;

[CreateAssetMenu(fileName = "CharConfig_", menuName = "2DPlatformer/Character Configuration")]
public class CharacterConfigurationSO : ScriptableObject
{
    [Header("Identity")]
    public string displayName = "Default";

    [Header("Control Mode")]
    public bool isPlayer = true;

    [Header("Movement (optional, used when Character.MovementSource = Configuration)")]
    public float walkSpeed = 3.5f;
    public float runSpeed = 6.5f;
    public float jumpForce = 11f;

    [Header("AI Ranges")]
    [Tooltip("Start chasing when player is inside this distance")]
    public float chaseRange = 7f;
    [Tooltip("Consider 'in range to attack' at this distance")]
    public float attackRange = 2.2f;
    [Tooltip("Stop chasing when player is beyond this distance")]
    public float giveUpRange = 11f;
}
