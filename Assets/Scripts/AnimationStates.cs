using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class AnimationStates : MonoBehaviour
{
    [Header("Config")]
    public CharacterConfig config;

    [Header("Frames")]
    public Image[] idleFrames;
    public Image[] walkFrames;
    public Image[] runFrames;
    public Image[] jumpFrames;
    public Image[] restFrames;
    public Image[] throwFrames;
    public Image[] pushFrames;
    public Image[] stunnedFrames;
    public Image[] hitBackFrames;

    public enum State { Idle, Walk, Run, Jump, Rest, Throw, Push, HitBack, Stunned }

    private State currentState = State.Idle;
    public State CurrentState => currentState;

    private Rigidbody2D rb;
    private float frameTimer;
    private int currentFrame;
    private float idleTimer;
    private State prevGroundedState;
    private float jumpDuration;
    private float jumpStartTime;
    private bool grounded;
    private bool stunnedLocked = false;

    // Throw helpers
    private bool airThrowOnce = false;
    private bool airThrowDone = false;

    public bool IsThrowing => currentState == State.Throw;

    public void SetRigidbody(Rigidbody2D body) { rb = body; }

    public void SetGrounded(bool isGrounded)
    {
        grounded = isGrounded;
        if (grounded && currentState == State.Jump)
            StartCoroutine(Landing());
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

            case State.Walk:
                AnimateLoop(walkFrames, config.walkRate);
                break;

            case State.Run:
                AnimateLoop(runFrames, config.runRate);
                break;

            case State.Rest:
                AnimateOnceThenHold(restFrames, config.restRate);
                break;

            case State.Jump:
                {
                    float elapsed = Time.time - jumpStartTime;
                    HandleJumpCycle(elapsed);
                    break;
                }

            case State.Throw:
                if (airThrowOnce)
                {
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
                        ShowFrame(jumpFrames, 4);
                    }
                }
                else
                {
                    AnimateLoop(throwFrames, config.throwRate);
                }
                break;

            case State.Push:
                AnimateLoop(pushFrames, config.pushRate);
                break;

            case State.HitBack:
                AnimateOnceThenCallback(hitBackFrames, config.hitBackRate, () => ChangeState(State.Stunned));
                break;

            case State.Stunned:
                if (!stunnedLocked)
                    AnimateOnceThenHold(stunnedFrames, config.stunnedRate, () => stunnedLocked = true);
                break;
        }
    }

    private void HandleJumpCycle(float elapsed)
    {
        if (jumpFrames == null || jumpFrames.Length != 6 || rb == null) return;

        float vy = rb.velocity.y;

        if (!grounded && vy > 0f)
        {
            if (elapsed < jumpDuration * 0.2f) ShowFrame(jumpFrames, 0);
            else if (elapsed < jumpDuration * 0.4f) ShowFrame(jumpFrames, 1);
            else if (elapsed < jumpDuration * 0.6f) ShowFrame(jumpFrames, 2);
            else ShowFrame(jumpFrames, 3);
        }
        else if (!grounded && vy <= 0f)
        {
            ShowFrame(jumpFrames, 4);
        }
    }

    private IEnumerator Landing()
    {
        ShowFrame(jumpFrames, 5);
        yield return new WaitForSeconds(config.landingDelay);

        if (prevGroundedState == State.Run) ChangeState(State.Run);
        else if (prevGroundedState == State.Walk) ChangeState(State.Walk);
        else ChangeState(State.Idle);
    }

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
            if (currentFrame < frames.Length - 1)
            {
                currentFrame++;
            }
            else
            {
                onComplete?.Invoke();
            }
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

    private void DisableAll()
    {
        void Off(Image[] arr) { if (arr != null) foreach (var i in arr) if (i) i.enabled = false; }
        Off(idleFrames); Off(walkFrames); Off(runFrames); Off(jumpFrames);
        Off(restFrames); Off(throwFrames); Off(pushFrames); Off(stunnedFrames); Off(hitBackFrames);
    }

    public void ChangeState(State newState, bool onceThrow = false)
    {
        if (newState == currentState) return;

        if (newState == State.Jump &&
            (currentState == State.Idle || currentState == State.Walk || currentState == State.Run))
            prevGroundedState = currentState;

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
            case State.Jump: ShowFrame(jumpFrames, 0); break;
            case State.Rest: ShowFrame(restFrames, 0); break;
            case State.Throw: ShowFrame(throwFrames, 0); break;
            case State.Push: ShowFrame(pushFrames, 0); break;
            case State.HitBack: ShowFrame(hitBackFrames, 0); break;
            case State.Stunned: ShowFrame(stunnedFrames, 0); break;
        }
    }

    public void StartJump(float duration, float distance = 0f)
    {
        jumpDuration = duration;
        jumpStartTime = Time.time;
        ChangeState(State.Jump);
    }

    public void StartFall()
    {
        if (currentState == State.Throw && airThrowOnce) return;

        ChangeState(State.Jump);
        ShowFrame(jumpFrames, 4);
    }

    public void ResetFromStunned()
    {
        stunnedLocked = false;
        ChangeState(State.Idle);
    }
}
