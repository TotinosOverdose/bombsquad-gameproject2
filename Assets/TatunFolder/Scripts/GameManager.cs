using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameManager : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI typeAScoreText;
    public TextMeshProUGUI typeBScoreText;

    private int totalScore = 0;
    private int typeAScore = 0;
    private int typeBScore = 0;

    private void Start()
    {
        UpdateUI();
    }

    // Compatibility method for UnityEvent bindings
    public void OnIncorrectMushroom()
    {
        // default penalty: -5 global score
        totalScore = Mathf.Max(0, totalScore - 5);
        UpdateUI();
    }

    public void OnCorrectMushroom(MushroomType type)
    {
        // global +10 when a mushroom is correctly placed
        totalScore += 10;

        if (type == MushroomType.TypeA)
        {
            typeAScore++;
        }
        else if (type == MushroomType.TypeB)
        {
            typeBScore++;
        }

        UpdateUI();

        Debug.Log($"Correct! {type} sorted. Total score: {totalScore}");
    }

    // Called when an incorrect mushroom is dropped into a sorting area
    // destroyedCount = number of mushrooms destroyed in that area
    public void OnIncorrectPlacement(MushroomType areaType, int destroyedCount)
    {
        // reduce the area-specific score by destroyedCount
        if (areaType == MushroomType.TypeA)
        {
            typeAScore = Mathf.Max(0, typeAScore - destroyedCount);
        }
        else if (areaType == MushroomType.TypeB)
        {
            typeBScore = Mathf.Max(0, typeBScore - destroyedCount);
        }

        // also reduce global score proportional to destroyedCount
        totalScore = Mathf.Max(0, totalScore - 10 * destroyedCount);

        UpdateUI();

        Debug.Log($"Incorrect placement in {areaType}. Destroyed: {destroyedCount}. Total: {totalScore}");
    }

    // Called when a mushroom's lifetime expires without being placed
    public void OnMushroomExpired(MushroomController mushroom)
    {
        // Game over logic - here we simply log and could trigger a game over screen
        Debug.Log($"Mushroom expired: {mushroom.name}. Game Over.");
        // For now, stop time
        Time.timeScale = 0f;
        // Optionally display a UI or call a game over handler
    }

    private void UpdateUI()
    {
        if (scoreText != null)
            scoreText.text = $"Score: {totalScore}";

        if (typeAScoreText != null)
            typeAScoreText.text = $"Type A: {typeAScore}";

        if (typeBScoreText != null)
            typeBScoreText.text = $"Type B: {typeBScore}";
    }
}