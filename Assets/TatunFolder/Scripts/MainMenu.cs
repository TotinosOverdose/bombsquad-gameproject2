using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class MainMenu : MonoBehaviour
{
    [SerializeField] Button startButton;
    [SerializeField] Button exitButton;
    [SerializeField] Button developersButton;

    [SerializeField] GameObject mainMenuPanel;
    [SerializeField] GameObject developersBG;
    [SerializeField] GameObject mainMenuBG;
    [SerializeField] bool devPanelShow = false;

    private void Awake()
    {
        startButton.onClick.AddListener(OnStartButtonClicked);
        exitButton.onClick.AddListener(OnExitButtonClicked);
        developersButton.onClick.AddListener(OnDevelopersButtonClicked);
    }

    private void OnEnable()
    {
        EnhancedTouchSupport.Enable();
    }

    private void OnDisable()
    {
        EnhancedTouchSupport.Disable();
    }

    private void OnStartButtonClicked()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("RuleScene");
    }

    // Exit application
    private void OnExitButtonClicked()
    {
        Application.Quit();
    }

    private void OnDevelopersButtonClicked()
    {
        if (!devPanelShow)
        {
            devPanelShow = true;
            mainMenuPanel.SetActive(false);
            mainMenuBG.SetActive(false);
            developersBG.SetActive(true);
            return;
        }
    }

    // If dev panel is showing, clicking or touching anywhere returns to main menu
    private void Update()
    {
        if (!devPanelShow) return;

        bool pressed = false;
        var touches = Touch.activeTouches;
        if (touches.Count > 0)
        {
            foreach (var t in touches)
            {
                if (t.phase == UnityEngine.InputSystem.TouchPhase.Began)
                {
                    pressed = true;
                    break;
                }
            }
        }

        // Mouse fallback via Input System
        if (!pressed && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            pressed = true;

        if (pressed)
        {
            devPanelShow = false;
            if (developersBG != null) developersBG.SetActive(false);
            if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
            if (mainMenuBG != null) mainMenuBG.SetActive(true);
        }
    }
}
