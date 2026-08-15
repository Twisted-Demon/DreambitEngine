# Content projects and Asset Baker

The Asset Baker command registered by the current executable is `bake-pak`:

```powershell
dotnet run --project DreambitEngine.AssetBaker -- bake-pak MyGame.Content/Assets MyGame/Build/Content/content.pak --premul --srgb
```

It recursively mirrors logical paths inside the pak and supports:

| Source | Baked entry |
| --- | --- |
| `.png`, `.jpg`, `.jpeg`, `.bmp`, `.tga` | `.texb` |
| `.wav`, `.ogg`, `.mp3` | `.audb` |
| `.json`, `.ldtk`, `.ldtkl` | `.jsonb` |
| `.asset`, `.blueprint`, `.particlefx`, `.scene`, `.soundcue`, `.sprite`, `.spriteanimation`, `.spritesheet`, `.tileset` | source extension + `.jsonb` |
| `.yaml`, `.cutscene` | `.yamlb` (`.cutscene` keeps its source extension) |

Serialized Dreambit assets use their semantic source extension as part of their logical runtime
name. For example, `Sprites/hero.sprite` bakes to `sprites/hero.sprite.jsonb` and is loaded as
`Resources.LoadAsset<Sprite>("Sprites/hero.sprite")`. Game-defined `DreambitAsset` classes use
`.asset` by default.

Texture options include `--mips`, `--premul`, `--max-size N`, and `--srgb`.
The runtime defaults to `Resources.UsePak = true` and
`Resources.PakName = "content.pak"`.

UI XML is loaded directly from disk by `UiFrame`, so copy `*.xml` into the host
output's `Content` tree. The example content project demonstrates a MonoGame
content builder with `IncludeCopy<WildcardRule>("*.xml")` and imports its
`BuildContent.targets` from the host project.

!!! note
    `BakeDirectoryCommand` exists in source but is not registered in the current
    Asset Baker command application. Use `bake-pak` unless you intentionally add
    that command to `Program.cs`.
