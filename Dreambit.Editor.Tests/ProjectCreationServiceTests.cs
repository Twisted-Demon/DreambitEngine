using Dreambit.Editor.Infrastructure;
using Dreambit.Editor.Logging;
using Dreambit.Editor.Projects;

namespace Dreambit.Editor.Tests;

public sealed class ProjectCreationServiceTests : IDisposable
{
    private readonly string _testRoot = Path.Combine(
        Path.GetTempPath(),
        "Dreambit.Editor.ProjectCreationTests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task OrchestratesTheVersionMatchedTemplateAndRestore()
    {
        var settings = Path.Combine(_testRoot, "settings");
        var projects = Path.Combine(_testRoot, "projects");
        Directory.CreateDirectory(projects);
        var paths = EditorPaths.Create(new EditorLaunchOptions(null, settings, false));
        InstallFakeSdk(paths);
        var runner = new FakeProjectProcessRunner();
        var logs = new EditorLogService();
        var sdkManager = new DreambitSdkManager(paths, logs, runner);
        var service = new ProjectCreationService(sdkManager, logs, processRunner: runner);

        var result = await service.CreateAsync(
            new CreateProjectRequest(
                "TestGame",
                projects,
                "Test Game",
                "DesktopVK",
                DreambitSdkConstants.CurrentVersion),
            CancellationToken.None);

        Assert.True(result.Succeeded, result.Message);
        Assert.Equal(Path.Combine(projects, "TestGame"), result.ProjectRoot);
        Assert.Equal(3, runner.Commands.Count);
        Assert.Contains("install", runner.Commands[0].Arguments);
        Assert.Contains("dreambit-game", runner.Commands[1].Arguments);
        Assert.Contains("--sdkVersion", runner.Commands[1].Arguments);
        Assert.Contains("--targetRenderer", runner.Commands[1].Arguments);
        Assert.Contains("restore", runner.Commands[2].Arguments);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testRoot))
            Directory.Delete(_testRoot, true);
    }

    private static void InstallFakeSdk(EditorPaths paths)
    {
        var packages = Path.Combine(
            paths.SdkRootDirectory,
            DreambitSdkConstants.CurrentVersion,
            "packages");
        Directory.CreateDirectory(packages);
        foreach (var packageId in DreambitSdkConstants.RequiredPackageIds)
        {
            File.WriteAllBytes(
                Path.Combine(
                    packages,
                    $"{packageId}.{DreambitSdkConstants.CurrentVersion}.nupkg"),
                []);
        }
    }

    private sealed class FakeProjectProcessRunner : IProcessRunner
    {
        public List<ProcessCommand> Commands { get; } = [];

        public Task<ProcessRunResult> RunAsync(
            ProcessCommand command,
            Action<string>? output,
            CancellationToken cancellationToken)
        {
            Commands.Add(command);
            if (command.Arguments.Contains("dreambit-game"))
                GenerateProject(command.Arguments);

            output?.Invoke("fake process completed");
            return Task.FromResult(new ProcessRunResult(0, ["fake process completed"]));
        }

        private static void GenerateProject(IReadOnlyList<string> arguments)
        {
            var output = GetArgumentValue(arguments, "-o");
            var name = GetArgumentValue(arguments, "-n");
            Directory.CreateDirectory(Path.Combine(output, "src", name));
            Directory.CreateDirectory(Path.Combine(output, "src", $"{name}.Content", "Assets"));
            Directory.CreateDirectory(Path.Combine(output, "src", $"{name}.VK"));
            File.WriteAllText(Path.Combine(output, $"{name}.sln"), string.Empty);
            File.WriteAllText(
                Path.Combine(output, "src", name, $"{name}.csproj"),
                "<Project />");
            File.WriteAllText(
                Path.Combine(output, "src", $"{name}.Content", $"{name}.Content.csproj"),
                "<Project />");
            File.WriteAllText(
                Path.Combine(output, "src", $"{name}.VK", $"{name}.VK.csproj"),
                "<Project />");

            var metadata = new DreambitProjectMetadata
            {
                ProjectId = Guid.NewGuid(),
                Name = name,
                Solution = $"{name}.sln",
                GameProject = $"src/{name}/{name}.csproj",
                ContentProject = $"src/{name}.Content/{name}.Content.csproj",
                ContentRoot = $"src/{name}.Content/Assets",
                LauncherProject = $"src/{name}.VK/{name}.VK.csproj",
                TargetRenderer = "DesktopVK",
                Sdk = new DreambitSdkReference
                {
                    Version = DreambitSdkConstants.CurrentVersion
                }
            };
            Assert.True(
                new DreambitProjectMetadataStore().TrySave(output, metadata, out var error),
                error);
        }

        private static string GetArgumentValue(
            IReadOnlyList<string> arguments,
            string option)
        {
            var index = -1;
            for (var argumentIndex = 0; argumentIndex < arguments.Count; argumentIndex++)
            {
                if (string.Equals(arguments[argumentIndex], option, StringComparison.Ordinal))
                {
                    index = argumentIndex;
                    break;
                }
            }
            Assert.InRange(index, 0, arguments.Count - 2);
            return arguments[index + 1];
        }
    }
}
