using UnityEngine;

public class PlayerDialogueTrigger : MonoBehaviour
{
    [Header("Audio del protagonista")]
    public AudioClip dialogueClip;

    [Header("Fuente de audio del protagonista")]
    public AudioSource playerAudioSource;

    private bool _triggered = false;

    void OnTriggerEnter(Collider other)
    {
        if (_triggered) return;
        if (!other.CompareTag("Player")) return;

        _triggered = true;

        if (playerAudioSource != null && dialogueClip != null)
        {
            playerAudioSource.PlayOneShot(dialogueClip);
            Debug.Log($"[PlayerDialogue] Reproduciendo: {dialogueClip.name}");
        }
        else
        {
            Debug.LogWarning("[PlayerDialogue] Falta AudioSource o AudioClip");
        }
    }
}