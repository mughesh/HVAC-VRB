// IStepHandler.cs
// Interface for modular step handling in VR training sequences
using UnityEngine;
using System;

// NO NAMESPACE - Follows existing project pattern

/// <summary>
/// Interface for handling specific types of interaction steps in training sequences
/// Enables modular, event-isolated step processing with framework awareness
/// </summary>
public interface IStepHandler
{
    /// <summary>
    /// Check if this handler can process the given step type
    /// </summary>
    bool CanHandle(InteractionStep.StepType stepType);

    /// <summary>
    /// Check if this handler supports the specified VR framework
    /// </summary>
    bool SupportsFramework(VRFramework framework);

    /// <summary>
    /// Initialize the handler with reference to main controller
    /// Called once during controller startup
    /// </summary>
    void Initialize(ModularTrainingSequenceController controller);

    /// <summary>
    /// Start handling a specific step
    /// Subscribe to events, apply parameters, etc.
    /// </summary>
    void StartStep(InteractionStep step);

    /// <summary>
    /// Stop handling a specific step
    /// Unsubscribe from events, cleanup state
    /// </summary>
    void StopStep(InteractionStep step);

    /// <summary>
    /// Clean up all resources when controller is destroyed
    /// </summary>
    void Cleanup();
}

/// <summary>
/// Event arguments for step completion
/// </summary>
public class StepCompletionEventArgs : EventArgs
{
    public InteractionStep step;
    public string reason;

    public StepCompletionEventArgs(InteractionStep step, string reason)
    {
        this.step = step;
        this.reason = reason;
    }
}

/// <summary>
/// Base class for step handlers with common functionality
/// </summary>
public abstract class BaseStepHandler : MonoBehaviour, IStepHandler
{
    protected ModularTrainingSequenceController controller;

    // Event for notifying controller of step completion
    public event EventHandler<StepCompletionEventArgs> OnStepCompleted;

    public abstract bool CanHandle(InteractionStep.StepType stepType);
    public abstract bool SupportsFramework(VRFramework framework);

    public virtual void Initialize(ModularTrainingSequenceController controller)
    {
        this.controller = controller;
    }

    public abstract void StartStep(InteractionStep step);
    public abstract void StopStep(InteractionStep step);

    public virtual void Cleanup()
    {
        OnStepCompleted = null;
    }

    /// <summary>
    /// Helper method for handlers to report step completion
    /// </summary>
    protected void CompleteStep(InteractionStep step, string reason)
    {
        step.isCompleted = true;
        OnStepCompleted?.Invoke(this, new StepCompletionEventArgs(step, reason));
    }

    /// <summary>
    /// Logging helper methods
    /// </summary>
    protected void LogInfo(string message)
    {
        Debug.Log($"[{GetType().Name}] {message}");
    }

    protected void LogDebug(string message)
    {
        if (controller?.enableDebugLogging == true)
        {
            Debug.Log($"[{GetType().Name}] {message}");
        }
    }

    protected void LogWarning(string message)
    {
        Debug.LogWarning($"[{GetType().Name}] {message}");
    }

    protected void LogError(string message)
    {
        Debug.LogError($"[{GetType().Name}] {message}");
    }
}

/// <summary>
/// Base class for XRI-specific step handlers
/// Provides framework support and common XRI functionality
/// </summary>
public abstract class BaseXRIStepHandler : BaseStepHandler
{
    public override bool SupportsFramework(VRFramework framework)
    {
        return framework == VRFramework.XRI;
    }

    public override void Initialize(ModularTrainingSequenceController controller)
    {
        base.Initialize(controller);

        // Verify XRI framework is available
        var currentFramework = VRFrameworkDetector.DetectCurrentFramework();
        if (currentFramework != VRFramework.XRI)
        {
            LogWarning($"XRI step handler initialized but current framework is: {VRFrameworkDetector.GetFrameworkDisplayName(currentFramework)}");
        }
        else
        {
            LogDebug("XRI step handler initialized successfully");
        }
    }

    /// <summary>
    /// Helper method to validate XRI component existence
    /// </summary>
    protected bool ValidateXRIComponent<T>(GameObject obj, string componentName) where T : Component
    {
        if (obj.GetComponent<T>() == null)
        {
            LogError($"Missing {componentName} component on {obj.name}");
            return false;
        }
        return true;
    }
}

/// <summary>
/// Base class for AutoHands-specific step handlers
/// Will be implemented in Phase 2
/// </summary>
public abstract class BaseAutoHandsStepHandler : BaseStepHandler
{
    public override bool SupportsFramework(VRFramework framework)
    {
        return framework == VRFramework.AutoHands;
    }

    public override void Initialize(ModularTrainingSequenceController controller)
    {
        base.Initialize(controller);

        // Verify AutoHands framework is available
        var currentFramework = VRFrameworkDetector.DetectCurrentFramework();
        if (currentFramework != VRFramework.AutoHands)
        {
            LogWarning($"AutoHands step handler initialized but current framework is: {VRFrameworkDetector.GetFrameworkDisplayName(currentFramework)}");
        }
        else
        {
            LogDebug("AutoHands step handler initialized successfully");
        }
    }

    /// <summary>
    /// Helper method to validate AutoHands Grabbable component existence using direct type reference
    /// </summary>
    protected bool ValidateGrabbableComponent(GameObject obj)
    {
        if (obj == null)
        {
            LogError($"GameObject is null when validating Grabbable component");
            return false;
        }

        var grabbable = obj.GetComponent<Autohand.Grabbable>();
        if (grabbable == null)
        {
            LogError($"❌ Missing Grabbable component on {obj.name}");
            return false;
        }

        LogDebug($"✅ Found Grabbable component on {obj.name}");
        return true;
    }

    /// <summary>
    /// Helper method to validate AutoHands component existence using reflection (fallback for other components)
    /// Uses same approach as InteractionSetupService AutoHands validation
    /// </summary>
    protected bool ValidateAutoHandsComponent<T>(GameObject obj, string componentName) where T : Component
    {
        if (obj == null)
        {
            LogError($"GameObject is null when validating {componentName}");
            return false;
        }

        // For generic Component type, check by name using reflection (AutoHands components)
        if (typeof(T) == typeof(Component) || typeof(T) == typeof(MonoBehaviour))
        {
            var components = obj.GetComponents<MonoBehaviour>();
            foreach (var component in components)
            {
                if (component != null && component.GetType().Name == componentName)
                {
                    LogDebug($"✅ Found {componentName} component on {obj.name}");
                    return true;
                }
            }
            LogError($"❌ Missing {componentName} component on {obj.name}");
            return false;
        }

        // For standard Unity components, use standard validation
        if (obj.GetComponent<T>() == null)
        {
            LogError($"❌ Missing {componentName} component on {obj.name}");
            return false;
        }

        LogDebug($"✅ Found {componentName} component on {obj.name}");
        return true;
    }

    /// <summary>
    /// Helper method to check for AutoHands components by name (reflection-based)
    /// </summary>
    protected bool HasAutoHandsComponent(GameObject obj, string componentName)
    {
        if (obj == null) return false;

        var components = obj.GetComponents<MonoBehaviour>();
        foreach (var component in components)
        {
            if (component != null && component.GetType().Name == componentName)
            {
                return true;
            }
        }
        return false;
    }
}