using System.Collections.Generic;
using Dreambit.ECS;
using LDtk;

namespace Dreambit.LDtk;

public class LDtkEntity<T> where T: new()
{
    protected Scene Scene = Core.Instance.CurrentScene;

    protected virtual void SetUp(LDtkLevel level)
    {
    }

    public static void SetUpEntities(LDtkLevel level)
    {
        var ldtkEntities = level.GetEntities<T>();

        foreach (var ldtkEntity in ldtkEntities)
        {
            var entity = ldtkEntity as LDtkEntity<T>;
            entity?.SetUp(level);
        }
    }

    protected Entity CreateEntity<TU>(TU data, string name = null, HashSet<string> tags = null)
        where TU : ILDtkEntity
    {
        name ??= data.Identifier;
        var entity = Entity.Create(name, tags, createAt: data.Position.ToVector3(), guidOverride: data.Iid);

        return entity;
    }
}
