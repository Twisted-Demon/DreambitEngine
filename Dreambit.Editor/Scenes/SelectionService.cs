using Dreambit.ECS;

namespace Dreambit.Editor.Scenes;

internal sealed class SelectionService
{
    private readonly List<Guid> _entityIds = [];

    public event Action? Changed;
    public IReadOnlyList<Guid> EntityIds => _entityIds;
    public Guid? ActiveEntityId => _entityIds.Count == 0 ? null : _entityIds[^1];

    public bool Contains(Entity entity) => _entityIds.Contains(entity.Id);

    public void Set(Entity? entity, bool additive = false)
    {
        if (!additive)
            _entityIds.Clear();

        if (entity is not null)
        {
            if (additive && _entityIds.Remove(entity.Id))
            {
                Changed?.Invoke();
                return;
            }

            if (!_entityIds.Contains(entity.Id))
                _entityIds.Add(entity.Id);
        }

        Changed?.Invoke();
    }

    public void Restore(IEnumerable<Guid> entityIds)
    {
        _entityIds.Clear();
        _entityIds.AddRange(entityIds.Distinct());
        Changed?.Invoke();
    }

    public void RemoveMissing(Scene? scene)
    {
        if (scene is null)
        {
            Clear();
            return;
        }

        var removed = _entityIds.RemoveAll(id => scene.FindEntity(id) is null) > 0;
        if (removed)
            Changed?.Invoke();
    }

    public Entity? GetActive(Scene? scene) =>
        scene is not null && ActiveEntityId is { } id ? scene.FindEntity(id) : null;

    public IReadOnlyList<Entity> Resolve(Scene? scene)
    {
        if (scene is null)
            return [];

        var entities = new List<Entity>(_entityIds.Count);
        foreach (var id in _entityIds)
            if (scene.FindEntity(id) is { } entity)
                entities.Add(entity);
        return entities;
    }

    public void Clear()
    {
        if (_entityIds.Count == 0)
            return;
        _entityIds.Clear();
        Changed?.Invoke();
    }
}
