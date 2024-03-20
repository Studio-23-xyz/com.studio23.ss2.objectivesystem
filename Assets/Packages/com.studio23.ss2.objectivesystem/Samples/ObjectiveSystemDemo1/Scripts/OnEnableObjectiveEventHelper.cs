using System;
using Cysharp.Threading.Tasks;
using Studio23.SS2.ObjectiveSystem.Core;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Studio23.SS2.ObjectiveSystem.Samples.ObjectiveSystemDemo1
{
    public class OnEnableObjectiveEventHelper : MonoBehaviour
    {
        public UnityEvent EnableEvent;
        public UnityEvent DisableEvent;
        public UnityEvent StartEvent;

        private async void OnEnable()
        {
            //it's only when we try to modify objectives on awake/start after creation we get problems.
            //this weird singleton waiting is due to savemanager dependency
            //realistically, this is never a problem in normal gameplay
            //because the singletons will be loaded properly by the time most events are fired
            await ObjectiveManager.Instance.WaitForInitialization();
            EnableEvent.Invoke();
        }

        private async void OnDisable()
        {
            await ObjectiveManager.Instance.WaitForInitialization();
            DisableEvent.Invoke();
        }

        private async void Start()
        {
            await ObjectiveManager.Instance.WaitForInitialization();
            StartEvent.Invoke();
        }
    }
}
