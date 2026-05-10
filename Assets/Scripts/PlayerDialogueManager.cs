using UnityEngine;
using System.Collections;

public class PlayerDialogueManager : MonoBehaviour
{
    public static PlayerDialogueManager Instance { get; private set; }

    private AudioSource _audioSource;
    private bool _isPlaying = false;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void SetAudioSource(AudioSource source)
    {
        _audioSource = source;
    }

    public void PlayDialogue(AudioClip clip)
    {
        if (clip == null) return;
        StartCoroutine(WaitAndPlay(clip));
    }

    IEnumerator WaitAndPlay(AudioClip clip)
    {
        // Espera a que termine el audio actual
        while (_isPlaying)
            yield return null;

        _isPlaying = true;
        _audioSource.PlayOneShot(clip);

        // Espera a que termine este clip
        yield return new WaitForSeconds(clip.length);

        _isPlaying = false;
    }
}