using System.Collections.Generic;
using Bdeshi.Helpers.Utility;
using Cysharp.Threading.Tasks;
using Studio23.SS2.ObjectiveSystem.Core;
using TMPro;
using UnityEngine;

namespace Studio23.SS2.ObjectiveSystem.Samples.ObjectiveSystemDemo1
{
    public class SelectedObjectiveView : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI _objectiveTitleTMP;
        [SerializeField] TextMeshProUGUI _objectiveDescTMP;
        
        List<ObjectiveTask> _sortedTasks;
        public int MaxTasksToShow = 3;
        [SerializeField] ObjectiveTaskView _objectiveTaskViewPrefab;
        SimpleManualMonoBehaviourPool<ObjectiveTaskView> _taskViewPool;
        [SerializeField] List<ObjectiveTaskView> _activeTaskViewList;
        [SerializeField] Transform _objectiveTaskViewContainer;

        List<ObjectiveHint> _sortedHints;
        public int MaxHintsToShow = 1;
        [SerializeField] HintView _hintViewPrefab;
        [SerializeField] Transform _hintViewsContainer;
        SimpleManualMonoBehaviourPool<HintView> _hintViewPool;
        [SerializeField] List<HintView> _hintViewList;

        private void Awake()
        {
            _sortedTasks = new List<ObjectiveTask> ();
            _sortedHints = new List<ObjectiveHint>();

            _activeTaskViewList.Clear();
            _hintViewList.Clear();
            _hintViewPool = new SimpleManualMonoBehaviourPool<HintView>(
                _hintViewPrefab, 0, _hintViewsContainer
            );
            _taskViewPool = new SimpleManualMonoBehaviourPool<ObjectiveTaskView>(
                _objectiveTaskViewPrefab,
                0,
                _objectiveTaskViewContainer
            );
        }

        private async void Start()
        {
            ObjectiveManager.Instance.SelectedObjectiveChanged += LoadObjectiveData;
            ObjectiveManager.Instance.SelectedObjectiveUpdated += LoadObjectiveData;
            
            while (!ObjectiveManager.Instance.Initialized)
            {
                await UniTask.Yield();
            }

            LoadObjectiveData();
        }

        private void OnDestroy()
        {
            ObjectiveManager.Instance.SelectedObjectiveChanged -= LoadObjectiveData;
            ObjectiveManager.Instance.SelectedObjectiveUpdated -= LoadObjectiveData;
        }



        [ContextMenu("force reload")]
        public void LoadObjectiveData() {
            LoadObjectiveData(ObjectiveManager.Instance.SelectedObjective);
        }
        public void LoadObjectiveData(ObjectiveBase objective)
        {
            Debug.Log("selected objective changed");
            if(objective == null)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive (true);
            _objectiveTitleTMP.text = objective.name;
            if (objective.IsCompleted)
                _objectiveTitleTMP.text = $"<s>{_objectiveTitleTMP.text}</s>";
            _objectiveDescTMP.text = objective.ObjectiveUIDesc;
 

            _sortedTasks.Clear();
            _sortedTasks.AddRange(objective.ActiveTasks);
            _sortedTasks.Sort(CompareTasks);
            int numTasksToShow = Mathf.Min(_sortedTasks.Count, MaxTasksToShow);

            _objectiveTaskViewContainer.gameObject.SetActive(numTasksToShow != 0);
            _taskViewPool.EnsureSpawnListCount(_activeTaskViewList, numTasksToShow);
            for (int i = 0; i < numTasksToShow; i++)
            {
                _activeTaskViewList[i].LoadTaskData(objective.ActiveTasks[i]);
            }


            _sortedHints.Clear();
            _sortedHints.AddRange(objective.ActiveHints);
            _sortedHints.Sort(CompareHints);
            int numHintsToShow = Mathf.Min(_sortedHints.Count, MaxHintsToShow);

            _hintViewsContainer.gameObject.SetActive(numHintsToShow != 0);
            _hintViewPool.EnsureSpawnListCount(_hintViewList, numHintsToShow);
            for (int i = 0; i < numHintsToShow; i++)
            {
                _hintViewList[i].LoadHint(objective.ActiveHints[i]);
            }
        }

        private int CompareHints(ObjectiveHint x, ObjectiveHint y)
        {
            return x.Priority.CompareTo(y.Priority);
        }

        private int CompareTasks(ObjectiveTask x, ObjectiveTask y)
        {
            if(x.IsCompleted != y.IsCompleted)
            {
                return x.IsCompleted? 1:-1;
            }

            return x.Priority.CompareTo(y.Priority);
        }
    }
}
