using System;

namespace PixelariaEngine;

public abstract class Singleton<T> where T : class
{
    protected Logger<T> Logger { get; } = new();
    private static T _instance;

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