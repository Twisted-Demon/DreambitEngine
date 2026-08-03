# LDtk integration

Dreambit wraps the bundled LDtk library with asset loading, world/level helpers,
tileset sprite sheets, a prerendered drawable, scene switching, and entity helper
types.

The dependable current path is:

1. Bake the `.ldtk` and external `.ldtkl` JSON files plus referenced textures.
2. Call `LDtkManager.SetUp` with the project logical path.
3. Select a world by its IID with `LoadWorld`.
4. Switch to a level by IID with `Scene.SetNextLDtkScene`.

Some convenience paths are incomplete in current source. The following pages
mark those limits explicitly.

