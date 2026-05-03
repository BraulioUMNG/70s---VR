// ════════════════════════════════════════════════════════════════════
//  CollectibleItem.cs  —  Objeto recogible (plata o mercado)
// ════════════════════════════════════════════════════════════════════

using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class CollectibleItem : MonoBehaviour
{
    [Header("Inventario")]
    [SerializeField] private Sprite inventorySprite;

    [Header("Misión")]
    [Tooltip("Activa esto si este objeto ES la plata que dio la abuela.")]
    [SerializeField] private bool isMoney = false;

    private XRGrabInteractable grabInteractable;
    private bool collected = false;

    // Mientras sea false, OnReleased ignora cualquier evento.
    // Se activa un frame después del spawn para evitar falsos disparos
    // que ocurren cuando Unity reactiva el objeto y el interactor
    // interno lanza selectExited automáticamente.
    private bool listenerReady = false;

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
    }

    private void OnEnable()
    {
        listenerReady = false; // Resetea al activarse (spawn o re-enable)

        if (grabInteractable != null)
            grabInteractable.selectExited.AddListener(OnReleased);

        // Espera un frame antes de aceptar eventos de soltar
        StartCoroutine(EnableListenerNextFrame());
    }

    private void OnDisable()
    {
        if (grabInteractable != null)
            grabInteractable.selectExited.RemoveListener(OnReleased);
    }

    private IEnumerator EnableListenerNextFrame()
    {
        yield return null; // Espera exactamente 1 frame
        listenerReady = true;
        Debug.Log($"[CollectibleItem] {gameObject.name} listo para ser recogido.");
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        // Ignora el evento si todavía estamos en el frame del spawn
        if (!listenerReady) return;

        // Solo procesa la primera vez que se suelta
        if (collected) return;
        collected = true;

        // Registra en el inventario visual
        InventoryManager.Instance.CollectItem(inventorySprite);

        // ── ACTUALIZAR MISIÓN ─────────────────────────────────────────
        if (isMoney)
        {
            MissionState.SetMoneyCollected();
            Debug.Log("[CollectibleItem] La plata fue recogida. El jugador puede ir a la tienda.");
        }
        // ─────────────────────────────────────────────────────────────

        gameObject.SetActive(false);
    }
}