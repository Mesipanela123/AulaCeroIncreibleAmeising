using UnityEngine;
using UnityEngine.InputSystem;

public class VRMenuToggle : MonoBehaviour
{
    [Header("Canvas del menú")]
    [SerializeField] private GameObject menu;

    [Header("Cámara del XR Origin")]
    [SerializeField] private Transform xrCamera;

    [Header("Posición del menú")]
    [SerializeField, Min(0.1f)] private float distanceFromCamera = 1.5f;
    [SerializeField] private float heightOffset = 1.7f;

    [Header("Escala del menú")]
    [Tooltip("Escala del Canvas en el mundo. Prueba valores entre 0.001 y 0.005.")]
    [SerializeField, Min(0.0001f)] public float menuScale = 0.002f;

    [Header("Configuración")]
    [SerializeField] private bool hideMenuOnStart = true;
    [SerializeField] private bool rotateOnlyOnYAxis = true;

    [Tooltip("Evita que el mismo clic abra y cierre el menú inmediatamente.")]
    [SerializeField, Min(0.05f)] private float inputCooldown = 0.3f;

    private InputAction menuAction;
    private float nextAllowedToggleTime;

    private void Awake()
    {
        FindCamera();
        CreateInputAction();

        if (menu == null)
        {
            Debug.LogError(
                "Asigna el Canvas 'menu' en el Inspector.",
                this
            );

            return;
        }

        ApplyMenuScale();

        if (hideMenuOnStart)
        {
            menu.SetActive(false);
        }
    }

    private void OnValidate()
    {
        if (menuScale < 0.0001f)
        {
            menuScale = 0.0001f;
        }

        ApplyMenuScale();
    }

    private void FindCamera()
    {
        if (xrCamera == null && Camera.main != null)
        {
            xrCamera = Camera.main.transform;
        }

        if (xrCamera == null)
        {
            Debug.LogWarning(
                "No se encontró la cámara XR. " +
                "Asigna la Main Camera del XR Origin.",
                this
            );
        }
    }

    private void CreateInputAction()
    {
        menuAction = new InputAction(
            name: "ToggleMenu",
            type: InputActionType.Button
        );

        // Botón de menú/Start del mando izquierdo usando OpenXR.
        menuAction.AddBinding(
            "<XRController>{LeftHand}/menuButton"
        );

        // Compatibilidad específica con Oculus Touch / Quest 2.
        menuAction.AddBinding(
            "<OculusTouchController>{LeftHand}/start"
        );
    }

    private void OnEnable()
    {
        if (menuAction == null)
        {
            return;
        }

        menuAction.performed += OnMenuPressed;
        menuAction.Enable();
    }

    private void OnDisable()
    {
        if (menuAction == null)
        {
            return;
        }

        menuAction.performed -= OnMenuPressed;
        menuAction.Disable();
    }

    private void OnDestroy()
    {
        menuAction?.Dispose();
    }

    private void Update()
    {
        // Permite cambiar la escala durante Play Mode.
        ApplyMenuScale();

        // Tecla P para probar en el Editor.
        if (Keyboard.current != null &&
            Keyboard.current.pKey.wasPressedThisFrame)
        {
            TryToggleMenu();
        }
    }

    private void ApplyMenuScale()
    {
        if (menu == null)
        {
            return;
        }

        menu.transform.localScale =
            Vector3.one * menuScale;
    }

    private void OnMenuPressed(InputAction.CallbackContext context)
    {
        TryToggleMenu();
    }

    private void TryToggleMenu()
    {
        if (Time.unscaledTime < nextAllowedToggleTime)
        {
            return;
        }

        nextAllowedToggleTime =
            Time.unscaledTime + inputCooldown;

        ToggleMenu();
    }

    public void ToggleMenu()
    {
        if (menu == null)
        {
            Debug.LogWarning(
                "El Canvas 'menu' no está asignado.",
                this
            );

            return;
        }

        bool shouldOpen = !menu.activeSelf;

        if (shouldOpen)
        {
            ApplyMenuScale();
            PositionMenuInFrontOfCamera();
        }

        menu.SetActive(shouldOpen);

        Debug.Log(
            shouldOpen
                ? $"Menú XR abierto. Escala: {menuScale}"
                : "Menú XR cerrado."
        );
    }

    public void OpenMenu()
    {
        if (menu == null)
        {
            return;
        }

        ApplyMenuScale();
        PositionMenuInFrontOfCamera();
        menu.SetActive(true);

        nextAllowedToggleTime =
            Time.unscaledTime + inputCooldown;
    }

    public void CloseMenu()
    {
        if (menu == null)
        {
            return;
        }

        menu.SetActive(false);

        nextAllowedToggleTime =
            Time.unscaledTime + inputCooldown;
    }

    private void PositionMenuInFrontOfCamera()
    {
        if (menu == null || xrCamera == null)
        {
            Debug.LogWarning(
                "No se puede posicionar el menú porque falta " +
                "'menu' o 'xrCamera'.",
                this
            );

            return;
        }

        Vector3 forward = xrCamera.forward;

        if (rotateOnlyOnYAxis)
        {
            forward.y = 0f;

            if (forward.sqrMagnitude < 0.001f)
            {
                forward = Vector3.forward;
            }

            forward.Normalize();
        }

        Vector3 targetPosition =
            xrCamera.position +
            forward * distanceFromCamera;

        targetPosition.y =
            xrCamera.position.y + heightOffset;

        menu.transform.position = targetPosition;

        Vector3 directionFromCamera =
            menu.transform.position - xrCamera.position;

        if (rotateOnlyOnYAxis)
        {
            directionFromCamera.y = 0f;
        }

        if (directionFromCamera.sqrMagnitude > 0.001f)
        {
            menu.transform.rotation =
                Quaternion.LookRotation(
                    directionFromCamera.normalized,
                    Vector3.up
                );
        }
    }
}