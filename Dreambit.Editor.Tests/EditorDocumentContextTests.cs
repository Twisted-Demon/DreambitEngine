using Dreambit.Editor.Assets;
using Dreambit.Editor.Compilation;
using Dreambit.Editor.Inspection;
using Dreambit.Editor.Projects;
using Dreambit.Editor.Scenes;

namespace Dreambit.Editor.Tests;

public sealed class EditorDocumentContextTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(),
        "Dreambit.Editor.DocumentContextTests",
        Guid.NewGuid().ToString("N"));

    private string ContentRoot => Path.Combine(_root, "Content", "Assets");

    public EditorDocumentContextTests() => Directory.CreateDirectory(ContentRoot);

    [Fact]
    public void SuspendedBlueprintContextDoesNotFallBackToSceneDocumentOrSelection()
    {
        using var fixture = CreateFixture();
        var scene = fixture.Scenes.New("Scene");
        scene.CreateEmpty("Scene Entity");
        fixture.Blueprints.Selection.Restore([Guid.NewGuid()]);

        fixture.Context.ActivateBlueprint();

        Assert.True(fixture.Context.IsBlueprint);
        Assert.Null(fixture.Blueprints.Current);
        Assert.Null(fixture.Context.Current);
        Assert.Same(fixture.Blueprints.Selection, fixture.Context.Selection);
        Assert.NotSame(fixture.Scenes.Selection, fixture.Context.Selection);
    }

    [Fact]
    public void AssetWithoutInspectableDocumentCanStillOwnEditorFocus()
    {
        File.WriteAllBytes(Path.Combine(ContentRoot, "icon.png"), [0]);
        using var fixture = CreateFixture();
        fixture.Scenes.New("Scene");
        var texture = Assert.Single(fixture.Assets.GetSnapshot().Assets);
        fixture.AssetEditing.Select(texture);

        Assert.Null(fixture.AssetEditing.Current);
        fixture.Context.ActivateAsset();

        Assert.True(fixture.Context.IsAsset);
        Assert.Equal(EditorDocumentKind.Asset, fixture.Context.ActiveKind);
        Assert.Same(fixture.Scenes.Current, fixture.Context.Current);
    }

    [Fact]
    public void BlueprintHistoryRemainsAssetOwnedWhilePreviewIsUnavailable()
    {
        File.WriteAllText(
            Path.Combine(ContentRoot, "hero.blueprint.json"),
            DreambitJson.Serialize(new EntityBlueprint { Name = "Hero" }));
        using var fixture = CreateFixture();
        var blueprint = Assert.Single(fixture.Assets.GetSnapshot().Assets);
        fixture.AssetEditing.Select(blueprint);

        fixture.Context.ActivateBlueprint();

        Assert.Null(fixture.Blueprints.Current);
        Assert.Same(fixture.AssetEditing.Current!.Undo, fixture.Context.Undo);
    }

    private Fixture CreateFixture()
    {
        var project = new DreambitProjectDefinition(
            _root,
            Path.Combine(_root, ".dreambit", "project.json"),
            new DreambitProjectMetadata(),
            Path.Combine(_root, "Game.sln"),
            Path.Combine(_root, "Game.csproj"),
            Path.Combine(_root, "Game.Content.csproj"),
            ContentRoot,
            Path.Combine(_root, "Game.VK.csproj"));
        return new Fixture(project, _root, ContentRoot);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, true);
    }

    private sealed class Fixture : IDisposable
    {
        private readonly InspectorMetadataCache _metadata = new();

        public Fixture(DreambitProjectDefinition project, string root, string contentRoot)
        {
            Assets = new AssetDatabase(root, contentRoot, enableWatcher: false);
            Assemblies = new GameAssemblyLoadService(root);
            Types = new EditorTypeRegistry(Assemblies, _metadata);
            AssetEditing = new AssetEditingService(
                project,
                Assets,
                Types,
                _metadata,
                Assemblies);
            BlueprintSources = new BlueprintSourceService(Assets);
            Scenes = new SceneDocumentService(
                project,
                Assemblies,
                Assets,
                BlueprintSources);
            Blueprints = new BlueprintEditingService(
                AssetEditing,
                Assemblies,
                BlueprintSources);
            Context = new EditorDocumentContext(Scenes, Blueprints, AssetEditing);
        }

        public AssetDatabase Assets { get; }
        public GameAssemblyLoadService Assemblies { get; }
        public EditorTypeRegistry Types { get; }
        public AssetEditingService AssetEditing { get; }
        public BlueprintSourceService BlueprintSources { get; }
        public SceneDocumentService Scenes { get; }
        public BlueprintEditingService Blueprints { get; }
        public EditorDocumentContext Context { get; }

        public void Dispose()
        {
            Blueprints.Dispose();
            Scenes.Dispose();
            BlueprintSources.Dispose();
            AssetEditing.Dispose();
            Types.Dispose();
            Assemblies.Dispose();
            Assets.Dispose();
        }
    }
}
