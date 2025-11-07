namespace Dreambit.Tests;
public class DrawableComponentTest
{
    private Scene _scene;

    [TearDown]
    [SetUp]
    public void SetUp()
    {
        _scene = new Scene();
        _scene.Tick();
    }

    [Test]
    public void AddRenderableComponent()
    {
        
    }
}