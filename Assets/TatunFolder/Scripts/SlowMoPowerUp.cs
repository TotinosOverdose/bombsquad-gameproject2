using UnityEngine;
using UnityEngine.Events;

public class SlowMoPowerUp : MonoBehaviour
{
    public UnityEvent onPowerUpActivated;
    private CameraController cameraController;

    private void Awake()
    {
        cameraController = FindFirstObjectByType<CameraController>();
    }
    // When  clicked on power up with either mouse or touch controls, activate slow motion effect
    private void OnMouseDown()
    {
        cameraController.StartSlowMo();
        Destroy(gameObject);
    }

}
