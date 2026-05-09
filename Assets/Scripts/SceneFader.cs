using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;

public class SceneFader : MonoBehaviour
{
    public static SceneFader Instance { get; private set; }

    private CanvasGroup _canvasGroup;

    [Header("Duración del fade")]
    public float fadeDuration = 1f;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        SetupCanvas();

        _canvasGroup.alpha = 0f;
        _canvasGroup.blocksRaycasts = false;

        SceneManager.sceneLoaded += OnSceneLoaded;

        StartCoroutine(InitAndFadeIn());
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ReconnectIfVR();
    }

    IEnumerator InitAndFadeIn()
    {
        // Espera a que XR inicialice
        yield return null;
        yield return null;

        ReconnectIfVR();
        yield return StartCoroutine(FadeIn());
    }

    void SetupCanvas()
    {
        // Canvas
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;

        // CanvasScaler
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

        // Image del color beige que cubre toda la pantalla
        var imageGO = new GameObject("FadeImage");
        imageGO.transform.SetParent(transform, false);

        var image = imageGO.AddComponent<Image>();
        image.color = new Color(0.933f, 0.918f, 0.878f, 1f); // #EEEAE0

        var rect = imageGO.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        // CanvasGroup en el objeto raíz
        _canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    void ReconnectIfVR()
    {
        var canvas = GetComponent<Canvas>();
        if (canvas == null) return;

        bool isVR = UnityEngine.XR.XRSettings.isDeviceActive;

        Debug.Log($"[SceneFader] ReconnectIfVR - isVR: {isVR}, Camera.main: {Camera.main?.name}");

        if (isVR && Camera.main != null)
        {
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = Camera.main;
            canvas.planeDistance = 0.3f;
            canvas.sortingOrder = 9999;
        }
        else
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9999;
        }
    }

    // Cubre la pantalla instantáneamente sin animación
    public void CoverInstant()
    {
        ReconnectIfVR();
        _canvasGroup.alpha = 1f;
        _canvasGroup.blocksRaycasts = true;
    }

    // ─── API pública ──────────────────────────────────────────────────

    public void LoadScene(string sceneName)
    {
        StartCoroutine(FadeAndLoad(sceneName));
    }

    // ─── Corrutinas ───────────────────────────────────────────────────

    IEnumerator FadeAndLoad(string sceneName)
    {
        yield return StartCoroutine(FadeOut());

        AsyncOperation load = SceneManager.LoadSceneAsync(sceneName);
        load.allowSceneActivation = false;

        while (load.progress < 0.9f)
            yield return null;

        _canvasGroup.alpha = 1f;
        _canvasGroup.blocksRaycasts = true;
        load.allowSceneActivation = true;

        bool sceneReady = false;
        SceneManager.sceneLoaded += (s, m) => sceneReady = true;

        while (!sceneReady)
            yield return null;

        // Espera a que Camera.main esté disponible en VR
        float timeout = 10f;
        while (Camera.main == null && timeout > 0f)
        {
            timeout -= Time.deltaTime;
            yield return null;
        }

        yield return new WaitForSeconds(0.2f);

        ReconnectIfVR();
        _canvasGroup.alpha = 1f;

        yield return StartCoroutine(FadeIn());
    }

    public IEnumerator FadeOut()
    {
        _canvasGroup.blocksRaycasts = true;
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            _canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
            yield return null;
        }
        _canvasGroup.alpha = 1f;
    }

    public IEnumerator FadeIn()
    {
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            _canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            yield return null;
        }
        _canvasGroup.alpha = 0f;
        _canvasGroup.blocksRaycasts = false;
    }
}