using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MapManager))]
public class MapManagerEditorWindow : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        MapManager mapManager = (MapManager)target;
    }

    private void OnEnable() {
        MapManager.RefreshMaps();
    }
}