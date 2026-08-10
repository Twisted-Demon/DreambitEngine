using Dreambit.ECS;

namespace Dreambit.LDtk.Loaders;

public interface ILDtkEntityBuilder
{
    string EntityDefinitionIdentifier { get; }

    void BuildEntity(LDtkLevelInstance level, Entity dreambitEntity, EntityInstance ldtkEntityInstance);
}

public abstract class LDtkEntityBuilder<T> where T : ILDtkEntityBuilder
{

}
