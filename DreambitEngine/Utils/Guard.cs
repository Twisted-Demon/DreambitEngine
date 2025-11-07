using System;
using System.Runtime.CompilerServices;

namespace Dreambit;

public class Guard
{
    private static readonly ILogger Logger = new Logger<Guard>();
    
    public static bool SafeCall(Action hook, string name)
    {
        // Runtime type of the instance the delegate is bound to (most-derived)
        var runtimeType = hook.Target?.GetType().Name;
        // The actual method the delegate will invoke (override if present)
        var method = hook.Method;
        
        // Prefer caller-supplied name, else "DerivedType.Method"
        var actionName = name ?? $"{( method.DeclaringType?.FullName ?? "<static>")}.{method.Name}";

        try 
        { 
            hook();
            return true; 
        }
        catch (Exception ex)
        {
            Logger.Error($"{actionName} threw '{ex}' (caller: {runtimeType})");
            return false;
        }
    }
}
