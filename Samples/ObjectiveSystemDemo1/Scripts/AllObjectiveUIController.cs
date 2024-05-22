using System;
using Bdeshi.Helpers.Utility;
using Studio23.SS2.ObjectiveSystem.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Studio23.SS2.ObjectiveSystem.Samples.ObjectiveSystemDemo1
{
    public class AllObjectiveUIController:MonoBehaviour
    {
        [SerializeField] InputActionReference _allObjectiveUIToggleAction;
        [SerializeField] private GameObject _objectiveUI;
        private void OnEnable()
        {
            _allObjectiveUIToggleAction.action.performed += HandleObjectiveUIToggled;
            
        }

        private void Start()
        {
            var o = Resources.LoadAll<ObjectiveBase>("Inventory System/Objectives");
            Debug.Log($"o {o.Length}");
            var t = Resources.LoadAll<ObjectiveTask>("Inventory System/Objectives");
            Debug.Log($"o {t.Length}");
            var h  = Resources.LoadAll<ObjectiveHint>("Inventory System/Objectives");
            Debug.Log($"o {h.Length}");
        }

        private void HandleObjectiveUIToggled(InputAction.CallbackContext obj)
        {
            _objectiveUI.gameObject.SetActive(!_objectiveUI.gameObject.activeSelf);
        }

        private void OnDisable()
        {
            _allObjectiveUIToggleAction.action.performed -= HandleObjectiveUIToggled;
        }
    }
}