using Dreambit.Editor.Persistence;
using Dreambit.Editor.UI.Panels;

namespace Dreambit.Editor.Tests;

public sealed class EditorPanelRegistryTests
{
    [Fact]
    public void AppliesPersistedVisibilityWhenRegistering()
    {
        var state = new EditorWorkspaceState
        {
            PanelVisibility = new Dictionary<string, bool>
            {
                ["test"] = false
            }
        };
        using var registry = new EditorPanelRegistry(state);
        var panel = new TestPanel("test");

        registry.Register(panel);

        Assert.False(panel.IsOpen);
    }

    [Fact]
    public void RejectsDuplicatePanelIds()
    {
        using var registry = new EditorPanelRegistry(new EditorWorkspaceState());
        registry.Register(new TestPanel("duplicate"));

        var exception = Assert.Throws<InvalidOperationException>(
            () => registry.Register(new TestPanel("duplicate")));

        Assert.Contains("already registered", exception.Message);
    }

    [Fact]
    public void DisposesRegisteredPanelsInReverseOrder()
    {
        var disposeOrder = new List<string>();
        var registry = new EditorPanelRegistry(new EditorWorkspaceState());
        registry.Register(new TestPanel("first", disposeOrder));
        registry.Register(new TestPanel("second", disposeOrder));

        registry.Dispose();

        Assert.Equal(["second", "first"], disposeOrder);
    }

    private sealed class TestPanel : IEditorPanel
    {
        private readonly List<string>? _disposeOrder;

        public TestPanel(string id, List<string>? disposeOrder = null)
        {
            Id = id;
            Title = id;
            WindowName = id;
            _disposeOrder = disposeOrder;
        }

        public string Id { get; }
        public string Title { get; }
        public string WindowName { get; }
        public bool IsOpen { get; set; } = true;

        public void Draw()
        {
        }

        public void Dispose()
        {
            _disposeOrder?.Add(Id);
        }
    }
}
