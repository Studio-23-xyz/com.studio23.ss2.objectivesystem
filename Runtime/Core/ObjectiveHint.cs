using System;
using BDeshi.Logging;
using NaughtyAttributes;
using Newtonsoft.Json;
using Studio23.SS2.InventorySystem.Data;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Studio23.SS2.ObjectiveSystem.Core
{
    [CreateAssetMenu(menuName = "Studio-23/Objective System/Hint", fileName = "objective Hint")]
    [Serializable]
    public class ObjectiveHint: ItemBase, ISubCategoryLoggerMixin<ObjectiveLogCategory>
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

        internal void SetActive(bool shouldBeActive)
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
            if (!ObjectiveManager.Instance.IsObjectiveActiveAndValid(_parentObjective))
            {
                Logger.LogWarning(ObjectiveLogCategory.Hint,$"can't remove hint {this} because parent objective is not active and valid");
                return;
            }
            Logger.Log(ObjectiveLogCategory.Hint,$"Remove Hint {this} ", this);

            Reset();
        }
        [Button(enabledMode:EButtonEnableMode.Playmode)]
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
#if UNITY_EDITOR     
        [Button]
        public void Rename()
        {
            Rename(_parentObjective.getFullHintAssetName(Name));
        }
                
        [Button]
        public void DestroyHint()
        {
            if (_parentObjective != null) 
            {
                Undo.RecordObject(_parentObjective, "Hint");
                _parentObjective.Hints.Remove(this);
            }

            EditorUtility.SetDirty(_parentObjective);
            Undo.DestroyObjectImmediate(this);
            AssetDatabase.SaveAssetIfDirty(_parentObjective);
        }
        
        
        public void Rename(string newName)
        {
            this.name = newName;

            AssetDatabase.SaveAssets();
            EditorUtility.SetDirty(this);
        }
#endif
        public GameObject gameObject => ObjectiveManager.Instance.gameObject;
        public ICategoryLogger<ObjectiveLogCategory> Logger => ObjectiveManager.Instance.Logger;
        public ObjectiveLogCategory Category => ObjectiveLogCategory.Hint;
    }
}

