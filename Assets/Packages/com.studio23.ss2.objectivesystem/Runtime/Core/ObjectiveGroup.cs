using System.Collections.Generic;
using UnityEngine;

namespace Studio23.SS2.ObjectiveSystem.Core
{
    /// <summary>
    /// When we have multiple simultaneuous objectives,
    /// this can be used to start/end them en masse
    /// </summary>
    [CreateAssetMenu(menuName = "Studio-23/Objective System/Basic Objective", fileName = "Basic Objective")]
    public class ObjectiveGroup:ScriptableObject
    {
        [SerializeField]List<ObjectiveBase> _objectives;

        public void StartObjectives()
        {
            foreach (var objective in _objectives)
            {
                objective.StartObjective();
            }
        }

        public void EndObjectives()
        {
            foreach (var objective in _objectives)
            {
                objective.EndObjective();
            }
        }
    }
}