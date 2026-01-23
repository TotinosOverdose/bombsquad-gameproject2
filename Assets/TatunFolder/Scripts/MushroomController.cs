
using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class MushroomController : MonoBehaviour
{
    [Header("Mushroom Type")]
    public MushroomType mushroomType;

    [Header("Movement Settings")]
    public float moveSpeed = 1.0f;
    public float moveDuration = 2.0f;
    public float pauseDuration = 1.0f;
    public float screenPadding = 0.1f;

    [Header("Avoidance Settings")]
    public float avoidanceCheckDistance = 1.5f;
    public LayerMask sortingAreaLayer;

    [Header("Lifetime")]
    public float lifeTimeSeconds = 5f;
    [SerializeField] private Sprite[] lifeIndicators;
    [SerializeField] Transform lifeIndicatorTransform;

    [Header("Erratic Movement")]
    public bool isErratic = false;
    public float erraticSpeedMultiplier = 1.5f;
    public float zigzagFrequency = 1f;
    public float zigzagAmplitude = 0.5f;

    [Header("Placed Behavior")]
    public float placedWanderSpeed = 0.3f;
    public Vector2 placedWanderPauseRange = new Vector2(1.0f, 2.0f);

    [HideInInspector] public MushroomSpawner originSpawner;

    [Header("Input")]
    [Tooltip("Only colliders on this layer mask will be considered for player touch/mouse input.")]
    public LayerMask touchableLayer;

    private Vector2 moveDirection;
    private bool isMoving = false;
    private bool isDragging = false;
    private Coroutine moveRoutine;

    private Camera mainCamera;
    private CameraController cameraController;
    public Animator animator;
    private Rigidbody2D rb;
    private Collider2D myCollider;
    private Vector3 dragOffset;
    private bool isPlaced = false;
    private float lifeTimer;
    private GameManager gameManager;
    private SpriteRenderer[] spriteRenderers;

    // Input tracking for multitouch support:
    // -1 = none, >=0 = finger index (EnhancedTouch Finger.index), -2 = mouse
    private int activeTouchId = -1;

    // Coroutine used for wandering while placed
    private Coroutine placedWanderRoutine;



    public bool IsDragging => isDragging;

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
        gameManager = GameManager.Instance ?? FindFirstObjectByType<GameManager>();
        mainCamera = Camera.main;
        cameraController = mainCamera.GetComponent<CameraController>();
        rb = GetComponent<Rigidbody2D>();
        myCollider = GetComponent<Collider2D>();
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>();

        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }
        rb.gravityScale = 0;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        // If no mask assigned in inspector, try to use layer named "Touchable"
        if (touchableLayer.value == 0)
            touchableLayer = LayerMask.GetMask("Touchable");
    }

    void Start()
    {
        //Check  current game level and adjust move speed and move duration
        if (gameManager != null)
        {
            int level = gameManager.currentLevel;
            moveSpeed += level * 0.2f; // Increase speed per level
            moveDuration += level * 0.2f;
        }

        moveRoutine = StartCoroutine(MovementRoutine());

        animator = GetComponentInChildren<Animator>();
        lifeTimer = lifeTimeSeconds;

        // ensure we have a gameManager reference as fallback
        if (gameManager == null)
            gameManager = FindObjectOfType<GameManager>();
    }

    void Update()
    {
        // When lifetime is running out, make mushroom flash
        if (!isPlaced && lifeTimer <= 2f)
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

        // Update life indicator and place at lifeIndicatorTransform
        if (lifeIndicatorTransform != null && lifeIndicators != null && lifeIndicators.Length > 0)
        {
            float lifeFraction = Mathf.Clamp01(lifeTimer / lifeTimeSeconds);
            int indicatorIndex = Mathf.FloorToInt(lifeFraction * lifeIndicators.Length);
            indicatorIndex = Mathf.Clamp(indicatorIndex, 0, lifeIndicators.Length - 1);
            SpriteRenderer sr = lifeIndicatorTransform.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = lifeIndicators[indicatorIndex];
            }
        }

        // Animation updates
        if (isPlaced)
        {
            if (animator != null)
                animator.SetBool("isMoving", false);

            return;
        }
        if (animator != null)
        {
            animator.SetBool("isMoving", isMoving);
        }

        if (!isDragging && isMoving)
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

        // Lifetime countdown when not placed
        if (!isPlaced)
        {
            lifeTimer -= Time.deltaTime;
            if (lifeTimer <= 0f)
            {
                if (gameManager != null)
                {
                    if (moveRoutine != null)
                    {
                        StopCoroutine(moveRoutine);
                    }
                    if (lifeIndicatorTransform != null)
                        lifeIndicatorTransform.gameObject.SetActive(false);

                    // Use singleton instance if available
                    if (GameManager.Instance != null)
                        StartCoroutine(GameManager.Instance.OnMushroomExpired(this));
                    else
                        StartCoroutine(gameManager.OnMushroomExpired(this));
                }
            }
        }

        // Handle input for touch/mouse (multi-touch supported):
        HandlePointerInput();
    }

    private void HandlePointerInput()
    {
        // Use Enhanced Touch (supports multi-touch on mobile)
        var activeTouches = Touch.activeTouches; // EnhancedTouch Touch list

        // ReadOnlyArray<T> is a value type — compare Count instead of null
        if (activeTouches.Count > 0)
        {
            foreach (var t in activeTouches)
            {
                Vector2 screenPos = t.screenPosition;
                Vector3 worldPoint = ScreenToWorldPoint(screenPos);

                var phase = t.phase;

                if (phase == UnityEngine.InputSystem.TouchPhase.Began)
                {
                    // only start drag if this touch hits this mushroom AND the hit collider is on the touchable layer
                    Collider2D hit = Physics2D.OverlapPoint((Vector2)worldPoint, touchableLayer);
                    if (hit == myCollider && !isPlaced && activeTouchId == -1)
                    {
                        // store finger index (EnhancedTouch Finger.index)
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
                            EndDrag();
                        }
                    }
                }
            }

            return;
        }

        // Fallback: mouse (editor / standalone) - single pointer only
        if (Mouse.current != null)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                Vector3 worldPoint = ScreenToWorldPoint(Mouse.current.position.ReadValue());
                Collider2D hit = Physics2D.OverlapPoint((Vector2)worldPoint, touchableLayer);
                if (hit == myCollider && !isPlaced && activeTouchId == -1)
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
                    EndDrag();
                }
            }
        }
    }

    private Vector3 ScreenToWorldPoint(Vector2 screenPos)
    {
        if (mainCamera == null) mainCamera = Camera.main;
        Vector3 inputPos = new Vector3(screenPos.x, screenPos.y, mainCamera.WorldToScreenPoint(transform.position).z);
        return mainCamera.ScreenToWorldPoint(inputPos);
    }

    private void StartDragWithId(int id, Vector3 inputWorldPos)
    {
        activeTouchId = id;
        StartDragging(inputWorldPos);
    }

    // keep the original StartDragging behavior but accept world position (used by multitouch)
    private void StartDragging(Vector3 inputWorldPos)
    {
        isDragging = true;
        isMoving = false;

        dragOffset = transform.position - inputWorldPos;
    }

    // mouse legacy entry points left as no-op to avoid duplicate behavior on some platforms
    void OnMouseDown() { }
    void OnMouseDrag() { }
    void OnMouseUp() { }

    private void ContinueDragTo(Vector3 inputWorldPos)
    {
        if (!isDragging) return;
        transform.position = inputWorldPos + dragOffset;
    }

    private void EndDrag()
    {
        isDragging = false;
        activeTouchId = -1;
    }

    private Vector3 GetInputWorldPosition()
    {
        // Not used for touch path (kept for compatibility)
        Vector3 inputPos = UnityEngine.Input.mousePosition;
        inputPos.z = mainCamera.WorldToScreenPoint(transform.position).z;
        return mainCamera.ScreenToWorldPoint(inputPos);
    }

    IEnumerator MovementRoutine()
    {
        while (true)
        {
            if (!isDragging)
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

    public void PlaceInArea(SortingArea area)
    {
        if (isPlaced) return;
        isPlaced = true;
        isMoving = false;
        isDragging = false;
        activeTouchId = -1;

        if (rb != null)
        {
            rb.simulated = false;
        }

        if (myCollider != null)
        {
            myCollider.enabled = false;
        }

        transform.SetParent(area.transform, worldPositionStays: true);

        // Stop lifetime countdown
        lifeTimer = float.MaxValue;
        // stop movement coroutine so it won't resume moving
        if (moveRoutine != null)
        {
            StopCoroutine(moveRoutine);
            moveRoutine = null;
        }

        // ensure animator updated
        if (animator != null)
            animator.SetBool("isMoving", false);

        if (lifeIndicatorTransform != null)
            lifeIndicatorTransform.gameObject.SetActive(false);

        // Notify the spawner that this mushroom has been placed (so its per-spawner count is updated)
        if (originSpawner != null)
        {
            originSpawner.NotifyMushroomPlaced(this);
        }

        // Notify GameManager that this mushroom is placed (so global active count is decremented)
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnMushroomPlaced(this);
        }
        else if (gameManager != null)
        {
            gameManager.OnMushroomPlaced(this);
        }

        // Apply sorting order based on area
        if (area != null)
        {
            int order = area.GetSortingOrderAtPosition(transform.position);
            ApplySortingOrderToRenderers(order);
        }

        // Start wandering aroud in placed area
        if (placedWanderRoutine != null) StopCoroutine(placedWanderRoutine);
        placedWanderRoutine = StartCoroutine(WanderInsideArea(area));
    }

    private IEnumerator WanderInsideArea(SortingArea area)
    {
        if (area == null) yield break;

        Collider2D areaCollider = area.GetComponent<Collider2D>();
        while (isPlaced)
        {
            Vector3 start = transform.position;
            Vector3 target = GetRandomPointInArea(areaCollider, area.transform);
            float distance = Vector3.Distance(start, target);
            float travelTime = Mathf.Max(0.1f, distance / Mathf.Max(0.01f, placedWanderSpeed));
            float elapsed = 0f;

            // smooth movement
            while (elapsed < travelTime && isPlaced)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / travelTime));
                transform.position = Vector3.Lerp(start, target, t);
                if (area != null)
                {
                    int order = area.GetSortingOrderAtPosition(transform.position);
                    ApplySortingOrderToRenderers(order);
                }
                yield return null;
            }

            // small random pause
            float pause = Random.Range(placedWanderPauseRange.x, placedWanderPauseRange.y);
            float wait = 0f;
            while (wait < pause && isPlaced)
            {
                wait += Time.deltaTime;
                yield return null;
            }
        }
    }

    private void ApplySortingOrderToRenderers(int order)
    {
        if (spriteRenderers == null) return;
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null)
                spriteRenderers[i].sortingOrder = order;
        }
    }

    private Vector3 GetRandomPointInArea(Collider2D areaCollider, Transform areaTransform)
    {
        if (areaCollider == null)
        {
            // fallback: small jitter around area's pivot
            return areaTransform != null ? areaTransform.position + (Vector3)(Random.insideUnitCircle * 0.3f) : transform.position;
        }

        // BoxCollider2D - sample inside an inset box so mushrooms visually stay inside.
        // Keep more inset on Y than X to avoid appearing to cross the top/bottom edges in 2D view.
        var box = areaCollider as BoxCollider2D;
        if (box != null)
        {
            Bounds b = box.bounds; // world-space bounds already account for scale/rotation
            Vector3 size = b.size;

            // scale factors to keep inside: 90% width, 80% height (more inset on Y)
            const float keepScaleX = 0.90f;
            const float keepScaleY = 0.80f;

            // compute margin (half of the removed portion)
            float marginX = Mathf.Max(0f, (1f - keepScaleX) * 0.5f * size.x);
            float marginY = Mathf.Max(0f, (1f - keepScaleY) * 0.5f * size.y);

            Vector3 min = b.min + new Vector3(marginX, marginY, 0f);
            Vector3 max = b.max - new Vector3(marginX, marginY, 0f);

            // safety clamp for very small boxes
            if (max.x < min.x) max.x = min.x;
            if (max.y < min.y) max.y = min.y;

            float x = Random.Range(min.x, max.x);
            float y = Random.Range(min.y, max.y);
            return new Vector3(x, y, transform.position.z);
        }

        // CircleCollider2D - sample inside slightly reduced radius for visual padding
        var circle = areaCollider as CircleCollider2D;
        if (circle != null)
        {
            Vector2 center = circle.bounds.center;
            float worldRadius = circle.radius * Mathf.Max(areaTransform.lossyScale.x, areaTransform.lossyScale.y);
            // reduce radius slightly so mushrooms don't hit the visual rim
            float keepRadius = Mathf.Max(0f, worldRadius * 0.85f);
            Vector2 p = Random.insideUnitCircle * keepRadius + (Vector2)center;
            return new Vector3(p.x, p.y, transform.position.z);
        }

        // Generic collider: use bounds as approximation with a small inset
        Bounds bounds = areaCollider.bounds;
        Vector3 inset = bounds.size * 0.08f; // keep ~84% area
        Vector3 gmin = bounds.min + inset * 0.5f;
        Vector3 gmax = bounds.max - inset * 0.5f;
        if (gmax.x < gmin.x) gmax.x = gmin.x;
        if (gmax.y < gmin.y) gmax.y = gmin.y;
        Vector3 candidate = new Vector3(Random.Range(gmin.x, gmax.x), Random.Range(gmin.y, gmax.y), transform.position.z);
        return candidate;
    }

    public bool IsPlaced => isPlaced;

    public MushroomType GetMushroomType()
    {
        return mushroomType;
    }
}

public enum MushroomType
{
    TypeA,
    TypeB,
    TypeC
}