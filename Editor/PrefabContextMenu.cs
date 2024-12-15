using UnityEngine;
using UnityEditor;

namespace FingTools.Internal
{
#if UNITY_EDITOR
public class PrefabContextMenu
{
    [MenuItem("GameObject/FingTools/Model Controller", false, 0)]
    private static void AddMyPrefab()
    {
        string prefabPath = "Packages/com.fingcorp.fingtools/Prefabs/Model.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

        if (prefab == null)
        {
            Debug.LogError($"Prefab not found at the specified path: {prefabPath}");
            return;
        }

        // Instantiate the prefab in the scene
        GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        if (instance != null)
        {
            // Optionally, set the position of the new instance
            instance.transform.position = Vector3.zero; // Change to desired position

            // Parent the new instance to the currently selected GameObject
            Transform parent = Selection.activeGameObject.transform;
            instance.transform.SetParent(parent);

            // Optionally, reset local position if desired
            instance.transform.localPosition = Vector3.zero; // Change to desired local position

            Selection.activeGameObject = instance; // Select the new instance in the hierarchy
        }
    }

    // This method ensures the menu item is only available when a GameObject is selected
    [MenuItem("GameObject/FingTools/Model Controller", true)]
    private static bool ValidateAddMyPrefab()
    {
        return Selection.activeGameObject != null; // Enable the menu item only if a GameObject is selected
    }
}
#endif
}