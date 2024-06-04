namespace WuWaTranslated.TaskState;

public class TaskResult<T> : TaskResult
{
    public static readonly TaskResult<T> Cancelled = new(Enums.TaskState.Cancelled);
    
    public readonly T? Content;

    public TaskResult(T content) : this(Enums.TaskState.Success)
    {
        Content = content;
    }

    public TaskResult(string errorMessage, Exception? exception) : base(errorMessage, exception)
    {
    }

    private TaskResult(Enums.TaskState taskState) : base(taskState)
    {
    }

    public static implicit operator TaskResult<T>(T content)
        => new(content);

    public static implicit operator TaskResult<T>(Exception exception)
        => new(exception.Message, exception);
}