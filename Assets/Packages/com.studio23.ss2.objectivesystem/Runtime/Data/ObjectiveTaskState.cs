namespace Studio23.SS2.ObjectiveSystem.Data
{
    public enum ObjectiveTaskState
    {
        NotStarted,// Not added yet. Won't be shown in UI
        InProgress,// Added but not completed. shows in UI
        Completed,// Added and completed. shows in UI
    }
}