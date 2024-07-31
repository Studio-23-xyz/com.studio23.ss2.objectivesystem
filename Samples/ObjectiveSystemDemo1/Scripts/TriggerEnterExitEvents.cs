using System;
using UnityEngine;
using UnityEngine.Events;

namespace Studio23.SS2.ObjectiveSystem.Samples.ObjectiveSystemDemo1
{
    public class TriggerEnterExitEvents:MonoBehaviour
    {
        public UnityEvent EnterEvent;
        public UnityEvent ExitEvent;
        private void OnTriggerEnter(Collider other)
        {
            if(other.gameObject.CompareTag("Player"))
                EnterEvent.Invoke();
        }

        private void OnTriggerExit(Collider other)
        {
            if(other.gameObject.CompareTag("Player"))
                ExitEvent.Invoke();
        }
    }
}