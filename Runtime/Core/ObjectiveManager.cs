using System;
using System.Collections.Generic;
using BDeshi.Logging;
using Cysharp.Threading.Tasks;
using NaughtyAttributes;
using Newtonsoft.Json;
using Studio23.SS2.InventorySystem.Core;
using Studio23.SS2.InventorySystem.Data;
using Studio23.SS2.ObjectiveSystem.Data;
using Studio23.SS2.ObjectiveSystem.Utilities;
using Studio23.SS2.SaveSystem.Interfaces;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Studio23.SS2.ObjectiveSystem.Core
{
    public class ObjectiveManager : TestMonoSingleton<ObjectiveManager>, ISaveable
    {
        public InventoryBase<ObjectiveBase> Objectives { get; private set; }
        public InventoryBase<ObjectiveTask> Tasks { get; private set; }
        public InventoryBase<ObjectiveHint> Hints { get; private set; }
        [SerializeField] List<ObjectiveBase> _activeObjectives;
        public List<ObjectiveBase> ActiveObjectives => _activeObjectives;

        [SerializeField] InputActionReference _selectedObjectiveChangeAction;
        [SerializeField] ObjectiveBase _selectedObjective;
        [SerializeField] int _selectedObjectiveIndex = 0;
        
        private bool _isDirty = true;
        
        //#TODO listen to objective events and set as true when any is modified.
        public bool IsDirty { 
            get => _isDirty;
            set => _isDirty = value;
        }

        public string GetUniqueID() => "ObjectiveSystem";

        public ObjectiveBase SelectedObjective => _selectedObjective;
        /// <summary>
        /// Fired when selected objective switches to another one
        /// Instance.SelectedObjective can be null when this is fired
        /// </summary>
        public event Action SelectedObjectiveChanged;
        /// <summary>
        /// Fired when selected objective gets any updates. Can be a hints update or a completed update
        /// </summary>
        public event Action SelectedObjectiveUpdated;
        public event Action OnActiveObjectiveListUpdated;
        public event Action<ObjectiveHint> OnActiveObjectiveHintToggled;
        public event Action<ObjectiveTask> OnActiveObjectiveTaskAdded;
        public event Action<ObjectiveTask> OnActiveObjectiveTaskRemoved;
        public bool Initialized { get; private set; } = false;

        public ICategoryLogger<ObjectiveLogCategory> Logger => _logger;
        [SerializeField] SerializableCategoryLogger<ObjectiveLogCategory> _logger = new ((ObjectiveLogCategory)(~0));

        public bool InstanceValid => Instance != null;

        protected override void Initialize()
        {
            Initialized = false;
            CreateInventories();
            _activeObjectives = new List<ObjectiveBase>();
        }

        private void CreateInventories()
        {
            Objectives = new InventoryBase<ObjectiveBase>("Objectives");
            Hints = new InventoryBase<ObjectiveHint>("Hints");
            Tasks = new InventoryBase<ObjectiveTask>("Tasks");
        }

        private async void Start()
        {
            if (!WillGetDestroyed)
            {
                //we need to wait for savesytem singleton to be up.
                //hence, call load() afterwards
                await Load();
            }
        }

        public async UniTask AwaitInitialization()
        {
            while (!ObjectiveManager.Instance.Initialized)
            {
                await UniTask.Yield();
            }
        }

        private void InitializeObjectives()
        {
            foreach (var objective in Objectives.GetAll())
            {
                objective.Initialize();
                SubToObjective(objective);
                if (objective.IsActive)
                {
                    AddObjectiveToActives(objective);
                }
            }
            
            HandleActiveObjectiveListUpdated();
        }

        private void SubToObjective(ObjectiveBase objective)
        {
            objective.ObjectiveActivationToggled += HandleObjectiveActivationToggle;
            objective.OnObjectiveTaskAdded += HandleObjectiveTaskAdded;
            objective.OnObjectiveTaskRemoved += HandleObjectiveTaskRemoved;
            objective.OnObjectiveHintUpdate += HandleObjectiveHintToggle;
            objective.ObjectiveActivationToggled += HandleObjectiveActivationToggle;
        }

        private void HandleObjectiveHintToggle(ObjectiveHint objectiveHint)
        {
            if (objectiveHint.IsActive)
            {
                Hints.AddItemUnique(objectiveHint);
            }
            else
            {
                Hints.RemoveItem(objectiveHint);
            }

            if (objectiveHint.ParentObjective == _selectedObjective)
            {
                HandleSelectedObjectiveUpdates(_selectedObjective);
            }
        }

        private void HandleObjectiveTaskRemoved(ObjectiveTask task)
        {
            Tasks.RemoveItem(task);
            if (task.ParentObjective == _selectedObjective)
            {
                HandleSelectedObjectiveUpdates(_selectedObjective);
            }
        }

        private void HandleObjectiveTaskAdded(ObjectiveTask task)
        {
            Tasks.AddItemUnique(task);
            if (task.ParentObjective == _selectedObjective)
            {
                HandleSelectedObjectiveUpdates(_selectedObjective);
            }
        }


        private void HandleObjectiveActivationToggle(ObjectiveBase objective)
        {
            if (objective.IsActive)
            {
                AddObjectiveToActives(objective);
            }
            else
            {
                RemoveObjectiveFromActives(objective);
            }
        }


        public void StartObjective(ObjectiveBase newObjective) {

            bool hadObjective = Objectives.HasItem(newObjective);
            //do not trust objective state unless it is in objectives
            //we allow starting any objective if it's not in objectives
            bool canStartObjective = !hadObjective || newObjective.CanStart;
            Logger.Log(ObjectiveLogCategory.ObjectiveStart, 
                $"start new objective request {newObjective}{hadObjective}{canStartObjective}", newObjective);
            if(!canStartObjective)
            {
                Logger.LogWarning(ObjectiveLogCategory.ObjectiveStart, $"can't start objective at state {newObjective.State}");
                return;
            }
            
            if (!hadObjective)
            {
                // if objectives inventory doesn't have the objective,
                // it must not have been part of save. We consider it not started.
                // otherwise, we can trust it's state. 
                // If it's not started, reset tasks and hints
                newObjective.ResetProgress();
                newObjective.Initialize();
                Objectives.AddItem(newObjective);
                SubToObjective(newObjective);
            }
            if (!hadObjective || newObjective.CanStart)
            {
                newObjective.UpdateActiveHintsAndTasks();
            }
            ForceAddObjectiveToActives(newObjective);
            newObjective.HandleObjectiveStarted();
        }
        /// <summary>
        /// This removes the objective from active list and marks it as cancelled or finished based on completion
        /// </summary>
        /// <param name="objective"></param>
        public void EndObjective(ObjectiveBase objective) {
            //do not trust objective state unless it is in objectives
            var hasItem = Objectives.HasItem(objective);
            var objectiveIsActive = objective.IsActive;
            if (hasItem && objectiveIsActive)
            {
                Logger.Log(ObjectiveLogCategory.ObjectiveEnd,$"End  objective {objective}", objective);
                RemoveObjectiveFromActives(objective);
            }
            else
            {
                if(!hasItem)
                    Logger.LogWarning(ObjectiveLogCategory.ObjectiveEnd,$"can't end objective {objective} that isn't in the objectives inventory");
                else
                    Logger.LogWarning(ObjectiveLogCategory.ObjectiveEnd,$"can't end objective {objective} in state {objective.State}");
            }
        }
        public void CompleteObjective(ObjectiveBase objective)
        {
            var hasItem = Objectives.HasItem(objective);
            var canComplete = objective.CanComplete;
            
            if (hasItem && canComplete)
            {
                objective.HandleObjectiveCompletion();
                HandleActiveObjectiveListUpdated();
                Logger.Log(ObjectiveLogCategory.ObjectiveComplete,$"End  objective {objective}", objective);

                OnActiveObjectiveListUpdated?.Invoke();
            }else
            {
                if(!hasItem)
                    Logger.LogWarning(ObjectiveLogCategory.ObjectiveComplete,$"can't complete objective {objective} that isn't in the objectives inventory");
                else
                    Logger.LogWarning(ObjectiveLogCategory.ObjectiveComplete,$"can't complete objective {objective} in state {objective.State}");
            }
        }
        public void CancelObjectiveCompletion(ObjectiveBase objective)
        {
            Logger.Log(ObjectiveLogCategory.ObjectiveCancel, $"complete new objective request {objective}", objective);
            var hasItem = Objectives.HasItem(objective);
            var objectiveCanCancelCompletion = objective.CanCancelCompletion;
            if (hasItem && objectiveCanCancelCompletion)
            {
                objective.HandleObjectiveCompletionCancel();
                HandleActiveObjectiveCompletionUpdate(objective);
                Logger.Log(ObjectiveLogCategory.ObjectiveCancel, $"restarted existing objective{objective}", objective);
            }else
            {
                if(!hasItem)
                    Logger.LogWarning(ObjectiveLogCategory.ObjectiveCancel, $"can't cancel objective {objective} that isn't in the objectives inventory");
                else
                    Logger.LogWarning(ObjectiveLogCategory.ObjectiveCancel, $"can't cancel objective {objective} in state {objective.State}");
            }
        }

        public bool IsObjectiveActiveAndValid(ObjectiveBase objective)
        {
            return Objectives.HasItem(objective) && objective.IsActive;
        }
        private void HandleHintUpdated(ObjectiveHint objectiveHint)
        {
            OnActiveObjectiveHintToggled?.Invoke(objectiveHint);
        }

        public void AddObjectiveToActives(ObjectiveBase objective)
        {
            if (ActiveObjectives.Contains(objective))
                return;
            ForceAddObjectiveToActives(objective);
        }
        public void RemoveObjectiveFromActives(ObjectiveBase objective)
        {
            if (!ActiveObjectives.Contains(objective))
                return;
            ForceRemoveObjectiveFromActives(objective);
        }

        private void ForceRemoveObjectiveFromActives(ObjectiveBase objective)
        {
            ActiveObjectives.Remove(objective);

            objective.HandleObjectiveEnded();
            objective.OnObjectiveCompletionUpdated -= HandleActiveObjectiveCompletionUpdate;
            objective.OnObjectiveHintUpdate -= HandleHintUpdated;
            objective.OnObjectiveTaskAdded -= OnActiveObjectiveTaskAdded;
            objective.OnObjectiveTaskRemoved -= OnActiveObjectiveTaskRemoved;
            OnActiveObjectiveListUpdated?.Invoke();

            if(SelectedObjective == objective)
            {
                SelectNewBestObjective();
            }
        }

        void ForceAddObjectiveToActives(ObjectiveBase newObjective)
        { 
            _activeObjectives.Add(newObjective);
            newObjective.OnObjectiveCompletionUpdated += HandleActiveObjectiveCompletionUpdate;
            newObjective.OnObjectiveHintUpdate += HandleActiveObjectiveUpdated;
            newObjective.OnObjectiveTaskAdded += OnActiveObjectiveTaskAdded;
            newObjective.OnObjectiveTaskRemoved += OnActiveObjectiveTaskRemoved;

            if (SelectedObjective == null)
            {
                SelectNewBestObjective();
            }
            OnActiveObjectiveListUpdated?.Invoke();
        }

        private void HandleActiveObjectiveCompletionUpdate(ObjectiveBase objective)
        {
            HandleActiveObjectiveListUpdated();
            if(SelectedObjective == null)
            {
                SelectNewBestObjective();
            }
        }

        private void HandleActiveObjectiveUpdated(ObjectiveHint objectiveHint)
        {
            HandleActiveObjectiveListUpdated();
        }
        private void HandleActiveObjectiveListUpdated()
        {
            //#TODO this should be moved elsewhere where it isn't invoked every time the list is modified
            ActiveObjectives.Sort(CompareActiveObjectives);
            OnActiveObjectiveListUpdated?.Invoke();
        }

        private static int CompareActiveObjectives(ObjectiveBase objective1, ObjectiveBase objective2)
        {
            if (objective1 == Instance._selectedObjective)
                return -1;
            else if  (objective2 == Instance._selectedObjective)
                return -1;
            
            
            if (objective1.IsCompleted == objective2.IsCompleted)
            {
                return objective1.Priority.CompareTo(objective2.Priority);
            }
            // at this point, only one is completed.
            // if objective1 is completed, then it should be "larger"
            // hence 1:-1
            return objective1.IsCompleted ? 1 : -1;
        }
        [ShowIf("InstanceValid")]
        [Button]
        public void PrintAllActiveObjectives() {
            Debug.Log("active objectives:");
            foreach (ObjectiveBase objective in ActiveObjectives)
            {
                Debug.Log(objective, objective);
            }
        }
        
        [ShowIf("InstanceValid")]
        [Button]
        public void PrintAllObjectives() {
            Debug.Log("Objectives:");
            foreach (ObjectiveBase objective in Objectives.GetAll())
            {
                Debug.Log(objective, objective);
            }
        }

        private void OnEnable()
        {
            //#TODO move this elsewhere
            _selectedObjectiveChangeAction.action.actionMap.Enable();

            _selectedObjectiveChangeAction.action.performed += HandleSelectedObjectiveChangeAction;
            if (!Initialized)
            {
                InitializeObjectives();
            }
        }


        private void OnDisable()
        {
            if(Instance == this)
            {
                _selectedObjectiveChangeAction.action.actionMap.Disable();
                _selectedObjectiveChangeAction.action.performed -= HandleSelectedObjectiveChangeAction;
                foreach(var objective in Objectives.GetAll())
                {
                    UnsubFromObjective(objective);

                    objective.Cleanup();
                }
            }
        }

        private void UnsubFromObjective(ObjectiveBase objective)
        {
            objective.ObjectiveActivationToggled -= HandleObjectiveActivationToggle;
            objective.OnObjectiveTaskAdded -= HandleObjectiveTaskAdded;
            objective.OnObjectiveTaskRemoved -= HandleObjectiveTaskRemoved;
            objective.OnObjectiveHintUpdate -= HandleObjectiveHintToggle;
            objective.ObjectiveActivationToggled -= HandleObjectiveActivationToggle;
        }

        private void HandleSelectedObjectiveChangeAction(InputAction.CallbackContext context)
        {
            if (ActiveObjectives.Count == 0)
            {
                _selectedObjective = null;
            }else if (ActiveObjectives.Count > 1)
            {
                int newIndex = -1;
                for (int i = 1; i < (ActiveObjectives.Count); i++)
                {
                    int testIndex = (_selectedObjectiveIndex + i + ActiveObjectives.Count) % ActiveObjectives.Count;
                    if (!ActiveObjectives[testIndex].IsCompleted)
                    {
                        newIndex = testIndex;
                    }
                } 
                
                //if we don't have another objective to selected an current selected objective is not null, don't change
                if (newIndex == -1 && !_selectedObjective.IsCompleted)
                {
                    return;
                }
                SetSelectedIndex(newIndex);
            }
        }
        /// <summary>
        /// Set selected objective index and _selectedObjective
        /// if invalid index sets it to null
        /// </summary>
        /// <param name="index"></param>
        private void SetSelectedIndex(int index)
        {
            if(SelectedObjective != null)
            {
                _selectedObjective.OnObjectiveCompletionUpdated -= HandleSelectedObjectiveUpdates;
                _selectedObjective.OnObjectiveHintUpdate -= HandleSelectedObjectiveUpdates;
            }

            _selectedObjectiveIndex = index;
            if(index < 0 || index >= ActiveObjectives.Count)
            {
                _selectedObjective = null;
            }
            else
            {
                var newSelectedObjective = ActiveObjectives[_selectedObjectiveIndex];
                newSelectedObjective.OnObjectiveCompletionUpdated += HandleSelectedObjectiveUpdates;
                newSelectedObjective.OnObjectiveHintUpdate += HandleSelectedObjectiveUpdates;

                _selectedObjective = newSelectedObjective;
            }

            SelectedObjectiveChanged?.Invoke();
        }

        private void HandleSelectedObjectiveUpdates(ObjectiveHint objectiveHint) {
            SelectedObjectiveUpdated?.Invoke();
        }
        private void HandleSelectedObjectiveUpdates(ObjectiveBase objective)
        {
            if (objective.IsCompleted)
            {
                SelectNewBestObjective();
            }
            else
            {
                SelectedObjectiveUpdated?.Invoke();
            }
        }

        void  SelectNewBestObjective() {
            ActiveObjectives.Sort(CompareActiveObjectives);
            int newIndex = -1;
            for (int i = 0; i < ActiveObjectives.Count; i++)
            {
                if (!ActiveObjectives[i].IsCompleted)
                {
                    newIndex = i;
                }
            }
            SetSelectedIndex(newIndex);
        }
        
        

        #region Save/Load

        [ShowIf("InstanceValid")]
        [Button]
        async UniTask Save()
        {
            Debug.Log("SAVE");

            await SaveSystem.Core.SaveSystem.Instance.Save();
        }

        /// <summary>
        /// do cleanup and  relaod objectives from save
        /// for initial load use load()
        /// </summary>
        [ShowIf("InstanceValid")]
        [Button]
        async UniTask Reload()
        {
            await SaveSystem.Core.SaveSystem.Instance.Load();

            foreach(var objective in Objectives.GetAll())
            {
                objective.ObjectiveActivationToggled -= HandleObjectiveActivationToggle;
                objective.OnObjectiveTaskAdded -= HandleObjectiveTaskAdded;
                objective.OnObjectiveTaskRemoved -= HandleObjectiveTaskRemoved;
                objective.OnObjectiveHintUpdate -= HandleObjectiveHintToggle;
                objective.ObjectiveActivationToggled -= HandleObjectiveActivationToggle;
                UnsubFromObjective(objective);
            }
            CreateInventories();
            _activeObjectives.Clear();
            
            await Load();

        }
        /// <summary>
        /// Load from save
        /// use reload() when already loaded.
        /// </summary>
        async UniTask Load()
        {
            Logger.Log(ObjectiveLogCategory.Initialization,"LOAD");
            await SaveSystem.Core.SaveSystem.Instance.Load();
            
            InitializeObjectives();
            HandleActiveObjectiveListUpdated();
            SelectNewBestObjective();
            Initialized = true;
        }
        [ShowIf("InstanceValid")]
        [Button]
        void NukeSave()
        {
            Debug.Log("#TODO");
            // Hints.NukeSave();
            // Tasks.NukeSave();
            // Objectives.NukeSave();
        }

        #endregion

        public string GetSerializedData()
        {
            return JsonConvert.SerializeObject(new ObjectiveSystemSaveData(this));
        }

        public void AssignSerializedData(string data)
        {
            var saveData = JsonConvert.DeserializeObject<ObjectiveSystemSaveData>(data);
            Hints.LoadInventoryData(saveData.HintsData);
            Tasks.LoadInventoryData(saveData.TasksData);
            Objectives.LoadInventoryData(saveData.ObjectivesData);
            
            InitializeObjectives();
            HandleActiveObjectiveListUpdated();
            SelectNewBestObjective();
        }


        [Serializable]
        public class ObjectiveSystemSaveData
        {
            public List<ItemSaveData> HintsData;
            public List<ItemSaveData> TasksData;
            public List<ItemSaveData> ObjectivesData;

            //This is called by jsonserializer
            //necessary
            public ObjectiveSystemSaveData()
            {
                
            }

            public ObjectiveSystemSaveData(ObjectiveManager objectiveManager)
            {
                HintsData = objectiveManager.Hints.GetInventorySaveData();
                TasksData = objectiveManager.Tasks.GetInventorySaveData();
                ObjectivesData = objectiveManager.Objectives.GetInventorySaveData();
            }
        }
    }

    [Flags]
    public enum ObjectiveLogCategory
    {
        Initialization = 1 << 0,
        ObjectiveStart = 1 << 1,
        ObjectiveEnd = 1 << 2,
        ObjectiveComplete = 1 << 3,
        ObjectiveCancel= 1 << 4,
        Task =  1 << 5,
        Hint =1 << 6,
    }
}