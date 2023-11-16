using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using NaughtyAttributes;
using Studio23.SS2.InventorySystem.Core;
using Studio23.SS2.ObjectiveSystem.Utilities;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Studio23.SS2.ObjectiveSystem.Core
{
    public class ObjectiveManager : TestMonoSingleton<ObjectiveManager>
    {
        public InventoryBase<ObjectiveBase> Objectives { get; private set; }
        public InventoryBase<ObjectiveTask> Tasks { get; private set; }
        public InventoryBase<ObjectiveHint> Hints { get; private set; }
        [SerializeField] List<ObjectiveBase> _activeObjectives;
        public List<ObjectiveBase> ActiveObjectives => _activeObjectives;

        [SerializeField] InputActionReference _selectedObjectiveChangeAction;
        [SerializeField] ObjectiveBase _selectedObjective;
        [SerializeField] int _selectedObjectiveIndex = 0;
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

        public bool IsDebug = false;
        public bool Initialized { get; private set; } = false;

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
        public void CompleteObjective(ObjectiveBase objective)
        {
            Debug.Log(Objectives.HasItem(objective) + " " + objective.CanComplete );
            if(!Objectives.HasItem(objective) || !objective.CanComplete)
                return;
            objective.HandleObjectiveCompletion();
            HandleActiveObjectiveListUpdated();

            OnActiveObjectiveListUpdated?.Invoke();
        }

        public void StartObjective(ObjectiveBase newObjective) {

            bool hadObjective = Objectives.HasItem(newObjective);
            //do not trust objective state unless it is in objectives
            //we allow starting any objective if it's not in objectives
            bool canStartObjective = !hadObjective || newObjective.CanStart;
            DLog("start new objective request " + newObjective + hadObjective + canStartObjective, newObjective);
            if(!canStartObjective)
                return;
            
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
        /// <param name="newObjective"></param>
        public void EndObjective(ObjectiveBase newObjective) {
            DLog("start new objective request " + newObjective, newObjective);
            //do not trust objective state unless it is in objectives
            if (Objectives.HasItem(newObjective) && newObjective.IsActive)
            {
                RemoveObjectiveFromActives(newObjective);
            }
        }

        public void CancelObjectiveCompletion(ObjectiveBase newObjective)
        {
            DLog("start new objective request " + newObjective, newObjective);
            if (Objectives.HasItem(newObjective) && newObjective.CanCancelCompletion)
            {
                newObjective.HandleObjectiveCompletionCancel();
                HandleActiveObjectiveCompletionUpdate(newObjective);
                DLog("restarted existing objective" + newObjective, newObjective);
            }
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
            DLog("removed new objective" + objective, objective);
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


        [HideInCallstack]
        public void DLog(string msg, UnityEngine.Object ctx = null)
        {
            if (IsDebug)
            {
                Debug.Log(msg, ctx);
            }
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
            //DLog("SortActiveObjectives ");
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
                for (int i = 1; i < (ActiveObjectives.Count + 1); i++)
                {
                    int testIndex = (_selectedObjectiveIndex + i + ActiveObjectives.Count) % ActiveObjectives.Count;
                    if (!ActiveObjectives[testIndex].IsCompleted)
                    {
                        newIndex = testIndex;
                    }
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

            await Hints.SaveInventory();
            await Tasks.SaveInventory();

            await Objectives.SaveInventory();
        }

        /// <summary>
        /// do cleanup and  relaod objectives from save
        /// for initial load use load()
        /// </summary>
        [ShowIf("InstanceValid")]
        [Button]
        async UniTask Reload()
        {
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
            DLog("LOAD");
            await Hints.SafeLoadInventory();
            await Tasks.SafeLoadInventory();
            await Objectives.SafeLoadInventory();
            InitializeObjectives();
            
            HandleActiveObjectiveListUpdated();
            SelectNewBestObjective();
            Initialized = true;
        }
        [ShowIf("InstanceValid")]
        [Button]
        void NukeSave()
        {
            Hints.NukeSave();
            Tasks.NukeSave();
            Objectives.NukeSave();
        }

        #endregion

    }
}