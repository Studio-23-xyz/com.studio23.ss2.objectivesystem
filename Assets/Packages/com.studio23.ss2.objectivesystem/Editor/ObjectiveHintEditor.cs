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
                Rename(hint);
            if (GUILayout.Button("Destroy Hint"))
                DestroyHint(hint);
            
            if (EditorApplication.isPlaying)
            {
                GUILayout.Space(5f);

                GUILayout.Label("Playmode Only");
                
                GUILayout.Space(5f);
                
                if (GUILayout.Button("Add")) 
                    hint.Add();
                
                
            }
        }
        

    

        public void Rename(ObjectiveHint hintBase)
        {
            Rename(ObjectiveBaseEditor.getFullHintAssetName(hintBase.ParentObjective, hintBase.Name));
        }

        public void DestroyHint(ObjectiveHint hint)
        {
            if (hint.ParentObjective != null) 
            {
                Undo.RecordObject(hint.ParentObjective, "Hint");
                hint.ParentObjective.Hints.Remove(hint);
            }

            EditorUtility.SetDirty(hint.ParentObjective);
            Undo.DestroyObjectImmediate(hint);
            AssetDatabase.SaveAssetIfDirty(hint.ParentObjective);
        }

        public void Rename(string newName)
        {
            this.name = newName;

            AssetDatabase.SaveAssets();
            EditorUtility.SetDirty(this);
        }
    }
}