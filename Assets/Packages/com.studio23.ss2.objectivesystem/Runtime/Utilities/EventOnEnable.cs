using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Studio23.SS2.ObjectiveSystem.Utilities
{
    public class EventOnEnable : MonoBehaviour
    {
        public UnityEvent EnableEvent;
        public UnityEvent DisableEvent;

        private void OnEnable()
        {
            EnableEvent.Invoke();
        }

        private void OnDisable()
        {
            DisableEvent.Invoke();
        }
    }
}
