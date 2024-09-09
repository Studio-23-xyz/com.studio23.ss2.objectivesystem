using Studio23.SS2.ObjectiveSystem.Core;
using UnityEditor;
using UnityEngine;

namespace Studio23.SS2.ObjectiveSystem.Editor
{
    public class ObjectiveHintEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            ObjectiveHint hint = (ObjectiveHint)target;
            
            GUILayout.Space(10f);

            if (GUILayout.Button("Rename"))
                hint.Rename();
            if (GUILayout.Button("Destroy Hint"))
                hint.DestroyHint();
            
            if (EditorApplication.isPlaying)
            {
                GUILayout.Space(5f);

                GUILayout.Label("Playmode Only");
                
                GUILayout.Space(5f);
                
                if (GUILayout.Button("Add")) 
                    hint.Add();
                
                
            }
        }
    }
}