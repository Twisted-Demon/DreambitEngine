namespace Dreambit;

public interface ILogger
{
    public void Trace(string format, params object[] args);
    public void Trace(string message);
    public void Info(string format, params object[] args);
    public void Info(string message);
    public void Debug(string format, params object[] args);
    public void Debug(string message);
    public void Warn(string format, params object[] args);
    public void Warn(string message);
    public void Error(string format, params object[] args);
    public void Error(string message);
}