using System;

namespace Dreambit;

public abstract class Singleton<T> where T : class
{
    private static T _instance;
    protected Logger<T> Logger { get; } = new();

    public static T Instance
    {
        get
        {
            if (_instance != null) return _instance;

            _instance = (T)Activator.CreateInstance(typeof(T));
            return _instance;
        }
    }
}