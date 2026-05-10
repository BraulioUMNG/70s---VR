using UnityEngine;

public class PlayerDialogueTrigger : MonoBehaviour
{
    [Header("Audio del protagonista")]
    public AudioClip dialogueClip;

    [Header("Fuente de audio del protagonista")]
    public AudioSource playerAudioSource;

    private bool _triggered = false;

    void Start()
    {
        // Registra el AudioSource en el manager
        if (playerAudioSource != null && PlayerDialogueManager.Instance != null)
            PlayerDialogueManager.Instance.SetAudioSource(playerAudioSource);
    }

    void OnTriggerEnter(Collider other)
    {
        if (_triggered) return;
        if (!other.CompareTag("Player")) return;

        _triggered = true;

        if (PlayerDialogueManager.Instance != null)
            PlayerDialogueManager.Instance.PlayDialogue(dialogueClip);
        else
            playerAudioSource?.PlayOneShot(dialogueClip);

        Debug.Log($"[PlayerDialogue] Solicitado: {dialogueClip?.name}");
    }
}