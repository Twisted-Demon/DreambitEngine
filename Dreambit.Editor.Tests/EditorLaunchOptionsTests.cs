namespace Dreambit.Editor.Tests;

public sealed class EditorLaunchOptionsTests
{
    [Fact]
    public void ParsesSupportedOptions()
    {
        var parsed = EditorLaunchOptions.TryParse(
            ["--project", "C:/Games/Test", "--settings-dir", "C:/Temp/Editor", "--smoke-test"],
            out var options,
            out var error);

        Assert.True(parsed, error);
        Assert.Equal("C:/Games/Test", options.ProjectPath);
        Assert.Equal("C:/Temp/Editor", options.SettingsDirectory);
        Assert.True(options.SmokeTest);
    }

    [Theory]
    [InlineData("--project")]
    [InlineData("--settings-dir")]
    public void RejectsMissingOptionValues(string option)
    {
        var parsed = EditorLaunchOptions.TryParse(
            [option],
            out _,
            out var error);

        Assert.False(parsed);
        Assert.Contains("requires a value", error);
    }

    [Fact]
    public void RejectsUnknownOptions()
    {
        var parsed = EditorLaunchOptions.TryParse(
            ["--surprise"],
            out _,
            out var error);

        Assert.False(parsed);
        Assert.Contains("Unknown argument", error);
    }
}
