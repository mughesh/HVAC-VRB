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
    private bool _isDraggingSplitter = false;
    private const string SPLITTER_PREF_KEY = "VRTrainingKit.SequenceTab.SplitterPos";
    private const string LAST_SEQUENCE_PREF_KEY = "VRTrainingKit.LastSequenceAssetPath";
    private const float SPLITTER_WIDTH = 4f;

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

        // Load saved splitter position
        _splitterPosition = EditorPrefs.GetFloat(SPLITTER_PREF_KEY, 0.4f);
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
    /// Draw the main two-panel editing interface with draggable splitter
    /// </summary>
    private void DrawTwoPanelLayout(float windowWidth)
    {
        EditorGUILayout.BeginHorizontal();

        // Left panel (Hierarchy/Tree view)
        float leftPanelWidth = (windowWidth * _splitterPosition) - (SPLITTER_WIDTH / 2);
        EditorGUILayout.BeginVertical(GUILayout.Width(leftPanelWidth));
        _treeView?.Draw(_currentProgram);
        EditorGUILayout.EndVertical();

        // Splitter (draggable divider) - reserve actual layout space for it
        DrawSplitter(windowWidth);

        // Right panel (Properties)
        EditorGUILayout.BeginVertical();
        _propertiesPanel?.Draw(_treeView?.SelectedItem, _treeView?.SelectedItemType);
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// Draw and handle the draggable splitter between panels
    /// </summary>
    private void DrawSplitter(float windowWidth)
    {
        // Reserve layout space for the splitter - this creates the gap!
        Rect splitterRect = GUILayoutUtility.GetRect(SPLITTER_WIDTH, 0, GUILayout.ExpandHeight(true));

        // Visual representation (draw semi-transparent bar)
        if (Event.current.type == EventType.Repaint)
        {
            EditorGUI.DrawRect(splitterRect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
        }

        // Change cursor when hovering over splitter
        EditorGUIUtility.AddCursorRect(splitterRect, MouseCursor.ResizeHorizontal);

        // Handle mouse events
        switch (Event.current.type)
        {
            case EventType.MouseDown:
                if (splitterRect.Contains(Event.current.mousePosition))
                {
                    _isDraggingSplitter = true;
                    Event.current.Use();
                }
                break;

            case EventType.MouseDrag:
                if (_isDraggingSplitter)
                {
                    // Update splitter position based on mouse X relative to window width
                    _splitterPosition = Mathf.Clamp(
                        Event.current.mousePosition.x / windowWidth,
                        0.2f,  // Min 20%
                        0.8f   // Max 80%
                    );

                    // Save to preferences
                    EditorPrefs.SetFloat(SPLITTER_PREF_KEY, _splitterPosition);

                    // Request repaint
                    _onRepaint?.Invoke();
                    Event.current.Use();
                }
                break;

            case EventType.MouseUp:
                if (_isDraggingSplitter)
                {
                    _isDraggingSplitter = false;
                    Event.current.Use();
                }
                break;
        }
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

                    // Save to EditorPrefs for persistence
                    string assetPath = AssetDatabase.GetAssetPath(_availableAssets[_selectedAssetIndex]);
                    EditorPrefs.SetString(LAST_SEQUENCE_PREF_KEY, assetPath);
                }
            }
        }
        else
        {
            EditorGUILayout.LabelField("No training sequence assets found in project");
        }

        // Use Controller Asset button
        if (GUILayout.Button("Use Controller Asset", GUILayout.Width(140)))
        {
            UseControllerAsset();
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
    /// Priority: 1) ModularTrainingController asset, 2) Last seen asset, 3) First asset
    /// </summary>
    public void LoadAvailableTrainingAssets()
    {
        _availableAssets = TrainingSequenceAssetManager.LoadAllSequenceAssets();
        _assetsLoaded = true;

        if (_availableAssets.Length > 0 && _currentTrainingAsset == null)
        {
            // Priority 1: Check ModularTrainingController in scene
            var controller = Object.FindObjectOfType<ModularTrainingSequenceController>();
            if (controller != null && controller.trainingAsset != null)
            {
                int index = System.Array.FindIndex(_availableAssets,
                    asset => asset == controller.trainingAsset);
                if (index >= 0)
                {
                    _selectedAssetIndex = index;
                    LoadTrainingAsset(_availableAssets[index]);
                    Debug.Log($"[SequenceTab] Loaded sequence from ModularTrainingController: {controller.trainingAsset.name}");
                    return;
                }
            }

            // Priority 2: Check EditorPrefs for last seen sequence
            string lastAssetPath = EditorPrefs.GetString(LAST_SEQUENCE_PREF_KEY, "");
            if (!string.IsNullOrEmpty(lastAssetPath))
            {
                int index = System.Array.FindIndex(_availableAssets,
                    asset => AssetDatabase.GetAssetPath(asset) == lastAssetPath);
                if (index >= 0)
                {
                    _selectedAssetIndex = index;
                    LoadTrainingAsset(_availableAssets[index]);
                    Debug.Log($"[SequenceTab] Loaded last seen sequence: {_availableAssets[index].name}");
                    return;
                }
            }

            // Priority 3: Default to first asset
            _selectedAssetIndex = 0;
            LoadTrainingAsset(_availableAssets[0]);
            Debug.Log($"[SequenceTab] Defaulted to first sequence: {_availableAssets[0].name}");
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
            // Clear reorderable list caches for new asset
            _treeView.ClearReorderableListCaches();
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
    /// Load the asset assigned to ModularTrainingController in the scene
    /// </summary>
    private void UseControllerAsset()
    {
        var controller = Object.FindObjectOfType<ModularTrainingSequenceController>();
        if (controller != null && controller.trainingAsset != null)
        {
            // Ensure assets are loaded
            if (!_assetsLoaded)
            {
                LoadAvailableTrainingAssets();
            }

            int index = System.Array.FindIndex(_availableAssets,
                asset => asset == controller.trainingAsset);
            if (index >= 0)
            {
                _selectedAssetIndex = index;
                LoadTrainingAsset(_availableAssets[index]);

                // Save to EditorPrefs
                string assetPath = AssetDatabase.GetAssetPath(_availableAssets[index]);
                EditorPrefs.SetString(LAST_SEQUENCE_PREF_KEY, assetPath);

                Debug.Log($"[SequenceTab] Loaded asset from controller: {controller.trainingAsset.name}");
            }
            else
            {
                EditorUtility.DisplayDialog("Asset Not Found",
                    "Controller's training asset not found in project assets.\n\n" +
                    $"Controller has: {controller.trainingAsset.name}\n" +
                    "Make sure this asset exists in your Assets folder.",
                    "OK");
            }
        }
        else
        {
            EditorUtility.DisplayDialog("No Controller Found",
                "ModularTrainingSequenceController not found in scene or has no asset assigned.\n\n" +
                "Add a ModularTrainingSequenceController to your scene and assign a training asset to it.",
                "OK");
        }
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
