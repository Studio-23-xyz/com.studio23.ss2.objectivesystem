using System;
using System.Linq;
using System.Reflection;
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
                CreateAndAddTask(objBase);
            }

            if (GUILayout.Button("Create and Add Hint"))
            {
                CreateAndAddHint(objBase);
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
            
            // Find all derived classes of Baz
            var derivedTypes = GetDerivedTypes<ObjectiveHintBase>();

            // Draw buttons for each derived class
            foreach (var type in derivedTypes)
            {
                if (GUILayout.Button($"Create {type.Name}"))
                {
                    // Create an instance of the derived class
                    var hint = ScriptableObject.CreateInstance(type) as ObjectiveHintBase;
                    SetupHintForObjective(objBase, hint);
                    Debug.Log($"{type.Name} hint created!");
                }
            }
        }

        private static Type[] GetDerivedTypes<T>()
        {
            Type baseType = typeof(T);
            // var derivedTypes = Assembly.GetAssembly(baseType).GetTypes()
            //     .Where(t => t.IsSubclassOf(baseType) && !t.IsAbstract)
            //     .ToArray();
            var derivedTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(t => t.IsSubclassOf(baseType) && !t.IsAbstract)
                .ToArray();
            return derivedTypes;
        }


        public void CreateAndAddTask(ObjectiveBase objectiveBase)
        {
            var task = ScriptableObject.CreateInstance<ObjectiveTask>();
            task.Name = (objectiveBase.Tasks.Count + 1).ToString();
            task.name = GetTaskFullAssetName(objectiveBase, task.Name);
            task.SetObjective(objectiveBase);
            objectiveBase.Tasks.Add(task);

            AssetDatabase.AddObjectToAsset(task, objectiveBase);
            AssetDatabase.SaveAssets();
            EditorUtility.SetDirty(objectiveBase);
        }

        public string GetTaskFullAssetName(ObjectiveBase objectiveBase, string baseTaskName)
        {
            return $"{objectiveBase.name}_task_{baseTaskName}";
        }

        public void CreateAndAddHint(ObjectiveBase objectiveBase)
        {
            var hint = ScriptableObject.CreateInstance<ObjectiveHint>();
            SetupHintForObjective(objectiveBase, hint);
        }

        private void SetupHintForObjective(ObjectiveBase objectiveBase, ObjectiveHintBase hint)
        {
            hint.Name = (objectiveBase.Hints.Count + 1).ToString();
            hint.name = getFullHintAssetName(objectiveBase, hint.Name);
            objectiveBase.Hints.Add(hint);
            hint.SetObjective(objectiveBase);

            AssetDatabase.AddObjectToAsset(hint, objectiveBase);
            AssetDatabase.SaveAssets();
            EditorUtility.SetDirty(objectiveBase);
        }

        public static string getFullHintAssetName(ObjectiveBase objectiveBase, string hintBaseName)
        {
            return $"{objectiveBase.name}_hint_{hintBaseName}";
        }
    }
}
