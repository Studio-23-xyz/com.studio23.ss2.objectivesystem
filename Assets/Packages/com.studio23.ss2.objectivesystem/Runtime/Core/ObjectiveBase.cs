using System;
using System.Collections.Generic;
using NaughtyAttributes;
using Newtonsoft.Json;
using Studio23.SS2.InventorySystem.Data;
using Studio23.SS2.ObjectiveSystem.Data;
# if UNITY_EDITOR
using UnityEditor;
# endif
using UnityEngine;
using UnityEngine.Serialization;

namespace Studio23.SS2.ObjectiveSystem.Core
{
    [CreateAssetMenu(menuName = "Studio-23/Objective System/Basic Objective", fileName = "Basic Objective")]
    [Serializable]
    public class ObjectiveBase : ItemBase
    {
        // we do not actually need to serialize this in editor
        // this will be different based on save data.
        // we prevent serialization to avoid merge conflicts
        // This is manually serialized when saved as needed.
        [ShowNonSerializedField] private ObjectiveState _state;
        public ObjectiveState State => _state;
        public string ObjectiveUITitle => Name;
        public string ObjectiveUIDesc => Description;
        [ShowNativeProperty]
        public bool CanStart => _state == ObjectiveState.NotStarted;
        [ShowNativeProperty]
        public bool IsActive => _state == ObjectiveState.InProgress || _state == ObjectiveState.Complete;
        [ShowNativeProperty]
        public bool IsCompleted => _state == ObjectiveState.Complete || _state == ObjectiveState.Finished;
        public bool CanComplete => _state == ObjectiveState.InProgress;
        public bool CanCancelCompletion => _state == ObjectiveState.Complete;

        [SerializeField] int _priority = 0;
        public int Priority => _priority;

        [Expandable]
        [SerializeField] protected List<ObjectiveTask> _tasks;
        public List<ObjectiveTask> Tasks => _tasks;
        [SerializeField] protected List<ObjectiveTask> _activeTasks;
        public List<ObjectiveTask> ActiveTasks => _activeTasks;
        [Expandable]
        [SerializeField] protected List<ObjectiveHint> _hints;
        public List<ObjectiveHint> Hints => _hints;
        [SerializeField] protected List<ObjectiveHint> _activeHints;
        public List<ObjectiveHint> ActiveHints => _activeHints;

        /// <summary>
        /// Fired when the objective is activated or deactivated
        /// </summary>
        public event Action<ObjectiveBase> ObjectiveActivationToggled;

        /// <summary>
        /// Fired when the objective completion state updates
        /// This may fire when completion progression changes as well
        /// like going 6/10 sticks obtained -> 7/10 sticks.
        /// </summary>
        public event Action<ObjectiveBase> OnObjectiveCompletionUpdated;
        public event Action<ObjectiveHint> OnObjectiveHintUpdate;
        public event Action<ObjectiveTask> OnObjectiveTaskAdded;
        public event Action<ObjectiveTask> OnObjectiveTaskRemoved;

        public bool ObjectiveManagerExists => ObjectiveManager.Instance != null;

        internal virtual void HandleObjectiveStarted()
        {
            _state = ObjectiveState.InProgress;
            ObjectiveActivationToggled?.Invoke(this);
        }

        internal void ResetTasksAndHints()
        {
            foreach (var subTask in Tasks)
            {
                subTask.ResetProgress();
            }

            foreach (var hint in Hints)
            {
                hint.Remove();
            }
        }

        internal virtual void HandleObjectiveEnded()
        {
            _state = _state == ObjectiveState.Complete? ObjectiveState.Finished : ObjectiveState.Cancelled;
            ObjectiveActivationToggled?.Invoke(this);
        }

        internal virtual void HandleObjectiveCompletionCancel()
        {
            _state = ObjectiveState.InProgress;
            OnObjectiveCompletionUpdated?.Invoke(this);
        }

        internal virtual void HandleObjectiveCompletion()
        {
            _state = ObjectiveState.Complete;
            Debug.Log(_state);
            OnObjectiveCompletionUpdated?.Invoke(this);
        }
        
        [ShowIf("ObjectiveManagerExists")]
        [Button]
        public void StartObjective()
        {
            ObjectiveManager.Instance.StartObjective(this);
        }
        [ShowIf("ObjectiveManagerExists")]
        [Button]
        public void EndObjective()
        {
            ObjectiveManager.Instance.EndObjective(this);
        }
        
        
        [ShowIf("ObjectiveManagerExists")]
        [Button]
        public void CompleteObjective()
        {
            ObjectiveManager.Instance.CompleteObjective(this);
        }
        
        [ShowIf("ObjectiveManagerExists")]
        [Button]
        public void CancelObjectiveCompletion()
        {
            ObjectiveManager.Instance.CancelObjectiveCompletion(this);
        }
        /// <summary>
        /// Needs to be called manually. should be called once
        /// </summary>
        public virtual void Initialize()
        {
            Debug.Log($" init {this}");
            foreach (var hint in Hints)
            {
                hint.SetObjective(this);
                hint.OnHintActivationToggled += HandleHintActivation;
            }
            
            foreach (var subTask in Tasks)
            {
                subTask.SetObjective(this);

                subTask.OnTaskActiveToggle += HandleTaskActivationUpdate;
                subTask.OnTaskCompletionToggle += HandleTaskCompletionUpdate;
            }

            UpdateActiveHintsAndTasks();
        }

        public void UpdateActiveHintsAndTasks()
        {
            _activeHints.Clear();
            foreach (var hint in Hints)
            {
                if (hint.IsActive)
                {
                    _activeHints.Add(hint);
                }
            }
            
            if (_activeTasks != null)
                _activeTasks.Clear();

            foreach (var subTask in Tasks)
            {
                if (subTask.IsActive)
                {
                    _activeTasks.Add(subTask);
                }
            }
            
            
        }

        /// <summary>
        /// Needs to be called manually
        /// Cleans up event subs to child hints/tasks.
        /// Assume that external systems call it
        /// </summary>
        public virtual void Cleanup()
        {
            Debug.Log($"cleanup {this}");
            ActiveTasks.Clear();
            ActiveHints.Clear();
            
            foreach (var hint in Hints)
            {
                hint.OnHintActivationToggled -= HandleHintActivation;
            }

            foreach (var subObjective in Tasks)
            {
                subObjective.OnTaskActiveToggle -= HandleTaskActivationUpdate;
                subObjective.OnTaskCompletionToggle -= HandleTaskCompletionUpdate;
            }
        }

        private void HandleTaskActivationUpdate(ObjectiveTask task)
        {
            if (task.IsActive)
            {
                _activeTasks.Add(task);
                OnObjectiveTaskAdded?.Invoke(task);
            }
            else
            {
                _activeTasks.Remove(task);
                OnObjectiveTaskRemoved?.Invoke(task);
            }
        }

        public void SetPriority(int priority)
        {
            this._priority = priority;
        }

        private void HandleTaskCompletionUpdate(ObjectiveTask task)
        {
            if(!IsActive)
                return;

            if (IsCompleted)
            {
                var shouldBeComplete = CheckCompletion();
                if (!shouldBeComplete)
                {
                    _state = ObjectiveState.InProgress;
                }
            }
            else
            {
                if(CanComplete && task.IsCompleted)
                {
                    if (task.CompleteParentObjectiveOnCompletion || CheckCompletion())
                    {
                        CompleteObjective();
                    }
                }
            }

            OnObjectiveCompletionUpdated?.Invoke(this);
        }


        public bool CheckCompletion() {
            int numRemaining = Tasks.Count;
            foreach (var task in ActiveTasks)
            {
                if (task.IsCompleted)
                {
                    numRemaining--;
                    if (task.CompleteParentObjectiveOnCompletion)
                    {
                        return true;
                    }
                }
            }
            return numRemaining == 0;
        }
        private void HandleHintActivation(ObjectiveHint puzzleObjectiveHint)
        {
            if (puzzleObjectiveHint.IsActive)
            {
                if (!_activeHints.Contains(puzzleObjectiveHint))
                {
                    _activeHints.Add(puzzleObjectiveHint);
                }
            }
            else
            {
                if (_activeHints.Contains(puzzleObjectiveHint))
                {
                    _activeHints.Remove(puzzleObjectiveHint);
                }
            }
            OnObjectiveHintUpdate?.Invoke(puzzleObjectiveHint);
        }
        [ShowIf("ObjectiveManagerExists")]
        [Button]
        internal void ResetProgress()
        {
            _state = ObjectiveState.NotStarted;
            
            foreach (var subTask in Tasks)
            {
                subTask.ResetProgress();
            }

            foreach (var hint in Hints)
            {
                hint.Remove();
            }
        }

        public void FullReset()
        {
            _tasks.Clear();
            _activeTasks.Clear();
            _hints.Clear();
            _activeHints.Clear();
            ResetProgress();
        }

        #region EDITOR
        # if UNITY_EDITOR
        
  
        [Button]
        public void CreateAndAddTask()
        {
            var task = ScriptableObject.CreateInstance<ObjectiveTask>();
            task.Name = (_tasks.Count + 1).ToString();
            task.name = GetTaskFullAssetName(task.Name);
            task.SetObjective(this);
            _tasks.Add(task);

            AssetDatabase.AddObjectToAsset(task, this);
            AssetDatabase.SaveAssets();
            EditorUtility.SetDirty(this);
        }

        public string GetTaskFullAssetName(string baseTaskName)
        {
            return $"{name}_task_{baseTaskName}";
        }

        [Button]
        public void CreateAndAddHint()
        {
            var hint = ScriptableObject.CreateInstance<ObjectiveHint>();
            hint.Name = (_hints.Count + 1).ToString();
            hint.name = getFullHintAssetName(hint.Name);
            _hints.Add(hint);
            hint.SetObjective(this);

            AssetDatabase.AddObjectToAsset(hint, this);
            AssetDatabase.SaveAssets();
            EditorUtility.SetDirty(this);
        }

        public string getFullHintAssetName(string hintBaseName)
        {
            return $"{name}_hint_{hintBaseName}";
        }
#endif
        #endregion

        #region Save/Load

        public override void AssignSerializedData(string data)
        {
            _state = JsonConvert.DeserializeObject<ObjectiveState>(data);
        }

        public override string GetSerializedData() {
            return JsonConvert.SerializeObject(_state);
        }

        #endregion

        public override string ToString()
        {
            return $"{name} {_state}";
        }
    }
}