using UnityEngine;

public class SaveManager : MonoBehaviour
{
    private const string HIGH_SCORE_KEY = "HighScore";
    private const string HIGHEST_LEVEL_KEY = "HighestLevel";

    public static SaveManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Overall high score across all attempts
    public void SaveHighScore(int score)
    {
        int currentHigh = GetHighScore();
        if (score > currentHigh)
        {
            PlayerPrefs.SetInt(HIGH_SCORE_KEY, score);
            PlayerPrefs.Save();
            Debug.Log($"New overall high score: {score}");
        }
    }

    public int GetHighScore()
    {
        return PlayerPrefs.GetInt(HIGH_SCORE_KEY, 0);
    }

    // Highest level reached across all attempts
    public void SaveHighestLevel(int level)
    {
        int currentHighest = GetHighestLevel();
        if (level > currentHighest)
        {
            PlayerPrefs.SetInt(HIGHEST_LEVEL_KEY, level);
            PlayerPrefs.Save();
            Debug.Log($"New highest level reached: {level}");
        }
    }

    public int GetHighestLevel()
    {
        return PlayerPrefs.GetInt(HIGHEST_LEVEL_KEY, 1);
    }

    // Per-level high scores (from your existing GameManager)
    public void SaveLevelScore(int level, int score)
    {
        string key = $"HighScore_Level_{level}";
        int prev = PlayerPrefs.GetInt(key, 0);
        if (score > prev)
        {
            PlayerPrefs.SetInt(key, score);
            PlayerPrefs.Save();
        }
    }

    public int GetLevelScore(int level)
    {
        string key = $"HighScore_Level_{level}";
        return PlayerPrefs.GetInt(key, 0);
    }

    // Clear all saved data (useful for testing)
    public void ClearAllData()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("All save data cleared");
    }
}