using Studio23.SS2.ObjectiveSystem.Core;
using UnityEditor;
using UnityEngine;

namespace Studio23.SS2.ObjectiveSystem.Editor
{
    [CustomEditor(typeof(ObjectiveTask))]
    public class ObjectiveTaskEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            ObjectiveTask task = (ObjectiveTask)target;

            GUILayout.Space(10f);
            
            if (GUILayout.Button("Rename"))
                task.Rename();
            
            if (GUILayout.Button("Destroy Task"))
                task.DestroyTask();

            if (EditorApplication.isPlaying)
            {
                GUILayout.Space(10f);
                GUILayout.Label("Playmode Only");
                
                if (GUILayout.Button("Add Task"))
                    task.AddTask();
                if (GUILayout.Button("Remove Task"))
                    task.RemoveTask();
                if (GUILayout.Button("Reset Progress"))
                    task.ResetProgress();
                if (GUILayout.Button("Complete Task"))
                    task.CompleteTask();
            }
        }
    }
}
