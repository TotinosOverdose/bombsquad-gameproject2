using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class UIManager : MonoBehaviour
{
    [Header("Game Over Panel")]
    public GameObject gameOverPanel;
    public TextMeshProUGUI finalScoreText;
    public TextMeshProUGUI highScoreText;
    public TextMeshProUGUI levelReachedText;
    public Button restartButton;
    public Button mainMenuButton;

    [Header("Level Transition Panel")]
    public GameObject levelTransitionPanel;
    public TextMeshProUGUI levelTransitionText;
    public float levelTransitionDuration = 2f;

    [Header("Pause Panel")]
    public GameObject pausePanel;
    public Button resumeButton;
    public Button pauseRestartButton;
    public Button pauseMainMenuButton;

    [Header("HUD")]
    public GameObject hudPanel;

    private GameManager gameManager;
    private bool isPaused = false;

    private void Awake()
    {
        gameManager = FindFirstObjectByType<GameManager>();

        // Setup button listeners
        if (restartButton != null)
            restartButton.onClick.AddListener(RestartGame);

        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(LoadMainMenu);

        if (resumeButton != null)
            resumeButton.onClick.AddListener(ResumeGame);

        if (pauseRestartButton != null)
            pauseRestartButton.onClick.AddListener(RestartGame);

        if (pauseMainMenuButton != null)
            pauseMainMenuButton.onClick.AddListener(LoadMainMenu);

        // Hide all panels at start
        HideAllPanels();
    }

    private void HideAllPanels()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        if (levelTransitionPanel != null)
            levelTransitionPanel.SetActive(false);

        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (hudPanel != null)
            hudPanel.SetActive(true);
    }

    public void ShowGameOver(int finalScore, int highScore, int levelReached)
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);

            if (finalScoreText != null)
                finalScoreText.text = $"Score: {finalScore}";

            if (highScoreText != null)
            {
                if (finalScore > highScore)
                    highScoreText.text = $"New High Score!";
                else
                    highScoreText.text = $"High Score: {highScore}";
            }

            if (levelReachedText != null)
                levelReachedText.text = $"Level Reached: {levelReached}";

            if (hudPanel != null)
                hudPanel.SetActive(false);
        }
    }

    public IEnumerator ShowLevelTransition(int level)
    {
        if (levelTransitionPanel != null && levelTransitionText != null)
        {
            levelTransitionPanel.SetActive(true);
            levelTransitionText.text = $"LEVEL {level}";

            if (levelTransitionPanel.TryGetComponent<CanvasGroup>(out var canvasGroup))
            {
                // Fade in
                canvasGroup.alpha = 0;
                float elapsed = 0;
                while (elapsed < 0.5f)
                {
                    elapsed += Time.unscaledDeltaTime;
                    canvasGroup.alpha = Mathf.Lerp(0, 1, elapsed / 0.5f);
                    yield return null;
                }
                canvasGroup.alpha = 1;

                // Hold
                yield return new WaitForSecondsRealtime(levelTransitionDuration - 1f);

                // Fade out
                elapsed = 0;
                while (elapsed < 0.5f)
                {
                    elapsed += Time.unscaledDeltaTime;
                    canvasGroup.alpha = Mathf.Lerp(1, 0, elapsed / 0.5f);
                    yield return null;
                }
                canvasGroup.alpha = 0;
            }
            else
            {
                yield return new WaitForSecondsRealtime(levelTransitionDuration);
            }

            levelTransitionPanel.SetActive(false);
        }
    }

    public void ShowPauseMenu()
    {
        if (pausePanel != null)
        {
            pausePanel.SetActive(true);
            Time.timeScale = 0f;
            isPaused = true;

            if (hudPanel != null)
                hudPanel.SetActive(false);
        }
    }

    public void ResumeGame()
    {
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
            Time.timeScale = 1f;
            isPaused = false;

            if (hudPanel != null)
                hudPanel.SetActive(true);
        }
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
}