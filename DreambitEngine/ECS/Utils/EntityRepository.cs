using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace Dreambit.ECS;

public class EntityRepository
    {
        private readonly Scene _scene;

        // Active entities
        private readonly List<Entity> _entities = new(256);
        private readonly HashSet<Entity> _entitiesSet = new();         // O(1) membership for fast checks
        private readonly Dictionary<Guid, Entity> _entitiesById = new(256);

        // Creation queue + O(1) membership + Id index (pending)
        private readonly List<Entity> _entitiesToCreate = new(64);
        private readonly HashSet<Entity> _entitiesToCreateSet = new();
        private readonly Dictionary<Guid, Entity> _toCreateById = new(64);

        // Destruction queue + O(1) membership
        private readonly List<Entity> _entitiesToDestroy = new(64);
        private readonly HashSet<Entity> _entitiesToDestroySet = new();

        // Optional “always update” bucket (kept as-is; semantics unchanged)
        private readonly List<Entity> _alwaysUpdateEntities = new(16);

        private readonly Logger<EntityRepository> _logger = new();

        public EntityRepository(Scene scene) { _scene = scene; }

        internal Entity CreateEntity(string name, HashSet<string> tags, bool enabled, Vector3? createAt, Guid? guidOverride = null)
        {
            tags ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "default" };

            var guid = guidOverride ?? Guid.NewGuid();
            var entity = new Entity(guid, name, tags, enabled, _scene);
            entity.Transform.Entity = entity;

            if (createAt.HasValue)
            {
                entity.Transform.Position = createAt.Value;
                entity.Transform.LastWorldPosition = entity.Transform.Position;
            }

            // Queue creation if not already queued/added
            if (!_entitiesSet.Contains(entity) && !_entitiesToCreateSet.Contains(entity))
            {
                _entitiesToCreate.Add(entity);
                _entitiesToCreateSet.Add(entity);
                _toCreateById[entity.Id] = entity;
            }

            return entity;
        }

        internal void DestroyEntity(Entity entity)
        {
            if (entity == null)
            {
                Console.WriteLine("Could not destroy entity, entity is null");
                return;
            }

            // Already queued for destroy?
            if (_entitiesToDestroySet.Contains(entity))
            {
                Console.WriteLine("Entity {0} is already being removed", entity.Name);
                return;
            }

            // If it’s still in the creation queue, cancel the creation
            if (_entitiesToCreateSet.Contains(entity))
            {
                // Remove from create list & indices
                _entitiesToCreateSet.Remove(entity);
                _toCreateById.Remove(entity.Id);

                // Remove from list without reallocating too much
                for (int i = 0; i < _entitiesToCreate.Count; i++)
                {
                    if (ReferenceEquals(_entitiesToCreate[i], entity))
                    {
                        int last = _entitiesToCreate.Count - 1;
                        _entitiesToCreate[i] = _entitiesToCreate[last];
                        _entitiesToCreate.RemoveAt(last);
                        break;
                    }
                }
                return;
            }

            // If it exists in active set, queue for deletion
            if (_entitiesSet.Contains(entity))
            {
                _entitiesToDestroy.Add(entity);
                _entitiesToDestroySet.Add(entity);
            }
        }

        internal void ClearLists()
        {
            // Destroy everything in all buckets
            for (int i = 0; i < _entitiesToCreate.Count; i++) _entitiesToCreate[i].Destroy();
            for (int i = 0; i < _entitiesToDestroy.Count; i++) _entitiesToDestroy[i].Destroy();
            for (int i = 0; i < _entities.Count; i++) _entities[i].Destroy();

            _entities.Clear(); _entitiesSet.Clear(); _entitiesById.Clear();
            _entitiesToCreate.Clear(); _entitiesToCreateSet.Clear(); _toCreateById.Clear();
            _entitiesToDestroy.Clear(); _entitiesToDestroySet.Clear();
            _alwaysUpdateEntities.Clear();
        }

        private void UpdateEntities()
        {
            // Update enabled entities
            for (int i = 0; i < _entities.Count; i++)
            {
                var e = _entities[i];
                if (e.Enabled) e.Update();
            }

            // If “always update” should override Enabled, do it here (kept same behavior: no override).
            // for (int i = 0; i < _alwaysUpdateEntities.Count; i++) _alwaysUpdateEntities[i].Update();
        }

        private void PhysicsUpdateEntities()
        {
            for (int i = 0; i < _entities.Count; i++)
            {
                var e = _entities[i];
                if (e.Enabled) e.PhysicsUpdate();
            }
        }

        public void SetEntityAlwaysUpdate(Entity entity, bool value)
        {
            if (value)
            {
                if (!_alwaysUpdateEntities.Contains(entity))
                    _alwaysUpdateEntities.Add(entity);
            }
            else
            {
                // Remove without preserving order to be O(1) average
                for (int i = 0; i < _alwaysUpdateEntities.Count; i++)
                {
                    if (ReferenceEquals(_alwaysUpdateEntities[i], entity))
                    {
                        int last = _alwaysUpdateEntities.Count - 1;
                        _alwaysUpdateEntities[i] = _alwaysUpdateEntities[last];
                        _alwaysUpdateEntities.RemoveAt(last);
                        break;
                    }
                }
            }
        }

        private void HandleEntityCreations()
        {
            // Move from create queue -> active, updating indices
            for (int i = 0; i < _entitiesToCreate.Count; i++)
            {
                var e = _entitiesToCreate[i];
                if (_entitiesSet.Contains(e)) continue;

                _entities.Add(e);
                _entitiesSet.Add(e);
                _entitiesById[e.Id] = e;
                _toCreateById.Remove(e.Id);

                e.OnAddedToScene();
            }

            _entitiesToCreate.Clear();
            _entitiesToCreateSet.Clear();
        }

        private void HandleEntityDeletions()
        {
            // Remove from active, updating indices
            for (int i = 0; i < _entitiesToDestroy.Count; i++)
            {
                var e = _entitiesToDestroy[i];
                if (!_entitiesSet.Contains(e)) continue;

                // Remove from active list without O(n) shifting (swap pop)
                for (int j = 0; j < _entities.Count; j++)
                {
                    if (ReferenceEquals(_entities[j], e))
                    {
                        int last = _entities.Count - 1;
                        _entities[j] = _entities[last];
                        _entities.RemoveAt(last);
                        break;
                    }
                }

                _entitiesSet.Remove(e);
                _entitiesById.Remove(e.Id);

                e.OnRemovedFromScene();
                e.Destroy();
                e.Dispose();
            }

            _entitiesToDestroy.Clear();
            _entitiesToDestroySet.Clear();
        }

        internal void OnTick()
        {
            HandleEntityCreations();
            UpdateEntities();
            HandleEntityDeletions();
        }

        internal void OnPhysicsTick()
        {
            PhysicsUpdateEntities();
        }

        public Entity GetEntity(Guid id)
        {
            // O(1): prefer active-by-id
            if (_entitiesById.TryGetValue(id, out var active))
                return active;

            // Also check pending creations
            if (_toCreateById.TryGetValue(id, out var pending))
                return pending;

            return null;
        }

        public Entity GetEntity(string name)
        {
            // Preserve original “first match” behavior without LINQ
            // Active first
            for (int i = 0; i < _entities.Count; i++)
                if (_entities[i].Name == name) return _entities[i];

            // Then pending creations
            for (int i = 0; i < _entitiesToCreate.Count; i++)
                if (_entitiesToCreate[i].Name == name) return _entitiesToCreate[i];

            return null;
        }

        public IReadOnlyList<Entity> GetEntitiesByTag(string tag)
        {
            // Rebuild result each call to stay correct if Tags mutate at runtime
            var result = new List<Entity>(16);

            for (int i = 0; i < _entities.Count; i++)
            {
                var e = _entities[i];
                if (e.Tags != null && e.Tags.Contains(tag))
                    result.Add(e);
            }

            for (int i = 0; i < _entitiesToCreate.Count; i++)
            {
                var e = _entitiesToCreate[i];
                if (e.Tags != null && e.Tags.Contains(tag))
                    result.Add(e);
            }

            return result;
        }
        
        public IReadOnlyList<Entity> GetActiveEntitiesByTag(string tag)
        {
            // Rebuild result each call to stay correct if Tags mutate at runtime
            var result = new List<Entity>(_entities.Count);

            for (int i = 0; i < _entities.Count; i++)
            {
                var e = _entities[i];
                if (e.Tags != null && e.Tags.Contains(tag) && e.Enabled)
                    result.Add(e);
            }
            
            return result;
        }

        public IReadOnlyList<Entity> GetAllActiveEntities()
        {
            // Allocate once per call, but pre-size
            var result = new List<Entity>(_entities.Count);
            for (int i = 0; i < _entities.Count; i++)
            {
                var e = _entities[i];
                if (e.Enabled) result.Add(e);
            }
            return result;
        }

        public List<Entity> GetAllAddedEntities()
        {
            // Copy without LINQ
            return new List<Entity>(_entities);
        }

        public List<Entity> GetAllEntities()
        {
            var result = new List<Entity>(_entities.Count + _entitiesToCreate.Count);
            result.AddRange(_entities);
            result.AddRange(_entitiesToCreate);
            return result;
        }
    }