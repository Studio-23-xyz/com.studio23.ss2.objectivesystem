using Studio23.SS2.ObjectiveSystem.Core;
using UnityEditor;
using UnityEngine;

namespace Studio23.SS2.ObjectiveSystem.Editor
{
    [CustomEditor(typeof(ObjectiveBase))]
    public class ObjectiveBaseEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            ObjectiveBase objBase = (ObjectiveBase)target;

            GUILayout.Space(10f);

            if (GUILayout.Button("Create and Add Task"))
            {
                objBase.CreateAndAddTask();
            }

            if (GUILayout.Button("Create and Add Hint"))
            {
                objBase.CreateAndAddHint();
            }

            if (GUILayout.Button("Reset"))
            {
                objBase.Reset();
            }
            
            if (EditorApplication.isPlaying)
            {
                GUILayout.Space(10f);

                GUILayout.Label("PlayMode Only");
                
                if (GUILayout.Button("Start Objective"))
                    objBase.StartObjective();
                
                if (GUILayout.Button("End Objective"))
                    objBase.EndObjective();
                
                if (GUILayout.Button("Complete Objective"))
                    objBase.CompleteObjective();
                
                if (GUILayout.Button("Cancel Objective Completion"))
                    objBase.CancelObjectiveCompletion();
            }
        }
    }
}
