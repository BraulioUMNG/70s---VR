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
    [Header("Animación NPC")]
    public Animator npcAnimator;

    [Header("── Caso A: Jugador trae la plata → Venta ─────")]
    public AudioSource saleAudioSource;
    public List<AnimationStep> saleAnimationSequence = new List<AnimationStep>();

    [Header("── Caso B: Jugador sin plata → Rechazo ───────")]
    public AudioSource rejectAudioSource;
    public List<AnimationStep> rejectAnimationSequence = new List<AnimationStep>();

    [Header("── Objetos a Spawnear ────────────────────────")]
    public SpawnableItem grocery1;
    public SpawnableItem grocery2;

    // 🔵 NUEVO: Proximidad
    [Header("Proximidad alternativa")]
    public float interactionDistance = 3f;
    private Transform playerTransform;
    private bool hasTriggeredByDistance = false;

    private bool isInteracting = false;

    private FirstPersonController playerController;
    private DynamicMoveProvider moveProvider;
    private Rigidbody playerRigidbody;

    private bool originalPlayerCanMove;
    private bool originalHeadBob;
    private bool moveProviderWasEnabled;
    private Vector3 originalVelocity;
    private Vector3 originalAngularVelocity;

    private GameObject dialogSistem;

    void Start()
    {
        if (npcAnimator != null)
            npcAnimator.applyRootMotion = false;

        if (transform.parent != null)
            dialogSistem = transform.parent.Find("DialogSistem")?.gameObject;

        // 🔵 Buscar jugador automáticamente
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerTransform = player.transform;
    }

    // 🔵 DETECCIÓN POR DISTANCIA (backup)
    void Update()
    {
        if (playerTransform == null || isInteracting || hasTriggeredByDistance) return;

        float distance = Vector3.Distance(transform.position, playerTransform.position);

        if (distance <= interactionDistance)
        {
            hasTriggeredByDistance = true;
            Debug.Log("[Tendero] Activado por proximidad");

            HandleInteraction(playerTransform.gameObject);
        }
    }

    // 🔵 TRIGGER ORIGINAL (simplificado)
    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        Debug.Log("[Tendero] Activado por trigger");

        HandleInteraction(other.gameObject);
    }

    // 🔵 LÓGICA CENTRAL (extraída)
    void HandleInteraction(GameObject player)
    {
        if (isInteracting) return;

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

        if (!MissionState.HasMoney)
        {
            Debug.Log("[Tendero] SIN plata → rechazo");
            StartCoroutine(RejectSequence());
            return;
        }

        Debug.Log("[Tendero] CON plata → venta");
        StartCoroutine(SaleSequence());
    }

    IEnumerator RejectSequence()
    {
        isInteracting = true;

        if (dialogSistem != null) dialogSistem.SetActive(false);

        LockPlayer();

        if (rejectAudioSource != null)
            rejectAudioSource.Play();

        yield return StartCoroutine(PlayAnimationSequence(rejectAnimationSequence));

        RestorePlayer();
        isInteracting = false;

        if (dialogSistem != null) dialogSistem.SetActive(true);

        hasTriggeredByDistance = false; // permite reintento

        Debug.Log("[Tendero] Rechazo completado.");
    }

    IEnumerator SaleSequence()
    {
        isInteracting = true;

        if (dialogSistem != null) dialogSistem.SetActive(false);

        LockPlayer();

        if (saleAudioSource != null)
            saleAudioSource.Play();

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

    // 🔵 AHORA USA TRIGGERS
    IEnumerator PlayAnimationSequence(List<AnimationStep> sequence)
    {
        if (npcAnimator == null) yield break;

        foreach (AnimationStep step in sequence)
        {
            if (!string.IsNullOrEmpty(step.stateName))
            {
                Debug.Log("Trigger lanzado: " + step.stateName);
                npcAnimator.SetTrigger(step.stateName);
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
        moveProvider = player.GetComponent<DynamicMoveProvider>();
        playerRigidbody = player.GetComponent<Rigidbody>();
        playerTransform = player.transform;
        SaveState();
    }

    void SaveState()
    {
        if (playerController != null)
        {
            originalPlayerCanMove = playerController.playerCanMove;
            originalHeadBob = playerController.enableHeadBob;
        }
        if (moveProvider != null) moveProviderWasEnabled = moveProvider.enabled;
        if (playerRigidbody != null)
        {
            originalVelocity = playerRigidbody.linearVelocity;
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
        if (moveProvider != null) moveProvider.enabled = false;
        if (playerRigidbody != null)
        {
            playerRigidbody.linearVelocity = Vector3.zero;
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
        if (moveProvider != null) moveProvider.enabled = moveProviderWasEnabled;
        if (playerRigidbody != null)
        {
            playerRigidbody.linearVelocity = originalVelocity;
            playerRigidbody.angularVelocity = originalAngularVelocity;
        }
    }
}