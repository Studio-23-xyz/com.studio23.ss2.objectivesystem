using System;
using UnityEngine;

namespace Studio23.SS2.ObjectiveSystem.Samples.ObjectiveSystemDemo1
{
    public class SaveSystemInitializer:MonoBehaviour
    {
        private void Awake()
        {
            var saveSystem = GetComponent<SaveSystem.Core.SaveSystem>();
            saveSystem.Initialize();
            saveSystem.SelectLastSelectedSlot();
        }
    }
}