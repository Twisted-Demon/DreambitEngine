# Dreambit Editor

Dreambit.Editor is the MonoGame 3.8.5/DesktopVK authoring application for
DreambitEngine. Its UI uses ImGui.NET docking and keeps machine-specific layout
and panel state outside game repositories.

## Run

```powershell
dotnet run --project Dreambit.Editor/Dreambit.Editor.csproj
dotnet run --project Dreambit.Editor/Dreambit.Editor.csproj -- --project C:\Repos\MyGame
```

Without `--project`, the Editor opens the project Hub. Opened projects must
contain a valid `.dreambit/project.json`. The Hub can create a DesktopVK project
from the version-matched Dreambit template and opens it in a separate Editor
process.

## Assets

Saving an asset updates the incremental blob store used by the Editor and Debug game builds; it
does not rewrite `content.pak`. Choose **Build > Bake Pak** only when preparing a Release build or
publish. Release verifies that the PAK matches the current blobs and explains how to refresh it if
it is missing or stale.

## Dreambit SDKs

Projects use coordinated `DreambitEngine`, `DreambitEngine.Build`, and
`DreambitEngine.Templates` packages. The selected version is stored in portable
project metadata and the repository's `Directory.Packages.props`; no
DreambitEngine source submodule is required.

Installed packages live under `Dreambit/Editor/sdks/<version>` in local
application data. A packaged Editor can bundle its SDK packages. A development
Editor build packs the adjacent engine checkout into this cache on first project
creation.

`dotnet publish` bundles the coordinated package set under
`SDK/<version>/packages` in the published Editor. Set
`BundleDreambitSdkOnPublish=false` only when another distribution step supplies
that folder.

## State

By default, user state is stored below the operating system's local application
data directory in `Dreambit/Editor`. Dock layouts are scoped by a hash of the
project path so separate project processes do not overwrite one another. A
session lock prevents two Editor processes from owning the same project.

`--settings-dir <path>` overrides that location for automated tests.

## Verify

```powershell
dotnet test Dreambit.Editor.Tests/Dreambit.Editor.Tests.csproj
dotnet run --no-build --project Dreambit.Editor/Dreambit.Editor.csproj -- --smoke-test
```

The smoke mode creates a real DesktopVK window, renders several ImGui frames,
persists state, and exits automatically.
