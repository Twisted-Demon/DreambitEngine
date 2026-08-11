using Dreambit.Editor.Projects;

namespace Dreambit.Editor.Tests;

public sealed class CreateProjectRequestTests : IDisposable
{
    private readonly string _location = Path.Combine(
        Path.GetTempPath(),
        "Dreambit.Editor.CreateRequestTests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public void AcceptsAPortableProjectRequest()
    {
        Directory.CreateDirectory(_location);
        var request = new CreateProjectRequest(
            "Orbital.Defense",
            _location,
            "Orbital Defense",
            "DesktopVK",
            "0.1.4");

        var valid = request.TryValidate(out var outputPath, out var error);

        Assert.True(valid, error);
        Assert.Equal(Path.Combine(_location, "Orbital.Defense"), outputPath);
    }

    [Theory]
    [InlineData("9Game")]
    [InlineData("Game/Other")]
    [InlineData("Game!")]
    [InlineData("CON")]
    [InlineData("LPT1.Tools")]
    [InlineData("")]
    public void RejectsUnsafeProjectNames(string name)
    {
        Directory.CreateDirectory(_location);
        var request = new CreateProjectRequest(
            name,
            _location,
            "Game",
            "DesktopVK",
            "0.1.4");

        Assert.False(request.TryValidate(out _, out _));
    }

    [Fact]
    public void RejectsANonEmptyOutputDirectory()
    {
        var output = Path.Combine(_location, "ExistingGame");
        Directory.CreateDirectory(output);
        File.WriteAllText(Path.Combine(output, "keep.txt"), "user data");
        var request = new CreateProjectRequest(
            "ExistingGame",
            _location,
            "Existing Game",
            "DesktopVK",
            "0.1.4");

        Assert.False(request.TryValidate(out _, out var error));
        Assert.Contains("not empty", error);
        Assert.True(File.Exists(Path.Combine(output, "keep.txt")));
    }

    [Fact]
    public void RejectsAnUnsafeSdkVersion()
    {
        Directory.CreateDirectory(_location);
        var request = new CreateProjectRequest(
            "Game",
            _location,
            "Game",
            "DesktopVK",
            "../../other");

        Assert.False(request.TryValidate(out _, out var error));
        Assert.Contains("package version", error);
    }

    public void Dispose()
    {
        if (Directory.Exists(_location))
            Directory.Delete(_location, true);
    }
}
