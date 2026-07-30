using System;
using System.Collections.Generic;
using System.Linq;
using Dreambit.ECS;

namespace Dreambit;

public sealed class BlueprintSpawnContext
{
    private readonly Dictionary<Guid, Entity> _entitiesByBlueprintGuid = [];
    
    public EntityBlueprint RootBlueprint { get; }

    public IReadOnlyList<EntityBlueprint> Hierarchy { get; }

    public IReadOnlyDictionary<Guid, EntityBlueprint> BlueprintsByGuid { get; }

    public IReadOnlyDictionary<Guid, Entity> SpawnedEntities =>
        _entitiesByBlueprintGuid;

    public BlueprintSpawnContext(EntityBlueprint rootBlueprint)
    {
        RootBlueprint = rootBlueprint
                        ?? throw new ArgumentNullException(nameof(rootBlueprint));

        Hierarchy = rootBlueprint
            .FlattenedHierarchy()
            .ToArray();

        var blueprintsByGuid =
            new Dictionary<Guid, EntityBlueprint>(Hierarchy.Count);

        foreach (var blueprint in Hierarchy)
        {
            if (blueprint.Guid == Guid.Empty)
            {
                throw new InvalidOperationException(
                    $"Blueprint entity '{blueprint.Name}' has an empty GUID.");
            }

            if (!blueprintsByGuid.TryAdd(blueprint.Guid, blueprint))
            {
                throw new InvalidOperationException(
                    $"Blueprint '{rootBlueprint.Name}' contains duplicate " +
                    $"entity GUID '{blueprint.Guid}'.");
            }
        }

        BlueprintsByGuid = blueprintsByGuid;
    }
    
    public void Register(EntityBlueprint blueprint, Entity entity)
    {
        ArgumentNullException.ThrowIfNull(blueprint);
        ArgumentNullException.ThrowIfNull(entity);

        if (!BlueprintsByGuid.ContainsKey(blueprint.Guid))
        {
            throw new InvalidOperationException(
                $"Entity blueprint '{blueprint.Name}' is not part " +
                "of this spawn context.");
        }

        if (!_entitiesByBlueprintGuid.TryAdd(
                blueprint.Guid,
                entity))
        {
            throw new InvalidOperationException(
                $"A runtime entity is already registered for " +
                $"blueprint GUID '{blueprint.Guid}'.");
        }
    }
    
    public bool TryGetEntity(Guid blueprintGuid, out Entity entity)
    {
        return _entitiesByBlueprintGuid.TryGetValue(
            blueprintGuid,
            out entity!);
    }

    public Entity GetEntity(Guid blueprintGuid)
    {
        if (_entitiesByBlueprintGuid.TryGetValue(
                blueprintGuid,
                out var entity))
        {
            return entity;
        }

        throw new KeyNotFoundException(
            $"No runtime entity was registered for " +
            $"blueprint GUID '{blueprintGuid}'.");
    }
}