// SequenceFlowRestrictionManager.cs
// PHASE 1: Enhanced restriction system with Rigidbody freezing
// Manages socket enable/disable AND Rigidbody freeze/unfreeze at task group level
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// NO NAMESPACE - Follows existing project pattern

/// <summary>
/// Enhanced Restriction Manager - Socket + Rigidbody Control
/// PHASE 1: Infrastructure for Rigidbody freezing (not yet integrated)
/// Controls socket/placepoint components AND Rigidbody constraints
/// When enforceSequentialFlow is enabled:
/// - All sockets in CURRENT task group are enabled
/// - All sockets in OTHER task groups are disabled
/// - All objects can be frozen/unfrozen to prevent physics interactions
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

    // PHASE 1: Rigidbody management
    private Dictionary<GameObject, Rigidbody> rigidbodyComponents = new Dictionary<GameObject, Rigidbody>();
    private Dictionary<GameObject, RigidbodyConstraints> originalConstraints = new Dictionary<GameObject, RigidbodyConstraints>();
    private TrainingProgram cachedProgram; // Store reference to entire program for global caching

    // Debug logging
    [Header("Debug Settings")]
    public bool enableDebugLogs = true;

    [Header("Phase 1 - Rigidbody Freezing (Infrastructure Only)")]
    [Tooltip("Enable Rigidbody freeze/unfreeze feature (Phase 1: Testing only, not integrated)")]
    public bool enableRigidbodyFreezing = false;

    #region Public API

    /// <summary>
    /// Initialize restriction manager with the entire training program
    /// Called ONCE at sequence start to cache ALL objects from ALL modules/task groups
    /// </summary>
    public void InitializeWithProgram(TrainingProgram program)
    {
        if (program == null)
        {
            LogError("Cannot initialize with null program");
            return;
        }

        cachedProgram = program;

        if (!enableRigidbodyFreezing)
        {
            LogDebug("Rigidbody freezing disabled - skipping initialization");
            return;
        }

        LogInfo($"🚀 PHASE 1: Initializing restriction manager with entire program: {program.programName}");

        // Cache ALL objects from ALL modules and task groups
        CacheAllRigidbodiesFromProgram(program);

        // PHASE 2 WILL ADD: Freeze all objects here
        // For now, just cache them
    }

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

        // PHASE 1: Rigidbodies are cached globally in InitializeWithProgram()
        // No per-task-group caching needed
        if (enableRigidbodyFreezing && rigidbodyComponents.Count > 0)
        {
            LogInfo($"   ✓ Using {rigidbodyComponents.Count} cached Rigidbodies from program initialization");
        }
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

        // PHASE 1: Unfreeze all objects before cleanup
        UnfreezeAllObjects();

        currentTaskGroup = null;
        activeSteps.Clear();
        completedSteps.Clear();
        allSteps.Clear();
        socketComponents.Clear();

        // PHASE 1: Clear Rigidbody caches
        rigidbodyComponents.Clear();
        originalConstraints.Clear();
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

    #region PHASE 1: Rigidbody Management (Infrastructure)

    /// <summary>
    /// PHASE 1: Cache ALL Rigidbody components from ENTIRE training program
    /// Scans ALL modules, task groups, and steps to find ALL interactive objects
    /// Called ONCE at sequence initialization
    /// </summary>
    private void CacheAllRigidbodiesFromProgram(TrainingProgram program)
    {
        if (program == null || program.modules == null)
        {
            LogError("Cannot cache Rigidbodies - null program or modules");
            return;
        }

        rigidbodyComponents.Clear();
        originalConstraints.Clear();

        LogInfo("🔍 PHASE 1: Scanning ENTIRE PROGRAM for Rigidbody components...");
        LogInfo($"   Program: {program.programName}");
        LogInfo($"   Modules: {program.modules.Count}");

        int foundCount = 0;
        int skippedSocketCount = 0;
        int skippedHingeJointCount = 0;
        int notFoundCount = 0;
        int totalModules = 0;
        int totalTaskGroups = 0;
        int totalSteps = 0;

        // Iterate through ALL modules
        foreach (var module in program.modules)
        {
            if (module == null || module.taskGroups == null) continue;
            totalModules++;

            LogInfo($"   📚 Module: {module.moduleName} ({module.taskGroups.Count} task groups)");

            // Iterate through ALL task groups
            foreach (var taskGroup in module.taskGroups)
            {
                if (taskGroup == null || taskGroup.steps == null) continue;
                totalTaskGroups++;

                LogDebug($"      📁 Task Group: {taskGroup.groupName} ({taskGroup.steps.Count} steps)");

                // Iterate through ALL steps
                foreach (var step in taskGroup.steps)
                {
                    if (step == null) continue;
                    totalSteps++;

                    // Process targetObject (the grabbable object)
                    if (step.targetObject != null && step.targetObject.GameObject != null)
                    {
                        GameObject targetObj = step.targetObject.GameObject;

                        // Skip if already cached (same object used in multiple steps)
                        if (rigidbodyComponents.ContainsKey(targetObj))
                        {
                            LogDebug($"         ⏭️ Already cached: {targetObj.name}");
                            continue;
                        }

                        // Skip if this is a socket object (destination/targetSocket)
                        if (IsSocketObjectInProgram(targetObj, program))
                        {
                            LogDebug($"         ⏭️ Skipping socket object: {targetObj.name}");
                            skippedSocketCount++;
                            continue;
                        }

                        // Find Rigidbody in hierarchy
                        Rigidbody rb = FindRigidbodyInHierarchy(targetObj);
                        if (rb != null)
                        {
                            // Check if object has HingeJoint (knobs, valves after snapping)
                            HingeJoint hingeJoint = rb.GetComponent<HingeJoint>();
                            if (hingeJoint != null)
                            {
                                LogDebug($"         ⚙️ HingeJoint on {targetObj.name} - will skip freezing");
                                skippedHingeJointCount++;
                                continue;
                            }

                            // Cache the Rigidbody and store original constraints
                            rigidbodyComponents[targetObj] = rb;
                            originalConstraints[targetObj] = rb.constraints;
                            foundCount++;
                            LogDebug($"         + {targetObj.name} (Constraints: {rb.constraints})");
                        }
                        else
                        {
                            LogDebug($"         ⚠️ No Rigidbody: {targetObj.name}");
                            notFoundCount++;
                        }
                    }
                }
            }
        }

        LogInfo($"📦 PHASE 1: Global Rigidbody cache complete:");
        LogInfo($"   📊 Scanned: {totalModules} modules, {totalTaskGroups} task groups, {totalSteps} steps");
        LogInfo($"   ✅ Found: {foundCount} Rigidbodies");
        LogInfo($"   ⏭️ Skipped: {skippedSocketCount} sockets, {skippedHingeJointCount} HingeJoint objects");
        if (notFoundCount > 0)
        {
            LogInfo($"   ⚠️ Not found: {notFoundCount} objects (may not have physics)");
        }
    }

    /// <summary>
    /// Check if GameObject is a socket anywhere in the program
    /// </summary>
    private bool IsSocketObjectInProgram(GameObject obj, TrainingProgram program)
    {
        if (obj == null || program == null || program.modules == null) return false;

        foreach (var module in program.modules)
        {
            if (module == null || module.taskGroups == null) continue;

            foreach (var taskGroup in module.taskGroups)
            {
                if (taskGroup == null || taskGroup.steps == null) continue;

                foreach (var step in taskGroup.steps)
                {
                    if (step == null) continue;

                    // Check destination and targetSocket
                    if (step.destination != null && step.destination.GameObject == obj)
                        return true;

                    if (step.targetSocket != null && step.targetSocket.GameObject == obj)
                        return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// PHASE 1: Cache all Rigidbody components from task group objects
    /// (DEPRECATED: Now using CacheAllRigidbodiesFromProgram instead)
    /// </summary>
    private void CacheRigidbodyComponents(TaskGroup taskGroup)
    {
        if (taskGroup == null || taskGroup.steps == null)
        {
            LogError("Cannot cache Rigidbodies - null task group or steps");
            return;
        }

        rigidbodyComponents.Clear();
        originalConstraints.Clear();

        LogInfo("🔍 PHASE 1: Scanning task group for Rigidbody components...");

        int foundCount = 0;
        int skippedSocketCount = 0;
        int skippedHingeJointCount = 0;
        int notFoundCount = 0;

        foreach (var step in taskGroup.steps)
        {
            if (step == null) continue;

            // Process targetObject (the grabbable object)
            if (step.targetObject != null && step.targetObject.GameObject != null)
            {
                GameObject targetObj = step.targetObject.GameObject;

                // Skip if this is a socket object (destination/targetSocket)
                if (IsSocketObject(targetObj))
                {
                    LogDebug($"   ⏭️ Skipping socket object: {targetObj.name} (sockets should not be frozen)");
                    skippedSocketCount++;
                    continue;
                }

                // Find Rigidbody in hierarchy
                Rigidbody rb = FindRigidbodyInHierarchy(targetObj);
                if (rb != null)
                {
                    // Check if object has HingeJoint (knobs, valves after snapping)
                    HingeJoint hingeJoint = rb.GetComponent<HingeJoint>();
                    if (hingeJoint != null)
                    {
                        LogDebug($"   ⚙️ Found HingeJoint on {targetObj.name} - will skip freezing (already constrained)");
                        skippedHingeJointCount++;
                        continue;
                    }

                    // Cache the Rigidbody and store original constraints
                    if (!rigidbodyComponents.ContainsKey(targetObj))
                    {
                        rigidbodyComponents[targetObj] = rb;
                        originalConstraints[targetObj] = rb.constraints;
                        foundCount++;
                        LogDebug($"   + Found Rigidbody on: {targetObj.name} (Original constraints: {rb.constraints})");
                    }
                }
                else
                {
                    LogDebug($"   ⚠️ No Rigidbody found for: {targetObj.name} (Type: {step.type})");
                    notFoundCount++;
                }
            }
        }

        LogInfo($"📦 PHASE 1: Rigidbody cache complete:");
        LogInfo($"   ✅ Found: {foundCount} Rigidbodies");
        LogInfo($"   ⏭️ Skipped: {skippedSocketCount} sockets, {skippedHingeJointCount} HingeJoint objects");
        if (notFoundCount > 0)
        {
            LogInfo($"   ⚠️ Not found: {notFoundCount} objects (may not have physics)");
        }
    }

    /// <summary>
    /// PHASE 1: Find Rigidbody component in GameObject hierarchy
    /// Checks parent first, then children
    /// </summary>
    private Rigidbody FindRigidbodyInHierarchy(GameObject obj)
    {
        if (obj == null) return null;

        // Check on the object itself
        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            LogDebug($"      → Found Rigidbody on parent: {obj.name}");
            return rb;
        }

        // Check children
        rb = obj.GetComponentInChildren<Rigidbody>();
        if (rb != null)
        {
            LogDebug($"      → Found Rigidbody on child: {rb.gameObject.name} (parent: {obj.name})");
            return rb;
        }

        // Check parent (in case the reference is to a child)
        if (obj.transform.parent != null)
        {
            rb = obj.transform.parent.GetComponent<Rigidbody>();
            if (rb != null)
            {
                LogDebug($"      → Found Rigidbody on parent: {obj.transform.parent.name} (child: {obj.name})");
                return rb;
            }
        }

        return null;
    }

    /// <summary>
    /// PHASE 1: Check if a GameObject is a socket (destination or targetSocket)
    /// Socket objects should never be frozen
    /// </summary>
    private bool IsSocketObject(GameObject obj)
    {
        if (obj == null) return false;

        // Check if this object is referenced as a destination or targetSocket in any step
        foreach (var step in allSteps)
        {
            if (step.destination != null && step.destination.GameObject == obj)
                return true;

            if (step.targetSocket != null && step.targetSocket.GameObject == obj)
                return true;
        }

        return false;
    }

    /// <summary>
    /// PHASE 1: Freeze a single object's Rigidbody
    /// Freezes position and rotation constraints
    /// </summary>
    public void FreezeObject(GameObject obj)
    {
        if (obj == null)
        {
            LogDebug("   ⚠️ FreezeObject called with null GameObject");
            return;
        }

        if (!rigidbodyComponents.ContainsKey(obj))
        {
            LogDebug($"   ⚠️ FreezeObject: No cached Rigidbody for {obj.name}");
            return;
        }

        Rigidbody rb = rigidbodyComponents[obj];
        if (rb == null)
        {
            LogDebug($"   ⚠️ FreezeObject: Rigidbody is null for {obj.name}");
            return;
        }

        // Check for HingeJoint before freezing
        HingeJoint hingeJoint = rb.GetComponent<HingeJoint>();
        if (hingeJoint != null)
        {
            LogDebug($"   ⚙️ Skipping freeze for {obj.name} - HingeJoint detected (already constrained)");
            return;
        }

        // Store original constraints if not already stored
        if (!originalConstraints.ContainsKey(obj))
        {
            originalConstraints[obj] = rb.constraints;
        }

        // Freeze all position and rotation axes
        rb.constraints = RigidbodyConstraints.FreezeAll;

        LogInfo($"   🧊 FROZEN: {obj.name} (Original: {originalConstraints[obj]} → New: FreezeAll)");
    }

    /// <summary>
    /// PHASE 1: Unfreeze a single object's Rigidbody
    /// Restores original constraints
    /// </summary>
    public void UnfreezeObject(GameObject obj)
    {
        if (obj == null)
        {
            LogDebug("   ⚠️ UnfreezeObject called with null GameObject");
            return;
        }

        if (!rigidbodyComponents.ContainsKey(obj))
        {
            LogDebug($"   ⚠️ UnfreezeObject: No cached Rigidbody for {obj.name}");
            return;
        }

        Rigidbody rb = rigidbodyComponents[obj];
        if (rb == null)
        {
            LogDebug($"   ⚠️ UnfreezeObject: Rigidbody is null for {obj.name}");
            return;
        }

        // Restore original constraints
        if (originalConstraints.ContainsKey(obj))
        {
            rb.constraints = originalConstraints[obj];
            LogInfo($"   🔓 UNFROZEN: {obj.name} (Restored to: {originalConstraints[obj]})");
        }
        else
        {
            // Fallback: unfreeze everything
            rb.constraints = RigidbodyConstraints.None;
            LogInfo($"   🔓 UNFROZEN: {obj.name} (No original constraints stored, set to None)");
        }
    }

    /// <summary>
    /// PHASE 1: Unfreeze all cached objects
    /// Used during cleanup/reset
    /// </summary>
    private void UnfreezeAllObjects()
    {
        if (rigidbodyComponents.Count == 0)
        {
            LogDebug("   No Rigidbodies to unfreeze");
            return;
        }

        LogInfo($"🔓 PHASE 1: Unfreezing all {rigidbodyComponents.Count} objects...");

        int unfrozenCount = 0;
        foreach (var kvp in rigidbodyComponents)
        {
            GameObject obj = kvp.Key;
            Rigidbody rb = kvp.Value;

            if (obj != null && rb != null)
            {
                // Restore original constraints
                if (originalConstraints.ContainsKey(obj))
                {
                    rb.constraints = originalConstraints[obj];
                    unfrozenCount++;
                }
            }
        }

        LogInfo($"   ✓ Unfrozen {unfrozenCount} objects");
    }

    /// <summary>
    /// PHASE 1 TEST METHOD: Manually test freezing a specific object
    /// Call this from Inspector or console to test freeze functionality
    /// </summary>
    public void TestFreezeObject(GameObject obj)
    {
        if (obj == null)
        {
            LogError("TEST: Cannot freeze null object");
            return;
        }

        LogInfo($"🧪 TEST: Manual freeze test for {obj.name}");

        // Find Rigidbody
        Rigidbody rb = FindRigidbodyInHierarchy(obj);
        if (rb == null)
        {
            LogError($"TEST: No Rigidbody found on {obj.name}");
            return;
        }

        // Cache it
        if (!rigidbodyComponents.ContainsKey(obj))
        {
            rigidbodyComponents[obj] = rb;
            originalConstraints[obj] = rb.constraints;
        }

        // Freeze it
        FreezeObject(obj);

        LogInfo($"TEST: {obj.name} should now be frozen. Try moving it with physics.");
        LogInfo($"TEST: Call TestUnfreezeObject({obj.name}) to restore.");
    }

    /// <summary>
    /// PHASE 1 TEST METHOD: Manually test unfreezing a specific object
    /// </summary>
    public void TestUnfreezeObject(GameObject obj)
    {
        if (obj == null)
        {
            LogError("TEST: Cannot unfreeze null object");
            return;
        }

        LogInfo($"🧪 TEST: Manual unfreeze test for {obj.name}");

        UnfreezeObject(obj);

        LogInfo($"TEST: {obj.name} should now be unfrozen. Original constraints restored.");
    }

    /// <summary>
    /// PHASE 1 TEST METHOD: Cache and display all Rigidbodies in current task group
    /// Call this to test the caching mechanism
    /// </summary>
    public void TestCacheRigidbodies()
    {
        if (currentTaskGroup == null)
        {
            LogError("TEST: No current task group. Start a sequence first.");
            return;
        }

        LogInfo($"🧪 TEST: Caching Rigidbodies for task group: {currentTaskGroup.groupName}");

        CacheRigidbodyComponents(currentTaskGroup);

        LogInfo($"TEST: Found {rigidbodyComponents.Count} Rigidbodies");
        foreach (var kvp in rigidbodyComponents)
        {
            LogInfo($"   - {kvp.Key.name}: {kvp.Value.constraints}");
        }
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
        return "Other Task Group (Available Later)";
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
