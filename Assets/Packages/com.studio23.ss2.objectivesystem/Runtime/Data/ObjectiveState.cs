namespace Studio23.SS2.ObjectiveSystem.Data
{
    public enum ObjectiveState
    {
        NotStarted, 
        InProgress,// running
        Complete,// completed task but still show in UI
        Finished,// completed task don't  show in UI
        Cancelled,// haven't completed task but don't show in UI. Don't allow adding this objective again
    }
}