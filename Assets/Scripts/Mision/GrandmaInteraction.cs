// ════════════════════════════════════════════════════════════════════
//  GrandmaInteraction.cs  —  NPC: La Abuela
//
//  FASE 1 (Idle → QuestGiven):
//    El jugador entra al trigger → la abuela hace su animación de encargo,
//    suena el audio de la misión y se spawnea la PLATA en el punto asignado.
//    El trigger se desactiva para no volver a dispararse.
//
//  FASE 2 (GroceriesGiven → MissionComplete):
//    El jugador regresa con el mercado → la abuela hace su animación de
//    agradecimiento y suena el audio de gracias.
//    Se activa un segundo trigger (agradecimiento) separado o el mismo
//    objeto se reactiva internamente.
//
//  NOTA: Este script necesita que MissionState.cs esté en el proyecto.
//        La plata debe tener CollectibleItem.cs con la llamada a
//        MissionState.SetMoneyCollected().
// ════════════════════════════════════════════════════════════════════
// ════════════════════════════════════════════════════════════════════
//  GrandmaInteraction.cs  —  NPC: La Abuela
// ════════════════════════════════════════════════════════════════════

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

public class GrandmaInteraction : MonoBehaviour
{
    // ── ANIMADOR NPC ──────────────────────────────────────────────────
    [Header("Animación NPC")]
    public Animator npcAnimator;

    // ── AUDIO ─────────────────────────────────────────────────────────
    [Header("Audio")]
    [Tooltip("El único AudioSource de la abuela. Se le cambia el clip según la fase.")]
    public AudioSource npcAudioSource;

    [Tooltip("Audio Fase 1 — encargo del mandado")]
    public AudioClip questClip;

    [Tooltip("Audio Fase 2 — agradecimiento al recibir el mercado")]
    public AudioClip thankClip;

    [Tooltip("Audio Fase 3 — regaño por volver sin mercado")]
    public AudioClip scoldClip;

    // ── SECUENCIAS DE ANIMACIÓN ───────────────────────────────────────
    [Header("── Fase 1: Encargo ──────────────────────────────────────")]
    public List<AnimationStep> questAnimationSequence = new List<AnimationStep>();

    [Header("── Fase 2: Agradecimiento ──────────────────────────────")]
    public List<AnimationStep> thankAnimationSequence = new List<AnimationStep>();

    [Header("── Fase 3: Regaño ─────────────────────────────────────")]
    public List<AnimationStep> scoldAnimationSequence = new List<AnimationStep>();

    // ── OBJETOS ───────────────────────────────────────────────────────
    [Header("Objetos a Spawnear")]
    public SpawnableItem moneyItem;

    // ── CONTROL INTERNO ───────────────────────────────────────────────
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

    // ── INIT ──────────────────────────────────────────────────────────
    void Start()
    {
        if (npcAnimator != null)
            npcAnimator.applyRootMotion = false;

        if (transform.parent != null)
            dialogSistem = transform.parent.Find("DialogSistem")?.gameObject;
    }

    // ── TRIGGER ───────────────────────────────────────────────────────
    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player") || isInteracting) return;

        switch (MissionState.CurrentPhase)
        {
            case MissionState.Phase.Idle:
                InitializePlayer(other.gameObject);
                StartCoroutine(QuestSequence());
                break;

            case MissionState.Phase.GroceriesGiven:
                InitializePlayer(other.gameObject);
                StartCoroutine(ThankSequence());
                break;

            case MissionState.Phase.MoneyCollected:
                InitializePlayer(other.gameObject);
                StartCoroutine(ScoldSequence());
                break;

            case MissionState.Phase.QuestGiven:
                Debug.Log("[Abuela] Jugador aún no recogió la plata.");
                break;

            case MissionState.Phase.MissionComplete:
                Debug.Log("[Abuela] Misión completa.");
                break;
        }
    }

    // ── SECUENCIAS ────────────────────────────────────────────────────

    IEnumerator QuestSequence()
    {
        isInteracting = true;
        if (dialogSistem != null) dialogSistem.SetActive(false);
        LockPlayer();

        PlayClip(questClip);

        if (moneyItem.objectToMove != null && moneyItem.spawnPoint != null)
            StartCoroutine(SpawnWithDelay(moneyItem));

        yield return StartCoroutine(PlayAnimationSequence(questAnimationSequence));

        RestorePlayer();
        isInteracting = false;
        if (dialogSistem != null) dialogSistem.SetActive(true);
        MissionState.SetQuestGiven();
    }

    IEnumerator ThankSequence()
    {
        isInteracting = true;
        if (dialogSistem != null) dialogSistem.SetActive(false);
        LockPlayer();

        PlayClip(thankClip);

        yield return StartCoroutine(PlayAnimationSequence(thankAnimationSequence));

        RestorePlayer();
        isInteracting = false;
        if (dialogSistem != null) dialogSistem.SetActive(true);
        MissionState.SetMissionComplete();
        gameObject.SetActive(false);
        Debug.Log("[Abuela] Misión completada. Trigger desactivado.");
    }

    IEnumerator ScoldSequence()
    {
        isInteracting = true;
        if (dialogSistem != null) dialogSistem.SetActive(false);
        LockPlayer();

        PlayClip(scoldClip);

        yield return StartCoroutine(PlayAnimationSequence(scoldAnimationSequence));

        RestorePlayer();
        isInteracting = false;
        if (dialogSistem != null) dialogSistem.SetActive(true);
        Debug.Log("[Abuela] El jugador fue regañado.");
    }

    // ── HELPER DE AUDIO ───────────────────────────────────────────────

    /// <summary>
    /// Detiene lo que esté sonando, asigna el clip y lo reproduce.
    /// Si el clip está vacío, avisa en consola sin crashear.
    /// </summary>
    void PlayClip(AudioClip clip)
    {
        if (npcAudioSource == null)
        {
            Debug.LogWarning("[Abuela] npcAudioSource no asignado en el Inspector.");
            return;
        }

        if (clip == null)
        {
            Debug.LogWarning("[Abuela] El AudioClip para esta fase no está asignado.");
            return;
        }

        npcAudioSource.Stop();       // Para cualquier audio previo
        npcAudioSource.clip = clip;  // Cambia el clip al de esta fase
        npcAudioSource.Play();       // Reproduce
    }

    // ── RESTO DE HELPERS (sin cambios) ────────────────────────────────

    IEnumerator PlayAnimationSequence(List<AnimationStep> sequence)
    {
        if (npcAnimator == null) yield break;
        foreach (AnimationStep step in sequence)
        {
            if (!string.IsNullOrEmpty(step.stateName))
            {
                npcAnimator.Play(step.stateName, 0, 0f);
                yield return new WaitForSeconds(step.duration);
            }
        }
    }

    IEnumerator SpawnWithDelay(SpawnableItem item)
    {
        yield return new WaitForSeconds(item.spawnDelay);
        item.objectToMove.transform.position = item.spawnPoint.position;
        item.objectToMove.transform.rotation = item.spawnPoint.rotation;
        item.objectToMove.SetActive(true);
        Debug.Log($"[Abuela] Spawneado: {item.objectToMove.name}");
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