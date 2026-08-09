# Dreambit Engine Templates

> Version 0.1.4 adds the referenced DreambitEngine projects to generated solutions. It also supports dotted project names such as `Dreambit.ExampleGame`.

This repository builds the installable `dreambit-game` template.

## What the generated game contains

- `DreambitGame` — reusable game code and scenes.
- `DreambitGame.Content` — the game's MonoGame content builder and raw assets.
- `DreambitGame.VK` — the Vulkan executable and deployment output.
- Automatic Dreambit engine content compilation and `content.pak` baking.
- No build-tool project references in the executable project, so content-builder and AssetBaker executables are not copied into the game output.
- Incremental content builds and IDE up-to-date inputs.
- Setup scripts that initialize Git, add DreambitEngine as a submodule, restore, and build.
- Generated solution entries for the DreambitEngine runtime, content builder, and AssetBaker projects.

## Pack and install locally

PowerShell:

```powershell
./scripts/install-local.ps1
```

Bash:

```bash
./scripts/install-local.sh
```

Or manually:

```powershell
dotnet pack -c Release
$version = ([xml](Get-Content ./DreambitEngine.Templates.csproj)).Project.PropertyGroup.Version | Select-Object -First 1
dotnet new install "./bin/Release/DreambitEngine.Templates.$version.nupkg" --force
dotnet new dreambit-game -n MyGame --game-title "My Game"
```

After generation:

```powershell
cd MyGame
./scripts/setup-engine.ps1
dotnet run --project src/MyGame.VK
```

Use a valid C# identifier for `-n`, such as `MyGame` or `OrbitalDefense`. Use `--game-title` for spaces and punctuation.

## Test without touching the user's installed templates

```powershell
./scripts/test-template.ps1
```

The test packs the template, installs it into an isolated template cache, generates `TemplateSmokeTest`, verifies the important project structure, and optionally builds it when a DreambitEngine checkout is supplied. It does not change the templates installed for your user account.

## Engine setup is required

After creating a game, change into the generated project directory and run:

```powershell
.\scripts\setup-engine.ps1
```

The .NET template engine creates files but does not clone the DreambitEngine Git submodule automatically. Building before running setup will fail because `external/DreambitEngine` does not exist yet.

To generate directly into the current empty directory, use `-o .`:

```powershell
dotnet new dreambit-game -n MyGame -o . --game-title "My Game"
```
