using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.InputSystem;

public class DesktopXRInteractor : MonoBehaviour
{
    [Header("XR")]
    public XRRayInteractor rayInteractor;
    public MonoBehaviour playerController;

    [Header("Distancia")]
    public float scrollSpeed = 2f;
    public float minDistance = 1f;
    public float maxDistance = 10f;

    [Header("Rotación")]
    public float rotationSpeed = 200f;

    [Header("Modo restringido")]
    public KeyCode teclaModoRestringido = KeyCode.LeftShift;

    private IXRSelectInteractable currentObject;
    private IXRSelectInteractor interactor;
    private InteractionConstraints constraints;

    private float currentDistance;
    private bool rotating = false;
    private bool modoRestringido = false;

    private bool restringirRotX, restringirRotY, restringirRotZ, restringirZoom, permitirRotacion, permitirZoom;
    private Vector3 initialObjectLocalPosition;

    [Header("Input VR")]
    public InputActionReference botonVR;

    void Start()
    {
        interactor = rayInteractor as IXRSelectInteractor;

        if (botonVR != null)
            botonVR.action.Enable();
    }

    void Update()
    {
        if (rayInteractor == null) return;

        modoRestringido = Input.GetKey(teclaModoRestringido);

        HandleGrab();
        HandleScroll();
        HandleRotation();
        HandleMovement();

        if (Input.GetKeyDown(KeyCode.E) && currentObject != null)
            ActivarModoRestringido();

        if (botonVR != null && currentObject != null && botonVR.action.WasPressedThisFrame())
            ActivarModoRestringido();
    }

    void ActivarModoRestringido()
    {
        if (constraints != null && constraints.usarRotacionE)
            currentObject.transform.rotation = Quaternion.Euler(constraints.rotacionInicialE);

        Transform grabPoint = currentObject.transform.Find("GrabPoint");
        if (grabPoint != null)
            rayInteractor.attachTransform.rotation = grabPoint.rotation;
        else
            rayInteractor.attachTransform.rotation = currentObject.transform.rotation;

        modoRestringido = true;
    }

    void HandleGrab()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (rayInteractor.TryGetCurrent3DRaycastHit(out RaycastHit hit))
            {
                var interactable = hit.collider.GetComponentInParent<IXRSelectInteractable>();
                if (interactable != null)
                {
                    currentObject = interactable;
                    rayInteractor.interactionManager.SelectEnter(interactor, interactable);
                    currentDistance = hit.distance;

                    constraints = hit.collider.GetComponentInParent<InteractionConstraints>();

                    if (currentObject != null)
                        initialObjectLocalPosition = currentObject.transform.localPosition;

                    if (constraints != null)
                    {
                        permitirRotacion = constraints.permitirRotacion;
                        permitirZoom = constraints.permitirZoom;
                        UpdateModoRestringido();
                    }
                    else
                    {
                        permitirRotacion = true;
                        permitirZoom = true;
                        restringirRotX = true;
                        restringirRotY = true;
                        restringirRotZ = true;
                        restringirZoom = false;
                    }

                    if (playerController != null)
                    {
                        Collider playerCollider = playerController.GetComponent<Collider>();
                        Collider objectCollider = currentObject.transform.GetComponent<Collider>();
                        if (playerCollider != null && objectCollider != null)
                            Physics.IgnoreCollision(objectCollider, playerCollider, true);
                    }

                    float safeDistance = Mathf.Max(currentDistance, 1f);
                    rayInteractor.attachTransform.localPosition = new Vector3(0, 0, safeDistance);
                }
            }
        }

        if (Input.GetMouseButton(0) && currentObject != null)
            HandleMovement();

        if (Input.GetMouseButtonUp(0) && currentObject != null)
        {
            if (playerController != null)
            {
                Collider playerCollider = playerController.GetComponent<Collider>();
                Collider objectCollider = currentObject.transform.GetComponent<Collider>();
                if (playerCollider != null && objectCollider != null)
                    Physics.IgnoreCollision(objectCollider, playerCollider, false);
            }

            rayInteractor.interactionManager.SelectExit(interactor, currentObject);
            currentObject = null;
            constraints = null;

            // ── FIX: si sueltas el objeto mientras rotabas, desbloquea
            // la cámara inmediatamente sin esperar al MouseButtonUp(1)
            if (rotating)
            {
                rotating = false;
                if (playerController != null)
                    playerController.enabled = true;

                Debug.Log("[DesktopXRInteractor] Objeto soltado durante rotación → cámara desbloqueada.");
            }
        }
    }

    void HandleScroll()
    {
        if (currentObject == null || rotating) return;

        bool puedeZoom = modoRestringido ? restringirZoom : permitirZoom;
        if (!puedeZoom) return;

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            currentDistance += scroll * scrollSpeed;
            currentDistance = Mathf.Clamp(currentDistance, minDistance, maxDistance);

            float safeDistance = Mathf.Max(currentDistance, 1.5f);
            rayInteractor.attachTransform.localPosition = new Vector3(0, 0, safeDistance);
        }
    }

    void HandleRotation()
    {
        // ── FIX: el desbloqueo de cámara va ANTES del early return para
        // que funcione aunque currentObject ya sea null al soltar el objeto
        if (Input.GetMouseButtonUp(1))
        {
            rotating = false;
            if (playerController != null)
                playerController.enabled = true;
        }

        if (currentObject == null) return;

        if (Input.GetMouseButtonDown(1))
        {
            rotating = true;
            if (playerController != null) playerController.enabled = false;
        }

        if (Input.GetMouseButton(1))
        {
            bool puedeRotar = modoRestringido ? restringirRotX || restringirRotY || restringirRotZ : permitirRotacion;
            if (!puedeRotar) return;

            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            bool usarX = modoRestringido ? restringirRotX : true;
            bool usarY = modoRestringido ? restringirRotY : true;
            bool usarZ = modoRestringido ? restringirRotZ : false;

            if (usarY) rayInteractor.attachTransform.Rotate(Vector3.up, mouseX * rotationSpeed * Time.deltaTime, Space.World);
            if (usarX) rayInteractor.attachTransform.Rotate(Vector3.right, -mouseY * rotationSpeed * Time.deltaTime, Space.Self);
            if (usarZ) rayInteractor.attachTransform.Rotate(Vector3.forward, mouseX * rotationSpeed * Time.deltaTime, Space.Self);
        }
    }

    void HandleMovement()
    {
        if (currentObject == null || !Input.GetMouseButton(0)) return;
        if (constraints == null || !constraints.permitirMovimiento) return;

        float moveX = Input.GetAxis("Mouse X") * 0.01f;
        float moveY = Input.GetAxis("Mouse Y") * 0.01f;

        Vector3 newPos = currentObject.transform.localPosition + new Vector3(moveX, moveY, 0f);

        newPos.x = Mathf.Clamp(newPos.x, constraints.minPosition.x, constraints.maxPosition.x);
        newPos.y = Mathf.Clamp(newPos.y, constraints.minPosition.y, constraints.maxPosition.y);
        newPos.z = Mathf.Clamp(newPos.z, constraints.minPosition.z, constraints.maxPosition.z);

        currentObject.transform.localPosition = newPos;
    }

    void UpdateModoRestringido()
    {
        if (constraints == null) return;

        restringirRotX = constraints.rotarX;
        restringirRotY = constraints.rotarY;
        restringirRotZ = constraints.rotarZ;
        restringirZoom = !constraints.permitirZoom;
    }
}