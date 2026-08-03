# Logging and debugging

Set the global threshold before the game runs:

```csharp
Core.Level = LogLevel.Debug;
```

Typed scenes (`Scene<T>`) expose a protected logger. Components already contain
a protected logger associated with their runtime type:

```csharp
Logger.Info("Wave {0} started", waveNumber);
Logger.Warn("Spawn point {0} is missing", spawnName);
Logger.Error("Could not load encounter data");
```

Enable scene debug rendering to invoke component `OnDebugDraw` hooks and the
debug render pass:

```csharp
DebugMode = true;
```

Colliders, path followers, lights, sprite bounds, and custom components can draw
diagnostics there. Keep debug drawing side-effect free.

If a component callback throws, Dreambit quarantines the owning entity and logs
the entity name, ID, component type, callback, and exception. Check
`Entity.IsFaulted`, `FaultSource`, `FaultCallback`, and `FaultException` while
diagnosing the failure.
