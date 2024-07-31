using System;
using Cysharp.Threading.Tasks;
using Studio23.SS2.ObjectiveSystem.Core;
using UnityEngine;
using UnityEngine.Events;

namespace Studio23.SS2.ObjectiveSystem.Samples.ObjectiveSystemDemo1
{
    /// <summary>
    /// Fires events when objective state changes
    /// Note that you can just sub to events on the scriptable itself.
    /// This is just to fire unity events easily.
    /// </summary>
    public class ObjectiveStateChangeEventHelper:MonoBehaviour
    {
        [SerializeField] private ObjectiveBase _objective;
        public UnityEvent ObjectiveCompletedEvent;

        private void OnEnable()
        {
            _objective.OnObjectiveCompletionUpdated += HandleObjectiveCompletionUpdate;
        }
        
        private void OnDisable()
        {
            _objective.OnObjectiveCompletionUpdated -= HandleObjectiveCompletionUpdate;
        }


        private void HandleObjectiveCompletionUpdate(ObjectiveBase obj)
        {
            if (obj.IsCompleted)
            {
                ObjectiveCompletedEvent.Invoke();
            }
        }
    }
}