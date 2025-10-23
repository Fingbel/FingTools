using UnityEditor;
using UnityEngine;
using FingTools.Internal;

namespace FingTools.Internal.Editor
{
    [CustomEditor(typeof(NavGrid))]
    public class NavGridInspector : UnityEditor.Editor
    {
        NavGrid grid;

        void OnEnable()
        {
            grid = target as NavGrid;
        }

        public override void OnInspectorGUI()
        {
            if (grid == null)
            {
                DrawDefaultInspector();
                return;
            }

            EditorGUILayout.LabelField("NavGrid Summary", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Source Map", grid.sourceMapName ?? "<none>");
            EditorGUILayout.LabelField("Size", $"{grid.width} x {grid.height}");
            EditorGUILayout.LabelField("Cell Size", grid.cellSize.ToString());
            EditorGUILayout.LabelField("Agent Radius", grid.bakedAgentRadius.ToString());
            EditorGUILayout.LabelField("Walkable cells", (grid.walkable != null) ? grid.walkable.Length.ToString() : "0");

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("The full walkable array is hidden here to avoid heavy inspector rendering. Use 'Open Grid Viewer' to inspect cells.", MessageType.Info);

            if (GUILayout.Button("Open Grid Viewer"))
            {
                NavGridViewerWindow.ShowWindow(grid);
            }

            EditorGUILayout.Space();
            if (GUILayout.Button("Re-bake from selected map"))
            {
                // Attempt to find Bake tool and open it for this map
                var w = EditorWindow.GetWindow(typeof(BakeNavGridWindow));
                // user can re-bake manually; we just focus the window
            }

            // allow editing of some fields
            EditorGUI.BeginChangeCheck();
            float newRadius = EditorGUILayout.FloatField("Baked Agent Radius", grid.bakedAgentRadius);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(grid, "Change Agent Radius");
                grid.bakedAgentRadius = newRadius;
                EditorUtility.SetDirty(grid);
            }
        }
    }

    // Simple lightweight viewer window (main-thread safe)
    public class NavGridViewerWindow : EditorWindow
    {
        NavGrid grid;
        Vector2 scroll;

        public static void ShowWindow(NavGrid g)
        {
            var w = GetWindow<NavGridViewerWindow>("NavGrid Viewer");
            w.grid = g;
            w.minSize = new Vector2(300, 200);
        }

        void OnGUI()
        {
            if (grid == null)
            {
                EditorGUILayout.LabelField("No NavGrid assigned");
                return;
            }

            EditorGUILayout.LabelField($"Grid: {grid.sourceMapName}");
            EditorGUILayout.LabelField($"Size: {grid.width} x {grid.height}");

            if (grid.walkable == null)
            {
                EditorGUILayout.LabelField("Empty walkable array");
                return;
            }

            // draw a compact textual representation: rows of 0/1
            scroll = EditorGUILayout.BeginScrollView(scroll);
            int w = grid.width;
            int h = grid.height;
            GUIStyle small = new GUIStyle(EditorStyles.label) { richText = false };
            for (int y = 0; y < h; y++)
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                for (int x = 0; x < w; x++)
                {
                    bool walk = grid.IsWalkableCell(x, y);
                    sb.Append(walk ? '·' : '#');
                }
                EditorGUILayout.LabelField(sb.ToString(), small);
            }
            EditorGUILayout.EndScrollView();
        }
    }
}
