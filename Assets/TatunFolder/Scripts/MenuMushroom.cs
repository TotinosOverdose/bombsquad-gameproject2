
using UnityEngine;
using System.Collections;

public class MenuMushroom : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 2.0f;
    public float moveDuration = 2.0f;
    public float pauseDuration = 1.0f;
    public float screenPadding = 0.1f;

    [Header("Erratic Movement (optional)")]
    public bool isErratic = false;
    public float erraticSpeedMultiplier = 1.5f;
    public float zigzagFrequency = 10f;
    public float zigzagAmplitude = 0.5f;

    private Vector2 moveDirection;
    private bool isMoving = false;
    private Coroutine moveRoutine;
    private Camera mainCamera;
    private Animator animator;

    private void Awake()
    {
        mainCamera = Camera.main;
        animator = GetComponentInChildren<Animator>();
    }

    private void OnEnable()
    {
        // start movement when enabled
        if (moveRoutine == null)
            moveRoutine = StartCoroutine(MovementRoutine());
    }

    private void OnDisable()
    {
        if (moveRoutine != null)
        {
            StopCoroutine(moveRoutine);
            moveRoutine = null;
        }
    }

    private void Update()
    {
        if (!isMoving)
        {
            if (animator != null)
                animator.SetBool("isMoving", false);
            return;
        }
        else
        {
            if (animator != null)
                animator.SetBool("isMoving", true);
        }

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


        KeepInsideScreenBounds();
    }

    private IEnumerator MovementRoutine()
    {
        while (true)
        {
            moveDirection = GetSafeDirection();
            isMoving = true;
            yield return new WaitForSeconds(moveDuration);

            isMoving = false;
            yield return new WaitForSeconds(pauseDuration);
        }
    }

    private Vector2 GetSafeDirection()
    {
        const int maxAttempts = 30;
        float checkDistance = Mathf.Max(1f, moveSpeed * moveDuration);

        for (int i = 0; i < maxAttempts; i++)
        {
            Vector2 candidate = Random.insideUnitCircle.normalized;
            Vector2 checkPos = (Vector2)transform.position + candidate * checkDistance;

            if (mainCamera != null)
            {
                Vector3 vp = mainCamera.WorldToViewportPoint(checkPos);
                if (vp.x < screenPadding || vp.x > 1f - screenPadding ||
                    vp.y < screenPadding || vp.y > 1f - screenPadding)
                {
                    continue;
                }
            }

            return candidate;
        }

        // fallback: head toward screen center
        if (mainCamera != null)
        {
            Vector3 centerWorld = mainCamera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, Mathf.Abs(mainCamera.transform.position.z - transform.position.z)));
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
}