// VRHandColliderRegistry.cs
// Global registry for VR hand finger tip colliders
// Used by button conditions to detect hand touches
using UnityEngine;
using System.Collections.Generic;

// NO NAMESPACE - Follows existing project pattern

/// <summary>
/// Singleton registry for VR hand finger tip colliders
/// Place this on your AutoHands rig and assign all finger tip colliders
/// All button conditions can then check if a collider belongs to a hand
/// </summary>
public class VRHandColliderRegistry : MonoBehaviour
{
    public static VRHandColliderRegistry Instance { get; private set; }

    [Header("Hand Finger Tip Colliders")]
    [Tooltip("Add all finger tip colliders from both hands here (left + right)")]
    public List<Collider> fingerTipColliders = new List<Collider>();


    [Header("Debug Settings")]
    [SerializeField] private bool enableDebugLogging = false;
    [SerializeField] private bool showGizmos = true;

    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LogInfo($"VR Hand Collider Registry initialized with {fingerTipColliders.Count} finger colliders");
        }
        else
        {
            Debug.LogWarning($"Duplicate VRHandColliderRegistry found on {gameObject.name} - destroying");
            Destroy(gameObject);
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// Check if a collider belongs to a hand finger tip
    /// </summary>
    public bool IsHandCollider(Collider collider)
    {
        if (collider == null) return false;

        bool isHand = fingerTipColliders.Contains(collider);

        if (isHand && enableDebugLogging)
        {
            LogDebug($"Hand collider detected: {collider.name}");
        }

        return isHand;
    }

    /// <summary>
    /// Get count of registered finger colliders
    /// </summary>
    public int GetColliderCount()
    {
        return fingerTipColliders.Count;
    }

    /// <summary>
    /// Validate that all colliders are still valid (not destroyed)
    /// </summary>
    public bool ValidateColliders()
    {
        bool allValid = true;

        for (int i = fingerTipColliders.Count - 1; i >= 0; i--)
        {
            if (fingerTipColliders[i] == null)
            {
                Debug.LogWarning($"VRHandColliderRegistry: Collider at index {i} is null - removing from list");
                fingerTipColliders.RemoveAt(i);
                allValid = false;
            }
        }

        return allValid;
    }

    void LogInfo(string message)
    {
        Debug.Log($"[VRHandColliderRegistry] {message}");
    }

    void LogDebug(string message)
    {
        if (enableDebugLogging)
        {
            Debug.Log($"[VRHandColliderRegistry] {message}");
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (!showGizmos || fingerTipColliders == null) return;

        // Draw spheres at finger tip collider positions
        Gizmos.color = Color.cyan;

        foreach (var collider in fingerTipColliders)
        {
            if (collider != null)
            {
                Gizmos.DrawWireSphere(collider.transform.position, 0.01f);
            }
        }
    }

    void OnValidate()
    {
        // Auto-cleanup null entries when editing in Inspector
        if (fingerTipColliders != null)
        {
            fingerTipColliders.RemoveAll(c => c == null);
        }
    }
#endif
}
