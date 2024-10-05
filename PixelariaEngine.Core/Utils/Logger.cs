namespace PixelariaEngine;

public class Logger<T> where T : class
{
    private readonly string _baseType = typeof(T).Name;

    public void Trace(string format, params object[] args)
    {
        Log.Trace(_baseType, format, args);
    }

    public void Trace(string message)
    {
        Log.Trace(_baseType, message);
    }

    public void Info(string format, params object[] args)
    {
        Log.Info(_baseType, format, args);
    }

    public void Info(string message)
    {
        Log.Info(_baseType, message);
    }

    public void Debug(string format, params object[] args)
    {
        Log.Debug(_baseType, format, args);
    }

    public void Debug(string message)
    {
        Log.Debug(_baseType, message);
    }

    public void Warn(string format, params object[] args)
    {
        Log.Warn(_baseType, format, args);
    }

    public void Warn(string message)
    {
        Log.Warn(_baseType, message);
    }

    public void Error(string format, params object[] args)
    {
        Log.Error(_baseType, format, args);
    }

    public void Error(string message)
    {
        Log.Error(_baseType, message);
    }
}