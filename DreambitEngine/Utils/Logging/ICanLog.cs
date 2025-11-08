namespace Dreambit;

public interface ICanLog<T> where T : class
{
    protected static ILogger Logger => new Logger<T>();
}