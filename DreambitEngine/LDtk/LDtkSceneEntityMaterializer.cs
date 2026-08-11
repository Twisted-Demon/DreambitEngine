using System.Collections.Generic;
using Dreambit.ECS;

namespace Dreambit.LDtk;

internal static class LDtkSceneEntityMaterializer
{
    public static void Materialize(
        Scene scene,
        LDtkLevelInstance level,
        IReadOnlyList<EntityInstance> entityInstances)
    {
        foreach (var entityInstance in entityInstances)
        {
            var ldtkEntity = level.CreateEntityData(entityInstance);
            var entity = CreateEntity(scene, level, ldtkEntity);
            if (entity is null)
                continue;

            ResetSpawnedHierarchyTransforms(entity);
            level.ApplyDrawLayer(entity, ldtkEntity.Instance);
            level.TrackEntity(entity);
        }
    }

    private static Entity CreateEntity(Scene scene, LDtkLevelInstance level, LDtkEntity ldtkEntity)
    {
        if (scene is LDtkScene ldtkScene &&
            LDtkEntityBuilderRepository.TryGetEntityBuilder(ldtkEntity.Identifier, out var builder))
        {
            return builder.BuildEntity(ldtkScene, level, ldtkEntity);
        }

        if (ldtkEntity.TryGetField<string>("Blueprint", out var blueprintPath) &&
            !string.IsNullOrWhiteSpace(blueprintPath))
        {
            var blueprint = Resources.LoadAsset<EntityBlueprint>(blueprintPath);
            if (blueprint is null)
            {
                scene.Logger.Warn(
                    $"Could not load blueprint '{blueprintPath}' for LDtk entity " +
                    $"'{ldtkEntity.Identifier}' ({ldtkEntity.Iid}).");
                return null;
            }

            scene.Logger.Info(
                $"Loading Entity {ldtkEntity.Identifier} from Blueprint: {blueprintPath}");
            return scene.CreateChildOfEntity(
                blueprint,
                level.RootEntity,
                createAt: ldtkEntity.Position.ToVector3());
        }

        scene.Logger.Info($"Generating Entity: {ldtkEntity.Identifier}");
        var entity = scene.CreateEntity(
            ldtkEntity.Identifier,
            createAt: ldtkEntity.Position.ToVector3(),
            tags: [..ldtkEntity.Tags]);
        entity.Parent = level.RootEntity;
        return entity;
    }

    private static void ResetSpawnedHierarchyTransforms(Entity entity)
    {
        entity.Transform.ResetLastWorldPosition();
        foreach (var child in entity.GetChildren())
            child.Transform.ResetLastWorldPosition();
    }
}
