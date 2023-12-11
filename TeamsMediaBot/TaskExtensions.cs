namespace TeamsMediaBot;

public static class TaskExtensions
{
    public static void OnException(this Task task, Action<AggregateException?> action) =>
        task.ContinueWith(it => action(it.Exception), TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);
}
