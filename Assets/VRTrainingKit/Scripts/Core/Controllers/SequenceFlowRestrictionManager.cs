// SequenceFlowRestrictionManager.cs
// Simple socket-only restriction system for training sequences
// Manages socket enable/disable at the task group level
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// NO NAMESPACE - Follows existing project pattern

/// <summary>
/// Socket Restriction Manager - Task Group Level
/// Controls socket/placepoint components at the task group level
/// When enforceSequentialFlow is enabled:
/// - All sockets in CURRENT task group are enabled
/// - All sockets in OTHER task groups are disabled
/// </summary>
public class SequenceFlowRestrictionManager : MonoBehaviour
{
    // State tracking
    private TaskGroup currentTaskGroup;
    private List<InteractionStep> allSteps = new List<InteractionStep>();
    private List<InteractionStep> activeSteps = new List<InteractionStep>();
    private List<InteractionStep> completedSteps = new List<InteractionStep>();

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
        activeSteps.Clear();
        completedSteps.Clear();

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
        LogInfo($"   ✓ All sockets disabled - ready for task group activation");
    }

    /// <summary>
    /// Called when a step becomes active
    /// Tracks active steps and updates socket states
    /// </summary>
    public void OnStepBecameActive(InteractionStep step)
    {
        if (step == null) return;

        // Add to active steps list
        if (!activeSteps.Contains(step))
        {
            activeSteps.Add(step);
            LogDebug($"🟢 Step activated: {step.stepName}");
        }

        // Update socket states for task group
        UpdateSocketStates();
    }

    /// <summary>
    /// Called when a step completes
    /// Updates tracking and socket states
    /// </summary>
    public void OnStepCompleted(InteractionStep step)
    {
        if (step == null) return;

        LogInfo($"✅ Step completed: {step.stepName}");

        // Move from active to completed
        activeSteps.Remove(step);
        if (!completedSteps.Contains(step))
        {
            completedSteps.Add(step);
        }

        // Update socket states
        UpdateSocketStates();
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
        activeSteps.Clear();
        completedSteps.Clear();
        allSteps.Clear();
        socketComponents.Clear();
    }

    #endregion

    #region Socket State Management

    /// <summary>
    /// Update socket states - enable all active step sockets, disable others
    /// Simple task-group level restriction (no lookahead needed)
    /// </summary>
    private void UpdateSocketStates()
    {
        LogInfo("🔄 Updating socket states...");

        // Step 1: Disable all sockets
        DisableAllSockets();

        // Step 2: Enable sockets for all active steps in current task group
        foreach (var step in activeSteps)
        {
            EnableSocketForStep(step);
            LogDebug($"   ✓ Enabled socket for active step: {step.stepName}");
        }

        LogInfo($"   ✓ Enabled {activeSteps.Count} step sockets in current task group");
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

    #region Editor/Debug API

    /// <summary>
    /// Get current socket states for debugging/editor display
    /// </summary>
    public List<SocketStateInfo> GetSocketStates()
    {
        var states = new List<SocketStateInfo>();

        foreach (var kvp in socketComponents)
        {
            var socketObj = kvp.Key;
            if (socketObj == null) continue;

            var component = kvp.Value;

            bool isEnabled = GetSocketEnabledState(socketObj);
            bool isInCurrentGroup = IsSocketInCurrentTaskGroup(socketObj);
            bool isOccupied = IsSocketOccupied(socketObj);

            states.Add(new SocketStateInfo
            {
                socketName = socketObj.name,
                isEnabled = isEnabled,
                isInCurrentTaskGroup = isInCurrentGroup,
                isOccupied = isOccupied,
                disabledReason = GetDisabledReason(isInCurrentGroup, isOccupied)
            });
        }

        return states;
    }

    /// <summary>
    /// Get the enabled state of a socket component
    /// </summary>
    private bool GetSocketEnabledState(GameObject socketObj)
    {
        if (socketObj == null) return false;

        // Check XRI Socket
        var xriSocket = socketObj.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor>();
        if (xriSocket != null)
        {
            return xriSocket.enabled;
        }

        // Check AutoHands PlacePoint
        var allComps = socketObj.GetComponents<MonoBehaviour>();
        foreach (var comp in allComps)
        {
            if (comp != null && comp.GetType().Name.Contains("PlacePoint"))
            {
                return comp.enabled;
            }
        }

        return false;
    }

    /// <summary>
    /// Check if a socket belongs to the current task group
    /// </summary>
    private bool IsSocketInCurrentTaskGroup(GameObject socketObj)
    {
        if (socketObj == null || activeSteps == null) return false;

        foreach (var step in activeSteps)
        {
            // Check destination socket
            if (step.destination != null && step.destination.GameObject == socketObj)
                return true;

            // Check target socket
            if (step.targetSocket != null && step.targetSocket.GameObject == socketObj)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Get the reason why a socket is disabled
    /// </summary>
    private string GetDisabledReason(bool isInCurrentGroup, bool isOccupied)
    {
        if (isInCurrentGroup)
            return "Current Task Group";
        if (isOccupied)
            return "Occupied (Not Disabled)";
        return "Wrong Task Group";
    }

    /// <summary>
    /// Get current task group name for display
    /// </summary>
    public string GetCurrentTaskGroupName()
    {
        return currentTaskGroup != null ? currentTaskGroup.groupName : "None";
    }

    /// <summary>
    /// Get active/completed step counts
    /// </summary>
    public (int active, int completed, int total) GetStepCounts()
    {
        return (activeSteps.Count, completedSteps.Count, allSteps.Count);
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

/// <summary>
/// Socket state information for editor/debug display
/// </summary>
[System.Serializable]
public class SocketStateInfo
{
    public string socketName;
    public bool isEnabled;
    public bool isInCurrentTaskGroup;
    public bool isOccupied;
    public string disabledReason;
}
