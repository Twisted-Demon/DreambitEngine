# Project and content layout

A practical solution keeps engine source, game code, content recipes, and source
assets separate:

```text
MyGame/
  MyGame.csproj
  Program.cs
  Scenes/
  Components/
MyGame.Content/
  MyGame.Content.csproj
  BuildContent.targets
  Builder/Builder.cs
  Assets/
    Ui/
    Textures/
    Audio/
    Blueprints/
```

The content build produces files under the host application's `Content`
directory. Runtime paths are logical paths relative to that root and normally
omit their baked extension:

```csharp
Resources.LoadAsset<Sprite>("Sprites/player");
Resources.LoadAsset<Texture2D>("Textures/background");
```

UI XML stays loose and is copied to `Content`, because `UiFrame` loads and
composes XML files directly. Other supported files can be baked into
`content.pak`. See [Content projects and Asset Baker](../assets/content-pipeline.md).

!!! tip
    Copy the structure and build-target wiring from `Dreambit.Examples.Content`
    before inventing a new content build. It already handles loose XML and the
    host project's output directory.

