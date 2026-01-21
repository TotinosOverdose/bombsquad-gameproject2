using UnityEngine;
using UnityEngine.Events;

public class SortingArea : MonoBehaviour
{
    [Header("Area Settings")]
    public MushroomType acceptedType;

    [Header("Visual Feedback")]
    public Color correctColor = Color.green;
    public Color incorrectColor = Color.red;
    public Color normalColor = Color.white;

    [Header("Events")]
    public UnityEvent<MushroomType> onCorrectMushroom;
    public UnityEvent onIncorrectMushroom;

    private SpriteRenderer spriteRenderer;
    private bool isHighlighted = false;
    private MushroomController hoveredMushroom = null;
    private System.Collections.Generic.List<MushroomController> placedMushrooms = new System.Collections.Generic.List<MushroomController>();
    private GameManager gameManager;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.color = normalColor;
        }

        // Prefer singleton instance if available
        gameManager = GameManager.Instance ?? FindObjectOfType<GameManager>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        MushroomController mushroom = other.GetComponent<MushroomController>();
        if (mushroom != null && mushroom.IsDragging)
        {
            hoveredMushroom = mushroom;

            // Visual feedback while hovering
            if (mushroom.GetMushroomType() == acceptedType)
            {
                SetHighlight(correctColor);
            }
            else
            {
                SetHighlight(incorrectColor);
            }
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
        // Check if hovered mushroom still exists and was just released
        if (hoveredMushroom != null && hoveredMushroom.gameObject != null && !hoveredMushroom.IsDragging)
        {
            // Cache the mushroom type before any operations
            MushroomType mushroomType = hoveredMushroom.GetMushroomType();
            bool isCorrectType = mushroomType == acceptedType;

            if (isCorrectType)
            {
                // Correct placement: keep mushroom in area and stop it
                hoveredMushroom.PlaceInArea(this);
                placedMushrooms.Add(hoveredMushroom);
                onCorrectMushroom?.Invoke(acceptedType);

                // Inform GameManager that a mushroom was placed
                if (gameManager == null)
                    gameManager = GameManager.Instance ?? FindObjectOfType<GameManager>();

                if (gameManager != null && hoveredMushroom != null)
                {
                    gameManager.OnMushroomPlaced(hoveredMushroom);
                }
            }
            else
            {
                // Incorrect placement: destroy all mushrooms in this area + the dropped one
                var toDestroy = new System.Collections.Generic.List<MushroomController>(placedMushrooms);

                // Include any children under this area
                var childMushrooms = GetComponentsInChildren<MushroomController>(includeInactive: true);
                foreach (var cm in childMushrooms)
                {
                    if (cm != null && cm.gameObject != null && !toDestroy.Contains(cm))
                        toDestroy.Add(cm);
                }

                // Add the hovered mushroom
                if (hoveredMushroom != null && hoveredMushroom.gameObject != null && !toDestroy.Contains(hoveredMushroom))
                    toDestroy.Add(hoveredMushroom);

                int destroyedCount = 0;
                foreach (var m in toDestroy)
                {
                    if (m != null && m.gameObject != null)
                    {
                        destroyedCount++;
                        Destroy(m.gameObject);
                    }
                }

                // Clear tracking list
                placedMushrooms.Clear();

                // Inform GameManager to reduce points
                if (GameManager.Instance != null && destroyedCount > 0)
                {
                    GameManager.Instance.OnIncorrectPlacement(acceptedType, destroyedCount);
                }

                onIncorrectMushroom?.Invoke();
            }

            // Clear hovered reference and highlight
            hoveredMushroom = null;
            ClearHighlight();
        }
        else if (hoveredMushroom != null && hoveredMushroom.gameObject == null)
        {
            // Mushroom was destroyed externally, just clear the reference
            hoveredMushroom = null;
            ClearHighlight();
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
}