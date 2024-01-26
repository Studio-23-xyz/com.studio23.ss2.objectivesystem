using System;
using Bdeshi.Helpers.Utility;
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