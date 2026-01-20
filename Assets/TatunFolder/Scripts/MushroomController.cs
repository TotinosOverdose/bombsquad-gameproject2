using UnityEngine;
using System.Collections;

public class MushroomController : MonoBehaviour
{
    [Header("Mushroom Type")]
    public MushroomType mushroomType;

    [Header("Movement Settings")]
    public float moveSpeed = 2.0f;
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

    private Vector2 moveDirection;
    private bool isMoving = false;
    private bool isDragging = false;
    private Coroutine moveRoutine;

    private Camera mainCamera;
    private Animator animator;
    private Rigidbody2D rb;
    private Collider2D myCollider;
    private Vector3 dragOffset;
    private bool isPlaced = false;
    private float lifeTimer;
    private GameManager gameManager;

    public bool IsDragging => isDragging;

    private void Awake()
    {
        
        mainCamera = Camera.main;
        rb = GetComponent<Rigidbody2D>();
        myCollider = GetComponent<Collider2D>();

        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }
        rb.gravityScale = 0;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

    }

    void Start()
    {
        moveRoutine = StartCoroutine(MovementRoutine());

        animator = GetComponentInChildren<Animator>();
        lifeTimer = lifeTimeSeconds;
        gameManager = FindObjectOfType<GameManager>();
    }

    void Update()
    {
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


        if (isPlaced)
        {
            if (animator != null)
                animator.SetBool("isMoving", false);
            return;
        }
        if (animator != null)
        {
            if (isMoving)
            {
                animator.SetBool("isMoving", true);
            }
            else
            {
                animator.SetBool("isMoving", false);
            }
        }

        if (!isDragging && isMoving)
        {
            transform.Translate(moveDirection * moveSpeed * Time.deltaTime, Space.World);
            KeepInsideScreenBounds();
        }

        // Lifetime countdown when not placed
        if (!isPlaced)
        {
            lifeTimer -= Time.deltaTime;
            if (lifeTimer <= 0f)
            {
                // expired -> notify GameManager (game over) and destroy self
                if (gameManager != null)
                {
                    gameManager.OnMushroomExpired(this);
                }
                // explode (destroy)
                animator.SetTrigger("Explode");
                Destroy(gameObject);
            }
        }
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

    void OnMouseDown()
    {
        StartDragging();
    }

    void OnMouseDrag()
    {
        if (isDragging)
        {
            Vector3 inputPos = GetInputWorldPosition();
            transform.position = inputPos + dragOffset;
        }
    }

    void OnMouseUp()
    {
        StopDragging();
    }

    private void StartDragging()
    {
        isDragging = true;
        isMoving = false;

        Vector3 inputPos = GetInputWorldPosition();
        dragOffset = transform.position - inputPos;
    }

    private void StopDragging()
    {
        isDragging = false;
    }

    public void PlaceInArea(SortingArea area)
    {
        if (isPlaced) return;
        isPlaced = true;
        isMoving = false;
        isDragging = false;

        // stop physics and disable collider so it stays put in the area
        if (rb != null)
        {
            rb.simulated = false;
        }

        if (myCollider != null)
        {
            myCollider.enabled = false;
        }

        // parent to the area so it follows if area moves and so it's grouped
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

        lifeIndicatorTransform.gameObject.SetActive(false);
    }

    public bool IsPlaced => isPlaced;

    private Vector3 GetInputWorldPosition()
    {
        Vector3 inputPos;


        inputPos = UnityEngine.Input.mousePosition;

        if (UnityEngine.Input.touchCount > 0)
        {
            inputPos = UnityEngine.Input.GetTouch(0).position;
        }
        else
        {
            inputPos = UnityEngine.Input.mousePosition;
        }

        inputPos.z = mainCamera.WorldToScreenPoint(transform.position).z;
        return mainCamera.ScreenToWorldPoint(inputPos);
    }

    public MushroomType GetMushroomType()
    {
        return mushroomType;
    }
}

public enum MushroomType
{
    TypeA,
    TypeB
}