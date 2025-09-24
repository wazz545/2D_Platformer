using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerAnimation : MonoBehaviour
{
    [Header("Frames")]
    public Image[] idleFrames;
    public Image[] restFrames;
    public Image[] walkFrames;
    public Image[] runFrames;
    public Image[] jumpFrames;   // exactly 6 frames
    public Image[] throwFrames;
    public Image[] pushFrames;
    public Image[] stunnedFrames;
    public Image[] hitBackFrames;

    [Header("Frame Rates (seconds per frame)")]
    public float idleRate = 0.30f;
    public float restRate = 0.40f;   // plays once, holds last
    public float walkRate = 0.20f;
    public float runRate = 0.10f;
    public float jumpRate = 0.12f;   // used for frames 1→2→3→4
    public float throwRate = 0.12f;
    public float pushRate = 0.18f;
    public float stunnedRate = 0.25f;
    public float hitBackRate = 0.20f;

    [Header("General")]
    public float restDelay = 5f;     // Idle → Rest after this
    public float stunnedWaitTime = 3f;

    [Header("Debug (read-only)")]
    [SerializeField] private bool isGrounded = true;

    public enum State { Idle, Rest, Walk, Run, Jump, Throw, Push, Stunned, HitBack, WalkToIdle, RunToIdle }
    private State currentState = State.Idle;
    public State CurrentState => currentState;
    public bool Grounded => isGrounded;

    // runtime
    private int currentFrame = 0;
    private float frameTimer = 0f;
    private float idleTimer = 0f;
    private Rigidbody2D rb;
    private State prevGroundedState = State.Idle;

    // ----- Setup -----
    public void SetRigidbody(Rigidbody2D body) { rb = body; }
    public void SetGrounded(bool grounded) { isGrounded = grounded; }

    void Start() { ShowFrame(idleFrames, 0); }

    void Update()
    {
        frameTimer += Time.deltaTime;

        switch (currentState)
        {
            case State.Idle:
                idleTimer += Time.deltaTime;
                if (idleTimer >= restDelay) ChangeState(State.Rest);
                AnimateCycle(idleFrames, idleRate);
                break;

            case State.Rest:
                AnimateCycle(restFrames, restRate, loop: false); // play once, hold last
                break;

            case State.Walk:
                AnimateCycle(walkFrames, walkRate);
                break;

            case State.Run:
                AnimateCycle(runFrames, runRate);
                break;

            case State.WalkToIdle:
                AnimateCycle(walkFrames, walkRate, loop: false, onComplete: () => ChangeState(State.Idle));
                break;

            case State.RunToIdle:
                AnimateCycle(runFrames, runRate, loop: false, onComplete: () => ChangeState(State.Idle));
                break;

            case State.Jump:
                HandleJumpCycleStrict6();
                break;

            case State.Throw:
                AnimateCycle(throwFrames, throwRate, loop: false, onComplete: () => ChangeState(State.Run));
                break;

            case State.Push:
                AnimateCycle(pushFrames, pushRate);
                break;

            case State.Stunned:
                AnimateCycle(stunnedFrames, stunnedRate, loop: false, onComplete: () => StartCoroutine(HandleStunned()));
                break;

            case State.HitBack:
                AnimateCycle(hitBackFrames, hitBackRate, loop: false);
                break;
        }
    }

    // ----- STRICT 6-FRAME JUMP -----
    // Frames index: 0,1,2,3 (rise, stop on 3) → falling show 4 (hold) → on ground show 5 then return to prev state
    private void HandleJumpCycleStrict6()
    {
        if (jumpFrames == null || jumpFrames.Length != 6 || rb == null) return;

        // Rising (not grounded, velocity > 0): advance 0→1→2→3 then hold 3
        if (!isGrounded && rb.velocity.y > 0f)
        {
            if (currentFrame < 3 && frameTimer >= jumpRate)
            {
                frameTimer = 0f;
                currentFrame++;
            }
            if (currentFrame > 3) currentFrame = 3;    // clamp/hold on 3
            ShowFrame(jumpFrames, currentFrame);
            return;
        }

        // Falling (not grounded, velocity < 0): show frame 4 and hold
        if (!isGrounded && rb.velocity.y < 0f)
        {
            ShowFrame(jumpFrames, 4);
            return;
        }

        // Landed: show frame 5 then restore previous ground state
        if (isGrounded)
        {
            ShowFrame(jumpFrames, 5);
            // immediately return next Update to previous state
            if (prevGroundedState == State.Run) ChangeState(State.Run);
            else if (prevGroundedState == State.Walk) ChangeState(State.Walk);
            else ChangeState(State.Idle);
        }
    }

    // ----- Helpers -----
    private void AnimateCycle(Image[] frames, float rate, bool loop = true, System.Action onComplete = null)
    {
        if (frames == null || frames.Length == 0) return;

        if (frameTimer >= rate)
        {
            frameTimer = 0f;
            currentFrame++;

            if (currentFrame >= frames.Length)
            {
                if (loop) currentFrame = 0;
                else { currentFrame = frames.Length - 1; onComplete?.Invoke(); }
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
        if (idleFrames != null) foreach (var i in idleFrames) if (i) i.enabled = false;
        if (restFrames != null) foreach (var i in restFrames) if (i) i.enabled = false;
        if (walkFrames != null) foreach (var i in walkFrames) if (i) i.enabled = false;
        if (runFrames != null) foreach (var i in runFrames) if (i) i.enabled = false;
        if (jumpFrames != null) foreach (var i in jumpFrames) if (i) i.enabled = false;
        if (throwFrames != null) foreach (var i in throwFrames) if (i) i.enabled = false;
        if (pushFrames != null) foreach (var i in pushFrames) if (i) i.enabled = false;
        if (stunnedFrames != null) foreach (var i in stunnedFrames) if (i) i.enabled = false;
        if (hitBackFrames != null) foreach (var i in hitBackFrames) if (i) i.enabled = false;
    }

    // ----- State control -----
    public void ChangeState(State newState)
    {
        if (newState == currentState) return;

        // remember what we were doing before jumping
        if (newState == State.Jump &&
            (currentState == State.Idle || currentState == State.Walk || currentState == State.Run))
        {
            prevGroundedState = currentState;
        }

        currentState = newState;
        currentFrame = 0;
        frameTimer = 0f;
        idleTimer = 0f;

        switch (newState)
        {
            case State.Idle: ShowFrame(idleFrames, 0); break;
            case State.Rest: ShowFrame(restFrames, 0); break;
            case State.Walk: ShowFrame(walkFrames, 0); break;
            case State.Run: ShowFrame(runFrames, 0); break;
            case State.Jump: ShowFrame(jumpFrames, 0); break;
            case State.Throw: ShowFrame(throwFrames, 0); break;
            case State.Push: ShowFrame(pushFrames, 0); break;
            case State.Stunned: ShowFrame(stunnedFrames, 0); break;
            case State.HitBack: ShowFrame(hitBackFrames, 0); break;
            case State.WalkToIdle: case State.RunToIdle: /* frames shown in anim */ break;
        }
    }

    private IEnumerator HandleStunned()
    {
        yield return new WaitForSeconds(stunnedWaitTime);
        ChangeState(State.Idle);
    }

    // External hooks
    public void DoJump() { if (currentState != State.Stunned) ChangeState(State.Jump); }
    public void DoThrow() { if (currentState == State.Run) ChangeState(State.Throw); }
    public void DoPush() { if (currentState != State.Jump && currentState != State.Stunned) ChangeState(State.Push); }
    public void WalkToIdle() { if (currentState == State.Walk) ChangeState(State.WalkToIdle); }
    public void RunToIdle() { if (currentState == State.Run) ChangeState(State.RunToIdle); }
}
