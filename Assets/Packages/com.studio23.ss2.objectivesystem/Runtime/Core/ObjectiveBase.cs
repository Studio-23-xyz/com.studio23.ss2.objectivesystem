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
using UnityEngine.Events;


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

        public bool ShouldDisplayObjectiveCount = false;
        [Expandable]
        [SerializeField] protected List<ObjectiveTask> _tasks;
        
        public List<ObjectiveTask> Tasks => _tasks;
        [SerializeField] protected List<ObjectiveTask> _activeTasks;
        public List<ObjectiveTask> ActiveTasks => _activeTasks;
        [Expandable]
        [SerializeField] protected List<ObjectiveHintBase> _hints;
        public List<ObjectiveHintBase> Hints => _hints;
        [SerializeField] protected List<ObjectiveHintBase> _activeHints;
        public List<ObjectiveHintBase> ActiveHints => _activeHints;


        public delegate void OnObjectiveActivationEvent(ObjectiveBase objective);
        public delegate void OnObjectiveHintEvent(ObjectiveHintBase hint);
        public delegate void OnObjectiveTaskEvent(ObjectiveTask task);

        /// <summary>
        /// Fired when the objective is activated or deactivated
        /// </summary>
        /// 
        public OnObjectiveActivationEvent ObjectiveActivationToggled;

        /// <summary>
        /// Fired when the objective completion state updates
        /// This may fire when completion progression changes as well
        /// like going 6/10 sticks obtained -> 7/10 sticks.
        /// </summary>

        public OnObjectiveActivationEvent OnObjectiveCompletionUpdated;
        public OnObjectiveHintEvent OnObjectiveHintUpdate;
        public OnObjectiveTaskEvent OnObjectiveTaskAdded;
        public OnObjectiveTaskEvent OnObjectiveTaskRemoved;
        public UnityEvent OnObjectiveStarted;
        public UnityEvent OnObjectiveCompleted;
        
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

            OnObjectiveCompletionUpdated?.Invoke(this);
        }
        
        [Button(enabledMode:EButtonEnableMode.Playmode)]
        public void StartObjective()
        {
            ObjectiveManager.Instance.StartObjective(this);
            foreach (var task in _tasks)
            {
                if (task.InitiallyActive)
                {
                    task.AddTask();
                }
            }
            OnObjectiveStarted?.Invoke();
        }
        [Button(enabledMode:EButtonEnableMode.Playmode)]
        public void EndObjective()
        {
            ObjectiveManager.Instance.EndObjective(this);
        }
        
        [Button(enabledMode:EButtonEnableMode.Playmode)]
        public void CompleteObjective()
        {
            ObjectiveManager.Instance.CompleteObjective(this);
            OnObjectiveCompleted?.Invoke();
        }

        [Button(enabledMode:EButtonEnableMode.Playmode)]
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
                if (!_activeTasks.Contains(task))
                {
                    _activeTasks.Add(task);
                    OnObjectiveTaskAdded?.Invoke(task);
                }
            }
            else
            {
                if (_activeTasks.Contains(task))
                {
                    _activeTasks.Remove(task);
                    OnObjectiveTaskRemoved?.Invoke(task);
                }
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
        
        private void HandleHintActivation(ObjectiveHintBase objectiveHintBase)
        {
            if (objectiveHintBase.IsActive)
            {
                if (!_activeHints.Contains(objectiveHintBase))
                {
                    _activeHints.Add(objectiveHintBase);
                }
            }
            else
            {
                if (_activeHints.Contains(objectiveHintBase))
                {
                    _activeHints.Remove(objectiveHintBase);
                }
            }
            OnObjectiveHintUpdate?.Invoke(objectiveHintBase);
        }
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
        [Button]
        public void Reset()
        {
            _activeTasks.Clear();
            _activeHints.Clear();
            ResetProgress();
        }

        #region Save/Load

        public override void AssignSerializedData(string data)
        {
            var saveData = JsonConvert.DeserializeObject<ObjectiveSaveData>(data);
            _state = saveData.ObjectiveState;

            for (int i = 0; i < _tasks.Count; i++)
            {
                _tasks[i].AssignSerializedData(saveData.TaskStates[i]);
            }
            
            for (int i = 0; i < _hints.Count; i++)
            {
                _hints[i].AssignSerializedData(saveData.HintStates[i]);
            }
            
            OnObjectiveCompletionUpdated?.Invoke(this);
        }

        public override string GetSerializedData() {
            return JsonConvert.SerializeObject(new ObjectiveSaveData(this));
        }

        #endregion

        public override string ToString()
        {
            return $"{name} {_state}";
        }

        [Serializable]
        public class ObjectiveSaveData
        {
            public ObjectiveState ObjectiveState;
            public List<string> TaskStates;
            public List<string> HintStates;

            //needed for serialize
            public ObjectiveSaveData()
            {
            }

            public ObjectiveSaveData(ObjectiveBase objectiveBase)
            {
                ObjectiveState = objectiveBase._state;
                TaskStates = new ();
                foreach (var task in objectiveBase.Tasks)
                {
                    TaskStates.Add(task.GetSerializedData());
                }

                HintStates = new();
                foreach (var objectiveHint in objectiveBase.Hints)
                {
                    HintStates.Add(objectiveHint.GetSerializedData());
                }
            }
        }
    }
}