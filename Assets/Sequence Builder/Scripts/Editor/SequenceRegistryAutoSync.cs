// SequenceRegistryAutoSync.cs
// Auto-sync hook that triggers registry sync before scene save
// Part of SequenceRegistry reliability improvements

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Automatically syncs SequenceRegistry from TrainingSequenceAsset before scene save.
/// This prevents data loss by ensuring registry is always up-to-date before persistence.
/// User can enable/disable via checkbox in SequenceRegistry component.
/// </summary>
[InitializeOnLoad]
public static class SequenceRegistryAutoSync
{
    static SequenceRegistryAutoSync()
    {
        // Subscribe to scene saving event
        EditorSceneManager.sceneSaving += OnSceneSaving;
        Debug.Log("[SequenceRegistryAutoSync] Auto-sync system initialized. Will sync before scene saves.");
    }

    /// <summary>
    /// Called just before a scene is saved (Ctrl+S, auto-save, play mode entry, etc.)
    /// </summary>
    private static void OnSceneSaving(UnityEngine.SceneManagement.Scene scene, string path)
    {
        // Find SequenceRegistry in the scene being saved
        var registry = Object.FindObjectOfType<SequenceRegistry>();

        if (registry == null)
        {
            // No registry in scene - nothing to sync
            return;
        }

        // Check if auto-sync is enabled
        if (!registry.autoSyncOnSave)
        {
            Debug.Log($"[SequenceRegistryAutoSync] Auto-sync disabled for scene '{scene.name}'. Skipping sync.");
            return;
        }

        if (registry.sequenceAsset == null)
        {
            // No sequence assigned - nothing to sync
            Debug.LogWarning($"[SequenceRegistryAutoSync] Scene '{scene.name}' has SequenceRegistry but no sequence asset assigned. Skipping sync.");
            return;
        }

        // Perform sync
        Debug.Log($"[SequenceRegistryAutoSync] 🔄 Auto-syncing registry before saving scene '{scene.name}'...");

        int synced = registry.SyncFromSequenceAsset();

        if (synced > 0)
        {
            EditorUtility.SetDirty(registry);
            Debug.Log($"[SequenceRegistryAutoSync] ✅ Auto-synced {synced} references before save");
        }
        else
        {
            Debug.LogWarning($"[SequenceRegistryAutoSync] ⚠ No references synced. Check if references are set in Sequence Builder.");
        }
    }
}
#endif
