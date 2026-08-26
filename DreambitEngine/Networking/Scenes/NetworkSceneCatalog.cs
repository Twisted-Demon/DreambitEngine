using System;
using System.Collections.Generic;

namespace Dreambit.Networking.Scenes;

/// <summary>Persistent game-defined mapping from stable network Scene keys to local factories.</summary>
public sealed class NetworkSceneCatalog
{
    private readonly Dictionary<string, Func<Scene>> _factories =
        new(StringComparer.Ordinal);
    private bool _frozen;

    public void Register(string key, Func<Scene> factory)
    {
        if (_frozen)
            throw new InvalidOperationException(
                "Network Scene registrations are frozen while a session is active.");
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(factory);
        if (key.Length > 256)
            throw new ArgumentException("A network Scene key cannot exceed 256 characters.", nameof(key));
        if (!_factories.TryAdd(key, factory))
            throw new InvalidOperationException($"Network Scene key '{key}' is already registered.");
    }

    public bool Contains(string key) =>
        !string.IsNullOrWhiteSpace(key) && _factories.ContainsKey(key);

    internal Scene Create(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        if (!_factories.TryGetValue(key, out var factory))
            throw new KeyNotFoundException($"Network Scene key '{key}' is not registered.");
        return factory() ??
               throw new InvalidOperationException($"Network Scene factory '{key}' returned null.");
    }

    internal void Freeze() => _frozen = true;
    internal void Unfreeze() => _frozen = false;
}
