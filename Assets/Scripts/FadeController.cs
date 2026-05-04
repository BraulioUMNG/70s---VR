// ════════════════════════════════════════════════════════════════════
//  FadeController.cs
//  Controla el fade a negro del Canvas overlay.
//  Requiere: estar en un GameObject que tenga CanvasGroup.
//  El Canvas padre debe ser Screen Space - Overlay, con una Image
//  negra que cubra toda la pantalla.
// ════════════════════════════════════════════════════════════════════
using System.Collections;
using UnityEngine;

public class FadeController : MonoBehaviour
{
    [Header("Duración del fade (segundos)")]
    [SerializeField] private float fadeDuration = 1.2f;

    private CanvasGroup _canvasGroup;

    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();

        // Empieza completamente transparente y sin bloquear input
        _canvasGroup.alpha = 0f;
        _canvasGroup.blocksRaycasts = false;
        _canvasGroup.interactable = false;
    }

    // ── Llama desde afuera con StartCoroutine(fadeController.FadeToBlack()) ──
    public IEnumerator FadeToBlack()
    {
        _canvasGroup.blocksRaycasts = true;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            _canvasGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);
            yield return null;
        }

        _canvasGroup.alpha = 1f;
    }

    // ── Llama desde afuera con StartCoroutine(fadeController.FadeFromBlack()) ──
    public IEnumerator FadeFromBlack()
    {
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            _canvasGroup.alpha = Mathf.Clamp01(1f - (elapsed / fadeDuration));
            yield return null;
        }

        _canvasGroup.alpha = 0f;
        _canvasGroup.blocksRaycasts = false;
        _canvasGroup.interactable = false;
    }

    // ── Utilidad: fuerza el negro instantáneamente (para inicializar) ──
    public void SetBlackInstant() => _canvasGroup.alpha = 1f;
    public void SetClearInstant() => _canvasGroup.alpha = 0f;
}