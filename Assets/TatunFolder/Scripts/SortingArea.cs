using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using TMPro;

public class SortingArea : MonoBehaviour
{
    [Header("Area Settings")]
    public MushroomType acceptedType;

    [Header("Visual Feedback")]
    public Color correctColor = Color.green;
    public Color incorrectColor = Color.red;
    public Color normalColor = Color.white;

    [Header("Sorting (placed mushrooms)")]
    public int baseSortingOrder = 0;
    public int sortingRange = 100;

    [Header("Events")]
    public UnityEvent<MushroomType> onCorrectMushroom;
    public UnityEvent onIncorrectMushroom;

    [Header("Score Popups")]
    [Tooltip("Prefab for score popup. Prefer a world-space TextMeshPro (component 'TextMeshPro') prefab. If using a UGI prefab (TextMeshProUGUI) a Canvas must exist.")]
    public GameObject scorePopupPrefab;
    Vector3 spawnPos;
    public Color popupPositiveColor = Color.white;
    public Color popupNegativeColor = Color.red;
    public float popupRiseDistance = 0.5f;
    public float popupDuration = 0.9f;

    private SpriteRenderer spriteRenderer;
    private bool isHighlighted = false;
    private MushroomController hoveredMushroom = null;
    private List<MushroomController> placedMushrooms = new List<MushroomController>();
    private GameManager gameManager;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            spriteRenderer.color = normalColor;

        gameManager = GameManager.Instance ?? FindObjectOfType<GameManager>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        MushroomController mushroom = other.GetComponent<MushroomController>();
        if (mushroom != null && mushroom.IsDragging)
        {
            hoveredMushroom = mushroom;
            if (mushroom.GetMushroomType() == acceptedType)
                SetHighlight(correctColor);
            else
                SetHighlight(incorrectColor);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        MushroomController mushroom = other.GetComponent<MushroomController>();
        if (mushroom != null && mushroom == hoveredMushroom)
        {
            hoveredMushroom = null;
            ClearHighlight();
        }
    }

    private void Update()
    {

        if (hoveredMushroom == null) return;

        spawnPos = hoveredMushroom.transform.position;

        if (hoveredMushroom.gameObject == null)
        {
            hoveredMushroom = null;
            ClearHighlight();
            return;
        }

        bool dragging = false;
        try { dragging = hoveredMushroom.IsDragging; } catch { dragging = false; }
        if (dragging) return;

        // Released while inside area
        MushroomType mushroomType = hoveredMushroom.GetMushroomType();
        bool isCorrectType = mushroomType == acceptedType;

        if (isCorrectType)
        {
            hoveredMushroom.PlaceInArea(this);
            placedMushrooms.Add(hoveredMushroom);
            onCorrectMushroom?.Invoke(acceptedType);

            int pts = 20;

            if (ScorePopupManager.Instance != null)
                ScorePopupManager.Instance.ShowAtWorldPosition(spawnPos, $"+{pts}", popupPositiveColor, popupRiseDistance, popupDuration);

            if (gameManager == null)
                gameManager = GameManager.Instance ?? FindObjectOfType<GameManager>();

            if (gameManager != null)
                gameManager.OnMushroomPlaced(hoveredMushroom);
        }
        else
        {
            var toDestroy = new List<MushroomController>(placedMushrooms);
            var childMushrooms = GetComponentsInChildren<MushroomController>(includeInactive: true);
            foreach (var cm in childMushrooms)
            {
                if (cm != null && cm.gameObject != null && !toDestroy.Contains(cm))
                    toDestroy.Add(cm);
            }

            // Add hovered mushroom to the top of the list to destroy

            if (hoveredMushroom != null && hoveredMushroom.gameObject != null && !toDestroy.Contains(hoveredMushroom))
            {
                toDestroy.Insert(0, hoveredMushroom);
            }


            int destroyedCount = 0;
            foreach (var m in toDestroy)
            {
                if (m != null && m.gameObject != null) destroyedCount++;
            }

            placedMushrooms.Clear();

            if (GameManager.Instance != null && destroyedCount > 0)
                GameManager.Instance.OnIncorrectPlacement(acceptedType, destroyedCount);

            onIncorrectMushroom?.Invoke();

            if (destroyedCount > 0)
            {
                Vector3 spawnPos = transform.position;
            }

            if (toDestroy.Count > 0)
                StartCoroutine(DestroyMushroomsSequentially(toDestroy, 0.15f, 0.08f));
        }

        hoveredMushroom = null;
        ClearHighlight();
    }

    private IEnumerator DestroyMushroomsSequentially(List<MushroomController> toDestroy, float delay, float stagger)
    {
        bool hasPopupManager = ScorePopupManager.Instance != null;

        foreach (var m in toDestroy)
        {
            if (m == null) continue;

            Animator anim = m.animator;
            if (anim == null) anim = m.GetComponentInChildren<Animator>();

            if (hasPopupManager && m != null && m.gameObject != null)
            {
                ScorePopupManager.Instance.ShowAtWorldPosition(m.transform.position, "-10", popupNegativeColor, popupRiseDistance, popupDuration);
            }

            if (anim != null)
            {
                anim.SetTrigger("Explode");
                yield return new WaitForSecondsRealtime(delay);
            }
            else
            {
                yield return new WaitForSecondsRealtime(Mathf.Min(0.1f, delay));
            }

            if (m != null && m.gameObject != null) Destroy(m.gameObject);

            if (stagger > 0f) yield return new WaitForSecondsRealtime(stagger);
        }
    }

    private void SetHighlight(Color color)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = color;
            isHighlighted = true;
        }
    }

    private void ClearHighlight()
    {
        if (spriteRenderer != null && isHighlighted)
        {
            spriteRenderer.color = normalColor;
            isHighlighted = false;
        }
    }

    public int GetSortingOrderAtPosition(Vector3 worldPos)
    {
        var col = GetComponent<Collider2D>();
        if (col == null) return baseSortingOrder;

        Bounds b = col.bounds;
        // map worldPos.y to 0..1 from top to bottom (so top => 0, bottom => 1)
        float t = (b.size.y > 0f) ? Mathf.InverseLerp(b.max.y, b.min.y, worldPos.y) : 0.5f;
        // clamp then convert to an int order inside the range
        t = Mathf.Clamp01(t);
        int order = baseSortingOrder + Mathf.RoundToInt(t * sortingRange);
        return order;
    }
}