using System.Collections.Generic;

namespace Dreambit;

/// <summary>Samples devices and updates registered action maps once per frame.</summary>
public class InputSystem : Singleton<InputSystem>
{
    private readonly Dictionary<string, InputActionMap> _byName = new();
    private readonly List<InputActionMap> _maps = new();

    public void Register(InputActionMap map)
    {
        if (!_byName.TryAdd(map.Name, map)) return;

        _maps.Add(map);
    }

    public void Unregister(InputActionMap map)
    {
        _maps.Remove(map);
        _byName.Remove(map.Name);
    }

    public InputActionMap Get(string mapName) => _byName.GetValueOrDefault(mapName);

    /// <summary>Samples raw device state and resets per-frame UI consumption.</summary>
    public void PreUpdate()
    {
        Input.PreUpdate();
    }

    /// <summary>Updates action maps after UI has had an opportunity to consume input.</summary>
    public void Update()
    {
        foreach (var map in _maps)
            map.Update();
    }

    /// <summary>Advances current device state to the previous-frame snapshot.</summary>
    public void PostUpdate()
    {
        Input.PostUpdate();
    }
}
