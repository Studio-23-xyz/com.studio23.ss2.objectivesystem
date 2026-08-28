using System;
using BDeshi.Logging;
using NaughtyAttributes;
using Newtonsoft.Json;
using Studio23.SS2.InventorySystem.Data;
using UnityEditor;
using UnityEngine;

namespace Studio23.SS2.ObjectiveSystem.Core
{
    public abstract class ObjectiveHintBase : ItemBase, ISubCategoryLoggerMixin<ObjectiveLogCategory>
    {
        [SerializeField] private ObjectiveBase _parentObjective;
        [SerializeField] private int _priority;
        [NonSerialized][ShowNonSerializedField]
        protected bool _isActive = false;
        public ObjectiveBase ParentObjective => _parentObjective;
        public int Priority => _priority;
        public bool IsActive => _isActive;
        public GameObject gameObject => ObjectiveManager.Instance.gameObject;
        public ICategoryLogger<ObjectiveLogCategory> Logger => ObjectiveManager.Instance.Logger;
        public ObjectiveLogCategory Category => ObjectiveLogCategory.Hint;
        public event Action<ObjectiveHintBase> OnHintActivationToggled;

        public abstract string GetLocalizedHintName();

        public abstract string GetLocalizedHintDescription();

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

        [Button(enabledMode:EButtonEnableMode.Playmode)]
        public void Add()
        {
            if (!ObjectiveManager.Instance.IsObjectiveActiveAndValid(_parentObjective))
            {
                Logger.LogWarning(ObjectiveLogCategory.Hint,$"can't add hint {this} because parent objective is not active and valid");
                return;
            }
            SetActive(true);
        }

        public void FullReset()
        {
            Remove();
        }
        public void Remove()
        {
            if (!ObjectiveManager.Instance.IsObjectiveActiveAndValid(ParentObjective))
            {
                Debug.LogWarning($"can't remove hint {this} because parent objective is not active and valid");
                return;
            }
            Debug.Log($"Remove Hint {this} ", this);
            Reset();
        }
        
        private void Reset()
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