// ISequenceTreeViewCallbacks.cs
// Callback interface for SequenceTreeView events
// Part of Phase 4: Sequence Tree View extraction refactoring

#if UNITY_EDITOR

/// <summary>
/// Callback interface for SequenceTreeView events.
/// Implemented by VRInteractionSetupWindow to handle tree view actions.
/// </summary>
public interface ISequenceTreeViewCallbacks
{
    /// <summary>
    /// Called when an item is selected in the tree view
    /// </summary>
    void OnItemSelected(object item, string itemType);

    /// <summary>
    /// Called when the tree view needs to auto-save
    /// </summary>
    void OnAutoSave();

    /// <summary>
    /// Called to request a repaint
    /// </summary>
    void OnRequestRepaint();
}
#endif
