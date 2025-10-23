using UnityEditor;
using UnityEngine;
using FingTools.Internal;
using UnityEngine.UIElements;

namespace FingTools.Internal.Editor
{
    [InitializeOnLoad]
    public static class NavGridSceneOverlay
    {
        const string PREF_KEY = "FingTools.NavGridOverlayEnabled";
        const string PREF_ALPHA_WALK = "FingTools.NavGridOverlayAlphaWalk";
        const string PREF_ALPHA_BLOCK = "FingTools.NavGridOverlayAlphaBlock";
    const string PREF_SHOW_OUTLINES = "FingTools.NavGridOverlayShowOutlines";
    const string PREF_OUTLINE_COLOR = "FingTools.NavGridOverlayOutlineColor";
    const string PREF_OUTLINE_WIDTH = "FingTools.NavGridOverlayOutlineWidth";
        const float DEFAULT_ALPHA_WALK = 0.25f;
        const float DEFAULT_ALPHA_BLOCK = 0.5f;
    const bool DEFAULT_SHOW_OUTLINES = true;
    const string DEFAULT_OUTLINE_COLOR = "#000000FF";
    const float DEFAULT_OUTLINE_WIDTH = 1f;
        static NavGridSceneOverlay()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }

        public static bool IsEnabled
        {
            get => EditorPrefs.GetBool(PREF_KEY, false);
            set { EditorPrefs.SetBool(PREF_KEY, value); SceneView.RepaintAll(); }
        }

        static float WalkAlpha
        {
            get => EditorPrefs.GetFloat(PREF_ALPHA_WALK, DEFAULT_ALPHA_WALK);
            set { EditorPrefs.SetFloat(PREF_ALPHA_WALK, Mathf.Clamp01(value)); SceneView.RepaintAll(); }
        }

        static float BlockAlpha
        {
            get => EditorPrefs.GetFloat(PREF_ALPHA_BLOCK, DEFAULT_ALPHA_BLOCK);
            set { EditorPrefs.SetFloat(PREF_ALPHA_BLOCK, Mathf.Clamp01(value)); SceneView.RepaintAll(); }
        }

        static bool ShowOutlines
        {
            get => EditorPrefs.GetBool(PREF_SHOW_OUTLINES, DEFAULT_SHOW_OUTLINES);
            set { EditorPrefs.SetBool(PREF_SHOW_OUTLINES, value); SceneView.RepaintAll(); }
        }

        static Color OutlineColor
        {
            get
            {
                string hex = EditorPrefs.GetString(PREF_OUTLINE_COLOR, DEFAULT_OUTLINE_COLOR);
                Color c;
                if (ColorUtility.TryParseHtmlString(hex, out c)) return c;
                return Color.black;
            }
            set { EditorPrefs.SetString(PREF_OUTLINE_COLOR, "#" + ColorUtility.ToHtmlStringRGBA(value)); SceneView.RepaintAll(); }
        }

        static float OutlineWidth
        {
            get => EditorPrefs.GetFloat(PREF_OUTLINE_WIDTH, DEFAULT_OUTLINE_WIDTH);
            set { EditorPrefs.SetFloat(PREF_OUTLINE_WIDTH, Mathf.Max(0f, value)); SceneView.RepaintAll(); }
        }

        public static void Toggle()
        {
            IsEnabled = !IsEnabled;
        }

        static void OnSceneGUI(SceneView sv)
        {
            if (!IsEnabled) return;

            Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
            // Compute base Y so the overlay snaps under the floating toolbar when possible
            float toolbarHeight = GetFloatingToolbarHeight(sv);
            float baseY = Mathf.Max(8f, toolbarHeight + 24f);

            // Draw a small overlay label
            Handles.BeginGUI();
            GUILayout.BeginArea(new Rect(8, baseY, 360, 140), EditorStyles.helpBox);
            GUILayout.Label("FingTools NavGrid Overlay", EditorStyles.boldLabel);
            GUILayout.Space(4);
            EditorGUI.BeginChangeCheck();
            float wa = EditorGUILayout.Slider("Walkable alpha", WalkAlpha, 0f, 1f);
            float ba = EditorGUILayout.Slider("Blocked alpha", BlockAlpha, 0f, 1f);
            bool so = EditorGUILayout.Toggle("Show outlines", ShowOutlines);
            Color oc = OutlineColor;
            oc = EditorGUILayout.ColorField("Outline color", oc);
            float ow = EditorGUILayout.FloatField("Outline width", OutlineWidth);
            if (EditorGUI.EndChangeCheck())
            {
                WalkAlpha = wa;
                BlockAlpha = ba;
                ShowOutlines = so;
                OutlineColor = oc;
                OutlineWidth = ow;
            }
            GUILayout.EndArea();
            Handles.EndGUI();

            if (MapManager.Instance == null) return;
            string mapName = MapManager.Instance.LoadedMapObject;
            if (string.IsNullOrEmpty(mapName))
            {
                Handles.BeginGUI();
                GUILayout.BeginArea(new Rect(8, baseY + 48, 320, 32));
                GUILayout.Label("No map loaded.");
                GUILayout.EndArea();
                Handles.EndGUI();
                return;
            }

            string path = $"Assets/MapData/NavGrids/{mapName}_NavGrid.asset";
            var grid = AssetDatabase.LoadAssetAtPath<NavGrid>(path);
            if (grid == null)
            {
                Handles.BeginGUI();
                GUILayout.BeginArea(new Rect(8, baseY + 48, 320, 80), EditorStyles.helpBox);
                GUILayout.Label($"NavGrid for '{mapName}' not found.");
                if (GUILayout.Button("Open Bake NavGrid Window"))
                {
                    EditorApplication.ExecuteMenuItem("FingTools/Navigation/Bake NavGrid");
                }
                GUILayout.EndArea();
                Handles.EndGUI();
                return;
            }

            if (!grid.IsValid) return;

            int w = grid.width;
            int h = grid.height;
            if (w * h > 20000)
            {
                Handles.BeginGUI();
                GUILayout.BeginArea(new Rect(8, baseY + 140, 320, 32));
                GUILayout.Label($"Grid too large to display ({w}x{h}).");
                GUILayout.EndArea();
                Handles.EndGUI();
                return;
            }

            Color walkableColor = new Color(0f, 1f, 0f, WalkAlpha);
            Color blockedColor = new Color(1f, 0f, 0f, BlockAlpha);

            float halfX = grid.cellSize.x * 0.5f;
            float halfY = grid.cellSize.y * 0.5f;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    bool walk = grid.IsWalkableCell(x, y);
                    Vector2 center = grid.CellCenterWorld(x, y);
                    Vector3 bl = new Vector3(center.x - halfX, center.y - halfY, 0f);
                    Vector3 br = new Vector3(center.x + halfX, center.y - halfY, 0f);
                    Vector3 tr = new Vector3(center.x + halfX, center.y + halfY, 0f);
                    Vector3 tl = new Vector3(center.x - halfX, center.y + halfY, 0f);
                    Color fill = walk ? walkableColor : blockedColor;
                    var verts = new Vector3[] { bl, br, tr, tl };
                    Handles.DrawSolidRectangleWithOutline(verts, fill, ShowOutlines ? OutlineColor : Color.clear);
                    if (ShowOutlines)
                    {
                        // optionally stroke width can be emulated with multiple draws (Unity Handles has no width param for polygon outline)
                        Handles.color = OutlineColor;
                        Handles.DrawAAPolyLine(OutlineWidth, new Vector3[] { bl, br, tr, tl, bl });
                        Handles.color = Color.white;
                    }
                }
            }
        }

        static float GetFloatingToolbarHeight(SceneView sv)
        {
            try
            {
                var root = sv.rootVisualElement;
                if (root != null)
                {
                    // Try to find our toolbar by class name or known element id
                    // We search for the first element that looks like a toolbar (has name or USS class containing 'toolbar' or 'FingToolbar')
                    var matches = root.Query<VisualElement>(null, "toolbar").ToList();
                    if (matches != null && matches.Count > 0)
                    {
                        // take the first visible one
                        foreach (var m in matches)
                        {
                            if (m.resolvedStyle.visibility == Visibility.Visible)
                                return m.layout.height;
                        }
                        return matches[0].layout.height;
                    }

                    // fallback: try by name
                    var named = root.Query<VisualElement>(name: "FingToolbar").ToList();
                    if (named != null && named.Count > 0)
                        return named[0].layout.height;
                }
            }
            catch { }

            // final fallback to a reasonable toolbar height
            try { return EditorStyles.toolbar.fixedHeight > 0 ? EditorStyles.toolbar.fixedHeight : 28f; } catch { return 28f; }
        }
    }
}
