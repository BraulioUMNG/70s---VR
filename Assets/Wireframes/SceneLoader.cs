using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneLoader : MonoBehaviour
{
    public FadeManager fadeManager;

    public Sprite imagenPatio;
    public Sprite imagenPueblo;

    public void IrAlPueblo()
    {
        StartCoroutine(IrAlPuebloConFade());
    }

    IEnumerator IrAlPuebloConFade()
    {
        // Muestra imagen de carga primero
        yield return StartCoroutine(fadeManager.FadeOut(imagenPueblo));
        yield return new WaitForSeconds(2f);

        // Luego fade negro y carga
        SceneFader.Instance.LoadScene("Principal VR - PC");
    }

    public void IrAPatioDeJuegos()
    {
        StartCoroutine(IrAPatioConFade());
    }

    IEnumerator IrAPatioConFade()
    {
        yield return StartCoroutine(fadeManager.FadeOut(imagenPatio));
        yield return new WaitForSeconds(2f);

        SceneFader.Instance.LoadScene("Patio de juegos");
    }
}