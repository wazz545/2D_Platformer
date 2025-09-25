using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class AnimationStates : MonoBehaviour
{
    [Header("Config Reference")]
    public CharacterConfig config;

    [Header("Animation Frames")]
    public Image[] idleFrames;
    public Image[] walkFrames;
    public Image[] runFrames;
    // Jump: [0]=rise1, [1]=rise2, [2]=rise3, [3]=apex, [4]=fall, [5]=land
    public Image[] jumpFrames;
    public Image[] restFrames;
    public Image[] throwFrames;
    public Image[] pushFrames;
    public Image[] stunnedFrames;
    public Image[] hitBackFrames;

    public enum State { Idle, Walk, Run, Jump, Rest, Throw, Push, HitBack, Stunned }
    public State CurrentState => currentState;

    private State currentState = State.Idle;
    private State prevGroundedState = State.Idle;

    private Rigidbody2D rb;
    private float frameTimer;
    private int currentFrame;
    private float idleTimer;

    private bool grounded;
    private Coroutine landingRoutine;

    // Air-throw helpers
    private bool airThrowOnce = false;
    private bool airThrowDone = false;

    // Force a tiny fall flash so you always see fall before land if needed
    private const float fallFlashMin = 0.05f;

    public bool IsThrowing => currentState == State.Throw;

    // ---------- Setup ----------
    public void SetRigidbody(Rigidbody2D body) => rb = body;

    public void SetGrounded(bool isGrounded)
    {
        grounded = isGrounded;
        if (grounded && currentState == State.Jump && landingRoutine == null)
            landingRoutine = StartCoroutine(LandingSequence());
    }

    void Start() => ShowFrame(idleFrames, 0);

    void Update()
    {
        frameTimer += Time.deltaTime;

        switch (currentState)
        {
            case State.Idle:
                idleTimer += Time.deltaTime;
                if (idleTimer >= config.restDelay) ChangeState(State.Rest);
                AnimateLoop(idleFrames, config.idleRate);
                break;

            case State.Walk: AnimateLoop(walkFrames, config.walkRate); break;
            case State.Run: AnimateLoop(runFrames, config.runRate); break;
            case State.Rest: AnimateOnceThenHold(restFrames, config.restRate); break;

            case State.Jump: HandleJumpCycle(); break;
            case State.Throw: HandleThrowCycle(); break;
            case State.Push: AnimateLoop(pushFrames, config.pushRate); break;

            case State.HitBack:
                AnimateOnceThenCallback(hitBackFrames, config.hitBackRate, () => ChangeState(State.Stunned));
                break;

            case State.Stunned:
                AnimateOnceThenHold(stunnedFrames, config.stunnedRate);
                break;
        }
    }

    // ---------- Jump ----------
    // -------------------
    // Jump cycle
    // -------------------
    private void HandleJumpCycle()
    {
        if (jumpFrames == null || jumpFrames.Length != 6 || rb == null) return;

        float vy = rb.velocity.y;

        if (!grounded && vy > 0f)
        {
            // Rising: 0→1→2→3 at jumpAnimRate
            if (frameTimer >= config.jumpAnimRate)
            {
                frameTimer = 0f;
                if (currentFrame < 3) currentFrame++;
                ShowFrame(jumpFrames, currentFrame); // rise1, rise2, rise3, apex
            }
        }
        else if (!grounded && vy <= 0f)
        {
            // Falling: always force fall frame
            ShowFrame(jumpFrames, 4);
        }
    }

    // -------------------
    // Landing coroutine
    // -------------------
    private IEnumerator LandingSequence()
    {
        // Always show land frame when grounded
        ShowFrame(jumpFrames, 5);
        yield return new WaitForSeconds(config.landingDelay);

        landingRoutine = null;

        // Restore state from before jump
        if (prevGroundedState == State.Run) ChangeState(State.Run);
        else if (prevGroundedState == State.Walk) ChangeState(State.Walk);
        else ChangeState(State.Idle);
    }


    // ---------- Throw ----------
    private void HandleThrowCycle()
    {
        if (airThrowOnce)
        {
            // Air throw: play once, then force fall
            if (!airThrowDone)
            {
                AnimateOnceThenCallback(throwFrames, config.throwRate, () =>
                {
                    airThrowDone = true;
                    ShowFrame(throwFrames, throwFrames.Length - 1);
                });
            }
            else
            {
                ChangeState(State.Jump);
                ShowFrame(jumpFrames, 4); // fall
            }
        }
        else
        {
            // Ground throw loops while held
            AnimateLoop(throwFrames, config.throwRate);
        }
    }

    // ---------- Helpers ----------
    private void AnimateLoop(Image[] frames, float rate)
    {
        if (frames == null || frames.Length == 0) return;
        if (frameTimer >= rate)
        {
            frameTimer = 0f;
            currentFrame = (currentFrame + 1) % frames.Length;
            ShowFrame(frames, currentFrame);
        }
    }

    private void AnimateOnceThenHold(Image[] frames, float rate, System.Action onComplete = null)
    {
        if (frames == null || frames.Length == 0) return;
        if (frameTimer >= rate)
        {
            frameTimer = 0f;
            if (currentFrame < frames.Length - 1) currentFrame++;
            else onComplete?.Invoke();
            ShowFrame(frames, currentFrame);
        }
    }

    private void AnimateOnceThenCallback(Image[] frames, float rate, System.Action onComplete)
    {
        if (frames == null || frames.Length == 0) return;
        if (frameTimer >= rate)
        {
            frameTimer = 0f;
            currentFrame++;
            if (currentFrame >= frames.Length)
            {
                currentFrame = frames.Length - 1;
                ShowFrame(frames, currentFrame);
                onComplete?.Invoke();
                return;
            }
            ShowFrame(frames, currentFrame);
        }
    }

    private void ShowFrame(Image[] frames, int index)
    {
        DisableAll();
        if (frames != null && index >= 0 && index < frames.Length && frames[index] != null)
            frames[index].enabled = true;
    }

    private int GetCurrentlyShownIndex(Image[] frames)
    {
        if (frames == null) return -1;
        for (int i = 0; i < frames.Length; i++)
            if (frames[i] != null && frames[i].enabled) return i;
        return -1;
    }

    private void DisableAll()
    {
        void Off(Image[] arr) { if (arr != null) foreach (var i in arr) if (i) i.enabled = false; }
        Off(idleFrames); Off(walkFrames); Off(runFrames); Off(jumpFrames);
        Off(restFrames); Off(throwFrames); Off(pushFrames); Off(stunnedFrames); Off(hitBackFrames);
    }

    // ---------- State entry ----------
    public void ChangeState(State newState, bool onceThrow = false)
    {
        if (newState == currentState) return;

        if (newState == State.Jump &&
            (currentState == State.Idle || currentState == State.Walk || currentState == State.Run))
            prevGroundedState = currentState;

        if (landingRoutine != null && newState != State.Jump)
        {
            StopCoroutine(landingRoutine);
            landingRoutine = null;
        }

        currentState = newState;
        airThrowOnce = (newState == State.Throw) && onceThrow;
        currentFrame = 0;
        frameTimer = 0f;
        idleTimer = 0f;
        airThrowDone = false;

        switch (newState)
        {
            case State.Idle: ShowFrame(idleFrames, 0); break;
            case State.Walk: ShowFrame(walkFrames, 0); break;
            case State.Run: ShowFrame(runFrames, 0); break;
            case State.Jump: ShowFrame(jumpFrames, 0); break;  // rise1
            case State.Rest: ShowFrame(restFrames, 0); break;
            case State.Throw: ShowFrame(throwFrames, 0); break;
            case State.Push: ShowFrame(pushFrames, 0); break;
            case State.HitBack: ShowFrame(hitBackFrames, 0); break;
            case State.Stunned: ShowFrame(stunnedFrames, 0); break;
        }
    }

    public void StartJump(float _ignoredDuration, float distance = 0f)
    {
        ChangeState(State.Jump);
    }

    public void StartFall()
    {
        if (currentState == State.Throw && airThrowOnce) return;
        ChangeState(State.Jump);
        ShowFrame(jumpFrames, 4); // fall
    }

    public void ResetFromStunned()
    {
        if (landingRoutine != null) { StopCoroutine(landingRoutine); landingRoutine = null; }
        ChangeState(State.Idle);
    }
}
