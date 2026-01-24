using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public float musicTempo = 128f;
    public bool slowMoActive = false;
    [SerializeField] UIManager uiManager;
    [SerializeField] AudioManager audioManager;

    // Store refs for reset state
    private Coroutine slowMoZoomRoutine;
    private Vector3 storedCameraPosition;
    private float storedOrthoSize = 5f;
    private float storedTimeScale = 1f;
    private float storedFixedDelta = 0.02f;

    private void Awake()
    {
        uiManager = FindFirstObjectByType<UIManager>();
        audioManager = FindFirstObjectByType<AudioManager>();
    }
    public void StartSlowMo()
    {
        if (slowMoZoomRoutine != null)
        {
            StopCoroutine(slowMoZoomRoutine);
            slowMoZoomRoutine = null;
            RestoreCameraImmediate();
        }

        uiManager.ShowSlowMoPanel(5.0f);
        audioManager.StartAudioPitchShift(5.0f);
        StartCoroutine(ActivateSlowMo());
    }


    public Coroutine StartSlowMoAndZoom(Transform targetTransform, float duration, float zoomAmount, float slowFactor = 0.2f)
    {
        // If another zoom coroutine is running, stop it and restore immediately before starting a new one.
        if (slowMoZoomRoutine != null)
        {
            StopCoroutine(slowMoZoomRoutine);
            slowMoZoomRoutine = null;
            RestoreCameraImmediate();
        }

        slowMoZoomRoutine = StartCoroutine(SlowMoAndZoomInternal(targetTransform, duration, zoomAmount, slowFactor));
        return slowMoZoomRoutine;
    }


    private void RestoreCameraImmediate()
    {
        // restore timescale and fixed delta
        Time.timeScale = storedTimeScale;
        Time.fixedDeltaTime = storedFixedDelta;

        // restore camera transform and size
        transform.position = storedCameraPosition;
        if (Camera.main != null)
            Camera.main.orthographicSize = storedOrthoSize;

        slowMoActive = false;
    }


    public IEnumerator ActivateSlowMo()
    {
        slowMoActive = true;

        float originalTimeScale = Time.timeScale;
        float originalFixedDelta = Time.fixedDeltaTime;

        Time.timeScale = 0.2f;
        Time.fixedDeltaTime = originalFixedDelta * Time.timeScale;

        yield return new WaitForSecondsRealtime(5f);

        // restore
        Time.timeScale = originalTimeScale;
        Time.fixedDeltaTime = originalFixedDelta;

        slowMoActive = false;
    }

    private IEnumerator SlowMoAndZoomInternal(Transform targetTransform, float duration, float zoomAmount, float slowFactor = 0.2f)
    {
        slowMoActive = true;
        // clamp values
        slowFactor = Mathf.Clamp(slowFactor, 0.01f, 1f);
        duration = Mathf.Max(0.01f, duration);
        zoomAmount = Mathf.Max(0f, zoomAmount);

        // remember originals (also store to instance fields for external restore)
        storedTimeScale = Time.timeScale;
        storedFixedDelta = Time.fixedDeltaTime;
        storedCameraPosition = transform.position;
        storedOrthoSize = Camera.main != null ? Camera.main.orthographicSize : storedOrthoSize;

        Vector3 initialPosition = storedCameraPosition;
        Vector3 lastKnownTarget = (targetTransform != null) ? targetTransform.position : initialPosition;
        float initialSize = storedOrthoSize;
        float targetSize = Mathf.Max(0.01f, initialSize - zoomAmount);

        // enter slow-mo
        Time.timeScale = slowFactor;
        Time.fixedDeltaTime = storedFixedDelta * Time.timeScale;

        // animate toward target using unscaled time so animation runs independent of timescale
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            if (targetTransform != null)
            {
                lastKnownTarget = new Vector3(targetTransform.position.x, targetTransform.position.y, initialPosition.z);
            }

            Vector3 desiredPos = Vector3.Lerp(initialPosition, lastKnownTarget, t);
            transform.position = desiredPos;
            if (Camera.main != null)
                Camera.main.orthographicSize = Mathf.Lerp(initialSize, targetSize, t);

            yield return null;
        }

        // small hold at peak
        yield return new WaitForSecondsRealtime(0.12f);

        // smoothly restore camera position & size (real-time)
        float restoreDuration = Mathf.Clamp(duration * 0.5f, 0.12f, 0.6f);
        float elapsed2 = 0f;
        Vector3 currentPosAtPeak = transform.position;
        float currentSizeAtPeak = Camera.main != null ? Camera.main.orthographicSize : initialSize;
        while (elapsed2 < restoreDuration)
        {
            elapsed2 += Time.unscaledDeltaTime;
            float t2 = Mathf.Clamp01(elapsed2 / restoreDuration);
            transform.position = Vector3.Lerp(currentPosAtPeak, initialPosition, t2);
            if (Camera.main != null)
                Camera.main.orthographicSize = Mathf.Lerp(currentSizeAtPeak, initialSize, t2);
            yield return null;
        }

        // restore timescale and fixed delta
        Time.timeScale = storedTimeScale;
        Time.fixedDeltaTime = storedFixedDelta;
        slowMoActive = false;

        // clear routine handle
        slowMoZoomRoutine = null;
    }

    private void Update()
    {
        if (slowMoActive)
            return;

        // zoom in every beat

        float beatInterval = 60f / musicTempo;
        float beatPhase = (Time.time % beatInterval) / beatInterval;
        float zoomAmount = 0.1f; // adjust as needed

        Camera.main.orthographicSize = Mathf.Lerp(5f, 5f - zoomAmount, Mathf.Sin(beatPhase * Mathf.PI));

    }
}
