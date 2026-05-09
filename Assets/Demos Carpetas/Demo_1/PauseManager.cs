using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR;
using System.Collections;
using System.Collections.Generic;

public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance { get; private set; }

    [Header("Canvas de pausa - Desktop")]
    public GameObject desktopPauseCanvas;

    [Header("Canvas de pausa - VR")]
    public GameObject vrPauseCanvas;

    [Header("Escena del menú principal")]
    public string mainMenuSceneName = "MainMenu";

    [Header("Referencias del jugador")]
    public FirstPersonController firstPersonController;

    [Header("VR - Main Camera del XR Origin")]
    public Transform xrCameraTransform;

    [Header("VR - Distancia del canvas frente al jugador")]
    public float vrCanvasDistance = 1.5f;

    private bool _isPaused = false;
    private bool _isVRMode = false;
    private InputDevice _leftController;
    private bool _buttonPressedLastFrame = false;

    // ─── Awake / Start ───────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        if (desktopPauseCanvas != null)
            desktopPauseCanvas.SetActive(false);

        if (vrPauseCanvas != null)
            vrPauseCanvas.SetActive(false);

        StartCoroutine(InitVR());
    }

    // ─── Inicialización VR ───────────────────────────────────────────────────

    IEnumerator InitVR()
    {
        // Espera hasta 10 segundos a que XR esté activo
        float timeout = 10f;
        while (!XRSettings.isDeviceActive && timeout > 0f)
        {
            timeout -= Time.deltaTime;
            yield return null;
        }

        _isVRMode = XRSettings.isDeviceActive;
        Debug.Log($"[PauseManager] VR listo. _isVRMode: {_isVRMode}");

        if (_isVRMode)
            TryGetLeftController();
    }

    void TryGetLeftController()
    {
        var devices = new List<InputDevice>();

        // Intento 1: por características
        InputDevices.GetDevicesWithCharacteristics(
            InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller,
            devices);

        // Intento 2: por nodo como fallback
        if (devices.Count == 0)
            InputDevices.GetDevicesAtXRNode(XRNode.LeftHand, devices);

        if (devices.Count > 0)
        {
            _leftController = devices[0];
            Debug.Log($"[PauseManager] Controlador encontrado: {_leftController.name}");
        }
        else
        {
            Debug.LogWarning("[PauseManager] No se encontró controlador izquierdo");
        }
    }

    // ─── Update ──────────────────────────────────────────────────────────────

    void Update()
    {
        // Escape siempre funciona (útil en editor)
        if (Input.GetKeyDown(KeyCode.Escape))
            TogglePause();

        if (_isVRMode)
            CheckVRButton();
    }

    void CheckVRButton()
    {
        // Si el controlador se desconectó, reintenta
        if (!_leftController.isValid)
        {
            TryGetLeftController();
            return;
        }

        // Botón Y del controlador izquierdo
        // (el botón ☰ Menu está reservado por Oculus, Unity no lo recibe)
        _leftController.TryGetFeatureValue(CommonUsages.secondaryButton, out bool pressed);

        if (pressed && !_buttonPressedLastFrame)
            TogglePause();

        _buttonPressedLastFrame = pressed;
    }

    // ─── Lógica de pausa ─────────────────────────────────────────────────────

    public void TogglePause()
    {
        if (_isPaused) Resume();
        else Pause();
    }

    public void Pause()
    {
        _isPaused = true;
        Time.timeScale = 0f;

        if (_isVRMode)
        {
            PositionVRCanvas();
            vrPauseCanvas?.SetActive(true);
        }
        else
        {
            desktopPauseCanvas?.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (firstPersonController != null)
        {
            firstPersonController.cameraCanMove = false;
            firstPersonController.playerCanMove = false;
        }
    }

    public void Resume()
    {
        _isPaused = false;
        Time.timeScale = 1f;

        vrPauseCanvas?.SetActive(false);
        desktopPauseCanvas?.SetActive(false);

        if (!_isVRMode)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (firstPersonController != null)
        {
            firstPersonController.cameraCanMove = true;
            firstPersonController.playerCanMove = true;
        }
    }

    // ─── Posicionamiento canvas VR ────────────────────────────────────────────

    void PositionVRCanvas()
    {
        if (vrPauseCanvas == null) return;

        // Si no tienes xrCameraTransform asignado, busca la cámara automáticamente
        if (xrCameraTransform == null)
        {
            xrCameraTransform = Camera.main?.transform;
            Debug.LogWarning("[PauseManager] xrCameraTransform no asignado, usando Camera.main");
        }

        if (xrCameraTransform == null) return;

        // Toma la dirección exacta donde el jugador mira EN ESTE MOMENTO
        Vector3 forward = xrCameraTransform.forward;
        forward.y = 0f;
        forward.Normalize();

        // Posición frente al jugador a la altura de sus ojos
        vrPauseCanvas.transform.position =
            xrCameraTransform.position + forward * vrCanvasDistance;

        // El canvas mira de frente al jugador
        vrPauseCanvas.transform.rotation =
            Quaternion.LookRotation(forward);
    }

    // ─── Botones UI ──────────────────────────────────────────────────────────

    public void OnResumeButton() => Resume();

    public void OnMainMenuButton()
    {
        Time.timeScale = 1f;
        SceneFader.Instance.LoadScene(mainMenuSceneName);
    }


}