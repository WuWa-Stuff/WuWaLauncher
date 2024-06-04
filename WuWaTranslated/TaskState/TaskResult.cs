using System.ComponentModel;

namespace WuWaTranslated.TaskState;

public class TaskResult
{
    public readonly Enums.TaskState State;

    public readonly string? ErrorMessage;
    public readonly Exception? ErrorException;

    public TaskResult(string errorMessage, Exception? exception = default) : this(Enums.TaskState.Failed)
    {
        ErrorMessage = errorMessage;
        ErrorException = exception;
    }

    protected TaskResult(Enums.TaskState state)
    {
        State = state;
    }

    public TaskResult<TNew> To<TNew>()
    {
        if (State is Enums.TaskState.Success)
            throw new NotSupportedException("Unable to convert TaskResult object when State is Success");

        if (State is Enums.TaskState.Cancelled)
            return TaskResult<TNew>.Cancelled;

        return new TaskResult<TNew>(ErrorMessage!, ErrorException);
    }

    public static implicit operator TaskResult(Enums.TaskState state)
    {
        if (state is Enums.TaskState.Failed)
            throw new InvalidEnumArgumentException(nameof(state), (int)state, typeof(Enums.TaskState));

        return new TaskResult(state);
    }

    public static implicit operator TaskResult(Exception exception)
        => new(exception.Message, exception);
}