#if UNITY_EDITOR
using UnityEditor;

namespace FingTools.Tiled
{
[CustomEditor(typeof(MapManager))]
public class MapManagerEditorWindow : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        MapManager mapManager = (MapManager)target;
    }

    private void OnEnable() {
        MapManager.RefreshUniverse();
    }
}
}
#endif