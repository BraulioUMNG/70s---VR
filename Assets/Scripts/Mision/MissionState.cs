// ════════════════════════════════════════════════════════════════════
//  MissionState.cs  —  Estado global de la misión del mandado
//  Singleton estático: no necesita estar en la escena como componente.
//  Cualquier script puede leer o escribir el estado desde cualquier lugar.
// ════════════════════════════════════════════════════════════════════
using UnityEngine;

public static class MissionState
{
    // ── FASES DE LA MISIÓN ────────────────────────────────────────────
    public enum Phase
    {
        Idle,
        QuestGiven,
        MoneyCollected,
        GroceriesGiven,
        MissionComplete
    }

    public static Phase CurrentPhase { get; private set; } = Phase.Idle;

    // ── EVENTO DE MISIÓN COMPLETADA ───────────────────────────────────
    // Cualquier script puede suscribirse: MissionState.OnMissionComplete += MiMétodo;
    public static event System.Action OnMissionComplete; // ← NUEVO

    // ── MÉTODOS DE AVANCE ─────────────────────────────────────────────
    public static void SetQuestGiven()
    {
        if (CurrentPhase == Phase.Idle)
        {
            CurrentPhase = Phase.QuestGiven;
            Debug.Log("[MissionState] ✅ Fase → QuestGiven: la abuela encargó el mandado y spawnó la plata.");
        }
    }

    public static void SetMoneyCollected()
    {
        if (CurrentPhase == Phase.Idle || CurrentPhase == Phase.QuestGiven)
        {
            CurrentPhase = Phase.MoneyCollected;
            Debug.Log("[MissionState] ✅ Fase → MoneyCollected: el jugador tiene la plata en el inventario.");
        }
    }

    public static void SetGroceriesGiven()
    {
        if (CurrentPhase == Phase.MoneyCollected)
        {
            CurrentPhase = Phase.GroceriesGiven;
            Debug.Log("[MissionState] ✅ Fase → GroceriesGiven: el tendero entregó el mercado al jugador.");
        }
    }

    public static void SetMissionComplete()
    {
        if (CurrentPhase == Phase.GroceriesGiven)
        {
            CurrentPhase = Phase.MissionComplete;
            Debug.Log("[MissionState] 🎉 Fase → MissionComplete: ¡misión completada! La abuela recibió el mandado.");
            OnMissionComplete?.Invoke(); // ← NUEVO: dispara el evento a todos los suscriptores
        }
    }

    // ── HELPERS DE CONSULTA ───────────────────────────────────────────
    public static bool HasMoney => CurrentPhase >= Phase.MoneyCollected;
    public static bool HasGroceries => CurrentPhase >= Phase.GroceriesGiven;
    public static bool IsComplete => CurrentPhase == Phase.MissionComplete;
}