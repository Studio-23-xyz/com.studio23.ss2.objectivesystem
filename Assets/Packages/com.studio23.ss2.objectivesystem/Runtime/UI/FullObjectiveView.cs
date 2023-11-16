using System.Collections.Generic;
using Bdeshi.Helpers.Utility;
using Studio23.SS2.ObjectiveSystem.Core;
using TMPro;
using UnityEngine;

namespace Studio23.SS2.ObjectiveSystem.UI
{
    public class FullObjectiveView : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI _objectiveTitleTMP;

        List<ObjectiveTask> _sortedTasks;
        [SerializeField] ObjectiveTaskView _objectiveTaskViewPrefab;
        SimpleManualMonoBehaviourPool<ObjectiveTaskView> _taskViewPool;
        [SerializeField] List<ObjectiveTaskView> _activeTaskViewList;
        [SerializeField] Transform _objectiveTaskViewContainer;

        [SerializeField] HintView _hintViewPrefab;
        [SerializeField] Transform _hintViewsContainer;
        [SerializeField] SimpleManualMonoBehaviourPool<HintView> _hintViewPool;
        [SerializeField] List<HintView> _hintViewList;

        private void Awake()
        {
            _sortedTasks = new List<ObjectiveTask>();
            _activeTaskViewList.Clear();
            _hintViewList.Clear();
            _hintViewPool = new SimpleManualMonoBehaviourPool<HintView> (
                _hintViewPrefab, 0, _hintViewsContainer
            );
            _taskViewPool = new SimpleManualMonoBehaviourPool<ObjectiveTaskView> (
                _objectiveTaskViewPrefab, 
                0,
                _objectiveTaskViewContainer
            );

        }
        public void LoadObjectiveData(ObjectiveBase objective)
        {
            _objectiveTitleTMP.text = objective.name;
            if (objective.IsCompleted)
                _objectiveTitleTMP.text = "<s>"+ _objectiveTitleTMP.text+"</s>";

            _sortedTasks.Clear();
            _sortedTasks.AddRange(objective.ActiveTasks);
            _sortedTasks.Sort(CompareTasks);

            _objectiveTaskViewContainer.gameObject.SetActive(_sortedTasks.Count != 0);
            _taskViewPool.EnsureSpawnListCount(_activeTaskViewList, _sortedTasks.Count);
            for (int i = 0; i < _sortedTasks.Count; i++)
            {
                _activeTaskViewList[i].LoadTaskData(objective.ActiveTasks[i]);
            }

            _hintViewsContainer.gameObject.SetActive(objective.ActiveHints.Count != 0);
            _hintViewPool.EnsureSpawnListCount(_hintViewList, objective.ActiveHints.Count);
            for (int i = 0; i < objective.ActiveHints.Count; i++)
            {
                _hintViewList[i].LoadHint(objective.ActiveHints[i]);
            }
        }

        private int CompareTasks(ObjectiveTask x, ObjectiveTask y)
        {
            if (x.IsCompleted != y.IsCompleted)
            {
                return x.IsCompleted ? 1 : -1;
            }

            return x.Priority.CompareTo(y.Priority);
        }
    }
}