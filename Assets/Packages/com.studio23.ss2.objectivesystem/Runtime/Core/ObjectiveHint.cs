using System;
using NaughtyAttributes;
using Newtonsoft.Json;
using Studio23.SS2.InventorySystem.Data;
using UnityEngine;
using UnityEngine.Serialization;

namespace Studio23.SS2.ObjectiveSystem.Core
{
    [CreateAssetMenu(menuName = "Studio-23/Objective System/Hint", fileName = "objective Hint")]
    [Serializable]
    public class ObjectiveHint: ItemBase
    {
        [SerializeField] ObjectiveBase _parentObjective;
        public ObjectiveBase ParentObjective => _parentObjective;
        [SerializeField] int _priority;
        public int Priority => _priority;

        [NonSerialized][ShowNonSerializedField] private bool _isActive = false;
        public bool IsActive => _isActive;
        public event Action<ObjectiveHint> OnHintActivationToggled;
        public bool ObjectiveManagerExists => ObjectiveManager.Instance != null;

        public void SetObjective(ObjectiveBase objective)
        {
            this._parentObjective = objective;
        }

        public void SetActive(bool shouldBeActive)
        {
            if (shouldBeActive == IsActive)
                return;
            _isActive = shouldBeActive;
            OnHintActivationToggled?.Invoke(this);
        }
        [ShowIf("ObjectiveManagerExists")]
        [Button]
        public void Add()
        {
            SetActive(true);
        }

        public void FullReset()
        {
            Remove();
        }
        [ShowIf("ObjectiveManagerExists")]
        [Button]
        public void Remove()
        {
            SetActive(false);
        }
        public override void AssignSerializedData(string data)
        {
            _isActive = JsonConvert.DeserializeObject<bool>(data);
        }

        public override string GetSerializedData()
        {
            return JsonConvert.SerializeObject(_isActive);
        }
    }
}

