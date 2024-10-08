﻿using System.Collections.Generic;
using Bdeshi.Helpers.Utility;
using NaughtyAttributes;
using Studio23.SS2.ObjectiveSystem.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Studio23.SS2.ObjectiveSystem.Samples.ObjectiveSystemDemo1
{
    public class AllObjectiveUI : MonoBehaviour
    {
        [SerializeField] FullObjectiveView _objectiveViewPrefab;
        [SerializeField] Transform _objectiveViewsContainer;
        SimpleManualMonoBehaviourPool<FullObjectiveView> _objectiveViewPool;
        [SerializeField] List<FullObjectiveView> _objectiveViewList;
        [SerializeField] private GameObject _noObjectivesIndicator;

        private void Start()
        {
            _objectiveViewPool = new SimpleManualMonoBehaviourPool<FullObjectiveView>(_objectiveViewPrefab, 0, _objectiveViewsContainer);
            _objectiveViewList.Clear();

            ObjectiveManager.Instance.OnActiveObjectiveListUpdated += LoadObjectives;
            ObjectiveManager.Instance.OnActiveObjectiveTaskAdded += LoadObjectives;
            ObjectiveManager.Instance.OnActiveObjectiveTaskRemoved += LoadObjectives;
            ObjectiveManager.Instance.OnActiveObjectiveHintToggled += LoadObjectives;
            LoadObjectives();
        }

        private void LoadObjectives(ObjectiveHintBase obj)
        {
            LoadObjectives();
        }


        private void LoadObjectives(ObjectiveTask obj)
        {
            LoadObjectives();
        }

        private void OnDestroy()
        {
            ObjectiveManager.Instance.OnActiveObjectiveListUpdated -= LoadObjectives;
            ObjectiveManager.Instance.OnActiveObjectiveTaskAdded -= LoadObjectives;
            ObjectiveManager.Instance.OnActiveObjectiveTaskRemoved -= LoadObjectives;
            ObjectiveManager.Instance.OnActiveObjectiveHintToggled -= LoadObjectives;
        }
        [Button]
        public void LoadObjectives()
        {
            var objectives = ObjectiveManager.Instance.ActiveObjectives;
            _objectiveViewPool.EnsureSpawnListCount(_objectiveViewList, objectives.Count);
            _noObjectivesIndicator.gameObject.SetActive(objectives.Count == 0);
            for (int i = 0; i < objectives.Count; i++)
            {
                _objectiveViewList[i].LoadObjectiveData(objectives[i]);
            }
            
            // to force rebuilding the scrollview
            //otherwise contentfitter doesn't change shape.
            LayoutRebuilder.ForceRebuildLayoutImmediate(_objectiveViewsContainer.GetComponentInParent<RectTransform>());
        }
    }


}

