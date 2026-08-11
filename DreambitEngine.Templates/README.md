# Dreambit Engine Templates

Version 0.1.4 creates package-based Dreambit projects with portable Editor metadata.

## Generated project

- `DreambitGame` — reusable game code and scenes.
- `DreambitGame.Content` — raw assets and the game's MonoGame content builder.
- `DreambitGame.VK` — the DesktopVK executable.
- `.dreambit/project.json` — portable paths, project identity, renderer, and SDK version.
- Repository-level central package versions for `DreambitEngine` and `DreambitEngine.Build`.
- No DreambitEngine Git submodule, setup scripts, machine-specific SDK path, or build-tool project references.

`DreambitEngine.Build` is the stable `buildTransitive` integration point. Real-time and explicit content baking is intentionally integrated in Editor Milestone 4.

## Pack and install locally

PowerShell:

```powershell
./scripts/install-local.ps1
```

Bash:

```bash
./scripts/install-local.sh
```

These scripts pack the coordinated runtime/build/template package set into the same user SDK-cache layout consumed by Dreambit.Editor, then install the project template for direct command-line use.

Create a project:

```powershell
dotnet new dreambit-game -n MyGame --game-title "My Game" --sdkVersion 0.1.4
```

Dreambit.Editor normally performs template installation, generation, and package restore automatically.

## Test

```powershell
./scripts/test-template.ps1
```

The smoke test packs all three SDK packages, uses an isolated template hive, generates a dotted-name project, validates `.dreambit/project.json`, restores from the local package feed, and builds the generated solution.
