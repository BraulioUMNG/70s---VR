using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRGrabInteractable))]
[RequireComponent(typeof(AudioSource))]
public class AudioProtagonista : MonoBehaviour
{
    [Header("Audio")]
    public AudioClip clipPrimeraVez;

    private bool yaReprodujo = false;
    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.spatialBlend = 0f; // 2D — suena igual sin importar distancia
        audioSource.volume = 1f;

        var interactable = GetComponent<XRGrabInteractable>();
        interactable.selectEntered.AddListener(OnAgarrar);
    }

    void OnAgarrar(SelectEnterEventArgs args)
    {
        if (yaReprodujo || clipPrimeraVez == null) return;

        yaReprodujo = true;
        audioSource.PlayOneShot(clipPrimeraVez);
    }

    void OnDestroy()
    {
        var interactable = GetComponent<XRGrabInteractable>();
        if (interactable != null)
            interactable.selectEntered.RemoveListener(OnAgarrar);
    }
}