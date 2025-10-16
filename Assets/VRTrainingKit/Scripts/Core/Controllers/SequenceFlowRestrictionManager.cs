// SequenceFlowRestrictionManager.cs
// Phase 1: Simple socket-only restriction system for training sequences
// Manages socket enable/disable to enforce sequential step completion
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// NO NAMESPACE - Follows existing project pattern

/// <summary>
/// PHASE 1: Socket Restriction Manager
/// Simple, focused manager that controls socket/placepoint components only
/// When enforceSequentialFlow is enabled, only the current step's socket is active
/// </summary>
public class SequenceFlowRestrictionManager : MonoBehaviour
{
    // State tracking
    private TaskGroup currentTaskGroup;
    private InteractionStep currentActiveStep;
    private List<InteractionStep> allSteps = new List<InteractionStep>();

    // Component caches for performance
    private Dictionary<GameObject, Component> socketComponents = new Dictionary<GameObject, Component>();

    // Debug logging
    [Header("Debug Settings")]
    public bool enableDebugLogs = true;

    #region Public API

    /// <summary>
    /// Initialize restriction manager for a new task group
    /// </summary>
    public void StartTaskGroup(TaskGroup taskGroup)
    {
        if (taskGroup == null)
        {
            LogError("Cannot start null task group");
            return;
        }

        LogInfo($"🔒 Starting restriction management for task group: {taskGroup.groupName}");

        currentTaskGroup = taskGroup;
        allSteps.Clear();
        currentActiveStep = null;

        // Store all steps for reference
        if (currentTaskGroup.steps != null)
        {
            allSteps.AddRange(currentTaskGroup.steps);
        }

        // Cache all socket components in the scene
        CacheSocketComponents();

        // Initial state: Disable all sockets
        DisableAllSockets();

        LogInfo($"   ✓ Cached {socketComponents.Count} socket components");
        LogInfo($"   ✓ All sockets disabled - waiting for first step activation");
    }

    /// <summary>
    /// Called when a step becomes active
    /// PHASE 1: Enable only this step's destination socket
    /// </summary>
    public void OnStepBecameActive(InteractionStep step)
    {
        if (step == null) return;

        currentActiveStep = step;
        LogInfo($"🟢 Step activated: {step.stepName}");

        // PHASE 1 LOGIC: Simple one-step-at-a-time
        DisableAllSockets();
        EnableSocketForStep(step);

        LogInfo($"   ✓ Enabled socket for current step only");
    }

    /// <summary>
    /// Called when a step completes
    /// </summary>
    public void OnStepCompleted(InteractionStep step)
    {
        if (step == null) return;

        LogInfo($"✅ Step completed: {step.stepName}");

        // Socket will be re-enabled by next step's OnStepBecameActive
        // For now, just log the completion

        if (currentActiveStep == step)
        {
            currentActiveStep = null;
        }
    }

    /// <summary>
    /// Reset and cleanup
    /// </summary>
    public void Reset()
    {
        LogInfo("🔄 Resetting restriction manager");

        // Re-enable all sockets before cleanup
        EnableAllSockets();

        currentTaskGroup = null;
        currentActiveStep = null;
        allSteps.Clear();
        socketComponents.Clear();
    }

    #endregion

    #region Socket Management

    /// <summary>
    /// Cache all socket/placepoint components in the scene
    /// Supports both XRI (XRSocketInteractor) and AutoHands (PlacePoint)
    /// </summary>
    private void CacheSocketComponents()
    {
        socketComponents.Clear();

        LogInfo("🔍 Scanning scene for socket components...");

        int xriCount = 0;
        int placePointCount = 0;

        // Find XRI sockets
        var xriSockets = FindObjectsOfType<UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor>();
        foreach (var socket in xriSockets)
        {
            socketComponents[socket.gameObject] = socket;
            LogInfo($"   + Found XRI Socket: {socket.gameObject.name}");
            xriCount++;
        }

        // Find AutoHands PlacePoints - try multiple detection methods
        LogInfo("   🔍 Searching for AutoHands PlacePoints...");
        var allComponents = FindObjectsOfType<MonoBehaviour>();

        foreach (var component in allComponents)
        {
            if (component == null) continue;

            string typeName = component.GetType().Name;
            string fullTypeName = component.GetType().FullName;

            // Check if this is a PlacePoint component
            if (typeName == "PlacePoint" || typeName.Contains("PlacePoint"))
            {
                socketComponents[component.gameObject] = component;
                LogInfo($"   + Found AutoHands PlacePoint: {component.gameObject.name} (Type: {fullTypeName})");
                placePointCount++;
            }
        }

        LogInfo($"📍 Cache complete: {xriCount} XRI Sockets, {placePointCount} AutoHands PlacePoints");
        LogInfo($"   Total: {socketComponents.Count} socket components");

        if (socketComponents.Count == 0)
        {
            LogError("⚠️ NO SOCKET COMPONENTS FOUND! Make sure your scene has XRSocketInteractor or PlacePoint components.");
        }
    }

    /// <summary>
    /// Enable the socket for a specific step
    /// </summary>
    private void EnableSocketForStep(InteractionStep step)
    {
        if (step == null) return;

        // Check destination (for GrabAndSnap steps)
        if (step.destination != null && step.destination.GameObject != null)
        {
            GameObject socketObj = step.destination.GameObject;
            SetSocketEnabled(socketObj, true);
            LogDebug($"   ✓ Enabled destination socket: {socketObj.name} (for step: {step.stepName})");
        }

        // Check targetSocket (for valve steps)
        if (step.targetSocket != null && step.targetSocket.GameObject != null)
        {
            GameObject socketObj = step.targetSocket.GameObject;
            SetSocketEnabled(socketObj, true);
            LogDebug($"   ✓ Enabled target socket: {socketObj.name} (for step: {step.stepName})");
        }

        // If step has no socket references, log warning
        if ((step.destination == null || step.destination.GameObject == null) &&
            (step.targetSocket == null || step.targetSocket.GameObject == null))
        {
            LogDebug($"   ⚠️ Step '{step.stepName}' has no socket references (type: {step.type})");
        }
    }

    /// <summary>
    /// Disable all sockets in the scene
    /// </summary>
    private void DisableAllSockets()
    {
        LogInfo("🔒 Disabling all sockets...");
        LogInfo($"   Processing {socketComponents.Count} socket components...");

        int disabledCount = 0;
        int skippedCount = 0;

        foreach (var kvp in socketComponents)
        {
            GameObject socketObj = kvp.Key;
            //LogInfo($"   → Disabling socket - 1: {socketObj.name}");
            if (socketObj == null)
            {
                LogDebug($"   ⚠️ Null socket object in cache, skipping");
                continue;
            }

            // Don't disable if currently occupied (has object snapped)
            if (IsSocketOccupied(socketObj))
            {
                LogInfo($"   ⏭️ Skipping occupied socket: {socketObj.name}");
                skippedCount++;
                continue;
            }

            LogInfo($"   → Disabling socket: {socketObj.name}");
            SetSocketEnabled(socketObj, false);
            disabledCount++;
        }

        LogInfo($"   ✓ Disabled {disabledCount} sockets, skipped {skippedCount} occupied");
    }

    /// <summary>
    /// Enable all sockets (for cleanup/reset)
    /// </summary>
    private void EnableAllSockets()
    {
        LogDebug("🔓 Enabling all sockets...");

        foreach (var kvp in socketComponents)
        {
            SetSocketEnabled(kvp.Key, true);
        }

        LogDebug($"   ✓ Enabled {socketComponents.Count} sockets");
    }

    /// <summary>
    /// Enable or disable a specific socket component
    /// Works with both XRI and AutoHands
    /// </summary>
    private void SetSocketEnabled(GameObject socketObj, bool enabled)
    {
        if (socketObj == null)
        {
            LogDebug($"      ⚠️ SetSocketEnabled called with null GameObject");
            return;
        }

        LogInfo($"      → Setting socket '{socketObj.name}' to {(enabled ? "ENABLED" : "DISABLED")}");

        bool componentFound = false;

        // XRI Socket
        var xriSocket = socketObj.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor>();
        if (xriSocket != null)
        {
            xriSocket.enabled = enabled;
            LogInfo($"      ✓ XRSocketInteractor '{socketObj.name}': {(enabled ? "ENABLED" : "DISABLED")}");
            componentFound = true;
        }

        // AutoHands PlacePoint - try multiple methods to find it
        // Method 1: Direct component search by type name
        Component placePoint = null;
        var allComponentsOnObj = socketObj.GetComponents<MonoBehaviour>();

        foreach (var comp in allComponentsOnObj)
        {
            if (comp != null && comp.GetType().Name.Contains("PlacePoint"))
            {
                placePoint = comp;
                break;
            }
        }

        if (placePoint != null)
        {
            ((MonoBehaviour)placePoint).enabled = enabled;
            LogInfo($"      ✓ AutoHands PlacePoint '{socketObj.name}': {(enabled ? "ENABLED" : "DISABLED")}");
            componentFound = true;
        }

        if (!componentFound)
        {
            LogError($"      ❌ No socket component found on '{socketObj.name}'! Available components:");
            foreach (var comp in allComponentsOnObj)
            {
                if (comp != null)
                {
                    LogError($"         - {comp.GetType().Name}");
                }
            }
        }
    }

    /// <summary>
    /// Check if a socket currently has an object snapped to it
    /// </summary>
    private bool IsSocketOccupied(GameObject socketObj)
    {
        if (socketObj == null)
        {
            LogDebug("      IsSocketOccupied: socketObj is null");
            return false;
        }

        // XRI Socket
        var xriSocket = socketObj.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor>();
        if (xriSocket != null)
        {
            bool hasSelection = xriSocket.hasSelection;
            LogDebug($"      IsSocketOccupied (XRI) '{socketObj.name}': {hasSelection}");
            return hasSelection;
        }

        // AutoHands PlacePoint - check the placedObject property
        // PlacePoint has: public Grabbable placedObject { get; protected set; } = null;
        var allComps = socketObj.GetComponents<MonoBehaviour>();
        foreach (var comp in allComps)
        {
            if (comp != null && comp.GetType().Name.Contains("PlacePoint"))
            {
                // Use reflection to check placedObject property
                var placedObjectProperty = comp.GetType().GetProperty("placedObject");
                if (placedObjectProperty != null)
                {
                    var placedObject = placedObjectProperty.GetValue(comp);
                    bool isOccupied = placedObject != null;
                    LogDebug($"      IsSocketOccupied (AutoHands) '{socketObj.name}': {isOccupied}");
                    return isOccupied;
                }
            }
        }

        LogDebug($"      IsSocketOccupied '{socketObj.name}': No socket component found, returning false");
        return false;
    }

    #endregion

    #region Logging

    private void LogInfo(string message)
    {
        Debug.Log($"[SequenceFlowRestriction] {message}");
    }

    private void LogDebug(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[SequenceFlowRestriction] {message}");
        }
    }

    private void LogError(string message)
    {
        Debug.LogError($"[SequenceFlowRestriction] {message}");
    }

    #endregion
}
