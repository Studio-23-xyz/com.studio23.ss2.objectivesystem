using System;
using NaughtyAttributes;
using Newtonsoft.Json;
using Studio23.SS2.InventorySystem.Data;
using Studio23.SS2.ObjectiveSystem.Data;
using UnityEngine;

namespace Studio23.SS2.ObjectiveSystem.Core
{
    [CreateAssetMenu(menuName = "Studio-23/Objective System/Task", fileName = "objective task")]
    [Serializable]
    public class ObjectiveTask : ItemBase
    {
        [SerializeField] ObjectiveBase _parentObjective;
        // we do not actually need to serialize this in editor
        // this will be different based on save data.
        // we prevent serialization to avoid merge conflicts
        // This is manually serialized when saved as needed.
        [ShowNonSerializedField]
        private ObjectiveTaskState _state;
        [SerializeField] int _priority;
        public int Priority => _priority;
        public ObjectiveBase ParentObjective => _parentObjective;
        [ShowNativeProperty]
        public bool IsActive => _state == ObjectiveTaskState.InProgress || _state == ObjectiveTaskState.Completed;
        [ShowNativeProperty]
        public bool IsCompleted => _state == ObjectiveTaskState.Completed;
        //this is just for the button
        public bool ObjectiveManagerExists => ObjectiveManager.Instance != null;
        public bool CompleteParentObjectiveOnCompletion = false;

        public event Action<ObjectiveTask> OnTaskCompletionToggle;
        public event Action<ObjectiveTask> OnTaskActiveToggle;

        public void SetObjective(ObjectiveBase parentObjective)
        {
            this._parentObjective = parentObjective;
        }

    
        [ShowIf("ObjectiveManagerExists")]
        [Button]
        public void AddTask()
        {
            if(_state != ObjectiveTaskState.NotStarted)
                return;
            _state = ObjectiveTaskState.InProgress;
            OnTaskActiveToggle?.Invoke(this);
        }
        [ShowIf("ObjectiveManagerExists")]
        [Button]
        public void RemoveTask()
        {
            if(_state == ObjectiveTaskState.NotStarted)
                return;
            _state = ObjectiveTaskState.NotStarted;
            OnTaskActiveToggle?.Invoke(this);
        }
        [ShowIf("ObjectiveManagerExists")]
        [Button]
        public void ResetProgress()
        {
            _state = ObjectiveTaskState.NotStarted;
            OnTaskActiveToggle?.Invoke(this);
            OnTaskCompletionToggle?.Invoke(this);
        }
        [ShowIf("ObjectiveManagerExists")][Button]
        public void CompleteTask()
        {
            if(_state != ObjectiveTaskState.InProgress)
                return;
            _state = ObjectiveTaskState.Completed;
            OnTaskCompletionToggle?.Invoke(this);
        }

        /// <summary>
        /// Use when we want to complete a task and immediately add another without allowing the objective to be completed
        /// </summary>
        /// <param name="taskToBeReplaced"></param>
        /// <param name="taskToReplaceWith"></param>
        public void CompleteAndReplaceSubtask(ObjectiveTask taskToBeReplaced, ObjectiveTask taskToReplaceWith)
        {
            taskToReplaceWith.AddTask();
            taskToBeReplaced.CompleteTask();

            OnTaskCompletionToggle?.Invoke(this);
        }

        public void FullReset()
        {
            ResetProgress();
            CompleteParentObjectiveOnCompletion = false;
            OnTaskCompletionToggle = null;
            OnTaskActiveToggle = null;
        }

        public override void AssignSerializedData(string data)
        {
            _state = JsonConvert.DeserializeObject<ObjectiveTaskState>(data);
        }

        public override string GetSerializedData()
        {
            return JsonConvert.SerializeObject(_state);
        }

        public override string ToString()
        {
            return Name +" " +  _state;
        }
    }
}