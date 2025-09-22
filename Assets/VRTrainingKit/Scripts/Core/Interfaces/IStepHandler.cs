// IStepHandler.cs
// Interface for modular step handling in VR training sequences
using UnityEngine;
using System;

// NO NAMESPACE - Follows existing project pattern

/// <summary>
/// Interface for handling specific types of interaction steps in training sequences
/// Enables modular, event-isolated step processing
/// </summary>
public interface IStepHandler
{
    /// <summary>
    /// Check if this handler can process the given step type
    /// </summary>
    bool CanHandle(InteractionStep.StepType stepType);

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