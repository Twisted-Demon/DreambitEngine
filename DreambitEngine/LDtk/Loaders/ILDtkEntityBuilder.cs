using Dreambit.ECS;

namespace Dreambit.LDtk.Loaders;

public interface ILDtkEntityBuilder
{
    string EntityDefinitionIdentifier { get; }

    Entity BuildEntity(LDtkScene scene, LDtkLevelInstance level, LDtkEntity ldtkEntity);
}

public abstract class LDtkEntityBuilder<T> : ILDtkEntityBuilder
{
    protected ILogger Logger => new Logger<T>();
    public abstract string EntityDefinitionIdentifier { get; }
    public abstract Entity BuildEntity(LDtkScene scene, LDtkLevelInstance level, LDtkEntity ldtkEntity);
}
