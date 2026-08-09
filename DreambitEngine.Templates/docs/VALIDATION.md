# Validation performed

The template source was checked for:

- Valid JSON in `template.json`.
- Valid XML in all `.csproj`, `.props`, `.targets`, and manifest files.
- Valid PowerShell syntax for every `.ps1` script.
- Valid Bash syntax for every `.sh` script.
- Installable NuGet template-package metadata and layout.
- Valid solution project paths and template GUID placeholders.
- Correct emulated `sourceName`, title, repository, target-FPS, filename, and GUID replacements.
- Successful MSBuild evaluation and build when `sourceName` contains namespace-style dots.
- Exactly one executable `ProjectReference`: the game-code project.
- Presence of the DreambitEngine runtime, content builder, and AssetBaker projects in the generated solution.
- Presence of the renamed content-build target after template generation.
- Absence of unresolved template placeholders and generated `bin`, `obj`, or repository metadata.

The included `scripts/test-template.ps1` and `scripts/test-template.sh` pack the current version, install the resulting `.nupkg` into an isolated template cache, generate a project with non-default parameters, and verify the generated output. Pass a DreambitEngine checkout to the PowerShell test's `-EnginePath` parameter to build the generated launcher as well.
