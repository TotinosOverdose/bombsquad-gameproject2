using UnityEngine;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private Button startButton;
    [SerializeField] private Button exitButton;


    private void Awake()
    {
        startButton.onClick.AddListener(OnStartButtonClicked);
        exitButton.onClick.AddListener(OnExitButtonClicked);
    }

    private void OnStartButtonClicked()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("GameScene");
    }

    // Exit application
    private void OnExitButtonClicked()
    {
        Application.Quit();
    }
}
