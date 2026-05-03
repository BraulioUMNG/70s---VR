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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

public class GrandmaInteraction : MonoBehaviour
{
    // ── REFERENCIA AL ANIMATOR ────────────────────────────────────────
    [Header("Animación NPC")]
    public Animator npcAnimator;

    // ── FASE 1: ENCARGO ───────────────────────────────────────────────
    [Header("── Fase 1: Encargo del mandado ──────────────")]
    [Tooltip("Audio que suena cuando la abuela da el encargo\n(ej: 'Mijo, vaya al mercado...')")]
    public AudioSource questAudioSource;

    [Tooltip("Animaciones de la abuela mientras da el encargo.\nAgrega un elemento por cada estado del Animator.")]
    public List<AnimationStep> questAnimationSequence = new List<AnimationStep>();

    // ── FASE 2: AGRADECIMIENTO ────────────────────────────────────────
    [Header("── Fase 2: Recibe el mercado y agradece ─────")]
    [Tooltip("Audio que suena cuando la abuela recibe el mercado\n(ej: '¡Gracias mijo, qué bueno!')")]
    public AudioSource thankAudioSource;

    [Tooltip("Animaciones de la abuela al recibir el mercado.\nAgrega un elemento por cada estado del Animator.")]
    public List<AnimationStep> thankAnimationSequence = new List<AnimationStep>();
    // ── FASE 3: NO TRAJO EL MANDADO ───────────────────────────
    [Header("── Fase 3: No trajo el mandado ─────────────")]
    [Tooltip("Audio cuando el jugador vuelve sin el mercado")]
    public AudioSource scoldAudioSource;

    [Tooltip("Animaciones de regaño")]
    public List<AnimationStep> scoldAnimationSequence = new List<AnimationStep>();

    // ── OBJETOS ───────────────────────────────────────────────────────
    [Header("── Objetos a Spawnear ────────────────────────")]
    [Tooltip("La plata que la abuela le entrega al jugador.\n· Object To Move → el prefab del billete\n· Spawn Point   → donde aparece\n· Spawn Delay   → segundos tras iniciar la animación")]
    public SpawnableItem moneyItem;

    // ── CONTROL INTERNO ───────────────────────────────────────────────
    private bool isInteracting = false;

    // Estado que guardamos para restaurar al jugador
    private FirstPersonController playerController;
    private DynamicMoveProvider    moveProvider;
    private Rigidbody              playerRigidbody;

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

        // Busca el sistema de diálogo hermano en el mismo padre
        if (transform.parent != null)
            dialogSistem = transform.parent.Find("DialogSistem")?.gameObject;
    }

    // ── TRIGGER ───────────────────────────────────────────────────────
    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player") || isInteracting) return;

        // ── FASE 1: La abuela da el encargo ───────────────────────────
        // Solo si la misión todavía no empezó
        if (MissionState.CurrentPhase == MissionState.Phase.Idle)
        {
            Debug.Log("[Abuela] El jugador llegó por primera vez → iniciando encargo.");
            InitializePlayer(other.gameObject);
            StartCoroutine(QuestSequence());
            return;
        }

        // ── FASE 2: El jugador regresa con el mercado ─────────────────
        // Solo activa el agradecimiento si el jugador ya tiene los comestibles
        if (MissionState.CurrentPhase == MissionState.Phase.GroceriesGiven)
        {
            Debug.Log("[Abuela] El jugador regresó con el mercado → iniciando agradecimiento.");
            InitializePlayer(other.gameObject);
            StartCoroutine(ThankSequence());
            return;
        }
        // ── FASE 3: VOLVIÓ SIN EL MERCADO ───────────────────────
        if (MissionState.CurrentPhase == MissionState.Phase.MoneyCollected)
        {
            Debug.Log("[Abuela] El jugador volvió sin el mercado → regaño.");
            InitializePlayer(other.gameObject);
            StartCoroutine(ScoldSequence());
            return;
        }


        // ── ESTADOS INTERMEDIOS: feedback sin bloquear al jugador ──────
        if (MissionState.CurrentPhase == MissionState.Phase.QuestGiven)
            Debug.Log("[Abuela] El jugador volvió sin recoger la plata. La abuela lo ignora.");

        if (MissionState.CurrentPhase == MissionState.Phase.MoneyCollected)
            Debug.Log("[Abuela] El jugador tiene la plata pero aún no fue a la tienda. La abuela espera.");

        if (MissionState.CurrentPhase == MissionState.Phase.MissionComplete)
            Debug.Log("[Abuela] La misión ya está completa. La abuela descansa.");
    }

    // ── SECUENCIA DE ENCARGO (Fase 1) ─────────────────────────────────
    IEnumerator QuestSequence()
    {
        isInteracting = true;

        // Desactiva diálogos flotantes mientras habla
        if (dialogSistem != null) dialogSistem.SetActive(false);

        LockPlayer();

        // Suena el audio del encargo
        if (questAudioSource != null)
            questAudioSource.Play();

        // Spawnea la plata con su delay (la abuela "la saca del bolsillo")
        if (moneyItem.objectToMove != null && moneyItem.spawnPoint != null)
            StartCoroutine(SpawnWithDelay(moneyItem));

        // Reproduce las animaciones del encargo
        yield return StartCoroutine(PlayAnimationSequence(questAnimationSequence));

        RestorePlayer();
        isInteracting = false;

        if (dialogSistem != null) dialogSistem.SetActive(true);

        // Avanza el estado de la misión
        MissionState.SetQuestGiven();
        // El trigger queda activo para que Fase 2 pueda dispararse al regresar
    }

    // ── SECUENCIA DE AGRADECIMIENTO (Fase 2) ──────────────────────────
    IEnumerator ThankSequence()
    {
        isInteracting = true;

        if (dialogSistem != null) dialogSistem.SetActive(false);

        LockPlayer();

        // Suena el audio de gracias
        if (thankAudioSource != null)
            thankAudioSource.Play();

        // Reproduce las animaciones de agradecimiento
        yield return StartCoroutine(PlayAnimationSequence(thankAnimationSequence));

        RestorePlayer();
        isInteracting = false;

        if (dialogSistem != null) dialogSistem.SetActive(true);

        // Avanza el estado final
        MissionState.SetMissionComplete();

        // Desactiva el trigger de la abuela permanentemente: misión terminada
        gameObject.SetActive(false);
        Debug.Log("[Abuela] Misión completada. Trigger desactivado.");
    }
    // ── SECUENCIA DE REGAÑO (Fase 3) ─────────────────────────
    IEnumerator ScoldSequence()
    {
        isInteracting = true;

        if (dialogSistem != null) dialogSistem.SetActive(false);

        LockPlayer();

        // Audio de regaño
        if (scoldAudioSource != null)
            scoldAudioSource.Play();

        // Animación de regaño
        yield return StartCoroutine(PlayAnimationSequence(scoldAnimationSequence));

        RestorePlayer();
        isInteracting = false;

        if (dialogSistem != null) dialogSistem.SetActive(true);

        Debug.Log("[Abuela] El jugador fue regañado por no traer el mandado.");
    }

    // ── HELPERS ───────────────────────────────────────────────────────

    /// <summary>Reproduce una lista de AnimationStep en orden.</summary>
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

    /// <summary>Mueve y activa un SpawnableItem después de su delay.</summary>
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