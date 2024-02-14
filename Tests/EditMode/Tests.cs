using System.Collections.Generic;
using NUnit.Framework;
using Studio23.SS2.ObjectiveSystem.Core;
using Studio23.SS2.ObjectiveSystem.Data;
using UnityEditor;
using UnityEngine;

namespace Studio23.SS2.ObjectiveSystem.Tests.EditMode
{
    public class Tests
    {
        List<ObjectiveBase> _objectives;
        List<ObjectiveTask> _tasks;
        List<ObjectiveHint> _hints;

        private string _objectivePath = "Assets/Packages/com.studio23.ss2.objectivesystem/Editor/TestResources/";
        [SetUp]
        public void SetUp()
        {
            var go = new GameObject();
            var om = go.AddComponent<ObjectiveManager>();
            om.InitializeAsInstanceForTests();
            
            _objectives = new List<ObjectiveBase>();
            _tasks = new List<ObjectiveTask>();
            _hints = new List<ObjectiveHint>();
            LoadScriptables(_objectives ,"TestObjectives" );
            LoadScriptables(_tasks ,"TestTasks" );
            LoadScriptables(_hints ,"TestHints" );
        }

        void LoadScriptables<T>(List<T> list, string folderName) where T: ScriptableObject
        {
            string[] assetNames = AssetDatabase.FindAssets("", new[] { _objectivePath+folderName });
            
            foreach (string soName in assetNames)
            {
                var sOpath = AssetDatabase.GUIDToAssetPath(soName);
                var so = AssetDatabase.LoadAssetAtPath<T>(sOpath);
                
                list.Add(so);
            }
        }

        [Test, Order(0)]
        public void CheckSingleton()
        {
            Assert.NotNull(ObjectiveManager.Instance);
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(ObjectiveManager.Instance.gameObject);

            foreach (var objectiveBase in _objectives)
            {
                objectiveBase.FullReset();
            }
        }
        [Test]
        public void activating_tasks_addsTask_to_objective()
        {
            var objective1 = _objectives[0];
            var task1 = _tasks[0];
            var task2 = _tasks[1];
            var task3 = _tasks[2];
            // Use the Assert class to test conditions.
            task1.FullReset();
            task2.FullReset();
            task3.FullReset();
            
            objective1.FullReset();
            Assert.IsEmpty(objective1.Tasks);
            objective1.Tasks.Add(task1);
            objective1.Tasks.Add(task2);
            objective1.Tasks.Add(task3);
            objective1.StartObjective();
            Assert.IsEmpty(objective1.ActiveTasks);
            Assert.IsTrue(objective1.IsActive);

            task1.AddTask();
            Assert.AreEqual(1, objective1.ActiveTasks.Count);
            task2.AddTask();
            Assert.AreEqual(2, objective1.ActiveTasks.Count);
            task3.AddTask();
            Assert.AreEqual(3, objective1.ActiveTasks.Count);
            
            Assert.IsTrue(task1.IsActive);            
            Assert.IsTrue(task2.IsActive);            
            Assert.IsTrue(task3.IsActive);       
            Assert.Contains(task1, objective1.ActiveTasks);
            Assert.Contains(task2, objective1.ActiveTasks);
            Assert.Contains(task3, objective1.ActiveTasks);
            
            Assert.IsFalse(objective1.IsCompleted);
            task1.CompleteTask();
            Assert.IsFalse(objective1.IsCompleted);
            task2.CompleteTask();
            Assert.IsFalse(objective1.IsCompleted);
            task3.CompleteTask();
            Debug.Log(objective1.CheckCompletion());
            Assert.IsTrue(objective1.IsCompleted);
        }

        [Test]
        public void complete_all_tasks_completes_objective()
        {
            var objective1 = _objectives[0];
            var task1 = _tasks[0];
            var task2 = _tasks[1];
            var task3 = _tasks[2];
            // Use the Assert class to test conditions.
            task1.FullReset();
            task2.FullReset();
            task3.FullReset();
            
            objective1.FullReset();
            objective1.Tasks.Add(task1);
            objective1.Tasks.Add(task2);
            objective1.Tasks.Add(task3);
            objective1.StartObjective();
            
            task1.AddTask();
            task2.AddTask();
            task3.AddTask();
            
            Assert.IsFalse(objective1.IsCompleted);
            task1.CompleteTask();
            Assert.IsFalse(objective1.IsCompleted);
            task2.CompleteTask();
            Assert.IsFalse(objective1.IsCompleted);
            task3.CompleteTask();
            Assert.IsTrue(objective1.IsCompleted);
        }
        
        [Test]
        public void complete_parentComplete_tasks_completes_objective()
        {
            var objective1 = _objectives[0];
            var task1 = _tasks[0];
            var task2 = _tasks[1];
            var task3 = _tasks[2];
            task1.FullReset();
            task2.FullReset();
            task3.FullReset();
            
            task3.CompleteParentObjectiveOnCompletion = true;
            
            objective1.FullReset();
            objective1.Tasks.Add(task1);
            objective1.Tasks.Add(task2);
            objective1.Tasks.Add(task3);
            objective1.StartObjective();
            
            task1.AddTask();
            task2.AddTask();
            task3.AddTask();
            
            Assert.IsFalse(objective1.IsCompleted);
            task3.CompleteTask();
            Assert.IsTrue(objective1.IsCompleted);
            
            task3.RemoveTask();
            Assert.IsFalse(objective1.IsCompleted);

            task1.CompleteTask();
            Assert.IsFalse(objective1.IsCompleted);
            task2.CompleteTask();
            Assert.IsFalse(objective1.IsCompleted);
            task3.AddTask();
            task3.CompleteTask();
            Assert.IsTrue(objective1.IsCompleted);
            
            task3.ResetProgress();
            Assert.IsFalse(objective1.IsCompleted);
        }
        
        [Test]
        public void maintain_objective_completion_from_parentCompleteTask_after_complete_nonParentCompleteTask()
        {
            var objective1 = _objectives[0];
            var task1 = _tasks[0];
            var task2 = _tasks[1];
            var task3 = _tasks[2];
            task1.FullReset();
            task2.FullReset();
            task3.FullReset();
            
            task3.CompleteParentObjectiveOnCompletion = true;
            
            objective1.FullReset();
            objective1.Tasks.Add(task1);
            objective1.Tasks.Add(task2);
            objective1.Tasks.Add(task3);
            objective1.StartObjective();
            
            task1.AddTask();
            task2.AddTask();
            task3.AddTask();
            
            Assert.IsFalse(objective1.IsCompleted);
            task3.CompleteTask();
            Assert.IsTrue(objective1.IsCompleted);
            
            task1.CompleteTask();
            Assert.IsTrue(objective1.IsCompleted);

            task2.CompleteTask();
            Assert.IsTrue(objective1.IsCompleted);
            
            objective1.Cleanup();
        }

        [Test]
        public void maintain_objective_completion_from_parentCompleteTask_after_cancel_nonParentCompleteTask()
        {
            var objective1 = _objectives[0];
            var task1 = _tasks[0];
            var task2 = _tasks[1];
            var task3 = _tasks[2];
            task1.FullReset();
            task2.FullReset();
            task3.FullReset();
            
            task3.CompleteParentObjectiveOnCompletion = true;
            
            objective1.FullReset();
            objective1.Tasks.Add(task1);
            objective1.Tasks.Add(task2);
            objective1.Tasks.Add(task3);
            objective1.StartObjective();
            
            task1.AddTask();
            task2.AddTask();
            task3.AddTask();
            
            Assert.IsFalse(objective1.IsCompleted);
            task3.CompleteTask();
            Assert.IsTrue(objective1.IsCompleted);
            
            task1.CompleteTask();
            Assert.IsTrue(objective1.IsCompleted);

            task2.CompleteTask();
            Assert.IsTrue(objective1.IsCompleted);
            
            task2.RemoveTask();
            Assert.IsTrue(objective1.IsCompleted);
            
            task1.RemoveTask();
            Assert.IsTrue(objective1.IsCompleted);
            
            task3.RemoveTask();
            Assert.IsFalse(objective1.IsCompleted);
        }
        
        [Test]
        public void cancelling_any_task_uncompletes_objective()
        {
            var objective1 = _objectives[0];
            var task1 = _tasks[0];
            var task2 = _tasks[1];
            var task3 = _tasks[2];
            task1.FullReset();
            task2.FullReset();
            task3.FullReset();
            
            objective1.FullReset();
            objective1.Tasks.Add(task1);
            objective1.Tasks.Add(task2);
            objective1.Tasks.Add(task3);
            objective1.StartObjective();
            
            task1.AddTask();
            task2.AddTask();
            task3.AddTask();
            
            Assert.IsFalse(objective1.IsCompleted);
            task1.CompleteTask();
            task2.CompleteTask();
            task3.CompleteTask();
            Assert.IsTrue(objective1.IsCompleted);
            
            task1.RemoveTask();
            Debug.Log(task1);
            Debug.Log(objective1);
            Assert.IsFalse(objective1.IsCompleted);
            task1.AddTask();
            task1.CompleteTask();
            Assert.IsTrue(objective1.IsCompleted);

            task2.RemoveTask();
            Assert.IsFalse(objective1.IsCompleted);
            task2.AddTask();
            task2.CompleteTask();
            Assert.IsTrue(objective1.IsCompleted);
            
            task3.RemoveTask();
            Assert.IsFalse(objective1.IsCompleted);
            task3.AddTask();
            task3.CompleteTask();
            Assert.IsTrue(objective1.IsCompleted);
        }
        
        [Test]
        public void adding_task_multiple_times_wont_duplicate()
        {
            var objective1 = _objectives[0];
            var task1 = _tasks[0];
            task1.FullReset();
            
            objective1.FullReset();
            objective1.Tasks.Add(task1);
            objective1.StartObjective();
            
            Assert.IsEmpty(objective1.ActiveTasks);
            task1.AddTask();
            Assert.IsTrue(objective1.ActiveTasks.Count == 1);
            task1.AddTask();
            Assert.IsTrue(objective1.ActiveTasks.Count == 1);
            task1.AddTask();
            task1.AddTask();
            task1.AddTask();
            Assert.IsTrue(objective1.ActiveTasks.Count == 1);
        }
        
        
        [Test]
        public void adding_hint_multiple_times_wont_duplicate()
        {
            var objective1 = _objectives[0];
            var hint1 = _hints[0];
            hint1.FullReset();
            
            objective1.FullReset();
            objective1.Hints.Add(hint1);
            objective1.StartObjective();
            
            Assert.IsEmpty(objective1.ActiveHints);
            hint1.Add();
            Assert.IsTrue(objective1.ActiveHints.Count == 1);
            hint1.Add();
            Assert.IsTrue(objective1.ActiveHints.Count == 1);
            hint1.Add();
            hint1.Add();
            hint1.Add();
            Assert.IsTrue(objective1.ActiveHints.Count == 1);
        }
        
        [Test]
        public void activatingHint_addsToActiveHints()
        {
            var objective1 = _objectives[0];
            var hint1 = _hints[0];
            var hint2 = _hints[1];
            var hint3 = _hints[2];
            hint1.FullReset();
            hint2.FullReset();
            hint3.FullReset();
            
            objective1.FullReset();
            objective1.Hints.Add(hint1);
            objective1.Hints.Add(hint2);
            objective1.Hints.Add(hint3);
            objective1.StartObjective();
            
            Assert.IsEmpty(objective1.ActiveHints);
            hint1.Add();
            hint2.Add();
            hint3.Add();
            Assert.IsTrue(objective1.ActiveHints.Count == 3);
            Assert.Contains(hint1, objective1.ActiveHints);
            Assert.Contains(hint2, objective1.ActiveHints);
            Assert.Contains(hint3, objective1.ActiveHints);
            
            hint1.Remove();
            hint2.Remove();
            Assert.IsTrue(objective1.ActiveHints.Count == 1);
            Assert.IsTrue(objective1.ActiveHints.Contains(hint3));
            Assert.IsFalse(objective1.ActiveHints.Contains(hint1));
            Assert.IsFalse(objective1.ActiveHints.Contains(hint2));
            hint3.Remove();
            Assert.IsTrue(objective1.ActiveHints.Count == 0);
            Assert.IsFalse(objective1.ActiveHints.Contains(hint1));
            Assert.IsFalse(objective1.ActiveHints.Contains(hint2));
            Assert.IsFalse(objective1.ActiveHints.Contains(hint3));
        }
    }
}