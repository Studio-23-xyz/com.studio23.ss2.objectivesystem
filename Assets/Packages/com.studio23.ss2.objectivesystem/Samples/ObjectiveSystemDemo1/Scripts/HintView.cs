using Studio23.SS2.ObjectiveSystem.Core;
using TMPro;
using UnityEngine;

namespace Studio23.SS2.ObjectiveSystem.Samples.ObjectiveSystemDemo1
{
    public class HintView : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI _hintTmp;

        public void LoadHint(ObjectiveHint objectiveHint)
        {
            _hintTmp.text = objectiveHint.Description;
        }
    }
}
