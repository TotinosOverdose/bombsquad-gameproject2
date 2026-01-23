
using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using UnityEngine.UIElements;

public class EvilShroomController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 1.2f;
    public float moveDuration = 2.0f;
    public float pauseDuration = 1.0f;
    public float screenPadding = 0.05f;

    [Header("Erratic (optional)")]
    public bool isErratic = false;
    public float erraticSpeedMultiplier = 1.5f;
    public float zigzagFrequency = 2f;
    public float zigzagAmplitude = 0.5f;

    [Header("Evil behavior")]
    public float lifeTimeSeconds = 6f;
    public float flickVelocityThreshold = 3.0f; // world units / sec
    public float flickForceMultiplier = 1.0f;
    public float offscreenMargin = 0.02f; // viewport margin for off-screen detection

    [Header("Avoidance Settings")]
    public float avoidanceCheckDistance = 1.5f;
    public LayerMask sortingAreaLayer;

    [Header("Input")]
    [Tooltip("Only colliders on this layer mask will be considered for player touch/mouse input.")]
    public LayerMask touchableLayer;

    private CameraController cameraController;
    public SpriteRenderer[] spriteRenderers;
    private Camera mainCamera;
    private Rigidbody2D rb;
    private Animator animator;
    private Vector2 moveDirection;
    private bool isMoving;
    private Coroutine moveRoutine;
    private float lifeTimer;
    private bool hasBeenFlicked = false;

    // Drag / flick tracking (single pointer fallback)
    private bool isDragging = false;
    private int activeTouchId = -1; // -1 = none, -2 = mouse
    private Vector3 dragOffset;

    // sampling for a robust flick velocity
    private Vector2 lastSamplePos;
    private float lastSampleTime;
    private Vector2 lastSampleVelocity;

    // velocity used to move the shroom off-screen (no physics simulation)
    private Vector2 flingVelocity;

    private void OnEnable()
    {
        EnhancedTouchSupport.Enable();
    }

    private void OnDisable()
    {
        EnhancedTouchSupport.Disable();
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponentInChildren<Animator>();

        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        lifeTimer = lifeTimeSeconds;

        spriteRenderers = GetComponentsInChildren<SpriteRenderer>();

        mainCamera = Camera.main;
        cameraController = mainCamera.GetComponent<CameraController>();

        // If no mask assigned in inspector, try to use layer named "Touchable"
        if (touchableLayer.value == 0)
            touchableLayer = LayerMask.GetMask("Touchable");
    }

    private void Start()
    {
        if (moveRoutine == null)
            moveRoutine = StartCoroutine(MovementRoutine());
    }

    private void Update()
    {
        // When lifetime is running out, make mushroom flash
        if (lifeTimer <= 2f)
        {
            float flashSpeed = 20f;
            float alpha = Mathf.Abs(Mathf.Sin(Time.time * flashSpeed));
            foreach (var sr in spriteRenderers)
            {
                if (sr != null)
                {
                    Color c = sr.color;
                    c.a = alpha;
                    sr.color = c;
                }
            }
        }

        // Movement while not flicked and not dragging
        if (!isDragging && !hasBeenFlicked && isMoving)
        {
            float speed = moveSpeed * (isErratic ? erraticSpeedMultiplier : 1f);
            if (!isErratic)
            {
                transform.Translate(moveDirection * speed * Time.deltaTime, Space.World);
            }
            else
            {
                Vector2 perp = new Vector2(-moveDirection.y, moveDirection.x);
                float offset = Mathf.Sin(Time.time * zigzagFrequency) * zigzagAmplitude;
                Vector2 combined = (moveDirection + perp * offset).normalized;
                transform.Translate(combined * speed * Time.deltaTime, Space.World);
            }
            if (!cameraController.slowMoActive)
            {
                KeepInsideScreenBounds();
            }

        }

        // If it has been flicked, move it along the fling vector (deterministic)
        if (hasBeenFlicked)
        {
            // move without physics simulation (consistent across platforms)
            transform.Translate((Vector3)flingVelocity * Time.deltaTime, Space.World);
            transform.Rotate(0f, 0f, 360f * Time.deltaTime * 2); // simple rotation for effect

            if (IsOffScreen())
            {
                // award points and remove
                if (GameManager.Instance != null)
                    GameManager.Instance.OnEvilShroomFlicked();

                Destroy(gameObject);
                return;
            }
        }

        // Lifetime countdown (if not already flicked)
        if (!hasBeenFlicked && !isDragging)
        {
            lifeTimer -= Time.deltaTime;
            if (lifeTimer <= 0f)
            {
                StartCoroutine(ExplodeAndEndGame());
            }
        }

        // Input handling (multitouch supported)
        HandlePointerInput();
    }

    private IEnumerator ExplodeAndEndGame()
    {
        // Trigger explode animation if present
        if (animator != null)
            animator.SetTrigger("Explode");

        // Stop movement
        if (moveRoutine != null)
        {
            StopCoroutine(moveRoutine);
            moveRoutine = null;
        }

        // Give animation a moment (use real time so this still waits if timescale changes)
        yield return new WaitForSecondsRealtime(1.2f);

        // Notify GameManager (start coroutine on the manager to ensure centralised game-over flow)
        if (GameManager.Instance != null)
            GameManager.Instance.TriggerEvilShroomExpired();

        // ensure this object is removed
        Destroy(gameObject);
    }

    private void HandlePointerInput()
    {
        var activeTouches = Touch.activeTouches;

        if (activeTouches.Count > 0)
        {
            foreach (var t in activeTouches)
            {
                Vector3 worldPoint = ScreenToWorldPoint(t.screenPosition);
                var phase = t.phase;

                if (phase == UnityEngine.InputSystem.TouchPhase.Began)
                {
                    // only consider hits on the touchableLayer
                    Collider2D hit = Physics2D.OverlapPoint((Vector2)worldPoint, touchableLayer);
                    if (hit != null && hit.gameObject == gameObject && !hasBeenFlicked && activeTouchId == -1)
                    {
                        StartDragWithId(t.finger.index, worldPoint);
                    }
                }
                else
                {
                    if (t.finger.index == activeTouchId)
                    {
                        if (phase == UnityEngine.InputSystem.TouchPhase.Moved || phase == UnityEngine.InputSystem.TouchPhase.Stationary)
                        {
                            ContinueDragTo(worldPoint);
                        }
                        else if (phase == UnityEngine.InputSystem.TouchPhase.Ended || phase == UnityEngine.InputSystem.TouchPhase.Canceled)
                        {
                            EndDragAndEvaluateFlick();
                        }
                    }
                }
            }
            return;
        }

        // Fallback: mouse
        if (Mouse.current != null)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                Vector3 worldPoint = ScreenToWorldPoint(Mouse.current.position.ReadValue());
                Collider2D hit = Physics2D.OverlapPoint((Vector2)worldPoint, touchableLayer);
                if (hit != null && hit.gameObject == gameObject && !hasBeenFlicked && activeTouchId == -1)
                {
                    StartDragWithId(-2, worldPoint);
                }
            }

            if (activeTouchId == -2)
            {
                if (Mouse.current.leftButton.isPressed)
                {
                    Vector3 worldPoint = ScreenToWorldPoint(Mouse.current.position.ReadValue());
                    ContinueDragTo(worldPoint);
                }
                else if (Mouse.current.leftButton.wasReleasedThisFrame)
                {
                    EndDragAndEvaluateFlick();
                }
            }
        }
    }

    private void StartDragWithId(int id, Vector3 inputWorldPos)
    {
        activeTouchId = id;
        isDragging = true;
        // Stop wandering while dragging
        if (moveRoutine != null)
        {
            StopCoroutine(moveRoutine);
            moveRoutine = null;
        }

        dragOffset = transform.position - inputWorldPos;

        // initialize sampling for velocity
        lastSamplePos = inputWorldPos;
        lastSampleTime = Time.time;
        lastSampleVelocity = Vector2.zero;
    }

    private void ContinueDragTo(Vector3 inputWorldPos)
    {
        if (!isDragging) return;

        // compute sample velocity between this input and the last sampled input
        float dt = Time.time - lastSampleTime;
        if (dt > 0f)
        {
            Vector2 newSampleVelocity = ((Vector2)inputWorldPos - lastSamplePos) / dt;
            // store the latest sample velocity (this will be used on release)
            lastSampleVelocity = newSampleVelocity;
        }

        // move to the requested position for a direct drag feel
        transform.position = inputWorldPos + dragOffset;

        // update sample buffers (after computing velocity)
        lastSamplePos = inputWorldPos;
        lastSampleTime = Time.time;
    }

    private void EndDragAndEvaluateFlick()
    {
        if (!isDragging) return;

        Vector2 releaseVelocity = lastSampleVelocity;

        isDragging = false;
        activeTouchId = -1;

        if (releaseVelocity.magnitude >= flickVelocityThreshold)
        {
            hasBeenFlicked = true;
            animator.SetTrigger("Shocked");
            Vector3 spawnPos = transform.position;
            int pts = 10;

            ScorePopupManager.Instance.ShowAtWorldPosition(spawnPos, $"+{pts}", Color.white, 0.5f, 0.9f);

            // compute fling velocity (no physics simulation)
            flingVelocity = releaseVelocity * flickForceMultiplier;

            // disable physics simulation to avoid interference
            if (rb != null)
                rb.simulated = false;

            // disable collider/other interactions if needed
            Collider2D col = GetComponent<Collider2D>();
            if (col != null) col.enabled = false;
            if (cameraController.slowMoActive == false)
            {
                cameraController.StartCoroutine(cameraController.SlowMoAndZoom(transform, 0.5f, 1.5f, 0.18f));
            }

        }
        else
        {
            // not a strong flick — resume wandering
            if (moveRoutine == null)
                moveRoutine = StartCoroutine(MovementRoutine());
        }
    }

    private Vector3 ScreenToWorldPoint(Vector2 screenPos)
    {
        if (mainCamera == null) mainCamera = Camera.main;
        Vector3 inputPos = new Vector3(screenPos.x, screenPos.y, mainCamera.WorldToScreenPoint(transform.position).z);
        return mainCamera.ScreenToWorldPoint(inputPos);
    }

    IEnumerator MovementRoutine()
    {
        while (true)
        {
            if (!isDragging && !hasBeenFlicked)
            {
                moveDirection = GetSafeDirection();
                isMoving = true;
                yield return new WaitForSeconds(moveDuration);

                isMoving = false;
                yield return new WaitForSeconds(pauseDuration);
            }
            else
            {
                yield return null;
            }
        }
    }

    private Vector2 GetSafeDirection()
    {
        const int maxAttempts = 30;

        for (int i = 0; i < maxAttempts; i++)
        {
            Vector2 candidate = Random.insideUnitCircle.normalized;
            Vector2 checkPos = (Vector2)transform.position + candidate * avoidanceCheckDistance;

            // Check if direction leads off-screen
            if (mainCamera != null)
            {
                Vector3 vp = mainCamera.WorldToViewportPoint(checkPos);
                if (vp.x < screenPadding || vp.x > 1f - screenPadding ||
                    vp.y < screenPadding || vp.y > 1f - screenPadding)
                {
                    continue;
                }
            }

            // Check if direction leads into sorting area
            Collider2D hit = Physics2D.OverlapCircle(checkPos, 0.3f, sortingAreaLayer);
            if (hit == null)
            {
                return candidate;
            }
        }

        // Fallback: move toward center of screen
        if (mainCamera != null)
        {
            Vector3 centerWorld = mainCamera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 10f));
            centerWorld.z = transform.position.z;
            Vector2 toCenter = ((Vector2)centerWorld - (Vector2)transform.position).normalized;
            if (toCenter.sqrMagnitude > 0.001f)
                return toCenter;
        }

        return Random.insideUnitCircle.normalized;
    }

    private void KeepInsideScreenBounds()
    {
        if (mainCamera == null) return;

        Vector3 vp = mainCamera.WorldToViewportPoint(transform.position);
        bool corrected = false;

        if (vp.x < screenPadding)
        {
            vp.x = screenPadding;
            moveDirection.x = Mathf.Abs(moveDirection.x);
            corrected = true;
        }
        else if (vp.x > 1f - screenPadding)
        {
            vp.x = 1f - screenPadding;
            moveDirection.x = -Mathf.Abs(moveDirection.x);
            corrected = true;
        }

        if (vp.y < screenPadding)
        {
            vp.y = screenPadding;
            moveDirection.y = Mathf.Abs(moveDirection.y);
            corrected = true;
        }
        else if (vp.y > 1f - screenPadding)
        {
            vp.y = 1f - screenPadding;
            moveDirection.y = -Mathf.Abs(moveDirection.y);
            corrected = true;
        }

        if (corrected)
        {
            Vector3 world = mainCamera.ViewportToWorldPoint(vp);
            world.z = transform.position.z;
            transform.position = world;
        }
    }

    private bool IsOffScreen()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        Vector3 vp = mainCamera.WorldToViewportPoint(transform.position);
        return (vp.x < -offscreenMargin || vp.x > 1f + offscreenMargin || vp.y < -offscreenMargin || vp.y > 1f + offscreenMargin);
    }
}