using System;
using BDeshi.Logging;
using NaughtyAttributes;
using Newtonsoft.Json;
using Studio23.SS2.InventorySystem.Data;
using Studio23.SS2.ObjectiveSystem.Data;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Studio23.SS2.ObjectiveSystem.Core
{
    [CreateAssetMenu(menuName = "Studio-23/Objective System/Task", fileName = "objective task")]
    [Serializable]
    public class ObjectiveTask : ItemBase, ISubCategoryLoggerMixin<ObjectiveLogCategory>
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

    
[Button(enabledMode:EButtonEnableMode.Playmode)]
        public void AddTask()
        {
            if (!ObjectiveManager.Instance.IsObjectiveActiveAndValid(_parentObjective))
            {
                Logger.LogWarning(ObjectiveLogCategory.Task,$"can't add task {this} because parent objective is not active and valid");
                return;
            }
            if(_state != ObjectiveTaskState.NotStarted)
            {
                Logger.LogWarning(ObjectiveLogCategory.Task,$"can't add task {this} because it has already started");
                return;
            }

            Logger.Log(ObjectiveLogCategory.Task,$"Add task {this}", this);
            _state = ObjectiveTaskState.InProgress;
            OnTaskActiveToggle?.Invoke(this);
        }
[Button(enabledMode:EButtonEnableMode.Playmode)]
        public void RemoveTask()
        {
            if (!ObjectiveManager.Instance.IsObjectiveActiveAndValid(_parentObjective))
            {
                Logger.LogWarning(ObjectiveLogCategory.Task,$"can't remove task {this} because parent objective is not active and valid");
                return;
            }
            if(_state == ObjectiveTaskState.NotStarted)
            {
                Logger.LogWarning(ObjectiveLogCategory.Task,$"can't remove task {this} because it hasn't been started");
                return;
            }
            Logger.Log(ObjectiveLogCategory.Task,$"Remove task {this}", this);

            _state = ObjectiveTaskState.NotStarted;
            OnTaskActiveToggle?.Invoke(this);
            OnTaskCompletionToggle?.Invoke(this);
        }
[Button(enabledMode:EButtonEnableMode.Playmode)]
        public void ResetProgress()
        {
            _state = ObjectiveTaskState.NotStarted;
            OnTaskActiveToggle?.Invoke(this);
            OnTaskCompletionToggle?.Invoke(this);
        }
[Button(enabledMode:EButtonEnableMode.Playmode)]
        public void CompleteTask()
        {
            if (!ObjectiveManager.Instance.IsObjectiveActiveAndValid(_parentObjective))
            {
                Logger.LogWarning(ObjectiveLogCategory.Task,$"can't complete task {this} because parent objective is not active and valid");
                return;
            }
            if(_state != ObjectiveTaskState.InProgress)
            {
                Logger.LogWarning(ObjectiveLogCategory.Task,$"can't complete task {this} because it isn't in progress");
                return;
            }
            Logger.Log(ObjectiveLogCategory.Task,$"Complete task {this}", this);
            
            _state = ObjectiveTaskState.Completed;
            OnTaskCompletionToggle?.Invoke(this);
        }


        public void FullReset()
        {
            ResetProgress();
            CompleteParentObjectiveOnCompletion = false;
            OnTaskCompletionToggle = null;
            OnTaskActiveToggle = null;
        }
# region save/load
        public override void AssignSerializedData(string data)
        {
            _state = JsonConvert.DeserializeObject<ObjectiveTaskState>(data);
        }

        public override string GetSerializedData()
        {
            return JsonConvert.SerializeObject(_state);
        }
# endregion
#if UNITY_EDITOR
        [Button]
        public void Rename()
        {
            Rename(this.Name);
        }
        
        public void Rename(string newName)
        {
            this.name = newName;
            AssetDatabase.SaveAssets();
            EditorUtility.SetDirty(this);
        }
        
        [Button]
        public void DestroyTask()
        {
            Undo.RecordObject(_parentObjective, "remove");
            _parentObjective.Tasks.Remove(this);
            EditorUtility.SetDirty(_parentObjective);
            Undo.DestroyObjectImmediate(this);
            AssetDatabase.SaveAssetIfDirty(_parentObjective);
        }
#endif

        public override string ToString()
        {
            return $"{Name} {_state}";
        }

        public GameObject gameObject => ObjectiveManager.Instance.gameObject;
        public ICategoryLogger<ObjectiveLogCategory> Logger => ObjectiveManager.Instance.Logger;
        public ObjectiveLogCategory Category => ObjectiveLogCategory.Task;
    }
}