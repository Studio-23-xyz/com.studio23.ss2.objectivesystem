using System;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Localization;

namespace Studio23.SS2.ObjectiveSystem.Core
{
    [CreateAssetMenu(menuName = "Studio-23/Objective System/Hint", fileName = "objective Hint")]
    [Serializable]
    public class ObjectiveHint: ObjectiveHintBase
    {
        public LocalizedString LocalizedHintName;
        public LocalizedString LocalizedHintDescription;
        
        public override string GetLocalizedHintName(){
            return LocalizedHintName.GetLocalizedString();
        }

        public override string GetLocalizedHintDescription(){
            return LocalizedHintDescription.GetLocalizedString();
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

