using UnityEngine;
using Autohand;

public class WristUIController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Hand hand;
    [SerializeField] private Camera headCamera;
    [SerializeField] private GameObject wristUI;

    [Header("Activation Settings")]
    [Tooltip("Maximum distance from head to wrist to activate")]
    [SerializeField] private float maxDistance = 0.75f;

    [Tooltip("Minimum angle (degrees) palm must face UP towards head. 0=straight up, 90=sideways")]
    [Range(0, 90)]
    [SerializeField] private float minPalmUpAngle = 30f;

    [Tooltip("Invert palm direction check (use if hand transform is inverted)")]
    [SerializeField] private bool invertPalmDirection = false;

    [Tooltip("Local axis of the palm that should point towards the head")]
    [SerializeField] private Vector3 trackingAxis = Vector3.up;

    [Tooltip("Hide UI while holding objects")]
    [SerializeField] private bool disableWhileHolding = true;

    [Tooltip("Hide UI while hovering/reaching for objects")]
    [SerializeField] private bool disableWhileHighlighting = true; // NEW SETTING

    [Header("Debug")]
    [SerializeField] private float currentAngle; // Read-only for debugging

    [Header("Events")]
    public UnityHandEvent OnShow;
    public UnityHandEvent OnHide;

    private bool isShowing = false;

    private void Start()
    {
        if (wristUI != null)
        {
            wristUI.SetActive(false);
        }
    }

    private void Update()
    {
        if (hand == null || headCamera == null || wristUI == null)
            return;

        bool shouldShow = CheckShouldShow();

        if (!isShowing && shouldShow)
        {
            if (wristUI != null)
                wristUI.SetActive(true);

            OnShow?.Invoke(hand);
            isShowing = true;
        }
        else if (isShowing && !shouldShow)
        {
            if (wristUI != null)
                wristUI.SetActive(false);

            OnHide?.Invoke(hand);
            isShowing = false;
        }
    }

    private bool CheckShouldShow()
    {
        // 1. Check if holding object OR currently grabbing (animation in progress)
        // 'holdingObj' is the object currently held
        // 'IsGrabbing()' returns true during the transition animation before the object is fully held
        if (disableWhileHolding && (hand.holdingObj != null || hand.IsGrabbing()))
        {
            return false;
        }

        // 2. Check if highlighting (reaching for) an object
        // 'lookingAtObj' returns the object the AutoHand system is currently targeting/highlighting
        if (disableWhileHighlighting && hand.lookingAtObj != null)
        {
            return false;
        }

        // 3. Check distance from head to wrist
        Vector3 handPos = hand.transform.position;
        Vector3 headPos = headCamera.transform.position;
        float distance = Vector3.Distance(headPos, handPos);

        if (distance > maxDistance)
        {
            return false;
        }

        // 4. Check if palm is facing UP towards the head
        // Use the configured tracking axis relative to the hand's rotation
        Vector3 trackingDir = hand.palmTransform.TransformDirection(trackingAxis);
        if (invertPalmDirection) trackingDir = -trackingDir;

        Vector3 toHead = (headPos - handPos).normalized;
        currentAngle = Vector3.Angle(trackingDir, toHead); // Update debug field

        return currentAngle >= minPalmUpAngle;
    }

    [ContextMenu("Align Tracking Axis To Camera")]
    public void AlignTrackingAxis()
    {
        if (hand == null || headCamera == null)
        {
            Debug.LogError("Hand or HeadCamera not assigned!");
            return;
        }

        Vector3 toHead = (headCamera.transform.position - hand.transform.position).normalized;
        // Convert world direction to local direction relative to palm
        Vector3 localDir = hand.palmTransform.InverseTransformDirection(toHead);

        // Snap to nearest cardinal axis for cleaner numbers (optional, but good for setup)
        // Or just use the exact vector
        trackingAxis = localDir;

        Debug.Log($"Aligned Tracking Axis to: {trackingAxis}. You may want to round this to the nearest whole number (0, 1, or -1).");
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (hand == null || headCamera == null)
            return;

        Vector3 handPos = hand.transform.position;
        Vector3 headPos = headCamera.transform.position;

        // Draw line to head
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(handPos, headPos);

        // Visualize the tracking axis
        Vector3 trackingDir = hand.palmTransform.TransformDirection(trackingAxis);
        if (invertPalmDirection) trackingDir = -trackingDir;

        // Draw the tracking vector
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(handPos, trackingDir * 0.3f);

        // Draw the "Success" cone visualization
        if (Application.isPlaying)
        {
            bool shouldShow = CheckShouldShow();
            Gizmos.color = shouldShow ? Color.green : Color.red;
            Gizmos.DrawWireSphere(handPos, 0.05f);
        }
    }
#endif
}