# Validation performed

The template source was checked for:

- Valid JSON in `template.json`.
- Valid XML in all `.csproj`, `.props`, `.targets`, and manifest files.
- Valid Bash syntax for every `.sh` script.
- Valid solution project paths and template GUID placeholders.
- Correct emulated `sourceName`, title, repository, target-FPS, filename, and GUID replacements.
- Exactly one executable `ProjectReference`: the game-code project.
- Presence of the renamed content-build target after template generation.
- Absence of Starfront-specific names and generated `bin`, `obj`, or repository metadata.

A real `dotnet pack`/`dotnet new install` execution was not possible in the artifact environment because the .NET SDK is not installed there. The included `scripts/test-template.ps1` and `scripts/test-template.sh` perform that smoke test on a development machine with the SDK installed.
