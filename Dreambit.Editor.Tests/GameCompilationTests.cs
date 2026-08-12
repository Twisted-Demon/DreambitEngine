using Dreambit.ECS;
using Dreambit.Editor.Compilation;
using Dreambit.Editor.Inspection;
using Dreambit.EditorApi;

namespace Dreambit.Editor.Tests;

public sealed class GameCompilationTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(),
        "Dreambit.Editor.GameCompilationTests",
        Guid.NewGuid().ToString("N"));

    public GameCompilationTests()
    {
        Directory.CreateDirectory(_root);
    }

    [Fact]
    public void DiagnosticParserExtractsCompilerLocationsAndDeduplicatesSummaryLines()
    {
        var line = @"C:\Game\Player.cs(12,7): error CS1002: ; expected [C:\Game\Game.csproj]";
        var warning = @"C:\Game\Mover.cs(4,2): warning CS0168: variable is never used [C:\Game\Game.csproj]";
        var diagnostics = GameBuildDiagnosticParser.Parse([line, line, warning]);

        Assert.Equal(2, diagnostics.Count);
        var error = Assert.Single(
            diagnostics,
            diagnostic => diagnostic.Severity == GameBuildDiagnosticSeverity.Error);
        Assert.Equal("CS1002", error.Code);
        Assert.Equal(12, error.Line);
        Assert.Equal(7, error.Column);
        Assert.Equal(@"C:\Game\Player.cs", error.File);
    }

    [Fact]
    public void CollectibleLoaderShadowCopiesAndDiscoversDreambitTypes()
    {
        var messages = new List<GameCodeMessage>();
        using var loader = new GameAssemblyLoadService(_root, messages.Add);

        Assert.True(loader.TryLoad(typeof(GameCompilationTests).Assembly.Location, out var error), error);
        var loaded = Assert.IsType<LoadedGameAssembly>(loader.Current);
        Assert.Contains(
            loaded.Types.ComponentTypes,
            type => type.FullName == typeof(ReloadTestComponent).FullName);
        Assert.Contains(
            loaded.Types.CustomEditorTypes,
            type => type.FullName == typeof(ReloadTestCustomEditor).FullName);
        Assert.Contains(
            loaded.Types.AssetTypes,
            type => type.FullName == typeof(TestCustomAsset).FullName);
        Assert.Contains(
            loaded.Types.AssetLoaderTypes,
            type => type.FullName == typeof(TestCustomAssetLoader).FullName);
        Assert.NotEqual(
            Path.GetDirectoryName(typeof(GameCompilationTests).Assembly.Location),
            loaded.ShadowDirectory);
        Assert.True(File.Exists(Path.Combine(
            loaded.ShadowDirectory,
            Path.GetFileName(typeof(GameCompilationTests).Assembly.Location))));
    }

    [Fact]
    public void FailedAssemblyLoadKeepsLastKnownGoodGeneration()
    {
        using var loader = new GameAssemblyLoadService(_root);
        Assert.True(loader.TryLoad(typeof(GameCompilationTests).Assembly.Location, out var loadError), loadError);
        var knownGood = loader.Current;

        Assert.False(loader.TryLoad(Path.Combine(_root, "missing-game.dll"), out var error));
        Assert.Contains("does not exist", error, StringComparison.OrdinalIgnoreCase);
        Assert.Same(knownGood, loader.Current);
    }

    [Fact]
    public void CustomEditorRegistryUsesTypesFromTheCollectibleGameGeneration()
    {
        using var loader = new GameAssemblyLoadService(_root);
        using var registry = new CustomEditorRegistry(loader);
        Assert.True(loader.TryLoad(typeof(GameCompilationTests).Assembly.Location, out var error), error);
        var target = Assert.Single(
            loader.Current!.Types.ComponentTypes,
            type => type.FullName == typeof(ReloadTestComponent).FullName);

        Assert.True(registry.TryGet(target, out var editor));
        Assert.Equal(typeof(ReloadTestCustomEditor).FullName, editor!.GetType().FullName);
    }

    [Fact]
    public void ReloadReleasesThePreviousCollectibleGameGeneration()
    {
        var messages = new List<GameCodeMessage>();
        using var loader = new GameAssemblyLoadService(_root, messages.Add);
        var assemblyPath = typeof(GameCompilationTests).Assembly.Location;

        Assert.True(loader.TryLoad(assemblyPath, out var firstError), firstError);
        Assert.True(loader.TryLoad(assemblyPath, out var secondError), secondError);

        Assert.DoesNotContain(messages, message =>
            message.Severity == GameCodeMessageSeverity.Warning &&
            message.Message.Contains("still referenced after unload", StringComparison.Ordinal));
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_root))
                Directory.Delete(_root, true);
        }
        catch (UnauthorizedAccessException)
        {
            // A failed collectible-load test may briefly retain a shadow-copy handle.
        }
        catch (IOException)
        {
        }
    }
}

public sealed class ReloadTestComponent : Component;

[DreambitCustomEditor(typeof(ReloadTestComponent))]
public sealed class ReloadTestCustomEditor : IDreambitCustomEditor
{
    public void Draw(IEditorInspectorContext context) => context.DrawDefaultInspector();
}
