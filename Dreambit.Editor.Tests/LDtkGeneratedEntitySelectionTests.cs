using Dreambit.ECS;
using Dreambit.Editor.Scenes;
using Dreambit.LDtk;

namespace Dreambit.Editor.Tests;

public sealed class LDtkGeneratedEntitySelectionTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(),
        "Dreambit.Editor.LDtkGeneratedEntitySelectionTests",
        Guid.NewGuid().ToString("N"));

    public LDtkGeneratedEntitySelectionTests() => Directory.CreateDirectory(_root);

    [Fact]
    public void ReimportPreservesSelectionForGeneratedEntityByStableSourceIdentity()
    {
        var contentRoot = Path.Combine(_root, "Assets");
        var mapsDirectory = Path.Combine(contentRoot, "maps");
        Directory.CreateDirectory(mapsDirectory);
        var projectPath = Path.Combine(mapsDirectory, "world.ldtk");
        var levelId = Guid.NewGuid();
        WriteProject(projectPath, levelId, "Level");

        LDtkFile ResolveProject(LDtkSceneReference _) =>
            LDtkFile.FromContentFile(projectPath, "maps/world", contentRoot);

        var selection = new SelectionService();
        using var document = new SceneDocument(
            new SceneBlueprint
            {
                Name = "Imported Selection",
                LDtk = new LDtkSceneReference { AssetName = "maps/world" }
            },
            null,
            selection,
            ldtkProjectResolver: ResolveProject);
        var originalScene = document.Scene;
        var selected = Assert.Single(
            document.Scene!.GetAllEntities()
                .SelectMany(entity => entity.GetAllComponents())
                .OfType<FilledRectDrawer>()).Entity;
        var sourceKey = selected.LDtkSourceKey;
        Assert.True(selected.IsLDtkGenerated);
        Assert.False(string.IsNullOrWhiteSpace(sourceKey));
        selection.Set(selected);

        WriteProject(projectPath, levelId, "Level Updated");
        document.ReimportLDtk();

        var restored = selection.GetActive(document.Scene);
        Assert.NotSame(originalScene, document.Scene);
        Assert.NotNull(restored);
        Assert.True(restored.IsLDtkGenerated);
        Assert.Equal(sourceKey, restored.LDtkSourceKey);
        Assert.Equal("LDtk Background Color: Level Updated", restored.Name);
        Assert.NotSame(selected, restored);
        Assert.NotEqual(selected.Id, restored.Id);
    }

    private static void WriteProject(string path, Guid levelId, string levelName)
    {
        File.WriteAllText(path, $$"""
        {
          "jsonVersion": "1.5.3",
          "levels": [{
            "__bgColor": "#123456",
            "identifier": "{{levelName}}",
            "iid": "{{levelId}}",
            "uid": 1,
            "pxWid": 16,
            "pxHei": 16,
            "worldX": 0,
            "worldY": 0,
            "worldDepth": 0,
            "fieldInstances": [],
            "__neighbours": [],
            "layerInstances": []
          }]
        }
        """);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, true);
    }
}
