// ════════════════════════════════════════════════════════════════════
//  ShopkeeperInteraction.cs — solo Collider Trigger
// ════════════════════════════════════════════════════════════════════

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

public class ShopkeeperInteraction : MonoBehaviour
{
    [Header("Animación NPC")]
    public Animator npcAnimator;

    [Header("Audio")]
    [Tooltip("El único AudioSource del tendero. Se le cambia el clip según el caso.")]
    public AudioSource npcAudioSource;

    [Tooltip("Audio Caso A — jugador sin plata, rechazo")]
    public AudioClip rejectClip;

    [Tooltip("Audio Caso B — jugador con plata, venta")]
    public AudioClip saleClip;

    [Header("── Caso A: Jugador SIN plata → Rechazo ─────")]
    public List<AnimationStep> rejectAnimationSequence = new List<AnimationStep>();

    [Header("── Caso B: Jugador CON plata → Venta ───────")]
    public List<AnimationStep> saleAnimationSequence = new List<AnimationStep>();

    [Header("── Objetos a Spawnear ────────────────────────")]
    public SpawnableItem grocery1;
    public SpawnableItem grocery2;

    // ── Control interno ───────────────────────────────────────────────
    private bool isInteracting = false;

    private FirstPersonController playerController;
    private DynamicMoveProvider   moveProvider;
    private Rigidbody             playerRigidbody;

    private bool    originalPlayerCanMove;
    private bool    originalHeadBob;
    private bool    moveProviderWasEnabled;
    private Vector3 originalVelocity;
    private Vector3 originalAngularVelocity;

    private GameObject dialogSistem;

    // ── Init ──────────────────────────────────────────────────────────
    void Start()
    {
        if (npcAnimator != null)
            npcAnimator.applyRootMotion = false;

        if (transform.parent != null)
            dialogSistem = transform.parent.Find("DialogSistem")?.gameObject;
    }

    // ── Trigger ───────────────────────────────────────────────────────
    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player") || isInteracting) return;

        HandleInteraction(other.gameObject);
    }

    // ── Lógica de decisión ────────────────────────────────────────────
    void HandleInteraction(GameObject player)
    {
        if (MissionState.CurrentPhase == MissionState.Phase.Idle)
        {
            Debug.Log("[Tendero] Misión no iniciada.");
            return;
        }

        if (MissionState.CurrentPhase == MissionState.Phase.GroceriesGiven ||
            MissionState.CurrentPhase == MissionState.Phase.MissionComplete)
        {
            Debug.Log("[Tendero] Ya completado.");
            return;
        }

        InitializePlayer(player);

        if (MissionState.CurrentPhase == MissionState.Phase.QuestGiven)
        {
            Debug.Log("[Tendero] SIN plata → rechazo");
            StartCoroutine(RejectSequence());
            return;
        }

        if (MissionState.CurrentPhase == MissionState.Phase.MoneyCollected)
        {
            Debug.Log("[Tendero] CON plata → venta");
            StartCoroutine(SaleSequence());
        }
    }

    // ── Secuencia de rechazo ──────────────────────────────────────────
    IEnumerator RejectSequence()
    {
        isInteracting = true;
        if (dialogSistem != null) dialogSistem.SetActive(false);
        LockPlayer();

        PlayClip(rejectClip);
        yield return StartCoroutine(PlayAnimationSequence(rejectAnimationSequence));

        RestorePlayer();
        isInteracting = false;
        if (dialogSistem != null) dialogSistem.SetActive(true);
        Debug.Log("[Tendero] Rechazo completado.");
    }

    // ── Secuencia de venta ────────────────────────────────────────────
    IEnumerator SaleSequence()
    {
        isInteracting = true;
        if (dialogSistem != null) dialogSistem.SetActive(false);
        LockPlayer();

        PlayClip(saleClip);

        if (grocery1.objectToMove != null && grocery1.spawnPoint != null)
            StartCoroutine(SpawnWithDelay(grocery1));
        if (grocery2.objectToMove != null && grocery2.spawnPoint != null)
            StartCoroutine(SpawnWithDelay(grocery2));

        yield return StartCoroutine(PlayAnimationSequence(saleAnimationSequence));

        RestorePlayer();
        isInteracting = false;
        if (dialogSistem != null) dialogSistem.SetActive(true);

        MissionState.SetGroceriesGiven();
        gameObject.SetActive(false);
        Debug.Log("[Tendero] Venta completada.");
    }

    // ── Helper de audio ───────────────────────────────────────────────
    void PlayClip(AudioClip clip)
    {
        if (npcAudioSource == null)
        {
            Debug.LogWarning("[Tendero] npcAudioSource no asignado en el Inspector.");
            return;
        }

        if (clip == null)
        {
            Debug.LogWarning("[Tendero] El AudioClip para este caso no está asignado.");
            return;
        }

        npcAudioSource.Stop();
        npcAudioSource.clip = clip;
        npcAudioSource.Play();
    }

    // ── Helpers ───────────────────────────────────────────────────────
    IEnumerator PlayAnimationSequence(List<AnimationStep> sequence)
    {
        if (npcAnimator == null)
        {
            Debug.LogWarning("[Tendero] npcAnimator es null. Asígnalo en el Inspector.");
            yield break;
        }

        if (sequence == null || sequence.Count == 0)
        {
            Debug.LogWarning("[Tendero] La secuencia de animación está vacía.");
            yield break;
        }

        foreach (AnimationStep step in sequence)
        {
            if (!string.IsNullOrEmpty(step.stateName))
            {
                Debug.Log("[Tendero] Reproduciendo: " + step.stateName);
                npcAnimator.Play(step.stateName, 0, 0f);
                yield return new WaitForSeconds(step.duration);
            }
        }
    }

    IEnumerator SpawnWithDelay(SpawnableItem item)
    {
        yield return new WaitForSeconds(item.spawnDelay);

        Rigidbody rb = item.objectToMove.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        item.objectToMove.transform.position = item.spawnPoint.position;
        item.objectToMove.transform.rotation = item.spawnPoint.rotation;
        item.objectToMove.SetActive(true);

        yield return null;
        yield return null;

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.WakeUp();
        }

        Debug.Log($"[Tendero] Spawneado: {item.objectToMove.name}");
    }

    void InitializePlayer(GameObject player)
    {
        playerController = player.GetComponent<FirstPersonController>();
        moveProvider     = player.GetComponent<DynamicMoveProvider>();
        playerRigidbody  = player.GetComponent<Rigidbody>();
        SaveState();
    }

    void SaveState()
    {
        if (playerController != null)
        {
            originalPlayerCanMove = playerController.playerCanMove;
            originalHeadBob       = playerController.enableHeadBob;
        }
        if (moveProvider    != null) moveProviderWasEnabled  = moveProvider.enabled;
        if (playerRigidbody != null)
        {
            originalVelocity        = playerRigidbody.linearVelocity;
            originalAngularVelocity = playerRigidbody.angularVelocity;
        }
    }

    void LockPlayer()
    {
        if (playerController != null)
        {
            playerController.playerCanMove = false;
            playerController.enableHeadBob = false;
        }
        if (moveProvider    != null) moveProvider.enabled = false;
        if (playerRigidbody != null)
        {
            playerRigidbody.linearVelocity  = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;
        }
    }

    void RestorePlayer()
    {
        if (playerController != null)
        {
            playerController.playerCanMove = originalPlayerCanMove;
            playerController.enableHeadBob = originalHeadBob;
        }
        if (moveProvider    != null) moveProvider.enabled         = moveProviderWasEnabled;
        if (playerRigidbody != null)
        {
            playerRigidbody.linearVelocity  = originalVelocity;
            playerRigidbody.angularVelocity = originalAngularVelocity;
        }
    }
}