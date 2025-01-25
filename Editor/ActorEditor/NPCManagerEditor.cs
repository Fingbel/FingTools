using UnityEngine;
using UnityEditor;

namespace FingTools.Internal
{
    #if UNITY_EDITOR
    [CustomEditor(typeof(NPCManager))]
    public class NPCManagerEditor : Editor {
        public override void OnInspectorGUI() {
            base.OnInspectorGUI();
            
        }
    }
    #endif
}