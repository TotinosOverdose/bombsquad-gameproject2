using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ScorePopupManager : MonoBehaviour
{
    public static ScorePopupManager Instance { get; private set; }

    [Tooltip("Assign scene Canvas for UGUI popups (optional if you only use world-space prefabs)")]
    public Canvas sceneCanvas;

    [Tooltip("Prefab for popup. Can be a world-space TextMeshPro or a TextMeshProUGUI (UI) object.")]
    public GameObject popupPrefab;

    public int poolSize = 10;

    private Queue<GameObject> pool;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        pool = new Queue<GameObject>();

        for (int i = 0; i < poolSize; i++)
        {
            var go = Instantiate(popupPrefab);
            go.SetActive(false);
            // keep pooled objects under manager for cleanliness
            go.transform.SetParent(transform, worldPositionStays: false);
            pool.Enqueue(go);
        }
    }

    GameObject GetPopup()
    {
        if (pool.Count > 0) return pool.Dequeue();
        var go = Instantiate(popupPrefab);
        go.SetActive(false);
        go.transform.SetParent(transform, worldPositionStays: false);
        return go;
    }

    public void ShowAtWorldPosition(Vector3 worldPos, string text, Color color, float rise = 0.5f, float duration = 0.9f)
    {
        if (popupPrefab == null)
            return;

        var go = GetPopup();
        if (go == null) return;

        // Try 3D TextMeshPro first
        var tmp3d = go.GetComponentInChildren<TextMeshPro>(true);
        if (tmp3d != null)
        {
            tmp3d.text = text;
            tmp3d.color = color;
            go.SetActive(true);
            // position in world
            go.transform.SetParent(null, worldPositionStays: true);
            go.transform.position = worldPos;
            StartCoroutine(AnimateWorldAndReturnToPool(go, tmp3d.gameObject, rise, duration));
            return;
        }

        // Fallback to UGUI TextMeshPro
        var tmpUI = go.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tmpUI != null)
        {
            if (sceneCanvas == null)
                sceneCanvas = FindObjectOfType<Canvas>();

            tmpUI.text = text;
            tmpUI.color = color;

            go.SetActive(true);
            // parent to canvas
            if (sceneCanvas != null)
                go.transform.SetParent(sceneCanvas.transform, worldPositionStays: false);
            else
                go.transform.SetParent(transform, worldPositionStays: false);

            Camera cam = (sceneCanvas != null && sceneCanvas.renderMode != RenderMode.ScreenSpaceOverlay) ? (sceneCanvas.worldCamera ?? Camera.main) : null;
            Vector3 screenPoint = (cam != null) ? cam.WorldToScreenPoint(worldPos) : (Camera.main != null ? Camera.main.WorldToScreenPoint(worldPos) : new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f));

            RectTransform canvasRect = (sceneCanvas != null) ? sceneCanvas.GetComponent<RectTransform>() : null;
            RectTransform rt = go.GetComponent<RectTransform>();
            Vector2 localPoint;
            if (canvasRect != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, cam, out localPoint))
                rt.anchoredPosition = localPoint;
            else
                rt.anchoredPosition = new Vector2(screenPoint.x, screenPoint.y);

            StartCoroutine(AnimateUIAndReturnToPool(go, rt, tmpUI, rise, duration));
            return;
        }

        // If no TMP found, disable and return
        go.SetActive(false);
        pool.Enqueue(go);
    }

    private IEnumerator AnimateWorldAndReturnToPool(GameObject root, GameObject tmpObj, float rise, float duration)
    {
        float elapsed = 0f;
        Vector3 start = root.transform.position;
        Vector3 end = start + Vector3.up * rise;

        // try to get a MeshRenderer color if needed (we fade TMP color instead)
        var tmp = tmpObj.GetComponent<TextMeshPro>();
        Color startColor = tmp != null ? tmp.color : Color.white;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            root.transform.position = Vector3.Lerp(start, end, t);
            if (tmp != null)
            {
                Color c = startColor; c.a = Mathf.Lerp(1f, 0f, t); tmp.color = c;
            }
            yield return null;
        }

        // return to pool
        root.SetActive(false);
        root.transform.SetParent(transform, worldPositionStays: false);
        pool.Enqueue(root);
    }

    private IEnumerator AnimateUIAndReturnToPool(GameObject root, RectTransform rt, TextMeshProUGUI tmp, float rise, float duration)
    {
        float elapsed = 0f;
        Vector2 start = rt.anchoredPosition;
        float pixelMultiplier = (Camera.main != null) ? (Screen.height / (Camera.main.orthographicSize * 2f)) : (Screen.height / 6f);
        Vector2 end = start + Vector2.up * (rise * pixelMultiplier);
        Color startColor = tmp != null ? tmp.color : Color.white;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            rt.anchoredPosition = Vector2.Lerp(start, end, t);
            if (tmp != null)
            {
                Color c = startColor; c.a = Mathf.Lerp(1f, 0f, t); tmp.color = c;
            }
            yield return null;
        }

        // return to pool
        root.SetActive(false);
        root.transform.SetParent(transform, worldPositionStays: false);
        pool.Enqueue(root);
    }
}
