namespace PixelariaEngine;

public class Logger<T> where T : class
{
    private readonly string _baseType = typeof(T).Name;
    public LogLevel Level { get; set; } = LogLevel.None;

    public void Trace(string format, params object[] args)
    {
        if (!CheckLogLevel(LogLevel.Trace)) return;
        Log.Trace(_baseType, format, args);
    }

    public void Trace(string message)
    {
        if (!CheckLogLevel(LogLevel.Trace)) return;
        Log.Trace(_baseType, message);
    }

    public void Info(string format, params object[] args)
    {
        if (!CheckLogLevel(LogLevel.Info)) return;
        Log.Info(_baseType, format, args);
    }

    public void Info(string message)
    {
        if (!CheckLogLevel(LogLevel.Info)) return;
        Log.Info(_baseType, message);
    }

    public void Debug(string format, params object[] args)
    {
        if (!CheckLogLevel(LogLevel.Debug)) return;
        Log.Debug(_baseType, format, args);
    }

    public void Debug(string message)
    {
        if (!CheckLogLevel(LogLevel.Debug)) return;
        Log.Debug(_baseType, message);
    }

    public void Warn(string format, params object[] args)
    {
        if (!CheckLogLevel(LogLevel.Warn)) return;
        Log.Warn(_baseType, format, args);
    }

    public void Warn(string message)
    {
        if (!CheckLogLevel(LogLevel.Warn)) return;
        Log.Warn(_baseType, message);
    }

    public void Error(string format, params object[] args)
    {
        if (!CheckLogLevel(LogLevel.Error)) return;
        Log.Error(_baseType, format, args);
    }

    public void Error(string message)
    {
        if (!CheckLogLevel(LogLevel.Error)) return;
        Log.Error(_baseType, message);
    }

    private bool CheckLogLevel(LogLevel logLevelToCheck)
    {
        var logLevel = Core.LogLevel;
        
        if(Level != LogLevel.None)
            logLevel = Level;

        return logLevel <= logLevelToCheck;
    }
}