using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("2DPlatformer/Character Combat (Player)")]
public class CharacterCombat : MonoBehaviour
{
    private Character character;
    private CharacterAnimations anim;

    void Awake()
    {
        character = GetComponent<Character>();
        if (character != null && character.Config != null && !character.Config.isPlayer)
        {
            Destroy(this);
            return;
        }

        var body = transform.Find("Body");
        if (body != null) anim = body.GetComponent<CharacterAnimations>();
    }

    void Update()
    {
        if (character == null || character.Config == null || !character.Config.isPlayer) return;

        if (Input.GetKeyDown(KeyCode.J)) anim?.PlayPush();
        if (Input.GetKeyDown(KeyCode.K)) anim?.PlayThrow();
    }
}
