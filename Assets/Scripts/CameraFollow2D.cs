using UnityEngine;
using System.Collections;

public class CameraFollow2D : MonoBehaviour
{
    [Header("Target")]
    public Transform target; // Player
    public Rigidbody2D playerRb; // For velocity checks

    [Header("Follow Settings")]
    public float followSpeed = 5f;
    public Vector2 followOffset = new Vector2(2f, 1f);
    public float verticalDeadZone = 0.5f; // Prevents small up/down jitter

    [Header("Look Ahead")]
    public float lookAheadFactor = 2f;
    public float lookAheadSpeed = 3f;

    [Header("Zoom Settings")]
    public Camera cam;
    public float defaultZoom = 5f;
    public float runZoomOut = 6f;
    public float zoomLerpSpeed = 2f;

    [Header("Shake Settings")]
    public float smallLandingShake = 0.2f;
    public float bigLandingShake = 0.5f;
    public float fallThreshold = 8f; // velocity.y threshold for big landing
    public float shakeDuration = 0.2f;

    private Vector3 velocity = Vector3.zero;
    private Vector3 targetPos;
    private Vector3 lookAhead;
    private bool isShaking = false;

    private float lastYVelocity;

    void Start()
    {
        if (cam == null) cam = Camera.main;
        if (target == null) Debug.LogWarning("CameraFollow2D: No target assigned!");
    }

    void LateUpdate()
    {
        if (target == null) return;

        // --- Follow base ---
        targetPos = target.position + (Vector3)followOffset;

        // --- Look ahead ---
        float xVel = playerRb != null ? playerRb.velocity.x : 0;
        lookAhead = Vector3.Lerp(lookAhead, new Vector3(xVel * lookAheadFactor, 0, 0), Time.deltaTime * lookAheadSpeed);
        targetPos += lookAhead;

        // --- Vertical dead zone ---
        float camY = transform.position.y;
        float targetY = target.position.y + followOffset.y;
        if (Mathf.Abs(targetY - camY) < verticalDeadZone)
            targetPos.y = camY; // lock Y

        // --- Smooth move ---
        transform.position = Vector3.SmoothDamp(transform.position, new Vector3(targetPos.x, targetPos.y, transform.position.z), ref velocity, 1f / followSpeed);

        // --- Zoom effect ---
        if (playerRb != null)
        {
            float speed = Mathf.Abs(playerRb.velocity.x);
            float targetZoom = speed > 2f ? runZoomOut : defaultZoom;
            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetZoom, Time.deltaTime * zoomLerpSpeed);
        }

        // --- Detect landings ---
        if (playerRb != null)
        {
            if (playerRb.velocity.y == 0 && lastYVelocity < -0.1f) // Landed
            {
                if (!isShaking)
                {
                    float shakeAmount = Mathf.Abs(lastYVelocity) > fallThreshold ? bigLandingShake : smallLandingShake;
                    StartCoroutine(Shake(shakeDuration, shakeAmount));
                }
            }
            lastYVelocity = playerRb.velocity.y;
        }
    }

    private IEnumerator Shake(float duration, float magnitude)
    {
        isShaking = true;
        Vector3 originalPos = transform.localPosition;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float offsetX = Random.Range(-1f, 1f) * magnitude;
            float offsetY = Random.Range(-1f, 1f) * magnitude;

            transform.localPosition = originalPos + new Vector3(offsetX, offsetY, 0);
            elapsed += Time.deltaTime;

            yield return null;
        }

        transform.localPosition = originalPos;
        isShaking = false;
    }
}
