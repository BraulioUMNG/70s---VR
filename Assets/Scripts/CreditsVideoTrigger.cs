// ════════════════════════════════════════════════════════════════════
//  CreditsVideoTrigger.cs
//  Reproduce el video de créditos cuando el jugador entra al trigger.
//  Al terminar el video carga la escena del menú principal.
//  Coloca este script en el mismo GameObject que tiene el Collider trigger.
// ════════════════════════════════════════════════════════════════════
using System.Collections;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

public class CreditsVideoTrigger : MonoBehaviour
{
    [Header("── Video ────────────────────────────────")]
    [Tooltip("El VideoPlayer que está sobre la pantalla del proyector")]
    [SerializeField] private VideoPlayer videoPlayer;

    [Header("── Tiempos ─────────────────────────────")]
    [Tooltip("Segundos de espera antes de que empiece el video")]
    [SerializeField] private float delayBeforePlay = 2f;

    [Header("── Escena de salida ─────────────────────")]
    [Tooltip("Nombre exacto de la escena en Build Settings")]
    [SerializeField] private string mainMenuScene = "Interfaz";

    private bool _triggered = false;

    // ─────────────────────────────────────────────────────────────────

    private void Start()
    {
        if (videoPlayer == null)
        {
            Debug.LogError("[CreditsVideoTrigger] VideoPlayer no asignado.");
            return;
        }

        // Preparar el video sin reproducirlo todavía
        videoPlayer.Prepare();
        videoPlayer.Stop();

        // Suscribirse al evento de fin de video
        videoPlayer.loopPointReached += OnVideoFinished;
    }

    private void OnDestroy()
    {
        if (videoPlayer != null)
            videoPlayer.loopPointReached -= OnVideoFinished;
    }

    // ─────────────────────────────────────────────────────────────────

    private void OnTriggerEnter(Collider other)
    {
        if (_triggered) return;
        if (!other.CompareTag("Player")) return;

        _triggered = true;
        Debug.Log("[CreditsVideoTrigger] Jugador detectado. Iniciando secuencia de video.");
        StartCoroutine(PlayWithDelay());
    }

    private IEnumerator PlayWithDelay()
    {
        Debug.Log($"[CreditsVideoTrigger] Esperando {delayBeforePlay}s antes de reproducir...");
        yield return new WaitForSeconds(delayBeforePlay);

        videoPlayer.Play();
        Debug.Log("[CreditsVideoTrigger] Video reproduciendo.");
    }

    private void OnVideoFinished(VideoPlayer vp)
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 1f;

        Debug.Log("[CreditsVideoTrigger] Video terminado. Cargando menú principal...");
        SceneFader.Instance.LoadScene(mainMenuScene);
    }
}