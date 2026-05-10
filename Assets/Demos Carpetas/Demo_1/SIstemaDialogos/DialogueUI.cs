using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controla el Canvas World-Space del panel de diálogo.
///
/// Jerarquía esperada en Unity:
///
///  DialogueCanvas   [Canvas – World Space] [GraphicRaycaster]
///    └─ DialoguePanel   [Image]
///         ├─ NPCNameText        [TextMeshProUGUI]
///         ├─ DialogueText       [TextMeshProUGUI]
///         ├─ ContinueButton     [Button] [Box Collider]
///         ├─ ExitButton         [Button] [Box Collider]
///         └─ ChoicesPanel       [GameObject vacío o Image]
///              ├─ Choice0Button [Button] [Box Collider]
///              └─ Choice1Button [Button] [Box Collider]
/// </summary>
public class DialogueUI : MonoBehaviour
{
    // ── Inspector ──────────────────────────────────────────────────────────────
    [Header("Textos")]
    public TextMeshProUGUI npcNameText;
    public TextMeshProUGUI dialogueText;

    [Header("Botones de navegación")]
    public Button continueButton;
    public Button exitButton;

    [Header("Panel de opciones")]
    [Tooltip("El GameObject padre que agrupa los dos botones de opción.")]
    public GameObject choicesPanel;

    [Tooltip("Botón de la opción 0 (respuesta personalizada).")]
    public Button choice0Button;
    [Tooltip("Label de texto del botón 0.")]
    public TextMeshProUGUI choice0Text;

    [Tooltip("Botón de la opción 1 (generalmente la de salida).")]
    public Button choice1Button;
    [Tooltip("Label de texto del botón 1.")]
    public TextMeshProUGUI choice1Text;

    [Header("Posición en el mundo")]
    [Tooltip("Desplazamiento del panel respecto al NPC. Y=2.5 lo pone sobre la cabeza.")]
    public Vector3 offsetFromNPC = new Vector3(0f, 2.5f, 0f);
    [Tooltip("Velocidad de seguimiento (lerp). 8 es suave pero rápido.")]
    public float appearSpeed = 8f;

    [Header("Efecto typewriter")]
    public bool  useTypewriter   = true;
    [Tooltip("Segundos entre cada carácter. 0.03 = rápido, 0.06 = dramático.")]
    public float typewriterSpeed = 0.03f;

    [Header("Billboard (panel mira a la cámara)")]
    public bool faceCamera = true;

    // ── Privados ───────────────────────────────────────────────────────────────
    private Transform  _npcTransform;
    private bool       _visible;
    private Coroutine  _typewriterCoroutine;

    // ── Unity ──────────────────────────────────────────────────────────────────
    private void Awake()
{
    continueButton.onClick.AddListener(() => DialogueManager.Instance.NextLine());

    // Exit button solo si es diferente a los botones de elección
    if (exitButton != null && exitButton != choice0Button && exitButton != choice1Button)
        exitButton.onClick.AddListener(() => DialogueManager.Instance.EndDialogue());

    choice0Button.onClick.AddListener(() => DialogueManager.Instance.OnChoiceSelected(0));
    choice1Button.onClick.AddListener(() => DialogueManager.Instance.OnChoiceSelected(1));

    gameObject.SetActive(false);
}

    private void LateUpdate()
    {
        if (!_visible) return;

        // Seguir al NPC suavemente
        if (_npcTransform != null)
        {
            Vector3 target = _npcTransform.position + offsetFromNPC;
            transform.position = Vector3.Lerp(transform.position, target, Time.deltaTime * appearSpeed);
        }

        // Billboard: el panel siempre mira a la cámara
        if (faceCamera && Camera.main != null)
        {
            transform.LookAt(Camera.main.transform);
            transform.Rotate(0f, 180f, 0f);
        }
    }

    // ── API pública ────────────────────────────────────────────────────────────

    /// <summary>Muestra el canvas y lo posiciona frente al NPC.</summary>
    public void Show(Transform npcTransform)
    {
        _npcTransform      = npcTransform;
        transform.position = npcTransform.position + offsetFromNPC;
        _visible           = true;
        gameObject.SetActive(true);
    }

    /// <summary>Oculta el canvas por completo.</summary>
    public void Hide()
    {
        _visible = false;
        gameObject.SetActive(false);
        StopTypewriter();
    }

    /// <summary>Muestra una línea de diálogo (con typewriter si está activo).</summary>
    public void ShowLine(string npcName, string line)
    {
        npcNameText.text = npcName;
        StopTypewriter();

        if (useTypewriter)
        {
            continueButton.interactable = false;   // no se puede avanzar mientras escribe
            _typewriterCoroutine = StartCoroutine(TypewriterRoutine(line));
        }
        else
        {
            dialogueText.text           = line;
            continueButton.interactable = true;
        }
    }

    /// <summary>Muestra u oculta el botón Continuar (y Salir durante las líneas).</summary>
    public void ShowContinueButton(bool visible)
    {
        continueButton.gameObject.SetActive(visible);
        exitButton.gameObject.SetActive(visible);    // el ExitButton acompaña al Continuar
    }

    /// <summary>Rellena y muestra el panel de opciones con los datos del asset.</summary>
    public void ShowChoices(DialogueChoice[] choices)
    {
        // Opción 0
        if (choices.Length > 0)
        {
            choice0Text.text = choices[0].choiceText;
            choice0Button.gameObject.SetActive(true);
        }
        else
        {
            choice0Button.gameObject.SetActive(false);
        }

        // Opción 1
        if (choices.Length > 1)
        {
            choice1Text.text = choices[1].choiceText;
            choice1Button.gameObject.SetActive(true);
        }
        else
        {
            choice1Button.gameObject.SetActive(false);
        }

        choicesPanel.SetActive(true);
    }

    /// <summary>Oculta el panel de opciones.</summary>
    public void HideChoices()
    {
        choicesPanel.SetActive(false);
    }

    // ── Privados ───────────────────────────────────────────────────────────────

    private IEnumerator TypewriterRoutine(string line)
    {
        dialogueText.text = "";
        foreach (char c in line)
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(typewriterSpeed);
        }
        continueButton.interactable = true;
        _typewriterCoroutine        = null;
    }

    private void StopTypewriter()
    {
        if (_typewriterCoroutine != null)
        {
            StopCoroutine(_typewriterCoroutine);
            _typewriterCoroutine = null;
        }
    }
}
