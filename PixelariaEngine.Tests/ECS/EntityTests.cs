using System.Diagnostics.CodeAnalysis;
using PixelariaEngine.ECS;

namespace PixelariaEngine.Tests;

[SuppressMessage("ReSharper", "ConditionIsAlwaysTrueOrFalse")]
public class EntityTests
{
    [SetUp]
    public void Setup()
    {
        
    }

    [Test]
    public void CreateAndDestroyEntityTest()
    {
        var scene = new Scene();
        
        scene.Tick();

        var entity = scene.CreateEntity();
        
        Assert.That(entity, Is.Not.Null);
        
        scene.Tick();
        
        
        var idToCheck = entity.Id;
        
        scene.DestroyEntity(entity);
        scene.Tick();

        
        var entity2 = scene.FindEntity(idToCheck);
        Assert.Multiple(() =>
        {
            Assert.That(entity2, Is.Null);
            Assert.That(Entity.IsNull(entity), Is.True);
            Assert.That(entity == null, Is.True);
            Assert.That(entity != null, Is.False);
        });

        Assert.That(Entity.IsNull(entity), Is.True);
    }

    [Test]
    public void DestroyMultipleEntitiesTest()
    {
        var scene = new Scene();
        scene.Tick();
        
        var entity1 = scene.CreateEntity();
        var entity2 = scene.CreateEntity();
        var entity3 = scene.CreateEntity();
        var entity4 = scene.CreateEntity();
        
        scene.Tick();
        
        scene.DestroyEntity(entity1);
        scene.DestroyEntity(entity2);
        scene.DestroyEntity(entity3);
        scene.DestroyEntity(entity4);
        
        scene.Tick();
        
        Assert.Multiple(() =>
        {
            Assert.That(Entity.IsNull(entity1));
            Assert.That(Entity.IsNull(entity2));
            Assert.That(Entity.IsNull(entity3));
            Assert.That(Entity.IsNull(entity4));
        });
    }

    [Test]
    public void GetEntityByIdTest()
    {
        var scene = new Scene();
        scene.Tick();
        
        var entity = scene.CreateEntity();
        var entityId = entity.Id;
        
        scene.Tick();
        
        var entityToCheck = scene.FindEntity(entityId);
        
        Assert.That(entityToCheck, Is.Not.Null);
        
        entityToCheck = scene.FindEntity(123455);
        Assert.That(entityToCheck, Is.Null);
    }

    [Test]
    public void GetEntityByNameTest()
    {
        var scene = new Scene();
        scene.Tick();
        
        scene.CreateEntity(name: "Test Entity");
        
        scene.Tick();
        
        var entityToCheck = scene.FindEntity("Test Entity");
        
        Assert.That(entityToCheck, Is.Not.Null);
        
        entityToCheck = scene.FindEntity("WRONG NAME");
        
        Assert.That(entityToCheck, Is.Null);
    }
}