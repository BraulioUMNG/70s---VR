// ════════════════════════════════════════════════════════════════════
//  CreditsSequenceManager.cs
//  Al completar la misión:
//    1. Espera un delay configurable
//    2. Teletransporta al jugador a la sala de créditos
//    3. Bloquea el movimiento (conserva rotación de cámara)
// ════════════════════════════════════════════════════════════════════
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
    [Tooltip("GameObject vacío orientado mirando hacia la pantalla del proyector")]
    [SerializeField] private Transform creditsSpawnPoint;

    [Header("── Tiempos ───────────────────────────────")]
    [SerializeField] private float delayBeforeTeleport = 5f;

    // ─────────────────────────────────────────────────────────────────

    private void OnEnable() => MissionState.OnMissionComplete += HandleMissionComplete;
    private void OnDisable() => MissionState.OnMissionComplete -= HandleMissionComplete;

    private void HandleMissionComplete()
    {
        Debug.Log("[CreditsSequenceManager] Misión completada. Iniciando secuencia.");
        StartCoroutine(CreditsSequence());
    }

    private IEnumerator CreditsSequence()
    {
        Debug.Log($"[CreditsSequenceManager] Esperando {delayBeforeTeleport}s...");
        yield return new WaitForSeconds(delayBeforeTeleport);

        TeleportPlayer();
        LockMovement();
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

        if (toDisable == null || toDisable.Length == 0)
        {
            Debug.LogWarning("[CreditsSequenceManager] Sin componentes de movimiento asignados.");
            return;
        }

        foreach (Behaviour comp in toDisable)
        {
            if (comp != null)
            {
                comp.enabled = false;
                Debug.Log($"[CreditsSequenceManager] Deshabilitado: {comp.GetType().Name}");
            }
        }
    }
}