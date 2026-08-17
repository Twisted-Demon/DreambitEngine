# DreambitEngine.Build

This package is the versioned `buildTransitive` integration boundary for Dreambit game projects.

The package carries the Dreambit AssetBaker and adds incremental baked-blob generation to game
executable projects. Debug builds run from those blobs. Shipping PAK creation is explicit: use
Dreambit Editor's **Build > Bake Pak** command (or the `BakeDreambitPak` MSBuild target), then build
or publish in Release. Release fails with a clear error if the PAK is missing or stale.

Set `DreambitContentBuildEnabled`, `DreambitProjectRoot`, `DreambitContentRoot`, and
`DreambitAssetRegistry` in the host project. Use `RebuildDreambitAssets` to bypass the incremental
blob cache.
