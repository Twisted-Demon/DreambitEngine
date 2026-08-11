# DreambitEngine.Build

This package is the versioned `buildTransitive` integration boundary for Dreambit game projects.

The package carries the Dreambit AssetBaker and adds incremental, deterministic PAK generation to
game executable projects. Set `DreambitContentBuildEnabled`, `DreambitProjectRoot`,
`DreambitContentRoot`, and `DreambitAssetRegistry` in the host project. Use the
`RebuildDreambitAssets` MSBuild target to bypass the baked-blob cache.
