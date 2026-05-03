// ════════════════════════════════════════════════════════════════════
//  MissionState.cs  —  Estado global de la misión del mandado
//  Singleton estático: no necesita estar en la escena como componente.
//  Cualquier script puede leer o escribir el estado desde cualquier lugar.
// ════════════════════════════════════════════════════════════════════

using UnityEngine;

public static class MissionState
{
    // ── FASES DE LA MISIÓN ────────────────────────────────────────────
    // Idle          → El juego acaba de empezar, la abuela no ha hablado
    // QuestGiven    → La abuela habló y spawnó la plata
    // MoneyCollected → El jugador recogió la plata (CollectibleItem lo activa)
    // GroceriesGiven → El tendero entregó el mercado
    // MissionComplete→ El jugador entregó el mercado a la abuela
    // ─────────────────────────────────────────────────────────────────
    public enum Phase
    {
        Idle,
        QuestGiven,
        MoneyCollected,
        GroceriesGiven,
        MissionComplete
    }

    // Estado actual — empieza en Idle
    public static Phase CurrentPhase { get; private set; } = Phase.Idle;

    // ── MÉTODOS DE AVANCE ─────────────────────────────────────────────

    /// <summary>
    /// Llama esto cuando la abuela termina su animación de encargo.
    /// </summary>
    public static void SetQuestGiven()
    {
        if (CurrentPhase == Phase.Idle)
        {
            CurrentPhase = Phase.QuestGiven;
            Debug.Log("[MissionState] ✅ Fase → QuestGiven: la abuela encargó el mandado y spawnó la plata.");
        }
    }

    /// <summary>
    /// Llama esto desde CollectibleItem cuando el jugador recoge la plata.
    /// </summary>
    public static void SetMoneyCollected()
    {
        // Acepta Idle también: el jugador puede agarrar la plata antes
        // de que la animación de la abuela termine y llame SetQuestGiven().
        if (CurrentPhase == Phase.Idle || CurrentPhase == Phase.QuestGiven)
        {
            CurrentPhase = Phase.MoneyCollected;
            Debug.Log("[MissionState] ✅ Fase → MoneyCollected: el jugador tiene la plata en el inventario.");
        }
    }

    /// <summary>
    /// Llama esto cuando el tendero termina de entregar el mercado.
    /// </summary>
    public static void SetGroceriesGiven()
    {
        if (CurrentPhase == Phase.MoneyCollected)
        {
            CurrentPhase = Phase.GroceriesGiven;
            Debug.Log("[MissionState] ✅ Fase → GroceriesGiven: el tendero entregó el mercado al jugador.");
        }
    }

    /// <summary>
    /// Llama esto cuando la abuela recibe el mercado y agradece.
    /// </summary>
    public static void SetMissionComplete()
    {
        if (CurrentPhase == Phase.GroceriesGiven)
        {
            CurrentPhase = Phase.MissionComplete;
            Debug.Log("[MissionState] 🎉 Fase → MissionComplete: ¡misión completada! La abuela recibió el mandado.");
        }
    }

    // ── HELPERS DE CONSULTA ───────────────────────────────────────────

    public static bool HasMoney      => CurrentPhase >= Phase.MoneyCollected;
    public static bool HasGroceries  => CurrentPhase >= Phase.GroceriesGiven;
    public static bool IsComplete    => CurrentPhase == Phase.MissionComplete;
}
