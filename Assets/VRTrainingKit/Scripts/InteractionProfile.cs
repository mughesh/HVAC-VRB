// InteractionProfile.cs
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace VRTrainingKit
{
    /// <summary>
    /// Base class for all interaction profiles
    /// </summary>
    public abstract class InteractionProfile : ScriptableObject
    {
        [Header("Base Settings")]
        public string profileName = "New Profile";
        public Color gizmoColor = Color.cyan;
        
        public abstract void ApplyToGameObject(GameObject target);
        public abstract bool ValidateGameObject(GameObject target);
    }

    /// <summary>
    /// Profile for grab interactions
    /// </summary>
    [CreateAssetMenu(fileName = "GrabProfile", menuName = "VR Training/Grab Profile")]
    public class GrabProfile : InteractionProfile
    {
        [Header("Grab Settings")]
        public XRBaseInteractable.MovementType movementType = XRBaseInteractable.MovementType.VelocityTracking;
        public bool trackPosition = true;
        public bool trackRotation = true;
        public bool throwOnDetach = true;
        
        [Header("Physics Settings")]
        public float throwVelocityScale = 1.5f;
        public float throwAngularVelocityScale = 1.0f;
        
        [Header("Attach Settings")]
        public bool useDynamicAttach = true;
        public float attachEaseInTime = 0.15f;
        
        public override void ApplyToGameObject(GameObject target)
        {
            // Add or get XRGrabInteractable
            XRGrabInteractable grabInteractable = target.GetComponent<XRGrabInteractable>();
            if (grabInteractable == null)
            {
                grabInteractable = target.AddComponent<XRGrabInteractable>();
            }
            
            // Apply settings
            grabInteractable.movementType = movementType;
            grabInteractable.trackPosition = trackPosition;
            grabInteractable.trackRotation = trackRotation;
            grabInteractable.throwOnDetach = throwOnDetach;
            grabInteractable.throwVelocityScale = throwVelocityScale;
            grabInteractable.throwAngularVelocityScale = throwAngularVelocityScale;
            grabInteractable.useDynamicAttach = useDynamicAttach;
            grabInteractable.attachEaseInTime = attachEaseInTime;
            
            // Ensure Rigidbody exists
            Rigidbody rb = target.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = target.AddComponent<Rigidbody>();
                rb.useGravity = true;
                rb.isKinematic = (movementType == XRBaseInteractable.MovementType.Kinematic);
            }
            
            // Ensure Collider exists
            if (target.GetComponent<Collider>() == null)
            {
                BoxCollider col = target.AddComponent<BoxCollider>();
                // Auto-size based on renderer if available
                MeshRenderer renderer = target.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    col.size = renderer.bounds.size;
                }
            }
        }
        
        public override bool ValidateGameObject(GameObject target)
        {
            return target != null && target.CompareTag("grab");
        }
    }

    /// <summary>
    /// Profile for knob/rotatable interactions
    /// </summary>
    [CreateAssetMenu(fileName = "KnobProfile", menuName = "VR Training/Knob Profile")]
    public class KnobProfile : InteractionProfile
    {
        public enum RotationAxis { X, Y, Z }
        
        [Header("Knob Settings")]
        public RotationAxis rotationAxis = RotationAxis.Y;
        public float minAngle = -90f;
        public float maxAngle = 90f;
        public bool useLimits = true;
        
        [Header("Interaction Settings")]
        public bool trackRotationOnly = true;
        public float rotationSpeed = 1.0f;
        
        [Header("Feedback")]
        public bool useHapticFeedback = true;
        public float hapticIntensity = 0.3f;
        public bool snapToAngles = false;
        public float snapAngleIncrement = 15f;
        
        public override void ApplyToGameObject(GameObject target)
        {
            // Add XRGrabInteractable
            XRGrabInteractable grabInteractable = target.GetComponent<XRGrabInteractable>();
            if (grabInteractable == null)
            {
                grabInteractable = target.AddComponent<XRGrabInteractable>();
            }
            
            // Configure for rotation only
            grabInteractable.movementType = XRBaseInteractable.MovementType.Kinematic;
            grabInteractable.trackPosition = false;
            grabInteractable.trackRotation = trackRotationOnly;
            
            // Add or configure Rigidbody
            Rigidbody rb = target.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = target.AddComponent<Rigidbody>();
            }
            rb.isKinematic = true;
            rb.useGravity = false;
            
            // Add ConfigurableJoint for rotation limits
            ConfigurableJoint joint = target.GetComponent<ConfigurableJoint>();
            if (joint == null && useLimits)
            {
                joint = target.AddComponent<ConfigurableJoint>();
                ConfigureJoint(joint);
            }
            
            // Add KnobController for advanced behavior
            KnobController knobController = target.GetComponent<KnobController>();
            if (knobController == null)
            {
                knobController = target.AddComponent<KnobController>();
            }
            knobController.Configure(this);
        }
        
        private void ConfigureJoint(ConfigurableJoint joint)
        {
            // Lock all motion
            joint.xMotion = ConfigurableJointMotion.Locked;
            joint.yMotion = ConfigurableJointMotion.Locked;
            joint.zMotion = ConfigurableJointMotion.Locked;
            
            // Configure rotation based on axis
            switch (rotationAxis)
            {
                case RotationAxis.X:
                    joint.angularXMotion = useLimits ? ConfigurableJointMotion.Limited : ConfigurableJointMotion.Free;
                    joint.angularYMotion = ConfigurableJointMotion.Locked;
                    joint.angularZMotion = ConfigurableJointMotion.Locked;
                    break;
                case RotationAxis.Y:
                    joint.angularXMotion = ConfigurableJointMotion.Locked;
                    joint.angularYMotion = useLimits ? ConfigurableJointMotion.Limited : ConfigurableJointMotion.Free;
                    joint.angularZMotion = ConfigurableJointMotion.Locked;
                    break;
                case RotationAxis.Z:
                    joint.angularXMotion = ConfigurableJointMotion.Locked;
                    joint.angularYMotion = ConfigurableJointMotion.Locked;
                    joint.angularZMotion = useLimits ? ConfigurableJointMotion.Limited : ConfigurableJointMotion.Free;
                    break;
            }
            
            // Set limits if enabled
            if (useLimits)
            {
                SoftJointLimit lowLimit = new SoftJointLimit();
                lowLimit.limit = minAngle;
                
                SoftJointLimit highLimit = new SoftJointLimit();
                highLimit.limit = maxAngle;
                
                joint.lowAngularXLimit = lowLimit;
                joint.highAngularXLimit = highLimit;
            }
        }
        
        public override bool ValidateGameObject(GameObject target)
        {
            return target != null && target.CompareTag("knob");
        }
    }

    /// <summary>
    /// Profile for snap/socket interactions
    /// </summary>
    [CreateAssetMenu(fileName = "SnapProfile", menuName = "VR Training/Snap Profile")]
    public class SnapProfile : InteractionProfile
    {
        [Header("Socket Settings")]
        public float socketRadius = 0.1f;
        public bool showInteractableHoverMeshes = true;
        public Material hoverMaterial;
        
        [Header("Snap Behavior")]
        public bool socketActive = true;
        public float recycleDelayTime = 1.0f;
        public bool ejectOnDisconnect = true;
        
        [Header("Validation")]
        public string[] acceptedTags = new string[] { "grab" };
        public bool requireSpecificObjects = false;
        public GameObject[] specificAcceptedObjects;
        
        public override void ApplyToGameObject(GameObject target)
        {
            // Add XRSocketInteractor
            XRSocketInteractor socketInteractor = target.GetComponent<XRSocketInteractor>();
            if (socketInteractor == null)
            {
                socketInteractor = target.AddComponent<XRSocketInteractor>();
            }
            
            // Apply settings
            socketInteractor.socketActive = socketActive;
            socketInteractor.showInteractableHoverMeshes = showInteractableHoverMeshes;
            socketInteractor.recycleDelayTime = recycleDelayTime;
            
            // Add SphereCollider for detection
            SphereCollider sphereCollider = target.GetComponent<SphereCollider>();
            if (sphereCollider == null)
            {
                sphereCollider = target.AddComponent<SphereCollider>();
            }
            sphereCollider.isTrigger = true;
            sphereCollider.radius = socketRadius;
            
            // Add Rigidbody (required for trigger detection)
            Rigidbody rb = target.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = target.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                rb.useGravity = false;
            }
            
            // Add SnapValidator for custom validation
            SnapValidator validator = target.GetComponent<SnapValidator>();
            if (validator == null)
            {
                validator = target.AddComponent<SnapValidator>();
            }
            validator.Configure(this);
        }
        
        public override bool ValidateGameObject(GameObject target)
        {
            return target != null && target.CompareTag("snap");
        }
    }
}