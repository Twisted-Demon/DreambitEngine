# Dreambit SDK releases

`Publish-DreambitSdk.ps1` is the supported release entry point. It updates every
engine, Editor, build-package, documentation, and project-template version,
restores once, runs the Editor test suite, creates all four NuGet packages,
pushes them, and installs the new project template locally.

```powershell
$env:NUGET_API_KEY = '<your nuget.org API key>'
./scripts/publish-sdk.cmd 0.1.8
```

The key is never printed. Packages are written under
`artifacts/packages/<version>` before they are pushed. Use a local dry run to
test a release without publishing or changing the installed template:

```powershell
./scripts/publish-sdk.cmd 0.1.8 -SkipPush -SkipTemplateInstall
```

The `.cmd` entry point applies a process-only PowerShell execution-policy
bypass, so it also works on Windows systems where local scripts are disabled.
It forwards any additional switches to `Publish-DreambitSdk.ps1`.

For another registry, pass `-NuGetSource` and either `-ApiKey` or the
`NUGET_API_KEY` environment variable. New package-based projects created after
the release use the new SDK automatically. Existing projects should update the
three Dreambit entries in `Directory.Packages.props` and their
`.dreambit/project.json` SDK version, then run `dotnet restore --force-evaluate`.
