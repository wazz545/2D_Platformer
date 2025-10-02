using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
[AddComponentMenu("2DPlatformer/Character Animations")]
public class CharacterAnimations : MonoBehaviour
{
    [Header("Binding")]
    [SerializeField] private Character character;

    [Header("Playback")]
    public float movementFPS = 10f;
    public float combatFPS = 12f;

    [Header("Idle → Rest")]
    [Tooltip("Seconds idle (grounded, no input) before Rest.")]
    public float idleToRestSeconds = 2f;

    private SpriteRenderer sr;
    private readonly Dictionary<string, Sprite[]> sets = new();
    private float timer; int frame;
    private float idleTimer;
    private bool holdingRestLastFrame;

    // Combat control
    private bool playingCombat;
    private string combatKey;

    // keys
    const string K_IDLE = "Idle", K_WALK = "Walking", K_RUN = "Running", K_JUMP = "Jump", K_REST = "Rest";
    const string KC_PUSH = "Push_PowerFromHands", KC_THROW = "Throw";

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (character == null) character = GetComponentInParent<Character>();
        LoadAll();
    }

    public void BindCharacter(Character c) => character = c;

    void LoadAll()
    {
        sets[K_IDLE] = Resources.LoadAll<Sprite>("Images/Character/CharacterMovement/Idle");
        sets[K_WALK] = Resources.LoadAll<Sprite>("Images/Character/CharacterMovement/Walking");
        sets[K_RUN] = Resources.LoadAll<Sprite>("Images/Character/CharacterMovement/Running");
        sets[K_JUMP] = Resources.LoadAll<Sprite>("Images/Character/CharacterMovement/Jump");
        sets[K_REST] = Resources.LoadAll<Sprite>("Images/Character/CharacterMovement/Rest");

        sets[KC_PUSH] = Resources.LoadAll<Sprite>("Images/Character/CharacterCombat/" + KC_PUSH);
        sets[KC_THROW] = Resources.LoadAll<Sprite>("Images/Character/CharacterCombat/" + KC_THROW);
    }

    void Update()
    {
        if (sr == null || character == null) return;

        // --- Combat clip takes priority until it finishes ---
        if (playingCombat)
        {
            PlaySet(combatKey, combatFPS, loop: false, holdLast: false, isCombat: true);
            return;
        }

        bool grounded = character.IsGrounded;
        float speed = character.AbsVelX;
        bool moving = grounded && speed > 0.05f;

        // break Rest hold on movement or leaving ground
        if (holdingRestLastFrame && (!grounded || moving))
        {
            holdingRestLastFrame = false;
            frame = 0; timer = 0;
        }

        if (!grounded)
        {
            idleTimer = 0f;
            PlaySet(K_JUMP, movementFPS, loop: true);
            return;
        }

        if (moving)
        {
            idleTimer = 0f;

            // Intent-based running: Player run key OR AI chase
            bool wantRun = (character.Config != null && character.Config.isPlayer) ? character.IsRunHeld
                                                                                   : character.AIRunning;

            PlaySet(wantRun ? K_RUN : K_WALK, movementFPS, loop: true);
            return;
        }

        // idle (no input). If AI told us to suppress Rest (AttackHold), stay on Idle.
        idleTimer += Time.deltaTime;

        if (character.PreventRest)
        {
            idleTimer = 0f;
            PlaySet(K_IDLE, movementFPS, loop: true);
            return;
        }

        if (idleTimer >= idleToRestSeconds && sets[K_REST] != null && sets[K_REST].Length > 0)
        {
            PlaySet(K_REST, movementFPS, loop: false, holdLast: true);
            return;
        }

        PlaySet(K_IDLE, movementFPS, loop: true);
    }

    void PlaySet(string key, float fps, bool loop, bool holdLast = false, bool isCombat = false)
    {
        var arr = sets.TryGetValue(key, out var s) ? s : null;
        if (arr == null || arr.Length == 0) return;

        if (holdLast && holdingRestLastFrame)
        {
            sr.sprite = arr[arr.Length - 1];
            return;
        }

        timer += Time.deltaTime;
        float step = 1f / Mathf.Max(1f, fps);

        while (timer >= step)
        {
            timer -= step;
            frame++;
            if (loop && frame >= arr.Length) frame = 0;
        }

        int idx = Mathf.Clamp(frame, 0, arr.Length - 1);
        sr.sprite = arr[idx];

        if (!loop && frame >= arr.Length - 1)
        {
            if (holdLast) holdingRestLastFrame = true;
            if (isCombat) { playingCombat = false; combatKey = null; }
            frame = Mathf.Min(frame, arr.Length - 1);
        }
    }

    // ---- public for Combat ----
    public void PlayPush() { StartCombat(KC_PUSH); }
    public void PlayThrow() { StartCombat(KC_THROW); }

    void StartCombat(string key)
    {
        holdingRestLastFrame = false;
        playingCombat = true;
        combatKey = key;
        frame = 0; timer = 0;
    }
}
