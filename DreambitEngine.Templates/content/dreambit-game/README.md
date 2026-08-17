# Dreambit Game

A game created with Dreambit Engine.

## Requirements

- A compatible .NET SDK
- Dreambit SDK `__DREAMBIT_SDK_VERSION__`

The game consumes Dreambit through coordinated NuGet-style packages. It does not require a DreambitEngine source checkout or Git submodule. Dreambit.Editor installs the selected SDK package set in its local SDK cache before creating and restoring a project.

Run the game:

```powershell
dotnet run --project src/DreambitGame.VK
```

In Rider or Visual Studio, open `DreambitGame.sln` and select `DreambitGame.VK` as the startup project.

## Project layout

```text
DreambitGame/
|-- .dreambit/
|   `-- project.json                 # Portable project metadata
|-- Directory.Packages.props         # Coordinated SDK/package versions
|-- scripts/
|   `-- update-dreambit.ps1          # Updates the project to a selected SDK
|-- src/
|   |-- Directory.Build.props
|   |-- DreambitGame/                # Game code and scenes
|   |-- DreambitGame.Content/        # Raw assets and content builder
|   `-- DreambitGame.VK/             # DesktopVK executable
`-- DreambitGame.sln
```

Machine-specific Editor state is stored outside the repository. `.dreambit/user/` is reserved and ignored if a future integration needs project-local user data.

## Adding assets

Place raw assets under:

```text
src/DreambitGame.Content/Assets
```

The `DreambitEngine.Build` package is the stable integration boundary for Dreambit content
processing. The Editor and Debug builds update incremental blobs automatically. Choose
**Build > Bake Pak** in Dreambit Editor before building or publishing Release; Release stops with a
helpful message if the shipping PAK is missing or stale.

## Useful commands

```powershell
dotnet restore DreambitGame.sln
dotnet build DreambitGame.sln
dotnet run --project src/DreambitGame.VK
dotnet build src/DreambitGame.VK/DreambitGame.VK.csproj -c Release
```

## Updating Dreambit

To update manually, run the project updater with the coordinated SDK version. If
the SDK packages are local rather than published to a NuGet feed, also provide
the directory containing the matching `.nupkg` files.

```powershell
./scripts/update-dreambit.ps1 -SdkVersion 0.3.3 -PackageSource C:\path\to\packages
```

The script updates `Directory.Packages.props` and `.dreambit/project.json`, then
restores the solution. If restore fails, it restores both files to their prior state.
