using Dreambit.Editor.Projects;

namespace Dreambit.Editor.Tests;

internal sealed class DreambitProjectTestFixture : IDisposable
{
    public DreambitProjectTestFixture()
    {
        Root = Path.Combine(
            Path.GetTempPath(),
            "Dreambit.Editor.ProjectMetadataTests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Root);
    }

    public string Root { get; }

    public DreambitProjectMetadata CreateValidProject()
    {
        Directory.CreateDirectory(Path.Combine(Root, "src", "TestGame"));
        Directory.CreateDirectory(Path.Combine(Root, "src", "TestGame.Content", "Assets"));
        Directory.CreateDirectory(Path.Combine(Root, "src", "TestGame.VK"));
        File.WriteAllText(Path.Combine(Root, "TestGame.sln"), string.Empty);
        File.WriteAllText(
            Path.Combine(Root, "src", "TestGame", "TestGame.csproj"),
            "<Project />");
        File.WriteAllText(
            Path.Combine(Root, "src", "TestGame.Content", "TestGame.Content.csproj"),
            "<Project />");
        File.WriteAllText(
            Path.Combine(Root, "src", "TestGame.VK", "TestGame.VK.csproj"),
            "<Project />");

        var metadata = new DreambitProjectMetadata
        {
            ProjectId = Guid.NewGuid(),
            Name = "TestGame",
            Solution = "TestGame.sln",
            GameProject = "src/TestGame/TestGame.csproj",
            ContentProject = "src/TestGame.Content/TestGame.Content.csproj",
            ContentRoot = "src/TestGame.Content/Assets",
            LauncherProject = "src/TestGame.VK/TestGame.VK.csproj",
            TargetRenderer = "DesktopVK",
            Sdk = new DreambitSdkReference { Version = DreambitSdkConstants.CurrentVersion }
        };
        Assert.True(
            new DreambitProjectMetadataStore().TrySave(Root, metadata, out var error),
            error);
        return metadata;
    }

    public void Dispose()
    {
        if (Directory.Exists(Root))
            Directory.Delete(Root, true);
    }
}
