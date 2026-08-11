using Dreambit.Editor.Projects;

namespace Dreambit.Editor.Tests;

public sealed class DreambitProjectMetadataStoreTests
{
    [Fact]
    public void RoundTripsPortableProjectMetadata()
    {
        using var fixture = new DreambitProjectTestFixture();
        var expected = fixture.CreateValidProject();

        var metadataPath = Path.Combine(fixture.Root, ".dreambit", "project.json");
        var loaded = new DreambitProjectMetadataStore().TryLoad(
            metadataPath,
            out var actual,
            out var diagnostic);

        Assert.True(loaded, diagnostic?.Message);
        Assert.NotNull(actual);
        Assert.Equal(expected.ProjectId, actual.ProjectId);
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.ContentRoot, actual.ContentRoot);
        Assert.Equal(expected.Sdk.Version, actual.Sdk.Version);
        Assert.DoesNotContain(fixture.Root, File.ReadAllText(metadataPath));
    }

    [Fact]
    public void ReportsMalformedMetadataWithoutReplacingIt()
    {
        using var fixture = new DreambitProjectTestFixture();
        var metadataPath = Path.Combine(fixture.Root, ".dreambit", "project.json");
        Directory.CreateDirectory(Path.GetDirectoryName(metadataPath)!);
        File.WriteAllText(metadataPath, "{ broken }");

        var loaded = new DreambitProjectMetadataStore().TryLoad(
            metadataPath,
            out var metadata,
            out var diagnostic);

        Assert.False(loaded);
        Assert.Null(metadata);
        Assert.Equal("DBP003", diagnostic?.Code);
        Assert.Equal("{ broken }", File.ReadAllText(metadataPath));
    }
}
