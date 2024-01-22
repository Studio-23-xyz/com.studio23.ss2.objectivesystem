using System;
using NaughtyAttributes;
using Newtonsoft.Json;
using Studio23.SS2.InventorySystem.Data;
using Studio23.SS2.ObjectiveSystem.Data;
using UnityEngine;

namespace Studio23.SS2.ObjectiveSystem.Core
{
    public enum MyTaskState
    {
        NotStarted,// Not added yet. Won't be shown in UI
        InProgress,// Added but not completed. shows in UI
        Completed,// Added and completed. shows in UI
    }
    
    [CreateAssetMenu(menuName = "Cy/My Task", fileName = "My Task")]
    [Serializable]
    public class MyTask : ItemBase
    {
        [ShowNonSerializedField]
        private MyTaskState _state;
        
        public override void AssignSerializedData(string data)
        {
            _state = JsonConvert.DeserializeObject<MyTaskState>(data);
        }

        public override string GetSerializedData()
        {
            return JsonConvert.SerializeObject(_state);
        }

        public override string ToString()
        {
            return $"{Name} {_state}";
        }
        
        public ObjectiveBase ParentObjective => _parentObjective;
        [SerializeField] ObjectiveBase _parentObjective;
        
        public void SetObjective(ObjectiveBase parentObjective)
        {
            _parentObjective = parentObjective;
        }
        public void AddTask()
        {
            
            _state = MyTaskState.InProgress;
           
        }
        public void RemoveTask()
        {
            
            _state = MyTaskState.NotStarted;
            
        }
        public void ResetProgress()
        {
            _state = MyTaskState.NotStarted;
            
        }
        public void CompleteTask()
        {
            

            _state = MyTaskState.Completed;
         
        }

    }
}