using Oculus.Interaction;
using Oculus.Interaction.Input;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Grabbable))]
public class WristDistanceMover : MonoBehaviour
{
    [Header("Referencias Meta XR")]
    [Tooltip("Grabbable del mismo cubo. Se encuentra automaticamente.")]
    [SerializeField] private Grabbable grabbable;

    [Tooltip("Componente Hand de OVRHandDataSourceLeft.")]
    [SerializeField] private Hand leftHand;

    [Tooltip("Componente Hand de OVRHandDataSourceRight.")]
    [SerializeField] private Hand rightHand;

    [Tooltip("Normalmente CenterEyeAnchor.")]
    [SerializeField] private Transform xrCamera;

    [Header("Movimiento de muneca")]
    [Tooltip("Metros por segundo que avanza o retrocede el objeto.")]
    [SerializeField, Min(0.05f)] private float movementSpeed = 1.2f;

    [Tooltip("Grados que se puede mover la muneca sin desplazar el objeto.")]
    [SerializeField, Range(0f, 25f)] private float wristDeadZone = 8f;

    [Tooltip("Inclinacion usada para alcanzar la velocidad maxima.")]
    [SerializeField, Range(10f, 70f)] private float maximumWristAngle = 40f;

    [Header("Limites de distancia")]
    [SerializeField, Min(0.1f)] private float minimumDistance = 0.35f;
    [SerializeField, Min(0.2f)] private float maximumDistance = 5f;

    [Tooltip("Activalo si arriba y abajo funcionan al reves.")]
    [SerializeField] private bool invertDirection;

    private Hand activeHand;
    private bool wasSelected;
    private float neutralWristAngle;
    private float desiredDistance;

    private void Awake()
    {
        if (grabbable == null)
        {
            grabbable = GetComponent<Grabbable>();
        }

        if (xrCamera == null && Camera.main != null)
        {
            xrCamera = Camera.main.transform;
        }
    }

    private void Start()
    {
        FindHandsIfNecessary();
    }

    private void LateUpdate()
    {
        bool isSelected = grabbable != null && grabbable.SelectingPointsCount > 0;

        if (!isSelected)
        {
            wasSelected = false;
            activeHand = null;
            return;
        }

        if (!wasSelected)
        {
            BeginWristMovement();
            wasSelected = true;
        }

        MoveUsingWrist();
    }

    private void BeginWristMovement()
    {
        activeHand = FindPinchingHand();

        if (xrCamera != null)
        {
            desiredDistance = Vector3.Distance(xrCamera.position, transform.position);
        }

        if (activeHand != null && TryGetWristAngle(activeHand, out float angle))
        {
            neutralWristAngle = angle;
        }
    }

    private void MoveUsingWrist()
    {
        if (xrCamera == null)
        {
            return;
        }

        if (!IsUsable(activeHand))
        {
            activeHand = FindPinchingHand();

            if (activeHand == null || !TryGetWristAngle(activeHand, out neutralWristAngle))
            {
                return;
            }
        }

        if (!TryGetWristAngle(activeHand, out float currentAngle))
        {
            return;
        }

        float angleDelta = Mathf.DeltaAngle(neutralWristAngle, currentAngle);
        float magnitude = Mathf.Abs(angleDelta);

        if (magnitude <= wristDeadZone)
        {
            return;
        }

        float usableAngle = Mathf.Max(1f, maximumWristAngle - wristDeadZone);
        float input = Mathf.Clamp01((magnitude - wristDeadZone) / usableAngle) * Mathf.Sign(angleDelta);

        if (invertDirection)
        {
            input = -input;
        }

        // Muneca arriba reduce la distancia; muneca abajo la aumenta.
        desiredDistance -= input * movementSpeed * Time.unscaledDeltaTime;
        desiredDistance = Mathf.Clamp(desiredDistance, minimumDistance, maximumDistance);

        Vector3 cameraToObject = transform.position - xrCamera.position;
        if (cameraToObject.sqrMagnitude < 0.0001f)
        {
            cameraToObject = xrCamera.forward;
        }

        transform.position = xrCamera.position + cameraToObject.normalized * desiredDistance;
    }

    private bool TryGetWristAngle(Hand hand, out float angle)
    {
        angle = 0f;

        if (!IsUsable(hand) ||
            !hand.GetJointPose(HandJointId.HandWristRoot, out Pose wrist) ||
            !hand.GetJointPose(HandJointId.HandMiddle2, out Pose middleJoint))
        {
            return false;
        }

        Vector3 fingerDirection = middleJoint.position - wrist.position;
        if (fingerDirection.sqrMagnitude < 0.000001f)
        {
            return false;
        }

        Vector3 referenceUp = xrCamera != null ? xrCamera.up : Vector3.up;
        float verticalAmount = Mathf.Clamp(
            Vector3.Dot(fingerDirection.normalized, referenceUp), -1f, 1f);

        angle = Mathf.Asin(verticalAmount) * Mathf.Rad2Deg;
        return true;
    }

    private Hand FindPinchingHand()
    {
        if (IsUsable(leftHand) && leftHand.GetIndexFingerIsPinching())
        {
            return leftHand;
        }

        if (IsUsable(rightHand) && rightHand.GetIndexFingerIsPinching())
        {
            return rightHand;
        }

        return null;
    }

    private static bool IsUsable(Hand hand)
    {
        return hand != null && hand.IsConnected &&
               hand.IsTrackedDataValid && hand.IsHighConfidence;
    }

    private void FindHandsIfNecessary()
    {
        if (leftHand != null && rightHand != null)
        {
            return;
        }

        Hand[] hands = FindObjectsByType<Hand>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (Hand hand in hands)
        {
            if (hand.Handedness == Handedness.Left && leftHand == null)
            {
                leftHand = hand;
            }
            else if (hand.Handedness == Handedness.Right && rightHand == null)
            {
                rightHand = hand;
            }
        }
    }

    private void OnValidate()
    {
        if (maximumDistance < minimumDistance)
        {
            maximumDistance = minimumDistance;
        }

        if (maximumWristAngle <= wristDeadZone)
        {
            maximumWristAngle = wristDeadZone + 1f;
        }
    }
}
