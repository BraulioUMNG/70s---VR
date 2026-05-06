using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class FadeToBlack : MonoBehaviour
{
    public static FadeToBlack Instance { get; private set; }

    [Header("Canvas negro")]
    public CanvasGroup fadeCanvasGroup;

    [Header("Velocidad del fade")]
    public float fadeDuration = 1f;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Empieza transparente
        if (fadeCanvasGroup != null)
            fadeCanvasGroup.alpha = 0f;
    }

    // Llama esto: yield return StartCoroutine(FadeToBlack.Instance.Fade(...))
    public IEnumerator Fade(System.Action actionDuringBlack)
    {
        // Fade a negro
        yield return StartCoroutine(FadeOut());

        // Ejecuta lo que necesites mientras está negro (mover al jugador, etc.)
        actionDuringBlack?.Invoke();

        // Pequeña pausa en negro
        yield return new WaitForSeconds(0.3f);

        // Fade de vuelta
        yield return StartCoroutine(FadeIn());
    }

    IEnumerator FadeOut()
    {
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
            yield return null;
        }
        fadeCanvasGroup.alpha = 1f;
    }

    IEnumerator FadeIn()
    {
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            yield return null;
        }
        fadeCanvasGroup.alpha = 0f;
    }
}