using System;
using Cysharp.Threading.Tasks;
using Studio23.SS2.SaveSystem.Interfaces;
using UnityEngine;

namespace Studio23.SS2.ObjectiveSystem.Samples.ObjectiveSystemDemo1
{
    public class SaveSystemInitializer:MonoBehaviour
    {
        public bool ShouldLoad = true;
        private void Awake()
        {
            var saveSystem = GetComponent<SaveSystem.Core.SaveSystem>();
            saveSystem.Initialize();
            saveSystem.SelectLastSelectedSlot();
        }

        private void Start()
        {
            if (ShouldLoad)
            {
                Debug.Log("FORCE LOAD");
                SaveSystem.Core.SaveSystem.Instance.Load();
            }
        }

    }
}