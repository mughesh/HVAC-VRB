// KnobController.cs
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using System;

// NO NAMESPACE - Fixes ScriptableObject issues
/// <summary>
/// Controls knob rotation behavior and constraints
/// </summary>
public class KnobController : MonoBehaviour
    {
        [Header("Profile Configuration")]
        [SerializeField] private KnobProfile profile;  // Made serialized to test persistence
        
        private XRGrabInteractable grabInteractable;
        private new HingeJoint hingeJoint;
        private float currentAngle = 0f;
        private float startAngle = 0f;
        private Transform originalParent;
        private Quaternion originalRotation;
        
        public float CurrentAngle => currentAngle;
        public float NormalizedValue => profile != null && profile.useLimits ? 
            (currentAngle - profile.minAngle) / (profile.maxAngle - profile.minAngle) : 0f;
        
        public event Action<float> OnAngleChanged;
        public event Action<float> OnSnapToAngle;
        
        private void Awake()
        {
            grabInteractable = GetComponent<XRGrabInteractable>();
            hingeJoint = GetComponent<HingeJoint>();
            originalRotation = transform.localRotation;
            
            Debug.Log($"[KnobController] {gameObject.name} Awake() - Profile state: {(profile != null ? profile.profileName : "NULL")}");
        }
        
        private void Start()
        {
            Debug.Log($"[KnobController] {gameObject.name} Start() - Profile state: {(profile != null ? profile.profileName : "NULL")}");
        }
        
        #if UNITY_EDITOR
        private void OnValidate()
        {
            // This gets called when the component is modified in the editor
            Debug.Log($"[KnobController] {gameObject.name} OnValidate() - Profile state: {(profile != null ? profile.profileName : "NULL")}");
        }
        #endif
        
        private void OnEnable()
        {
            if (grabInteractable != null)
            {
                grabInteractable.selectEntered.AddListener(OnGrab);
                grabInteractable.selectExited.AddListener(OnRelease);
            }
        }
        
        private void OnDisable()
        {
            if (grabInteractable != null)
            {
                grabInteractable.selectEntered.RemoveListener(OnGrab);
                grabInteractable.selectExited.RemoveListener(OnRelease);
            }
        }
        
        public void Configure(KnobProfile knobProfile)
        {
            var previousProfile = profile?.profileName ?? "NULL";
            profile = knobProfile;
            currentAngle = GetCurrentAngle();
            startAngle = currentAngle;
            
            // Debug configuration info
            Debug.Log($"[KnobController] Configure() called for {gameObject.name}: " +
                     $"Previous={previousProfile} → New={profile.profileName}, " +
                     $"Axis={profile.rotationAxis}, Range=[{profile.minAngle:F1}° to {profile.maxAngle:F1}°], " +
                     $"HingeJoint={(hingeJoint != null ? "Yes" : "No")}, StartAngle={startAngle:F2}°");
                     
            #if UNITY_EDITOR
            // Force the object to be dirty in editor so changes are saved
            UnityEditor.EditorUtility.SetDirty(this);
            #endif
        }
        
        private void OnGrab(SelectEnterEventArgs args)
        {
            startAngle = GetCurrentAngle();
        }
        
        private void OnRelease(SelectExitEventArgs args)
        {
            if (profile != null && profile.snapToAngles)
            {
                SnapToNearestAngle();
            }
        }
        
        private void Update()
        {
            if (grabInteractable != null && grabInteractable.isSelected && profile != null)
            {
                UpdateRotation();
            }
            else if (grabInteractable != null && grabInteractable.isSelected && profile == null)
            {
                Debug.LogWarning($"[KnobController] {gameObject.name} is grabbed but has no profile configured!");
            }
        }
        
        private void UpdateRotation()
        {
            float newAngle = GetCurrentAngle();
            
            // Apply limits if enabled
            if (profile.useLimits)
            {
                float clampedAngle = Mathf.Clamp(newAngle, profile.minAngle, profile.maxAngle);
                if (clampedAngle != newAngle)
                {
                  //  Debug.Log($"[KnobController] {gameObject.name} Angle clamped from {newAngle:F2}° to {clampedAngle:F2}° (limits: {profile.minAngle}° to {profile.maxAngle}°)");
                    newAngle = clampedAngle;
                    ApplyRotation(newAngle);
                }
            }
            
            // Fire event if angle changed - LOWERED THRESHOLD FOR TESTING
            float angleDifference = Mathf.Abs(newAngle - currentAngle);
            if (angleDifference > 0.001f) // Lowered from 0.01f to 0.001f
            {
                float previousAngle = currentAngle;
                currentAngle = newAngle;
                
                //Debug.Log($"[KnobController] {gameObject.name} ANGLE CHANGED! {previousAngle:F3}° → {currentAngle:F3}° (diff: {angleDifference:F3}°) - FIRING EVENT");
                OnAngleChanged?.Invoke(currentAngle);
                
                // Haptic feedback
                if (profile.useHapticFeedback && grabInteractable.isSelected)
                {
                    var controller = grabInteractable.interactorsSelecting[0] as XRBaseInputInteractor;
                    controller?.SendHapticImpulse(profile.hapticIntensity, 0.1f);
                }
            }
        }
        
        private float GetCurrentAngle()
        {
            // Try to get angle from HingeJoint first (more accurate)
            if (hingeJoint != null)
            {
                return GetHingeAngle();
            }
            
            // Fallback to transform-based angle reading
            return GetTransformAngle();
        }
        
        /// <summary>
        /// Get angle from HingeJoint - more accurate for physics-based knobs
        /// </summary>
        private float GetHingeAngle()
        {
            if (hingeJoint == null) 
            {
                Debug.LogWarning($"[KnobController] {gameObject.name} GetHingeAngle(): HingeJoint is null, falling back to transform");
                return GetTransformAngle();
            }
            
            // HingeJoint angle is relative to its initial position
            float jointAngle = hingeJoint.angle;
            
            // Safety check for NaN values
            if (float.IsNaN(jointAngle))
            {
             //   Debug.LogError($"[KnobController] {gameObject.name} HingeJoint angle is NaN! Returning 0°");
                return 0f;
            }
            
            return jointAngle;
        }
        
        /// <summary>
        /// Get angle from Transform - fallback method
        /// </summary>
        private float GetTransformAngle()
        {
            Vector3 euler = transform.localEulerAngles;
            
            // Safety check for NaN values in euler angles
            if (float.IsNaN(euler.x) || float.IsNaN(euler.y) || float.IsNaN(euler.z))
            {
                Debug.LogError($"[KnobController] {gameObject.name} Transform euler angles contain NaN! Returning 0°");
                return 0f;
            }
            
            float angle = 0f;
            
            switch (profile?.rotationAxis ?? KnobProfile.RotationAxis.Y)
            {
                case KnobProfile.RotationAxis.X:
                    angle = euler.x;
                    break;
                case KnobProfile.RotationAxis.Y:
                    angle = euler.y;
                    break;
                case KnobProfile.RotationAxis.Z:
                    angle = euler.z;
                    break;
            }
            
            // Convert to -180 to 180 range
            if (angle > 180f) angle -= 360f;
            
            // Final NaN check
            if (float.IsNaN(angle))
            {
                Debug.LogError($"[KnobController] {gameObject.name} Final angle calculation resulted in NaN! Returning 0°");
                return 0f;
            }
            
            return angle;
        }
        
        private void ApplyRotation(float angle)
        {
            Vector3 euler = transform.localEulerAngles;
            
            switch (profile.rotationAxis)
            {
                case KnobProfile.RotationAxis.X:
                    euler.x = angle;
                    break;
                case KnobProfile.RotationAxis.Y:
                    euler.y = angle;
                    break;
                case KnobProfile.RotationAxis.Z:
                    euler.z = angle;
                    break;
            }
            
            transform.localEulerAngles = euler;
        }
        
        private void SnapToNearestAngle()
        {
            float snappedAngle = Mathf.Round(currentAngle / profile.snapAngleIncrement) * profile.snapAngleIncrement;
            
            if (profile.useLimits)
            {
                snappedAngle = Mathf.Clamp(snappedAngle, profile.minAngle, profile.maxAngle);
            }
            
            ApplyRotation(snappedAngle);
            currentAngle = snappedAngle;
            OnSnapToAngle?.Invoke(snappedAngle);
        }
        
        public void SetAngle(float angle, bool immediate = false)
        {
            if (profile != null && profile.useLimits)
            {
                angle = Mathf.Clamp(angle, profile.minAngle, profile.maxAngle);
            }
            
            if (immediate)
            {
                ApplyRotation(angle);
                currentAngle = angle;
                OnAngleChanged?.Invoke(currentAngle);
            }
            else
            {
                // Could implement smooth rotation here
                StartCoroutine(SmoothRotateToAngle(angle));
            }
        }
        
        private System.Collections.IEnumerator SmoothRotateToAngle(float targetAngle)
        {
            float startTime = Time.time;
            float startAngle = currentAngle;
            float duration = 0.5f;
            
            while (Time.time - startTime < duration)
            {
                float t = (Time.time - startTime) / duration;
                float angle = Mathf.Lerp(startAngle, targetAngle, t);
                ApplyRotation(angle);
                currentAngle = angle;
                OnAngleChanged?.Invoke(currentAngle);
                yield return null;
            }
            
            ApplyRotation(targetAngle);
            currentAngle = targetAngle;
            OnAngleChanged?.Invoke(currentAngle);
        }
    }

    /// <summary>
    /// Validates what can be snapped to a socket
    /// </summary>
    public class SnapValidator : MonoBehaviour
    {
        private SnapProfile profile;
        private XRSocketInteractor socketInteractor;
        
        private void Awake()
        {
            socketInteractor = GetComponent<XRSocketInteractor>();
        }
        
        private void OnEnable()
        {
            if (socketInteractor != null)
            {
                socketInteractor.selectEntered.AddListener(OnObjectSnapped);
                socketInteractor.selectExited.AddListener(OnObjectRemoved);
            }
        }
        
        private void OnDisable()
        {
            if (socketInteractor != null)
            {
                socketInteractor.selectEntered.RemoveListener(OnObjectSnapped);
                socketInteractor.selectExited.RemoveListener(OnObjectRemoved);
            }
        }
        
        public void Configure(SnapProfile snapProfile)
        {
            profile = snapProfile;
            
            if (socketInteractor != null)
            {
                // Set up hover validation
                socketInteractor.hoverEntered.AddListener(OnHoverEntered);
            }
        }
        
        private void OnHoverEntered(HoverEnterEventArgs args)
        {
            // Validate if this object can be snapped
            if (!IsValidForSocket(args.interactableObject.transform.gameObject))
            {
                // Could add visual feedback for invalid object
                Debug.Log($"Object {args.interactableObject.transform.name} is not valid for this socket");
            }
        }
        
        private bool IsValidForSocket(GameObject obj)
        {
            if (profile == null) return true;
            
            // Check if requires specific objects
            if (profile.requireSpecificObjects && profile.specificAcceptedObjects != null)
            {
                foreach (var acceptedObj in profile.specificAcceptedObjects)
                {
                    if (obj == acceptedObj) return true;
                }
                return false;
            }
            
            // Check tags
            if (profile.acceptedTags != null && profile.acceptedTags.Length > 0)
            {
                foreach (var tag in profile.acceptedTags)
                {
                    if (obj.CompareTag(tag)) return true;
                }
                return false;
            }
            
            return true;
        }
        
        private void OnObjectSnapped(SelectEnterEventArgs args)
        {
            GameObject snappedObject = args.interactableObject.transform.gameObject;
            
            if (!IsValidForSocket(snappedObject))
            {
                // Eject invalid object
                StartCoroutine(EjectInvalidObject());
            }
            else
            {
                Debug.Log($"Successfully snapped: {snappedObject.name}");
                
                // Fire event for sequence system
                var sequenceController = FindObjectOfType<SequenceController>();
                sequenceController?.OnObjectSnapped(gameObject, snappedObject);
            }
        }
        
        private void OnObjectRemoved(SelectExitEventArgs args)
        {
            GameObject removedObject = args.interactableObject.transform.gameObject;
            Debug.Log($"Object removed: {removedObject.name}");
            
            // Fire event for sequence system
            var sequenceController = FindObjectOfType<SequenceController>();
            sequenceController?.OnObjectUnsnapped(gameObject, removedObject);
        }
        
        private System.Collections.IEnumerator EjectInvalidObject()
        {
            yield return new WaitForSeconds(0.1f);
            
            if (socketInteractor != null && socketInteractor.hasSelection)
            {
                // Force eject
                var interactable = socketInteractor.GetOldestInteractableSelected();
                socketInteractor.interactionManager.SelectExit(socketInteractor, interactable);
            }
        }
    }

    /// <summary>
    /// Validates sequence requirements before allowing interactions
    /// </summary>
    public class SequenceValidator : MonoBehaviour
    {
        public string requiredStateGroup = "";
        public bool allowWithWarning = true;
        public string warningMessage = "This action should not be performed yet!";
        
        private XRBaseInteractable interactable;
        private SequenceController sequenceController;
        private bool isLocked = false;
        
        private void Awake()
        {
            interactable = GetComponent<XRBaseInteractable>();
            sequenceController = FindObjectOfType<SequenceController>();
        }
        
        private void OnEnable()
        {
            if (interactable != null)
            {
                interactable.hoverEntered.AddListener(OnHoverEntered);
                interactable.selectEntered.AddListener(OnSelectEntered);
            }
        }
        
        private void OnDisable()
        {
            if (interactable != null)
            {
                interactable.hoverEntered.RemoveListener(OnHoverEntered);
                interactable.selectEntered.RemoveListener(OnSelectEntered);
            }
        }
        
        private void OnHoverEntered(HoverEnterEventArgs args)
        {
            CheckSequenceRequirements();
        }
        
        private void OnSelectEntered(SelectEnterEventArgs args)
        {
            if (isLocked && !allowWithWarning)
            {
                // Prevent interaction
                StartCoroutine(ForceDeselect(args));
            }
            else if (isLocked && allowWithWarning)
            {
                // Show warning but allow
                ShowWarning();
            }
        }
        
        private void CheckSequenceRequirements()
        {
            if (sequenceController != null && !string.IsNullOrEmpty(requiredStateGroup))
            {
                isLocked = !sequenceController.IsStateGroupActive(requiredStateGroup);
                UpdateVisualFeedback();
            }
        }
        
        private void UpdateVisualFeedback()
        {
            // Change material or outline color based on lock state
            Renderer renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                // This is simplified - you'd want a more sophisticated material swapping system
                if (isLocked)
                {
                    // Could tint red or add outline
                    renderer.material.color = new Color(1f, 0.7f, 0.7f);
                }
                else
                {
                    renderer.material.color = Color.white;
                }
            }
        }
        
        private void ShowWarning()
        {
            Debug.LogWarning($"[Sequence Warning] {warningMessage}");
            // In a real implementation, this would show UI feedback
        }
        
        private System.Collections.IEnumerator ForceDeselect(SelectEnterEventArgs args)
        {
            yield return null; // Wait one frame
            
            if (interactable != null && args.interactorObject != null)
            {
                interactable.interactionManager.SelectExit(
                    args.interactorObject as IXRSelectInteractor, 
                    interactable as IXRSelectInteractable
                );
            }
        }
    }
// End of file - no namespace closing brace