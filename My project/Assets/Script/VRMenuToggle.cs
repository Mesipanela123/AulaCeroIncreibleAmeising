using UnityEngine;

public class VRMenuToggle : MonoBehaviour
{
    [Header("Canvas del menu")]
    [SerializeField] private GameObject menu;

    [Header("Camara del Meta XR Rig")]
    [SerializeField] private Transform xrCamera;

    [Header("Posicion del menu")]
    [SerializeField, Min(0.1f)]
    private float distanceFromCamera = 1.5f;

    [SerializeField]
    private float heightOffset = 0f;

    [Header("Escala del menu")]
    [SerializeField, Min(0.0001f)]
    private float menuScale = 0.002f;

    [Header("Configuracion")]
    [SerializeField]
    private bool hideMenuOnStart = true;

    [SerializeField]
    private bool rotateOnlyOnYAxis = true;

    private void Awake()
    {
        // Si no asignaste la camara manualmente,
        // intenta encontrar la Main Camera.
        if (xrCamera == null && Camera.main != null)
        {
            xrCamera = Camera.main.transform;
        }

        if (menu == null)
        {
            Debug.LogError(
                "VRMenuToggle: Asigna el Canvas del menu en el campo 'Menu'.",
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

    /// <summary>
    /// Abre o cierra el menu.
    /// Esta funcion sera llamada por el boton
    /// "Opciones" mediante el Interaction SDK.
    /// </summary>
    public void ToggleMenu()
    {
        if (menu == null)
        {
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
                ? "Menu abierto."
                : "Menu cerrado."
        );
    }

    /// <summary>
    /// Abre el menu directamente.
    /// Puedes utilizarlo desde cualquier UnityEvent.
    /// </summary>
    public void OpenMenu()
    {
        if (menu == null)
        {
            return;
        }

        ApplyMenuScale();
        PositionMenuInFrontOfCamera();

        menu.SetActive(true);
    }

    /// <summary>
    /// Cierra el menu directamente.
    /// </summary>
    public void CloseMenu()
    {
        if (menu != null)
        {
            menu.SetActive(false);
        }
    }

    private void ApplyMenuScale()
    {
        if (menu != null)
        {
            menu.transform.localScale =
                Vector3.one * menuScale;
        }
    }

    private void PositionMenuInFrontOfCamera()
    {
        if (menu == null || xrCamera == null)
        {
            Debug.LogWarning(
                "VRMenuToggle: Falta asignar el Menu o la Camara del Meta XR Rig.",
                this
            );

            return;
        }

        // Direccion hacia donde mira el usuario.
        Vector3 forward = xrCamera.forward;

        // Evita que el menu se incline hacia arriba o abajo.
        if (rotateOnlyOnYAxis)
        {
            forward.y = 0f;

            if (forward.sqrMagnitude > 0.001f)
            {
                forward.Normalize();
            }
            else
            {
                forward = Vector3.forward;
            }
        }

        // Posicion del menu frente al usuario.
        Vector3 targetPosition =
            xrCamera.position +
            forward * distanceFromCamera;

        targetPosition.y =
            xrCamera.position.y +
            heightOffset;

        menu.transform.position = targetPosition;

        // Hacer que el menu mire hacia el usuario.
        Vector3 directionFromCamera =
            menu.transform.position -
            xrCamera.position;

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
