# Dreambit SDK package distribution

Dreambit games consume a coordinated SDK version rather than a DreambitEngine source submodule.

The Milestone 2 package set is:

```text
DreambitEngine             Runtime assembly and runtime dependencies
DreambitEngine.Build       buildTransitive MSBuild integration boundary
DreambitEngine.Templates   dotnet new templates
```

All packages selected for a project use the same version recorded in `.dreambit/project.json` and in the repository-root `Directory.Packages.props`.

## Editor installation layout

Dreambit.Editor maintains installed SDKs outside game repositories:

```text
%LocalAppData%/Dreambit/Editor/sdks/<version>/
|-- sdk.json
|-- packages/
|   |-- DreambitEngine.<version>.nupkg
|   |-- DreambitEngine.Build.<version>.nupkg
|   `-- DreambitEngine.Templates.<version>.nupkg
`-- template-hive/
```

Packaged Editor distributions can bundle the same package set under `SDK/<version>/packages`. Development builds fall back to packing the adjacent DreambitEngine source checkout into the user SDK cache. Generated projects never record either machine-specific location.

The `Dreambit.Editor` publish target produces this bundled SDK layout by default.

The Editor passes the installed package feed as an additional NuGet source while restoring a newly created project. Normal NuGet sources remain enabled. Once public packages are published, the same generated project files restore without Editor-specific paths.

## Build package boundary

`DreambitEngine.Build` ships through `buildTransitive`. Milestone 2 establishes the stable import boundary and SDK version property. Milestone 4 will place the real content builder and AssetBaker integration behind this package without changing every generated game project.

Future package splits such as dedicated content-pipeline or tool packages remain implementation details of `DreambitEngine.Build`; they do not need to become direct runtime dependencies of the game executable.
