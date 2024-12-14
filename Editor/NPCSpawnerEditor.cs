using UnityEngine;
using UnityEditor;
using FingTools.Lime;

#if UNITY_EDITOR
[CustomEditor(typeof(NPCSpawner))]
public class NPCSpawnerEditor : Editor {
    private SerializedProperty npcTemplateProperty;

    private void OnEnable() {
        npcTemplateProperty = serializedObject.FindProperty("npcTemplate");
    }
    public override void OnInspectorGUI() {
        base.OnInspectorGUI();
        // Check if the npcTemplate is assigned
        if (npcTemplateProperty.objectReferenceValue == null)
        {
            if (GUILayout.Button("Create NPC Actor"))
                ActorEditorWindow.ShowWindow(RemoveNPCPrefix(target.name), (NPCSpawner)target);                      
        }
        else
        {
            if (GUILayout.Button("Edit NPC Actor"))            
                ActorEditorWindow.SetActorToPreview(npcTemplateProperty.objectReferenceValue as Actor_SO);
        }
    }

    private string RemoveNPCPrefix(string name)
    {
        // Assuming "_NPC" is the prefix, remove it
        if (name.StartsWith("NPC_"))
        {
            return name.Substring(4); // Remove the first 4 characters ("_NPC")
        }
        return name;
    }
}
#endif