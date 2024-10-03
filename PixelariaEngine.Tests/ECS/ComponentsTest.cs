using PixelariaEngine;
using PixelariaEngine.ECS;

namespace PixelariaEngine.Tests;

public class ComponentsTest
{
    private Scene _scene;
    
    [SetUp]
    public void SetUp()
    {
        _scene = new Scene();
        _scene.Tick();
    }
    
    [Test]
    public void AddComponentBeforeSceneUpdateTest()
    {
        var entity = _scene.CreateEntity();

        var component = entity.AttachComponent<UnitTestComponent>();
        
        Assert.That(component, Is.Not.Null);
        Assert.That(component, Is.TypeOf<UnitTestComponent>());
    }
    
    [Test]
    public void AddComponentAfterSceneUpdateTest()
    {
        var entity = _scene.CreateEntity();
        
        _scene.Tick();

        var component = entity.AttachComponent<UnitTestComponent>();
        
        Assert.That(component, Is.Not.Null);
        Assert.That(component, Is.TypeOf<UnitTestComponent>());
    }

    [Test]
    public void GetComponentBeforeSceneUpdateTest()
    {
        var entity = _scene.CreateEntity();
        
        _scene.Tick();
        
        entity.AttachComponent<UnitTestComponent>();
        
        var component = entity.GetComponent<UnitTestComponent>();
        
        Assert.That(component, Is.Not.Null);
        Assert.That(component, Is.TypeOf<UnitTestComponent>());
    }

    [Test]
    public void GetComponentsAfterSceneUpdateTest()
    {
        var entity = _scene.CreateEntity();
        
        _scene.Tick();
        
        entity.AttachComponent<UnitTestComponent>();
        
        _scene.Tick();
        
        var component = entity.GetComponent<UnitTestComponent>();
        
        Assert.That(component, Is.Not.Null);
        Assert.That(component, Is.TypeOf<UnitTestComponent>());
    }

    [Test]
    public void RemoveComponentBeforeSceneUpdateTest()
    {
        var entity = _scene.CreateEntity();
        _scene.Tick();
        entity.AttachComponent<UnitTestComponent>();
        
        entity.DetachComponent<UnitTestComponent>();
        
        var component = entity.GetComponent<UnitTestComponent>();
        Assert.That(component, Is.Null);
    }
    
    [Test]
    public void RemoveComponentAfterSceneUpdateTest()
    {
        var entity = _scene.CreateEntity();
        _scene.Tick();
        entity.AttachComponent<UnitTestComponent>();
        
        entity.DetachComponent<UnitTestComponent>();
        
        _scene.Tick();
        
        var component = entity.GetComponent<UnitTestComponent>();
        Assert.That(component, Is.Null);
    }

    [Test]
    public void CheckComponentsAfterEntityDestroyedTest()
    {
        var entity = _scene.CreateEntity();
        
        _scene.Tick();
        var component = entity.AttachComponent<UnitTestComponent>();
        
        _scene.Tick();

        _scene.DestroyEntity(entity);
        
        _scene.Tick();
        
        Assert.True(Component.IsNull(component));
        Assert.That(Entity.IsNull(entity), Is.True);
    }

    [Test]
    public void TestComponentUpdate()
    {
        var entity = _scene.CreateEntity();
        
        _scene.Tick();
        var component = entity.AttachComponent<UnitTestComponent>();
        
        _scene.Tick();
        
        Assert.That(component.FrameCount, Is.EqualTo(1));
        
        _scene.Tick();
        
        Assert.That(component.FrameCount, Is.EqualTo(2));
    }

    [Test]
    public void TestComponentLifeCycle()
    {
        var entity = _scene.CreateEntity();
        
        _scene.Tick();
        
        //this should only call OnCreated
        var component = entity.AttachComponent<UnitTestComponent>();
        
        Assert.Multiple(() =>
        {
            Assert.That(component.OnCreatedCalled, Is.True);
            Assert.That(component.OnAddedToEntityCalled, Is.False);
            Assert.That(component.OnEnabledCalled, Is.False);
        });
        
        _scene.Tick(); //here we add the component and enable it by default
        
        Assert.Multiple(() =>
        {
            Assert.That(component.OnAddedToEntityCalled, Is.True);
            Assert.That(component.OnEnabledCalled, Is.False);
        });
        
        //test disable
        component.Enabled = false;
        Assert.That(component.OnDisabledCalled, Is.True);
        
        //test enable
        component.Enabled = true;
        Assert.That(component.OnEnabledCalled, Is.True);
        
        //we schedule the removal, but havent called onRemove or destroy yet
        entity.DetachComponent<UnitTestComponent>(); 
        
        Assert.Multiple(() =>
        {
            Assert.That(component.OnDestroyedCalled, Is.False);
            Assert.That(component.OnRemovedFromEntityCalled, Is.False);
        });
        
        _scene.Tick(); //here we remove and delete the component
        
        Assert.Multiple(() =>
        {
            Assert.That(component.OnDestroyedCalled, Is.True);
            Assert.That(component.OnRemovedFromEntityCalled, Is.True);
        });
        
        //now we test the destroyal lifetime by destroying the entity
        entity = _scene.CreateEntity();
        _scene.Tick();
        component = entity.AttachComponent<UnitTestComponent>();
        _scene.Tick();

        _scene.DestroyEntity(entity);
        
        _scene.Tick();
        
        Assert.Multiple(() =>
        {
            Assert.That(component.OnDestroyedCalled, Is.True);
            Assert.That(component.OnRemovedFromEntityCalled, Is.True);
        });
    }
}