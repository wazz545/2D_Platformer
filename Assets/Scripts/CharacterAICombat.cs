using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("2DPlatformer/Character Combat (AI)")]
public class CharacterAICombat : MonoBehaviour
{
    [Header("Timing")]
    public float attackCooldown = 1.2f;

    private float cd;
    private Character character;
    private CharacterAnimations anim;
    private Transform player;

    void Awake()
    {
        character = GetComponent<Character>();
        var body = transform.Find("Body");
        if (body != null) anim = body.GetComponent<CharacterAnimations>();
        var playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO != null) player = playerGO.transform;
    }

    void Update()
    {
        if (character == null || character.Config == null || character.Config.isPlayer) return;
        if (player == null) return;

        cd -= Time.deltaTime;

        float dist = Vector2.Distance(transform.position, player.position);
        if (dist <= character.Config.attackRange && cd <= 0f)
        {
            if (Random.value < 0.5f) anim?.PlayPush(); else anim?.PlayThrow();
            cd = attackCooldown;
        }
    }
}
