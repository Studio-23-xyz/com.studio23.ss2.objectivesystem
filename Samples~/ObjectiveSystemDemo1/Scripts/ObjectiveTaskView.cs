using Studio23.SS2.ObjectiveSystem.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Studio23.SS2.ObjectiveSystem.Samples.ObjectiveSystemDemo1
{
    public class ObjectiveTaskView : MonoBehaviour
    {
        [SerializeField] Toggle _checkMark;
        [SerializeField] TextMeshProUGUI _text;
        public void LoadTaskData(ObjectiveTask task) { 
            _checkMark.isOn = task.IsCompleted;
            _text.text = task.Name;
        }
    }
}
