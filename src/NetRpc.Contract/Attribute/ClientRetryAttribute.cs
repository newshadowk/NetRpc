namespace NetRpc.Contract;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Interface, Inherited = false)]
public sealed class ClientRetryAttribute : Attribute
{
    /// <param name="sleepDurations">Milliseconds</param>
    public ClientRetryAttribute(params int[] sleepDurations) : this(typeof(Exception), sleepDurations)
    {
    }

    public ClientRetryAttribute(Type exceptionType, params int[] sleepDurations) : this(new[] { exceptionType }, sleepDurations)
    {
    }

    public ClientRetryAttribute(Type[] exceptionTypes, params int[] sleepDurations)
    {
        SleepDurations = sleepDurations;
        ExceptionTypes = exceptionTypes;
        if (sleepDurations.Length == 0)
            throw new ArgumentException("sleepDurations length can not be 0");
    }

    /// <summary>
    /// Milliseconds.
    /// </summary>
    public int[] SleepDurations { get; }

    public Type[] ExceptionTypes { get; }
}