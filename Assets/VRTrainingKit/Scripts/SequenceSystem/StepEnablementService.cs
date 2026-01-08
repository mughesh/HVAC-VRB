// StepEnablementService.cs
// Manages component enable/disable states for training sequence steps
// Implements lookahead logic to enable sockets for upcoming snap operations
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// NO NAMESPACE - Follows existing project pattern

/// <summary>
/// Service that manages which interaction components are enabled/disabled during training sequences
/// Prevents wrong interactions by only enabling components for current and prepared (lookahead) steps
/// </summary>
public class StepEnablementService : MonoBehaviour
{
    /// <summary>
    /// States that a step can be in regarding component enablement
    /// </summary>
    public enum StepEnablementState
    {
        Locked,      // Future step - all components disabled
        Prepared,    // Next-in-line - destination components enabled (for previous step's snap targets)
        Active,      // Current step - all components enabled
        Completed,   // Done - components disabled unless needed by future steps
        Retained     // Completed but components kept enabled for future steps
    }

    // State tracking
    private Dictionary<InteractionStep, StepEnablementState> stepStates = new Dictionary<InteractionStep, StepEnablementState>();
    private TaskGroup currentTaskGroup;
    private List<InteractionStep> activeSteps = new List<InteractionStep>();
    private List<InteractionStep> completedSteps = new List<InteractionStep>();

    // Debug logging
    private bool enableDebugLogs = true;

    #region Public API

    /// <summary>
    /// Initialize service for a new task group
    /// </summary>
    public void StartTaskGroup(TaskGroup group)
    {
        if (group == null)
        {
            LogError("Cannot start null task group");
            return;
        }

        LogInfo($"📋 StepEnablementService: Starting task group '{group.groupName}'");

        currentTaskGroup = group;
        activeSteps.Clear();
        completedSteps.Clear();
        stepStates.Clear();

        // Initialize all steps as Locked
        foreach (var step in currentTaskGroup.steps)
        {
            if (step != null)
            {
                stepStates[step] = StepEnablementState.Locked;
            }
        }

        // Initial update will be triggered when first steps become active
        LogDebug($"   - Initialized {stepStates.Count} steps in Locked state");
    }

    /// <summary>
    /// Called when a step becomes active (starts execution)
    /// </summary>
    public void OnStepBecameActive(InteractionStep step)
    {
        if (step == null || currentTaskGroup == null) return;

        if (!activeSteps.Contains(step))
        {
            activeSteps.Add(step);
            LogDebug($"🟢 Step became active: {step.stepName}");
        }

        UpdateStepStates();
    }

    /// <summary>
    /// Called when a step completes
    /// </summary>
    public void OnStepCompleted(InteractionStep step)
    {
        if (step == null || currentTaskGroup == null) return;

        activeSteps.Remove(step);

        if (!completedSteps.Contains(step))
        {
            completedSteps.Add(step);
            LogDebug($"✅ Step completed: {step.stepName}");
        }

        UpdateStepStates();
    }

    /// <summary>
    /// Reset all state tracking
    /// </summary>
    public void Reset()
    {
        LogDebug("🔄 Resetting StepEnablementService");

        // Re-enable all components before reset
        foreach (var kvp in stepStates)
        {
            EnableComponentsForStep(kvp.Key, StepEnablementState.Active);
        }

        activeSteps.Clear();
        completedSteps.Clear();
        stepStates.Clear();
        currentTaskGroup = null;
    }

    /// <summary>
    /// Get current state of a step
    /// </summary>
    public StepEnablementState GetStepState(InteractionStep step)
    {
        if (step == null || !stepStates.ContainsKey(step))
            return StepEnablementState.Locked;

        return stepStates[step];
    }

    #endregion

    #region State Machine Logic

    /// <summary>
    /// Main state update logic - determines which steps should be in which states
    /// </summary>
    private void UpdateStepStates()
    {
        if (currentTaskGroup == null || currentTaskGroup.steps == null)
            return;

        LogDebug($"🔄 Updating step states - Active: {activeSteps.Count}, Completed: {completedSteps.Count}");

        // Create a new state dictionary
        var newStates = new Dictionary<InteractionStep, StepEnablementState>();

        // STEP 1: Initialize all steps as Locked
        foreach (var step in currentTaskGroup.steps)
        {
            if (step != null)
            {
                newStates[step] = StepEnablementState.Locked;
            }
        }

        // STEP 2: Mark active steps
        foreach (var step in activeSteps)
        {
            newStates[step] = StepEnablementState.Active;
            LogDebug($"   - {step.stepName}: Active");
        }

        // STEP 3: Mark prepared steps (lookahead for grab→snap workflows)
        var preparedSteps = GetPreparedSteps();
        foreach (var step in preparedSteps)
        {
            // Only mark as Prepared if not already Active
            if (newStates[step] != StepEnablementState.Active)
            {
                newStates[step] = StepEnablementState.Prepared;
                LogDebug($"   - {step.stepName}: Prepared (destination enabled for current step's snap)");
            }
        }

        // STEP 4: Mark completed steps (check if components should be retained)
        foreach (var step in completedSteps)
        {
            if (IsComponentNeededByFutureStep(step))
            {
                newStates[step] = StepEnablementState.Retained;
                LogDebug($"   - {step.stepName}: Retained (needed by future step)");
            }
            else
            {
                newStates[step] = StepEnablementState.Completed;
                LogDebug($"   - {step.stepName}: Completed (components disabled)");
            }
        }

        // STEP 5: Apply state changes and enable/disable components
        foreach (var kvp in newStates)
        {
            var step = kvp.Key;
            var newState = kvp.Value;
            var oldState = stepStates.ContainsKey(step) ? stepStates[step] : StepEnablementState.Locked;

            // Only update if state changed
            if (oldState != newState)
            {
                LogDebug($"   - State change: {step.stepName} ({oldState} → {newState})");
                EnableComponentsForStep(step, newState);
            }

            stepStates[step] = newState;
        }
    }

    /// <summary>
    /// Get steps that should be in Prepared state (their destinations need to be enabled for current active steps)
    /// </summary>
    private List<InteractionStep> GetPreparedSteps()
    {
        var preparedSteps = new List<InteractionStep>();

        // For each active step, check if upcoming steps need to be prepared
        foreach (var activeStep in activeSteps)
        {
            var nextSteps = GetNextSteps(activeStep);

            foreach (var nextStep in nextSteps)
            {
                // If next step is a snap operation, prepare it (enable its socket)
                if (IsSnapStep(nextStep))
                {
                    if (!preparedSteps.Contains(nextStep) && !activeSteps.Contains(nextStep))
                    {
                        preparedSteps.Add(nextStep);
                    }
                }
            }
        }

        return preparedSteps;
    }

    /// <summary>
    /// Get next steps in sequence after a given step (respects parallel execution)
    /// </summary>
    private List<InteractionStep> GetNextSteps(InteractionStep currentStep)
    {
        var nextSteps = new List<InteractionStep>();

        if (currentTaskGroup == null || currentTaskGroup.steps == null)
            return nextSteps;

        int currentIndex = currentTaskGroup.steps.IndexOf(currentStep);
        if (currentIndex == -1) return nextSteps;

        // Look ahead in sequence
        for (int i = currentIndex + 1; i < currentTaskGroup.steps.Count; i++)
        {
            var step = currentTaskGroup.steps[i];

            // Skip completed or optional steps
            if (step.isCompleted || step.isOptional)
                continue;

            // Add this step
            nextSteps.Add(step);

            // If this step is NOT parallel, stop lookahead
            if (!step.allowParallel)
                break;
        }

        return nextSteps;
    }

    /// <summary>
    /// Check if a completed step's components are needed by future steps
    /// </summary>
    private bool IsComponentNeededByFutureStep(InteractionStep completedStep)
    {
        if (completedStep == null || completedStep.targetObject == null)
            return false;

        // Check if any active or upcoming steps reference this step's target object
        var targetObj = completedStep.targetObject.GameObject;
        if (targetObj == null) return false;

        // Check active steps
        foreach (var step in activeSteps)
        {
            if (StepReferencesObject(step, targetObj))
                return true;
        }

        // Check upcoming steps (not yet active, not completed)
        int completedIndex = currentTaskGroup.steps.IndexOf(completedStep);
        for (int i = completedIndex + 1; i < currentTaskGroup.steps.Count; i++)
        {
            var futureStep = currentTaskGroup.steps[i];
            if (!futureStep.isCompleted && StepReferencesObject(futureStep, targetObj))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Check if a step references a specific GameObject
    /// </summary>
    private bool StepReferencesObject(InteractionStep step, GameObject obj)
    {
        if (step == null || obj == null) return false;

        // Check targetObject
        if (step.targetObject != null && step.targetObject.GameObject == obj)
            return true;

        // Check destination
        if (step.destination != null && step.destination.GameObject == obj)
            return true;

        // Check targetSocket (for valve steps)
        if (step.targetSocket != null && step.targetSocket.GameObject == obj)
            return true;

        return false;
    }

    /// <summary>
    /// Check if a step type is a snap operation (needs socket enabled)
    /// </summary>
    private bool IsSnapStep(InteractionStep step)
    {
        if (step == null) return false;

        return step.type == InteractionStep.StepType.GrabAndSnap ||
               step.type == InteractionStep.StepType.InstallScrew;
    }

    #endregion

    #region Component Enable/Disable

    /// <summary>
    /// Enable or disable components based on step state
    /// </summary>
    private void EnableComponentsForStep(InteractionStep step, StepEnablementState state)
    {
        if (step == null) return;

        switch (state)
        {
            case StepEnablementState.Locked:
                // Disable all components
                DisableTargetComponents(step);
                DisableDestinationComponents(step);
                break;

            case StepEnablementState.Prepared:
                // Only enable destination (for previous step's snap)
                DisableTargetComponents(step);
                EnableDestinationComponents(step);
                break;

            case StepEnablementState.Active:
                // Enable everything
                EnableTargetComponents(step);
                EnableDestinationComponents(step);
                break;

            case StepEnablementState.Completed:
                // Disable all components
                DisableTargetComponents(step);
                DisableDestinationComponents(step);
                break;

            case StepEnablementState.Retained:
                // Keep everything enabled
                EnableTargetComponents(step);
                EnableDestinationComponents(step);
                break;
        }
    }

    /// <summary>
    /// Enable target object's grabbable components
    /// PHASE 1: Grabbable restrictions commented out - only sockets are restricted for now
    /// </summary>
    private void EnableTargetComponents(InteractionStep step)
    {
        if (step.targetObject == null) return;

        var targetObj = step.targetObject.GameObject;
        if (targetObj == null) return;

        // PHASE 1: Keep grabbables always enabled - comment out for socket-only restrictions
        //SetGrabbableEnabled(targetObj, true);

        // Also enable controllers (knob, valve controllers)
        SetControllerEnabled(targetObj, true);
    }

    /// <summary>
    /// Disable target object's grabbable components
    /// PHASE 1: Grabbable restrictions commented out - only sockets are restricted for now
    /// </summary>
    private void DisableTargetComponents(InteractionStep step)
    {
        if (step.targetObject == null) return;

        var targetObj = step.targetObject.GameObject;
        if (targetObj == null) return;

        // PHASE 1: Keep grabbables always enabled - comment out for socket-only restrictions
        // Only disable grabbable, keep controllers enabled (to maintain state)
        //SetGrabbableEnabled(targetObj, false);
    }

    /// <summary>
    /// Enable destination socket components
    /// </summary>
    private void EnableDestinationComponents(InteractionStep step)
    {
        // Check destination for snap steps
        if (step.destination != null)
        {
            var destObj = step.destination.GameObject;
            if (destObj != null)
            {
                SetSocketEnabled(destObj, true);
            }
        }

        // Check targetSocket for valve steps
        if (step.targetSocket != null)
        {
            var socketObj = step.targetSocket.GameObject;
            if (socketObj != null)
            {
                SetSocketEnabled(socketObj, true);
            }
        }
    }

    /// <summary>
    /// Disable destination socket components
    /// </summary>
    private void DisableDestinationComponents(InteractionStep step)
    {
        // Check if socket has an object snapped before disabling
        // (don't break existing snaps)

        // Check destination for snap steps
        if (step.destination != null)
        {
            var destObj = step.destination.GameObject;
            if (destObj != null && !IsSocketOccupied(destObj))
            {
                SetSocketEnabled(destObj, false);
            }
        }

        // Check targetSocket for valve steps
        if (step.targetSocket != null)
        {
            var socketObj = step.targetSocket.GameObject;
            if (socketObj != null && !IsSocketOccupied(socketObj))
            {
                SetSocketEnabled(socketObj, false);
            }
        }
    }

    /// <summary>
    /// Enable/disable grabbable component (works with both XRI and AutoHands)
    /// </summary>
    private void SetGrabbableEnabled(GameObject obj, bool enabled)
    {
        if (obj == null) return;

        // XRI Framework
        var xrGrab = obj.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        if (xrGrab != null)
        {
            xrGrab.enabled = enabled;
            LogDebug($"      - {(enabled ? "Enabled" : "Disabled")} XRGrabInteractable on {obj.name}");
        }

        // AutoHands Framework
        var autoHandsGrab = obj.GetComponent(System.Type.GetType("Autohand.Grabbable, Assembly-CSharp"));
        if (autoHandsGrab != null)
        {
            ((MonoBehaviour)autoHandsGrab).enabled = enabled;
            LogDebug($"      - {(enabled ? "Enabled" : "Disabled")} AutoHands Grabbable on {obj.name}");
        }
    }

    /// <summary>
    /// Enable/disable controller components (knob, valve controllers)
    /// </summary>
    private void SetControllerEnabled(GameObject obj, bool enabled)
    {
        if (obj == null) return;

        // Knob controllers
        var knobController = obj.GetComponent(System.Type.GetType("KnobController"));
        if (knobController != null)
        {
            ((MonoBehaviour)knobController).enabled = enabled;
        }

        var autoHandsKnobController = obj.GetComponent(System.Type.GetType("AutoHandsKnobController"));
        if (autoHandsKnobController != null)
        {
            ((MonoBehaviour)autoHandsKnobController).enabled = enabled;
        }

        // Valve controllers - always keep enabled to maintain state
        // (Don't disable valve controllers, they need to maintain tight/loose state)
    }

    /// <summary>
    /// Enable/disable socket component (works with both XRI and AutoHands)
    /// </summary>
    private void SetSocketEnabled(GameObject socket, bool enabled)
    {
        if (socket == null) return;

        // XRI Framework
        var xrSocket = socket.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor>();
        if (xrSocket != null)
        {
            xrSocket.enabled = enabled;
            LogDebug($"      - {(enabled ? "Enabled" : "Disabled")} XRSocketInteractor on {socket.name}");
        }

        // AutoHands Framework (PlacePoint)
        var placePoint = socket.GetComponent(System.Type.GetType("PlacePoint, Assembly-CSharp"));
        if (placePoint != null)
        {
            ((MonoBehaviour)placePoint).enabled = enabled;
            LogDebug($"      - {(enabled ? "Enabled" : "Disabled")} AutoHands PlacePoint on {socket.name}");
        }
    }

    /// <summary>
    /// Check if a socket has an object currently snapped to it
    /// </summary>
    private bool IsSocketOccupied(GameObject socket)
    {
        if (socket == null) return false;

        // XRI Framework
        var xrSocket = socket.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor>();
        if (xrSocket != null)
        {
            return xrSocket.hasSelection;
        }

        // AutoHands Framework - check if PlacePoint has a placed object
        // (PlacePoint doesn't have a simple "hasSelection" property, so we check children or use reflection)
        var placePoint = socket.GetComponent(System.Type.GetType("PlacePoint, Assembly-CSharp"));
        if (placePoint != null)
        {
            // Simple heuristic: check if socket has child objects (placed object is parented)
            return socket.transform.childCount > 0;
        }

        return false;
    }

    #endregion

    #region Logging

    private void LogInfo(string message)
    {
        if (enableDebugLogs)
            Debug.Log($"[StepEnablementService] {message}");
    }

    private void LogDebug(string message)
    {
        if (enableDebugLogs)
            Debug.Log($"[StepEnablementService] {message}");
    }

    private void LogError(string message)
    {
        Debug.LogError($"[StepEnablementService] {message}");
    }

    #endregion
}
