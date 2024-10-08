using System;
using System.Collections.Generic;
using System.Linq;

namespace PixelariaEngine.ECS;

public class EntityList(Scene scene)
{
    private readonly List<Entity> _entities = [];
    private readonly List<Entity> _entitiesToCreate = [];
    private readonly List<Entity> _entitiesToDestroy = [];

    private uint _nextEntityId;

    internal Entity CreateEntity(string name, HashSet<string> tags, bool enabled)
    {
        tags ??= ["default"];
        
        //create the entity
        var entity = new Entity(_nextEntityId, name, tags, enabled, scene);
        _entitiesToCreate.Add(entity);

        _nextEntityId++; //increment for the next entity ID.

        return entity;
    }

    internal void DestroyEntity(Entity entity)
    {
        if (entity == null)
        {
            Console.WriteLine("Could not destroy entity, entity is null");
            return;
        }

        if (_entitiesToDestroy.Contains(entity))
        {
            Console.WriteLine("Entity {0} is already being removed", entity.Name);
            return;
        }

        if (_entitiesToCreate.Contains(entity))
        {
            _entitiesToCreate.Remove(entity);
            return;
        }

        if (!_entitiesToDestroy.Contains(entity))
            _entitiesToDestroy.Add(entity);
    }

    internal void ClearLists()
    {
        foreach (var entity in _entitiesToCreate)
            entity.Destroy();

        foreach (var entity in _entitiesToDestroy)
            entity.Destroy();

        foreach (var entity in _entities)
            entity.Destroy();
    }

    private void UpdateEntities()
    {
        foreach (var entity in _entities
                     .Where(entity => entity.Enabled))
            entity.Update();
    }

    private void UpdateLists()
    {
        //Handle Creation
        foreach (var entityToCreate in _entitiesToCreate
                     .Where(entityToCreate => !_entities.Contains(entityToCreate)))
        {
            _entities.Add(entityToCreate);
            entityToCreate.OnAddedToScene();
        }

        _entitiesToCreate.Clear();

        //Handle Deletion
        foreach (var entityToDestroy in _entitiesToDestroy
                     .Where(e => _entities.Contains(e)))
        {
            _entities.Remove(entityToDestroy);
            entityToDestroy.OnRemovedFromScene();
            entityToDestroy.Destroy();
            entityToDestroy.Dispose();
        }

        _entitiesToDestroy.Clear();
    }

    internal void OnTick()
    {
        UpdateLists();
        UpdateEntities();
    }

    public Entity GetEntity(uint id)
    {
        var entityResult = _entities.FirstOrDefault(entity => entity.Id == id);
        if (entityResult != null)
            return entityResult;

        entityResult = _entitiesToCreate.FirstOrDefault(entity => entity.Id == id);
        return entityResult;
    }

    public Entity GetEntity(string name)
    {
        var entityResult = _entities.FirstOrDefault(entity => entity.Name == name);
        if (entityResult != null)
            return entityResult;

        entityResult = _entitiesToCreate.FirstOrDefault(entity => entity.Name == name);
        return entityResult;
    }

    public List<Entity> GetEntitiesByTag(string tag)
    {
        var entites = _entities.Where(e => e.Tags.Contains(tag)).ToList();
        entites.AddRange(_entitiesToCreate.Where(e => e.Tags.Contains(tag)));

        return _entities;
    }
    

    public List<Entity> GetAllActiveEntitiesEntities()
    {
        return _entities.Where(entity => entity.Enabled).ToList();
    }

    public List<Entity> GetAllAddedEntities()
    {
        return _entities.ToList();
    }

    public List<Entity> GetAllEntities()
    {
        var list = new List<Entity>();
        list.AddRange(_entities);
        list.AddRange(_entitiesToCreate);
        return list;
    }
}