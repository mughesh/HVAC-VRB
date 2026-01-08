// GameObjectReference.cs
// Safe GameObject reference that works in both runtime and asset files
using UnityEngine;
using System.Linq;

// NO NAMESPACE - Follows existing project pattern

/// <summary>
/// Safe GameObject reference that works in both runtime and asset files
/// Uses Unity's instance ID system for reliable object references that survive name changes
/// </summary>
[System.Serializable]
public class GameObjectReference
{
    [SerializeField] private GameObject _gameObject;
    [SerializeField] private int _instanceID = 0;
    [SerializeField] private string _gameObjectName = "";
    [SerializeField] private string _hierarchyPath = ""; // Full hierarchy path for reliable lookup
    [SerializeField] private string _scenePath = "";
    [SerializeField] private bool _isValid = false;

    /// <summary>
    /// The referenced GameObject (null if not found or invalid)
    /// </summary>
    public GameObject GameObject
    {
        get
        {
            // Try to return the direct reference first
            if (_gameObject != null)
            {
                // Verify the reference is still valid (not destroyed)
                try
                {
                    var name = _gameObject.name; // This will throw if destroyed
                    return _gameObject;
                }
                catch
                {
                    _gameObject = null;
                }
            }

            // Try to find by instance ID if we have one
            if (_instanceID != 0)
            {
                #if UNITY_EDITOR
                var found = UnityEditor.EditorUtility.InstanceIDToObject(_instanceID) as GameObject;
                #else
                var found = Resources.FindObjectsOfTypeAll<GameObject>()
                    .FirstOrDefault(obj => obj.GetInstanceID() == _instanceID);
                #endif

                if (found != null)
                {
                    _gameObject = found;
                    _gameObjectName = found.name; // Update name cache
                    _isValid = true;
                    return found;
                }
            }

            // Fallback 1: Try hierarchical path search (most reliable for unique objects)
            if (!string.IsNullOrEmpty(_hierarchyPath))
            {
                var found = FindByHierarchyPath(_hierarchyPath);
                if (found != null)
                {
                    _gameObject = found;
                    _instanceID = found.GetInstanceID();
                    _gameObjectName = found.name;
                    _isValid = true;
                    return found;
                }
            }

            // Fallback 2: Try simple name search (for backward compatibility - least reliable)
            if (!string.IsNullOrEmpty(_gameObjectName))
            {
                var found = GameObject.Find(_gameObjectName);
                if (found != null)
                {
                    _gameObject = found;
                    _instanceID = found.GetInstanceID();
                    _hierarchyPath = GetHierarchyPath(found); // Update hierarchy path
                    _isValid = true;
                    return found;
                }
            }

            _isValid = false;
            return null;
        }
        set
        {
            _gameObject = value;
            _instanceID = value != null ? value.GetInstanceID() : 0;
            _gameObjectName = value != null ? value.name : "";
            _hierarchyPath = value != null ? GetHierarchyPath(value) : "";
            _scenePath = value != null && value.scene.IsValid() ? value.scene.path : "";
            _isValid = value != null;
        }
    }

    /// <summary>
    /// Whether this reference is valid and points to an existing GameObject
    /// </summary>
    public bool IsValid => GameObject != null;

    /// <summary>
    /// Name of the referenced GameObject (even if the reference is broken)
    /// </summary>
    public string GameObjectName => _gameObjectName;

    /// <summary>
    /// Scene path where the GameObject was found (for debugging)
    /// </summary>
    public string ScenePath => _scenePath;

    public GameObjectReference()
    {
        _gameObject = null;
        _gameObjectName = "";
        _hierarchyPath = "";
        _scenePath = "";
        _isValid = false;
    }

    public GameObjectReference(GameObject gameObject)
    {
        GameObject = gameObject;
    }

    /// <summary>
    /// Implicit conversion from GameObject
    /// </summary>
    public static implicit operator GameObjectReference(GameObject gameObject)
    {
        return new GameObjectReference(gameObject);
    }

    /// <summary>
    /// Implicit conversion to GameObject
    /// </summary>
    public static implicit operator GameObject(GameObjectReference reference)
    {
        return reference?.GameObject;
    }

    /// <summary>
    /// Refreshes the reference, useful when objects may have been renamed or moved
    /// </summary>
    public void RefreshReference()
    {
        // Force a lookup through the property getter
        var current = GameObject;
        if (current != null)
        {
            // Update cached data in case object changed
            _gameObjectName = current.name;
            _hierarchyPath = GetHierarchyPath(current);
            _instanceID = current.GetInstanceID();
        }
    }

    /// <summary>
    /// Returns display name for editor UI
    /// </summary>
    public override string ToString()
    {
        var obj = GameObject; // Use the property to get the latest reference
        if (obj != null)
            return obj.name;
        if (!string.IsNullOrEmpty(_gameObjectName))
            return $"{_gameObjectName} (Missing)";
        return "None";
    }

    /// <summary>
    /// Gets the full hierarchy path of a GameObject (e.g., "Parent/Child/Object")
    /// </summary>
    private static string GetHierarchyPath(GameObject obj)
    {
        if (obj == null) return "";

        string path = obj.name;
        Transform current = obj.transform.parent;

        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }

        return path;
    }

    /// <summary>
    /// Finds a GameObject by its full hierarchy path
    /// </summary>
    private static GameObject FindByHierarchyPath(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;

        // Split the path into parts
        string[] parts = path.Split('/');
        if (parts.Length == 0) return null;

        // Find all root objects with the first name
        GameObject[] rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        GameObject current = null;

        foreach (var root in rootObjects)
        {
            if (root.name == parts[0])
            {
                current = root;
                break;
            }
        }

        if (current == null) return null;

        // Traverse down the hierarchy
        for (int i = 1; i < parts.Length; i++)
        {
            Transform child = current.transform.Find(parts[i]);
            if (child == null) return null;
            current = child.gameObject;
        }

        return current;
    }
}
