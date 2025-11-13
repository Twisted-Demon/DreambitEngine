using System.Collections.Generic;

namespace Dreambit;

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

    public void Update()
    {
        Input.PreUpdate();
        
        foreach (var map in _maps)
            map.Update();
    }

    public void PostUpdate()
    {
        Input.PostUpdate();
    }
}