
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("UI References")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI typeAScoreText;
    public TextMeshProUGUI typeBScoreText;

    [SerializeField] int mushroomScore = 10;

    private int totalScore = 0;
    private int typeAScore = 0;
    private int typeBScore = 0;

    [Header("Level Settings")]
    public int currentLevel = 1;

    [Header("UI Manager")]
    public UIManager uiManager;

    // runtime tracking
    // authoritative set of active mushrooms (spawned and not yet placed/destroyed)
    private readonly HashSet<MushroomController> activeMushrooms = new HashSet<MushroomController>();
    private int spawnersFinishedCount = 0;
    private int totalSpawners = 0;
    private List<MushroomSpawner> spawners = new List<MushroomSpawner>();
    private bool gameIsOver = false;
    private bool isTransitioningLevel = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Debug.LogWarning("Duplicate GameManager detected - destroying duplicate.");
            Destroy(gameObject);
            return;
        }

        if (uiManager == null)
            uiManager = FindFirstObjectByType<UIManager>();

        // Ensure SaveManager exists
        if (SaveManager.Instance == null)
        {
            GameObject sm = new GameObject("SaveManager");
            sm.AddComponent<SaveManager>();
        }
    }

    private void Start()
    {
        StartCoroutine(StartGameSequence());
    }

    private IEnumerator StartGameSequence()
    {
        if (uiManager != null)
            yield return StartCoroutine(uiManager.ShowLevelTransition(currentLevel));

        InitializeLevel();
        UpdateUI();

        foreach (var spawner in spawners)
        {
            spawner.StartSpawning();
        }
    }

    private void InitializeLevel()
    {
        Time.timeScale = 1f;
        gameIsOver = false;
        isTransitioningLevel = false;

        spawners.Clear();
        spawners.AddRange(FindObjectsOfType<MushroomSpawner>());
        totalSpawners = spawners.Count;
        spawnersFinishedCount = 0;

        // clear registry
        activeMushrooms.Clear();

        Debug.Log($"InitializeLevel: Found {totalSpawners} spawners");

        foreach (var s in spawners)
        {
            s.onMushroomSpawned += OnMushroomSpawned;
            s.onSpawnerFinished += NotifySpawnerFinished;
            s.SetGameLevel(currentLevel);
        }
    }

    // authoritative register: call when a spawner *creates* a mushroom
    public void RegisterMushroom(MushroomController mc)
    {
        if (mc == null) return;
        if (activeMushrooms.Add(mc))
        {
            Debug.Log($"RegisterMushroom: Added. Active count: {activeMushrooms.Count}");
            UpdateActiveCountUIIfNeeded();
        }
    }

    // authoritative unregister: call when a mushroom is placed OR destroyed
    public void UnregisterMushroom(MushroomController mc)
    {
        if (mc == null) return;
        if (activeMushrooms.Remove(mc))
        {
            Debug.Log($"UnregisterMushroom: Removed. Active count: {activeMushrooms.Count}");
            UpdateActiveCountUIIfNeeded();
            CheckLevelComplete();
        }
    }

    // Called when spawner spawns a mushroom (subscribed)
    public void OnMushroomSpawned(MushroomController mc)
    {
        RegisterMushroom(mc);
    }

    // Called when a placed mushroom is placed into a sorting area (not destroyed)
    public void OnMushroomPlaced(MushroomController mc)
    {
        if (mc == null)
        {
            Debug.LogWarning("OnMushroomPlaced called with null mushroom controller");
            return;
        }

        // award points for correct placement
        OnCorrectMushroom(mc.GetMushroomType());

        // Unregister from active list (authoritative)
        UnregisterMushroom(mc);
    }

    // Called by MushroomTracker when a spawned mushroom is destroyed (e.g. incorrect placement or otherwise)
    public void OnMushroomRemoved(MushroomController mc)
    {
        // route through Unregister (idempotent)
        UnregisterMushroom(mc);
    }

    // spawner calls this when it has spawned all its mushrooms
    public void NotifySpawnerFinished(MushroomSpawner spawner)
    {
        spawnersFinishedCount++;
        Debug.Log($"Spawner finished. Total finished: {spawnersFinishedCount}/{totalSpawners}");
        CheckLevelComplete();
    }

    private void CheckLevelComplete()
    {
        if (gameIsOver || isTransitioningLevel) return;

        Debug.Log($"CheckLevelComplete: Spawners finished: {spawnersFinishedCount}/{totalSpawners}, Active mushrooms: {activeMushrooms.Count}");

        if (spawnersFinishedCount >= totalSpawners && activeMushrooms.Count == 0)
        {
            Debug.Log($"Level {currentLevel} completed! Proceeding to next level.");
            isTransitioningLevel = true;
            SaveHighScoreForLevel(currentLevel);
            StartCoroutine(ProceedToNextLevel());
        }
    }

    private IEnumerator ProceedToNextLevel()
    {
        foreach (var spawner in spawners)
        {
            if (spawner != null)
                spawner.StopSpawning();
        }

        yield return new WaitForSecondsRealtime(1.0f);

        NextLevel();

        if (uiManager != null)
            yield return StartCoroutine(uiManager.ShowLevelTransition(currentLevel));

        InitializeLevel();
        UpdateUI();

        foreach (var spawner in spawners)
        {
            if (spawner != null)
                spawner.StartSpawning();
        }

        isTransitioningLevel = false;
    }

    public void SaveHighScoreForLevel(int level)
    {
        string key = $"HighScore_Level_{level}";
        int prev = PlayerPrefs.GetInt(key, 0);
        if (totalScore > prev)
        {
            PlayerPrefs.SetInt(key, totalScore);
            PlayerPrefs.Save();
            Debug.Log($"New high score for level {level}: {totalScore}");
        }

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.SaveLevelScore(level, totalScore);
        }
    }

    public int GetHighScoreForLevel(int level)
    {
        string key = $"HighScore_Level_{level}";
        return PlayerPrefs.GetInt(key, 0);
    }

    public void OnCorrectMushroom(MushroomType type)
    {
        totalScore += mushroomScore;

        if (type == MushroomType.TypeA)
        {
            typeAScore++;
        }
        else if (type == MushroomType.TypeB)
        {
            typeBScore++;
        }

        UpdateUI();
    }

    public void OnIncorrectPlacement(MushroomType areaType, int destroyedCount)
    {
        totalScore -= (destroyedCount * mushroomScore);
        totalScore = Mathf.Max(0, totalScore);

        UpdateUI();

        Debug.Log($"Incorrect placement in {areaType}. Destroyed: {destroyedCount}. Total: {totalScore}");
    }

    public IEnumerator OnMushroomExpired(MushroomController mushroom)
    {
        if (gameIsOver) yield break;
        gameIsOver = true;

        foreach (var spawner in spawners)
        {
            if (spawner != null)
                spawner.StopSpawning();
        }

        if (mushroom != null && mushroom.animator != null)
            mushroom.animator.SetTrigger("Explode");

        yield return new WaitForSeconds(1.5f);

        SaveHighScoreForLevel(currentLevel);

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.SaveHighScore(totalScore);
            SaveManager.Instance.SaveHighestLevel(currentLevel);
        }

        Time.timeScale = 0f;
        Debug.Log("Mushroom expired! Game Over.");

        if (uiManager != null)
        {
            int highScore = SaveManager.Instance != null ? SaveManager.Instance.GetHighScore() : 0;
            uiManager.ShowGameOver(totalScore, highScore, currentLevel);
        }

        yield return null;
    }

    private void UpdateUI()
    {
        if (scoreText != null)
            scoreText.text = $"Score: {totalScore}";

        if (levelText != null)
            levelText.text = $"Level: {currentLevel}";

        if (typeAScoreText != null)
            typeAScoreText.text = $"Type A: {typeAScore}";

        if (typeBScoreText != null)
            typeBScoreText.text = $"Type B: {typeBScore}";
    }

    private void UpdateActiveCountUIIfNeeded()
    {
        // Optional hook to update debug/visual UI for active count in future.
    }

    public void NextLevel()
    {
        currentLevel++;

        MushroomSpawner[] all = FindObjectsOfType<MushroomSpawner>();
        foreach (var s in all)
        {
            s.spawnInterval *= 0.9f;
            s.maxMushroomsSpawned += 5;
        }

        Debug.Log($"Proceeding to level {currentLevel}");
    }

    // Public getters for UI
    public int GetTotalScore() => totalScore;
    public int GetCurrentLevel() => currentLevel;
}