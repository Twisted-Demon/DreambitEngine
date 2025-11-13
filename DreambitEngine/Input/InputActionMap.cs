using System.Collections.Generic;

namespace Dreambit;

public abstract class InputActionMap<T> : InputActionMap
{
    public override string Name { get; init; } = typeof(T).Name;
}

public abstract class InputActionMap
{
    public abstract string Name { get; init; }
    protected readonly List<InputAction> Actions = new(16);
    protected readonly Dictionary<string, InputAction> ByName = new();
    protected bool Enabled = true;
    
    public InputAction Add(InputAction action)
    {
        if (ByName.ContainsKey(action.Name)) return null;
        
        Actions.Add(action);
        ByName[action.Name] = action;
        return action;
    }

    public InputAction Find(string name)
    {
        return ByName.GetValueOrDefault(name);
    }

    public bool Remove(InputAction action)
    {
        if (!ByName.ContainsKey(action.Name)) return false;
        
        Actions.Remove(action);
        ByName.Remove(action.Name);
        return true;
    }
    
    public void Enable()  => Enabled = true;
    public void Disable() => Enabled = false;
    public void SetEnabled(bool v) => Enabled = v;
    
    internal void Update()
    {
        if (!Enabled) return;

        foreach (var action in Actions)
        {
            action.Update();
        }
    }
}