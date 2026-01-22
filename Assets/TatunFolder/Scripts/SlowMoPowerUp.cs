using UnityEngine;
using UnityEngine.Events;

public class SlowMoPowerUp : MonoBehaviour
{
    public UnityEvent onPowerUpActivated;
    // When  clicked on power up with either mouse or touch controls, activate slow motion effect
    private void OnMouseDown()
    {
        onPowerUpActivated.Invoke();
        Destroy(gameObject);
    }

}
