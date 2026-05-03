// ════════════════════════════════════════════════════════════════════
//  ShopkeeperInteraction.cs  —  NPC: El Señor de la Tienda
//
//  CASO A — Jugador llega SIN plata (fase QuestGiven):
//    El tendero hace una animación de rechazo y dice que vuelva con la plata.
//    No spawnea nada. El trigger se mantiene activo para el reintento.
//
//  CASO B — Jugador llega CON plata (fase MoneyCollected):
//    El tendero hace su animación de atención, suena el audio de venta,
//    spawnea los artículos del mercado y avanza la misión a GroceriesGiven.
//    El trigger se desactiva: la transacción ya ocurrió.
//
//  NOTA: Requiere MissionState.cs en el proyecto.
// ════════════════════════════════════════════════════════════════════

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

public class ShopkeeperInteraction : MonoBehaviour
{
    // ── REFERENCIA AL ANIMATOR ────────────────────────────────────────
    [Header("Animación NPC")]
    public Animator npcAnimator;

    // ── CASO A: CON PLATA → VENTA ─────────────────────────────────────
    [Header("── Caso A: Jugador trae la plata → Venta ─────")]
    [Tooltip("Audio que suena cuando el tendero atiende la compra\n(ej: '¡Buenas! ¿Qué le voy a dar?')")]
    public AudioSource saleAudioSource;

    [Tooltip("Animaciones del tendero durante la venta.\nAgrega un elemento por cada estado del Animator.")]
    public List<AnimationStep> saleAnimationSequence = new List<AnimationStep>();

    // ── CASO B: SIN PLATA → RECHAZO ──────────────────────────────────
    [Header("── Caso B: Jugador sin plata → Rechazo ───────")]
    [Tooltip("Audio que suena cuando el tendero rechaza al jugador\n(ej: 'Mijo, primero tráigame la plata...')")]
    public AudioSource rejectAudioSource;

    [Tooltip("Animaciones del tendero al rechazar al jugador.\nAgrega un elemento por cada estado del Animator.")]
    public List<AnimationStep> rejectAnimationSequence = new List<AnimationStep>();

    // ── OBJETOS A VENDER ──────────────────────────────────────────────
    [Header("── Objetos a Spawnear ────────────────────────")]
    [Tooltip("Primer artículo del mandado (ej: panela).\n· Object To Move → el prefab\n· Spawn Point   → donde aparece en el mostrador\n· Spawn Delay   → segundos tras iniciar la animación")]
    public SpawnableItem grocery1;

    [Tooltip("Segundo artículo del mandado (ej: arroz).\nMismo formato que grocery1.")]
    public SpawnableItem grocery2;

    // Para añadir más productos, duplica la línea de grocery2
    // y crea grocery3, grocery4, etc. con el mismo patrón.

    // ── CONTROL INTERNO ───────────────────────────────────────────────
    private bool isInteracting = false;

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

        if (transform.parent != null)
            dialogSistem = transform.parent.Find("DialogSistem")?.gameObject;
    }

    // ── TRIGGER ───────────────────────────────────────────────────────
    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player") || isInteracting) return;

        // Solo reacciona si la misión está activa y no terminada
        if (MissionState.CurrentPhase == MissionState.Phase.Idle)
        {
            // El jugador llegó antes de hablar con la abuela → ignorar
            Debug.Log("[Tendero] El jugador entró pero la misión no ha empezado. El tendero ignora.");
            return;
        }

        if (MissionState.CurrentPhase == MissionState.Phase.GroceriesGiven ||
            MissionState.CurrentPhase == MissionState.Phase.MissionComplete)
        {
            // La venta ya ocurrió → no interactúa de nuevo
            Debug.Log("[Tendero] La venta ya se realizó. El tendero ya no tiene nada que dar.");
            return;
        }

        InitializePlayer(other.gameObject);

        // ── CASO A: jugador NO tiene la plata ─────────────────────────
        if (!MissionState.HasMoney)
        {
            Debug.Log("[Tendero] El jugador llegó SIN plata → animación de rechazo.");
            StartCoroutine(RejectSequence());
            return;
        }

        // ── CASO B: jugador SÍ tiene la plata ────────────────────────
        Debug.Log("[Tendero] El jugador llegó CON plata → animación de venta y spawn del mercado.");
        StartCoroutine(SaleSequence());
    }

    // ── SECUENCIA DE RECHAZO (sin plata) ──────────────────────────────
    IEnumerator RejectSequence()
    {
        isInteracting = true;

        if (dialogSistem != null) dialogSistem.SetActive(false);

        LockPlayer();

        // Suena el audio de rechazo
        if (rejectAudioSource != null)
            rejectAudioSource.Play();

        // Reproduce las animaciones de rechazo (ej: negar con la cabeza)
        yield return StartCoroutine(PlayAnimationSequence(rejectAnimationSequence));

        RestorePlayer();
        isInteracting = false;

        if (dialogSistem != null) dialogSistem.SetActive(true);

        // El trigger QUEDA ACTIVO para que el jugador pueda volver con la plata
        Debug.Log("[Tendero] Rechazo completado. Esperando que el jugador traiga la plata.");
    }

    // ── SECUENCIA DE VENTA (con plata) ────────────────────────────────
    IEnumerator SaleSequence()
    {
        isInteracting = true;

        if (dialogSistem != null) dialogSistem.SetActive(false);

        LockPlayer();

        // Suena el audio de venta
        if (saleAudioSource != null)
            saleAudioSource.Play();

        // Spawnea los artículos del mercado con sus delays individuales
        // (el tendero "los va poniendo en la bolsa" con el tiempo)
        if (grocery1.objectToMove != null && grocery1.spawnPoint != null)
            StartCoroutine(SpawnWithDelay(grocery1));

        if (grocery2.objectToMove != null && grocery2.spawnPoint != null)
            StartCoroutine(SpawnWithDelay(grocery2));

        // Reproduce las animaciones de venta (ej: buscar producto, entregar)
        yield return StartCoroutine(PlayAnimationSequence(saleAnimationSequence));

        RestorePlayer();
        isInteracting = false;

        if (dialogSistem != null) dialogSistem.SetActive(true);

        // Avanza la misión: el jugador ahora tiene el mercado
        MissionState.SetGroceriesGiven();

        // Desactiva el trigger: la transacción ya terminó
        gameObject.SetActive(false);
        Debug.Log("[Tendero] Venta completada. Trigger desactivado.");
    }

    // ── HELPERS ───────────────────────────────────────────────────────

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

        Rigidbody rb = item.objectToMove.GetComponent<Rigidbody>();
        RigidbodyInterpolation originalInterpolation = RigidbodyInterpolation.None;

        if (rb != null)
        {
            originalInterpolation = rb.interpolation;

            // Desactiva interpolación ANTES de mover: evita que el interpolador
            // recuerde la posición anterior (bajo el mapa) y arrastre el objeto hacia allá
            rb.interpolation   = RigidbodyInterpolation.None;
            rb.isKinematic     = true;
            rb.linearVelocity  = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.Sleep();
        }

        item.objectToMove.transform.position = item.spawnPoint.position;
        item.objectToMove.transform.rotation = item.spawnPoint.rotation;
        item.objectToMove.SetActive(true);

        // Limpia de nuevo post-activación
        if (rb != null)
        {
            rb.linearVelocity  = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Espera 2 frames para que Unity registre la nueva posición sin interpolación
        yield return null;
        yield return null;

        // Devuelve todo al estado original
        if (rb != null)
        {
            rb.isKinematic  = false;
            rb.interpolation = originalInterpolation; // Restaura Interpolate si lo tenía
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