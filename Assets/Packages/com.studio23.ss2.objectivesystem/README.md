# Objective System
Objective System is a package for handling objectives, objective subtask etc. It also supports hints tied to tasks. 

## Objectives
An objective is a SO that contains tasks and hints. It maintains a list of active tasks and hints. When all of an objective's tasks are complete, the objective itself will get completed. 

An objective can be in the following states:
```csharp
public enum ObjectiveState
{
    NotStarted,// not started yet,
    InProgress,// running
    Complete,// completed task but still show in UI
    Finished,// completed task don't  show in UI
    Cancelled,// haven't completed task but don't show in UI. Don't allow adding this objective again
}
```
## Tasks
Tasks are SOs that are each tied to an objective. A task can be in 3 States:
```csharp
public enum ObjectiveTaskState
{
    NotStarted,// Not added yet. Won't be shown in UI. 
    InProgress,// Added but not completed. shows in UI
    Completed,// Added and completed. shows in UI
}
```
When a task is started, it is added to an objective's list of active tasks. Tasks that are not added will not be in the objective's active tasks list.
When a task is updated, the parent objective is notified and is completed if conditions are fulfilled.

## ObjectiveHints
Hints are unlockable tips for an objective. A hint can either be active or inactive. Once obtained, a hint will always be active and present in objective.ActiveHints.

# Usage
You need an `ObjectiveManager` and a `SaveSystem` singleton in your scene. The sample scene already has them.

Objectives and tasks do not implement start/completion logic. You have to call `objective.CompleteObjective()` or `task.CompleteTask()` yourself. However, handling dependencies and objective/task start/end/completion is handled by the objective system.

The functions need to called in correct order. Ex: Calling CompleteObjective before StartObjective will do nothing and fire a logwarning. The flowcharts show the order:

![](https://github.com/Studio-23-xyz/ObjectiveSystem/blob/b2e993c968145ede24ad1e76bc33be08412aacfb/Assets/Packages/com.studio23.ss2.objectivesystem/Screenshots~/objectiveState.png)

![](https://github.com/Studio-23-xyz/ObjectiveSystem/blob/fa5314e097a83cda23eee333f7f33db4b5b90a75/Assets/Packages/com.studio23.ss2.objectivesystem/Screenshots~/taskstate.png)

## Setting tasks/hints for an objective
You can edit the `Tasks` and `Hints` serialized list under the Objective SO to add/remove tasks/hints to the objective. Don't edit them in Playmode.

You don't need to edit the ActiveTasks/ActiveHints list.

You also don't need to set the `ParentObjective` field in the task/hint SO. That is automatically set based on the `Tasks` and `Hints` serialized list.

# Save/Load
Saving and loading requires that the SOs be put in the resources folder d in `Resources/Inventory System/Objectives`, `Resources/Inventory System/Tasks`,`Resources/Inventory System/Hints`. SOs that are not in those folder work in play mode but won't survive saving/loading.
