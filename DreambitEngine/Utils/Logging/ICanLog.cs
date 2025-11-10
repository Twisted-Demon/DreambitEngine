namespace Dreambit;

public interface ICanLog<T> where T : class
{
    public ILogger Logger { get; }
}