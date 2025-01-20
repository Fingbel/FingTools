using UnityEditor;
using UnityEngine;
using System;

#if UNITY_EDITOR
namespace FingTools.Internal
{
    [CustomEditor(typeof(ActorModelController))]
    public class ActorModelControllerEditor : Editor
    {
        private SerializedProperty actorSO;
        private Actor_SO[] availableActors;
        private string[] actorNames;
        private int selectedActorIndex;
        private Vector2 scrollPosition;
        
        private void OnEnable()
        {
            actorSO = serializedObject.FindProperty("actor_SO");
            ActorEditorWindow.OnActorAvailableUpdated += UpdateAvailableActors;

            UpdateAvailableActors();

            // Set the initial selected index if the current actor is already assigned
            if (actorSO.objectReferenceValue != null)
            {
                Actor_SO currentActor = (Actor_SO)actorSO.objectReferenceValue;
                selectedActorIndex = Array.FindIndex(availableActors, actor => actor == currentActor);
            }
            else
            {
                selectedActorIndex = -1; // No actor selected
            }

            // Subscribe to the update event
            EditorApplication.update += OnEditorUpdate;
            ActorEditorWindow.OnActorUpdated += Redraw;       
        }

        private void Redraw(Actor_SO sO)
        {
            ActorModelController baseController = (ActorModelController)target;            
            if(actorSO?.objectReferenceValue?.name == sO?.name)
            {
                baseController.UpdatePreviewSprites(); //EDITOR               
                baseController.ApplyPrebuiltLibraries(availableActors[selectedActorIndex]);//RUNTIME                    
            }
        }

        private void OnDisable()
        {
            ActorEditorWindow.OnActorAvailableUpdated -= UpdateAvailableActors;
            // Unsubscribe from the update event
            EditorApplication.update -= OnEditorUpdate;
            ActorEditorWindow.OnActorUpdated -= Redraw;
        }

        private void OnEditorUpdate()
        {
            
            // Check if the available actors need to be updated
            var previousActorCount = availableActors.Length;
            UpdateAvailableActors();
            if (availableActors.Length != previousActorCount)
            {
                Repaint(); // Force the inspector to repaint
            }
        }

        private void UpdateAvailableActors()
        {
            ActorModelController baseController = (ActorModelController)target;            
           
            // Load all Actor_SO objects from Resources/FingTools/Actors
            availableActors = Resources.LoadAll<Actor_SO>("FingTools/Actors");    
                    
            // Create an array of actor names for display in the dropdown
            actorNames = new string[availableActors.Length];
            for (int i = 0; i < availableActors.Length; i++)
            {
                actorNames[i] = availableActors[i].name;
            }

            Actor_SO currentActor = (Actor_SO)actorSO.objectReferenceValue;
            if(!Array.Find(availableActors,actor => actor == currentActor))
            {
                baseController.ClearPreviewSprites();
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Select an Actor", EditorStyles.boldLabel);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(100));

            if (availableActors.Length > 0)
            {
                int newSelectedActorIndex = EditorGUILayout.Popup(selectedActorIndex, actorNames);
                if (newSelectedActorIndex >= 0 && newSelectedActorIndex < availableActors.Length)
                {
                    selectedActorIndex = newSelectedActorIndex;
                    actorSO.objectReferenceValue = availableActors[selectedActorIndex];
                                                            
                    Redraw(availableActors[selectedActorIndex]);
                }
                
            }
            else
            {
                EditorGUILayout.LabelField("No Actors Found.");
            }

            EditorGUILayout.EndScrollView();
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif