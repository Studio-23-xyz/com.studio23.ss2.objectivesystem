using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;
using Studio23.SS2.ObjectiveSystem.Core;
using UnityEngine;

public class ResourceTest : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        // foo();
    }
    [ContextMenu("foo")]
    [Button]
    public void foo()
    {
        Debug.LogError("break");
        var testAll  = Resources.LoadAll("Inventory System/Test");
        foreach (var testObjective in testAll)
        {
            Debug.LogError(testObjective, testObjective);
        }
        Debug.LogError("break");
        
        var testObjectives  = Resources.LoadAll<ObjectiveBase>("Inventory System/Test");
        foreach (var t in testObjectives)
        {
            Debug.LogError(t, t);
        }
        Debug.LogError("break");
        
        var testTasks  = Resources.LoadAll<ObjectiveTask>("Inventory System/Test");
        foreach (var t in testTasks)
        {
            Debug.LogError(t, t);
        }
        Debug.LogError("break");
        var testHints  = Resources.LoadAll<ObjectiveHint>("Inventory System/Test");
        foreach (var t in testHints)
        {
            Debug.LogError(t, t);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
