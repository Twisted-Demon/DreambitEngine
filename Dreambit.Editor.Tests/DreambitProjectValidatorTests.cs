using Dreambit.Editor.Projects;

namespace Dreambit.Editor.Tests;

public sealed class DreambitProjectValidatorTests
{
    [Fact]
    public void ResolvesAValidProjectToAbsolutePaths()
    {
        using var fixture = new DreambitProjectTestFixture();
        var metadata = fixture.CreateValidProject();

        var result = new DreambitProjectValidator().Validate(fixture.Root);

        Assert.True(result.IsValid, result.ErrorSummary);
        Assert.NotNull(result.Project);
        Assert.Equal(metadata.ProjectId, result.Project.Metadata.ProjectId);
        Assert.Equal(
            Path.Combine(fixture.Root, "src", "TestGame.Content", "Assets"),
            result.Project.ContentRootPath);
    }

    [Fact]
    public void RejectsADirectoryWithoutDreambitMetadata()
    {
        using var fixture = new DreambitProjectTestFixture();

        var result = new DreambitProjectValidator().Validate(fixture.Root);

        Assert.False(result.IsValid);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "DBP002");
    }

    [Fact]
    public void RejectsPathsThatEscapeTheProject()
    {
        using var fixture = new DreambitProjectTestFixture();
        var metadata = fixture.CreateValidProject();
        metadata.GameProject = "../Outside.csproj";
        Assert.True(
            new DreambitProjectMetadataStore().TrySave(fixture.Root, metadata, out var error),
            error);

        var result = new DreambitProjectValidator().Validate(fixture.Root);

        Assert.False(result.IsValid);
        Assert.Contains(result.Diagnostics, diagnostic =>
            diagnostic.Code == "DBP009" && diagnostic.Message.Contains("escapes"));
    }

    [Fact]
    public void RejectsMissingConfiguredFiles()
    {
        using var fixture = new DreambitProjectTestFixture();
        fixture.CreateValidProject();
        File.Delete(Path.Combine(fixture.Root, "TestGame.sln"));

        var result = new DreambitProjectValidator().Validate(fixture.Root);

        Assert.False(result.IsValid);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "DBP010");
    }

    [Fact]
    public void RejectsUnsupportedSchemaAndRenderer()
    {
        using var fixture = new DreambitProjectTestFixture();
        var metadata = fixture.CreateValidProject();
        metadata.SchemaVersion = 99;
        metadata.TargetRenderer = "MysteryRenderer";
        Assert.True(
            new DreambitProjectMetadataStore().TrySave(fixture.Root, metadata, out var error),
            error);

        var result = new DreambitProjectValidator().Validate(fixture.Root);

        Assert.False(result.IsValid);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "DBP004");
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "DBP007");
    }

    [Fact]
    public void RejectsAnIncompatibleSdkVersion()
    {
        using var fixture = new DreambitProjectTestFixture();
        var metadata = fixture.CreateValidProject();
        metadata.Sdk.Version = "99.0.0";
        Assert.True(
            new DreambitProjectMetadataStore().TrySave(fixture.Root, metadata, out var error),
            error);

        var result = new DreambitProjectValidator().Validate(fixture.Root);

        Assert.False(result.IsValid);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "DBP011");
    }
}
