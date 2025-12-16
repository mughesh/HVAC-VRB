// SequenceTabDrawer.cs
// Extracted Sequence tab drawer for VRInteractionSetupWindow
// Part of Phase 6: Final tab extraction refactoring

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// Draws the Sequence tab content for VRInteractionSetupWindow.
/// Handles training asset management and coordinates tree view and properties panel.
/// </summary>
public class SequenceTabDrawer
{
    // Dependencies
    private readonly SequenceTreeView _treeView;
    private readonly SequencePropertiesPanel _propertiesPanel;

    // State
    private TrainingSequenceAsset _currentTrainingAsset;
    private TrainingProgram _currentProgram;
    private Vector2 _sequenceScrollPos;
    private TrainingSequenceAsset[] _availableAssets;
    private int _selectedAssetIndex = 0;
    private bool _assetsLoaded = false;
    private float _splitterPosition = 0.4f; // 40% tree view, 60% details panel

    // Callbacks
    private readonly System.Action _onRepaint;

    public SequenceTabDrawer(
        SequenceTreeView treeView,
        SequencePropertiesPanel propertiesPanel,
        System.Action onRepaint)
    {
        _treeView = treeView;
        _propertiesPanel = propertiesPanel;
        _onRepaint = onRepaint;
    }

    /// <summary>
    /// Gets the current training asset
    /// </summary>
    public TrainingSequenceAsset CurrentTrainingAsset => _currentTrainingAsset;

    /// <summary>
    /// Gets the current program
    /// </summary>
    public TrainingProgram CurrentProgram => _currentProgram;

    /// <summary>
    /// Gets or sets the splitter position
    /// </summary>
    public float SplitterPosition
    {
        get => _splitterPosition;
        set => _splitterPosition = value;
    }

    /// <summary>
    /// Draw the Sequence tab content
    /// </summary>
    public void Draw(float windowWidth)
    {
        EditorGUILayout.LabelField("Training Sequence Builder", VRTrainingEditorStyles.HeaderStyle);
        EditorGUILayout.Space(5);

        // Load available assets if needed
        if (!_assetsLoaded)
        {
            LoadAvailableTrainingAssets();
        }

        // Asset selection bar
        DrawAssetSelectionBar();

        EditorGUILayout.Space(10);

        // Debug information
        if (_currentTrainingAsset == null)
        {
            EditorGUILayout.HelpBox("currentTrainingAsset is null. Try selecting an asset from the dropdown above.", MessageType.Warning);
            return;
        }

        if (_currentProgram == null)
        {
            EditorGUILayout.HelpBox($"currentProgram is null for asset: {_currentTrainingAsset.name}. The asset may be corrupted.", MessageType.Error);
            return;
        }

        // Main content area with two-panel layout
        DrawTwoPanelLayout(windowWidth);
    }

    /// <summary>
    /// Draw the main two-panel editing interface
    /// </summary>
    private void DrawTwoPanelLayout(float windowWidth)
    {
        // Use horizontal layout for simplicity
        EditorGUILayout.BeginHorizontal();

        // Tree view panel (left) - 40% width
        EditorGUILayout.BeginVertical(GUILayout.Width(windowWidth * _splitterPosition));
        _treeView?.Draw(_currentProgram);
        EditorGUILayout.EndVertical();

        // Details panel (right) - 60% width
        EditorGUILayout.BeginVertical();
        _propertiesPanel?.Draw(_treeView?.SelectedItem, _treeView?.SelectedItemType);
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// Draw the asset selection bar
    /// </summary>
    private void DrawAssetSelectionBar()
    {
        EditorGUILayout.BeginHorizontal();

        // Asset dropdown
        if (_availableAssets != null && _availableAssets.Length > 0)
        {
            EditorGUILayout.LabelField("Program:", GUILayout.Width(60));

            string[] assetNames = new string[_availableAssets.Length];
            for (int i = 0; i < _availableAssets.Length; i++)
            {
                assetNames[i] = _availableAssets[i] != null ? _availableAssets[i].name : "Missing Asset";
            }

            EditorGUI.BeginChangeCheck();
            _selectedAssetIndex = EditorGUILayout.Popup(_selectedAssetIndex, assetNames);
            if (EditorGUI.EndChangeCheck())
            {
                if (_selectedAssetIndex >= 0 && _selectedAssetIndex < _availableAssets.Length)
                {
                    LoadTrainingAsset(_availableAssets[_selectedAssetIndex]);
                }
            }
        }
        else
        {
            EditorGUILayout.LabelField("No training sequence assets found in project");
        }

        // Control buttons
        if (GUILayout.Button("New", GUILayout.Width(50)))
        {
            CreateNewTrainingAsset();
        }

        if (GUILayout.Button("Load", GUILayout.Width(50)))
        {
            LoadAvailableTrainingAssets();
        }

        if (GUILayout.Button("Save", GUILayout.Width(50)) && _currentTrainingAsset != null)
        {
            SaveCurrentAsset();
        }

        // Info about keyboard shortcuts
        if (_currentTrainingAsset != null)
        {
            GUILayout.Label("Ctrl+S: Force Save | Ctrl+R: Refresh References", EditorStyles.miniLabel);
        }

        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// Load available training assets from the project
    /// </summary>
    public void LoadAvailableTrainingAssets()
    {
        _availableAssets = TrainingSequenceAssetManager.LoadAllSequenceAssets();
        _assetsLoaded = true;

        // Auto-select first asset if available
        if (_availableAssets.Length > 0 && _currentTrainingAsset == null)
        {
            _selectedAssetIndex = 0;
            LoadTrainingAsset(_availableAssets[0]);
        }
    }

    /// <summary>
    /// Load a specific training asset
    /// </summary>
    public void LoadTrainingAsset(TrainingSequenceAsset asset)
    {
        _currentTrainingAsset = asset;
        _currentProgram = asset?.Program;

        // Clear selection when loading new asset
        if (_treeView != null)
        {
            _treeView.SelectedItem = null;
            _treeView.SelectedItemType = null;
        }

        if (_currentTrainingAsset != null)
        {
            Debug.Log($"Loaded training asset: {_currentTrainingAsset.name}");
            var stats = _currentTrainingAsset.GetStats();
            Debug.Log($"Asset stats: {stats}");
        }
    }

    /// <summary>
    /// Create a new training asset
    /// </summary>
    private void CreateNewTrainingAsset()
    {
        // Show options for template or empty
        if (EditorUtility.DisplayDialog("Create New Training Sequence",
            "Would you like to start with the HVAC template or create an empty sequence?",
            "HVAC Template", "Empty Sequence"))
        {
            var asset = TrainingSequenceAssetManager.CreateHVACTemplateAsset();
            TrainingSequenceAssetManager.SaveAssetToSequencesFolder(asset);
        }
        else
        {
            var asset = TrainingSequenceAssetManager.CreateEmptyAsset();
            TrainingSequenceAssetManager.SaveAssetToSequencesFolder(asset);
        }

        // Reload assets list
        LoadAvailableTrainingAssets();
    }

    /// <summary>
    /// Save the current asset
    /// </summary>
    private void SaveCurrentAsset()
    {
        if (_currentTrainingAsset != null)
        {
            UnityEditor.EditorUtility.SetDirty(_currentTrainingAsset);
            UnityEditor.AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("Save Complete", $"Saved {_currentTrainingAsset.name}", "OK");
        }
    }

    /// <summary>
    /// Auto-save the current asset without dialog
    /// </summary>
    public void AutoSaveCurrentAsset()
    {
        if (_currentTrainingAsset != null)
        {
            UnityEditor.EditorUtility.SetDirty(_currentTrainingAsset);
            Debug.Log($"[VRTrainingKit] Auto-saved training asset: {_currentTrainingAsset.name}");
        }
    }

    /// <summary>
    /// Force save all assets
    /// </summary>
    public void ForceSaveAllAssets()
    {
        if (_currentTrainingAsset != null)
        {
            UnityEditor.EditorUtility.SetDirty(_currentTrainingAsset);
            UnityEditor.AssetDatabase.SaveAssets();
            Debug.Log($"[VRTrainingKit] Force saved all assets including: {_currentTrainingAsset.name}");
        }
    }

    /// <summary>
    /// Refresh training asset references after play mode or asset changes
    /// </summary>
    public void RefreshTrainingAssetReferences()
    {
        if (_currentTrainingAsset != null)
        {
            // Store current selection
            string currentAssetName = _currentTrainingAsset.name;

            // Reload all available assets
            LoadAvailableTrainingAssets();

            // Try to restore the previously selected asset
            if (_availableAssets != null)
            {
                for (int i = 0; i < _availableAssets.Length; i++)
                {
                    if (_availableAssets[i] != null && _availableAssets[i].name == currentAssetName)
                    {
                        _selectedAssetIndex = i;
                        LoadTrainingAsset(_availableAssets[i]);
                        Debug.Log($"[VRInteractionSetupWindow] Restored training asset: {currentAssetName}");
                        return;
                    }
                }
            }

            Debug.LogWarning($"[VRInteractionSetupWindow] Could not restore training asset: {currentAssetName}");
        }
    }

    /// <summary>
    /// Refresh all GameObject references in the current training program
    /// </summary>
    public void RefreshAllObjectReferences()
    {
        if (_currentProgram == null) return;

        int refreshCount = 0;

        foreach (var module in _currentProgram.modules)
        {
            foreach (var taskGroup in module.taskGroups)
            {
                foreach (var step in taskGroup.steps)
                {
                    if (step.targetObject != null)
                    {
                        step.targetObject.RefreshReference();
                        refreshCount++;
                    }
                    if (step.destination != null)
                    {
                        step.destination.RefreshReference();
                        refreshCount++;
                    }
                    if (step.targetSocket != null)
                    {
                        step.targetSocket.RefreshReference();
                        refreshCount++;
                    }
                }
            }
        }

        if (refreshCount > 0)
        {
            AutoSaveCurrentAsset();
            Debug.Log($"[VRTrainingKit] Refreshed {refreshCount} object references. Use Ctrl+R to refresh again if needed.");
        }
        else
        {
            Debug.Log("[VRTrainingKit] No object references found to refresh.");
        }
    }
}
#endif
