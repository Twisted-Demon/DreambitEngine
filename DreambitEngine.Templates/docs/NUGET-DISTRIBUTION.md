# Moving DreambitEngine from a source submodule to NuGet

The current template uses a Git submodule because the existing DreambitEngine repository is source-oriented and its runtime, LDtk dependency, engine content builder, and AssetBaker are not yet published as a coordinated package set. The generated executable is still clean because build tools are invoked explicitly and are not project dependencies.

For the lowest-friction public release, publish these packages later:

```text
DreambitEngine
DreambitEngine.ContentPipeline
DreambitEngine.AssetBaker
DreambitEngine.Build
DreambitEngine.Templates
```

## Runtime package

Make `DreambitEngine/DreambitEngine.csproj` packable and ensure its referenced LDtk project is either:

1. Packed as its own dependency, or
2. Included in the DreambitEngine package output deliberately.

The generated game project can then replace:

```xml
<ProjectReference Include="$(DreambitEngineRoot)/DreambitEngine/DreambitEngine.csproj" />
```

with:

```xml
<PackageReference Include="DreambitEngine" />
```

## Build package

`DreambitEngine.Build` should contain build logic under:

```text
buildTransitive/DreambitEngine.Build.props
buildTransitive/DreambitEngine.Build.targets
```

It should provide or depend on the content builder and AssetBaker tools without adding them as runtime dependencies. A consuming launcher would use:

```xml
<PackageReference Include="DreambitEngine.Build" PrivateAssets="all" />
```

At that point, remove the generated `build/*.targets` file and the engine setup/update scripts from the game template.

## Central package versions

The current generated template places `Directory.Packages.props` under `src/`, not at repository root. This deliberately prevents central package management from affecting the source-based engine submodule. Once the engine is consumed entirely through packages, the file can safely move to the repository root.

## Recommended release order

1. Make engine and LDtk projects packable.
2. Publish the runtime packages to a private test feed.
3. Package the content builder and AssetBaker as tools.
4. Publish `DreambitEngine.Build` with imported MSBuild targets.
5. Change the template to PackageReferences.
6. Run the template smoke test against a clean NuGet cache.
7. Publish `DreambitEngine.Templates` last.
