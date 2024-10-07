using System;

namespace PixelariaEngine;

public class Singleton<T> where T : class
{
    private static T _instance;

    protected Logger<T> Logger = new();

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