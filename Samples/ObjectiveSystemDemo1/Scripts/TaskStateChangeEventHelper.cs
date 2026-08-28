using Studio23.SS2.ObjectiveSystem.Core;
using UnityEngine;
using UnityEngine.Events;

namespace Studio23.SS2.ObjectiveSystem.Samples.ObjectiveSystemDemo1
{
    public class TaskStateChangeEventHelper:MonoBehaviour
    {
        [SerializeField] private ObjectiveTask _task;
        public UnityEvent TaskCompletedEvent;

        private void OnEnable()
        {
            _task.OnTaskCompletionToggle += HandleTaskCompletionUpdate;
        }
        
        private void OnDisable()
        {
            _task.OnTaskCompletionToggle -= HandleTaskCompletionUpdate;
        }


        private void HandleTaskCompletionUpdate(ObjectiveTask task)
        {
            if (task.IsCompleted)
            {
                TaskCompletedEvent.Invoke();
            }
        }
    }
}