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
        gameManager = FindObjectOfType<GameManager>();
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
        // Check if a hovered mushroom was just released
        if (hoveredMushroom != null && !hoveredMushroom.IsDragging)
        {
            if (hoveredMushroom.GetMushroomType() == acceptedType)
            {
                // Correct placement: keep mushroom in area and stop it
                hoveredMushroom.PlaceInArea(this);
                placedMushrooms.Add(hoveredMushroom);
                onCorrectMushroom?.Invoke(acceptedType);
                if (gameManager != null)
                {
                    gameManager.OnCorrectMushroom(acceptedType);
                }
            }
            else
            {
                // Incorrect placement: destroy all mushrooms in this area (and the one just dropped)
                onIncorrectMushroom?.Invoke();

                // gather all to destroy: include placedMushrooms, any child mushrooms, and the hovered one
                var toDestroy = new System.Collections.Generic.List<MushroomController>(placedMushrooms);

                // include any children under this area (covers placed items that might not be tracked)
                var childMushrooms = GetComponentsInChildren<MushroomController>(includeInactive: true);
                foreach (var cm in childMushrooms)
                {
                    if (cm != null && !toDestroy.Contains(cm))
                        toDestroy.Add(cm);
                }

                if (hoveredMushroom != null && !toDestroy.Contains(hoveredMushroom))
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

                // clear tracking list
                placedMushrooms.Clear();

                // Inform GameManager to reduce points for this area
                if (gameManager != null && destroyedCount > 0)
                {
                    gameManager.OnIncorrectPlacement(acceptedType, destroyedCount);
                }
            }

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