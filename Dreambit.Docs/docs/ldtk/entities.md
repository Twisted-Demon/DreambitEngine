# Reading LDtk entities

Dreambit exposes entity instances as raw LDtk data and deliberately does not
instantiate ECS entities.

```csharp
foreach (var layer in level.LayerInstances ?? [])
{
    foreach (var instance in layer.EntityInstances ?? [])
    {
        Console.WriteLine($"{instance._Identifier}: {instance.Iid}");

        foreach (var field in instance.FieldInstances ?? [])
            Console.WriteLine($"  {field._Identifier}: {field._Value}");
    }
}
```

`FieldInstance.GetValue<T>()` deserializes a raw field value on demand.
`ResolveFilePath()` resolves FilePath fields relative to the LDtk project.
EntityRef fields can be followed with `ResolveEntityReference()` or parsed with
`TryGetEntityReference` and resolved later through `LDtkFile.ResolveEntity`.

Entity definitions remain available through `instance.Definition`; tileset
references on definitions and layers are connected to their corresponding
`TilesetDefinition` objects.
