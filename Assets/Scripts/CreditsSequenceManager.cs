using System.Collections;
using UnityEngine;

public class CreditsSequenceManager : MonoBehaviour
{
    [Header("── Detección de modo ──────────────────────")]
    [SerializeField] private PlayerModeManager playerModeManager;

    [Header("── Jugador Desktop ────────────────────────")]
    [SerializeField] private Transform desktopPlayerRoot;
    [SerializeField] private Behaviour[] desktopMovementComponents;

    [Header("── Jugador VR ──────────────────────────────")]
    [SerializeField] private Transform xrOriginRoot;
    [SerializeField] private Behaviour[] vrMovementComponents;

    [Header("── Sala de créditos ──────────────────────")]
    [SerializeField] private Transform creditsSpawnPoint;

    [Header("── Tiempos ───────────────────────────────")]
    [SerializeField] private float delayBeforeFade = 2f; // Espera antes del fade

    // ─────────────────────────────────────────────────────────────────

    private void OnEnable()  => MissionState.OnMissionComplete += HandleMissionComplete;
    private void OnDisable() => MissionState.OnMissionComplete -= HandleMissionComplete;

    private void HandleMissionComplete()
    {
        Debug.Log("[CreditsSequenceManager] Misión completada. Iniciando secuencia.");
        StartCoroutine(CreditsSequence());
    }

    private IEnumerator CreditsSequence()
    {
        // Espera un momento antes de arrancar el fade
        Debug.Log($"[CreditsSequenceManager] Esperando {delayBeforeFade}s antes del fade...");
        yield return new WaitForSeconds(delayBeforeFade);

        // Fade a negro → TP → fade de vuelta
        if (FadeToBlack.Instance != null)
        {
            yield return StartCoroutine(FadeToBlack.Instance.Fade(() =>
            {
                TeleportPlayer();
                LockMovement();
            }));
        }
        else
        {
            // Sin fade, hace el TP directo
            Debug.LogWarning("[CreditsSequenceManager] FadeToBlack no encontrado, TP directo.");
            TeleportPlayer();
            LockMovement();
        }
    }

    // ─────────────────────────────────────────────────────────────────

    private void TeleportPlayer()
    {
        if (creditsSpawnPoint == null)
        {
            Debug.LogError("[CreditsSequenceManager] creditsSpawnPoint no asignado.");
            return;
        }

        bool isVR = playerModeManager != null && playerModeManager.IsVRMode;

        if (isVR)
        {
            if (xrOriginRoot == null) { Debug.LogError("[CreditsSequenceManager] xrOriginRoot no asignado."); return; }
            xrOriginRoot.position = creditsSpawnPoint.position;
            xrOriginRoot.rotation = creditsSpawnPoint.rotation;
            Debug.Log("[CreditsSequenceManager] VR: teletransportado.");
        }
        else
        {
            if (desktopPlayerRoot == null) { Debug.LogError("[CreditsSequenceManager] desktopPlayerRoot no asignado."); return; }
            desktopPlayerRoot.position = creditsSpawnPoint.position;
            desktopPlayerRoot.rotation = creditsSpawnPoint.rotation;
            Debug.Log("[CreditsSequenceManager] Desktop: teletransportado.");
        }
    }

    private void LockMovement()
    {
        bool isVR = playerModeManager != null && playerModeManager.IsVRMode;
        Behaviour[] toDisable = isVR ? vrMovementComponents : desktopMovementComponents;

        if (toDisable == null || toDisable.Length == 0) return;

        foreach (Behaviour comp in toDisable)
        {
            // No desactiva el PauseManager ni el FadeToBlack
            if (comp == null) continue;
            if (comp is PauseManager) continue;
            if (comp is FadeToBlack) continue;

            comp.enabled = false;
            Debug.Log($"[CreditsSequenceManager] Deshabilitado: {comp.GetType().Name}");
        }
    }
}