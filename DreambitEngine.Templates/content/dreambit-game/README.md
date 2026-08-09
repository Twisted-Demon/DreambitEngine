# Dreambit Game

A game created with Dreambit Engine.

## First-time setup

The generated project keeps DreambitEngine in `external/DreambitEngine` as a Git submodule. The setup script initializes Git when necessary, adds the engine submodule, restores all projects, and performs the first build.

**Run the setup script before opening the solution in Rider or building any project.** `dotnet new` does not clone Git submodules automatically.

PowerShell:

```powershell
./scripts/setup-engine.ps1
```

Bash:

```bash
./scripts/setup-engine.sh
```

Then run the game:

```powershell
dotnet run --project src/DreambitGame.VK
```

In Rider, open `DreambitGame.sln` and select `DreambitGame.VK` as the run project.

## Project layout

```text
DreambitGame/
├── build/
│   └── DreambitGame.Content.targets
├── external/
│   └── DreambitEngine/              # Created by setup script
├── scripts/
├── src/
│   ├── Directory.Build.props
│   ├── Directory.Packages.props
│   ├── DreambitGame/                # Game code and scenes
│   ├── DreambitGame.Content/        # Assets and content builder
│   └── DreambitGame.VK/             # Vulkan executable
└── DreambitGame.sln
```

## Adding assets

Place game assets under:

```text
src/DreambitGame.Content/Assets
```

Building `DreambitGame.VK` automatically:

1. Restores and builds Dreambit's content builder, the game content builder, and AssetBaker in their own project directories.
2. Builds Dreambit engine content.
3. Builds the game's MonoGame content.
4. Bakes the raw game assets into `content.pak`.
5. Places runtime content under `src/DreambitGame.VK/Build/<Configuration>/Content`.

The content projects and AssetBaker are deliberately **not** `ProjectReference`s of the executable. Their executables and private dependencies therefore stay out of the game's runtime output.

## Build output

```text
src/DreambitGame.VK/Build/Debug/
src/DreambitGame.VK/Build/Release/
```

## Updating DreambitEngine

PowerShell:

```powershell
./scripts/update-engine.ps1
```

Bash:

```bash
./scripts/update-engine.sh
```

## Useful commands

```powershell
# Build
dotnet build src/DreambitGame.VK/DreambitGame.VK.csproj

# Run
dotnet run --project src/DreambitGame.VK

# Release build
dotnet build src/DreambitGame.VK/DreambitGame.VK.csproj -c Release

# Force a complete content rebuild
dotnet clean src/DreambitGame.VK/DreambitGame.VK.csproj
dotnet build src/DreambitGame.VK/DreambitGame.VK.csproj
```
