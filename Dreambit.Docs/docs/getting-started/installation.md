# Install and reference Dreambit

Create a .NET 8 executable project and reference `DreambitEngine.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\DreambitEngine\DreambitEngine.csproj" />
    <PackageReference Include="MonoGame.Framework.Native" Version="3.8.5" />
    <PackageReference Include="MonoGame.Runtime.Windows.Vulkan" Version="3.8.5" />
  </ItemGroup>
</Project>
```

Use the runtime package for each platform you ship. The example project includes
Windows, macOS, and Linux Vulkan runtimes.

## Confirm the reference

The smallest program creates `Core`, schedules a scene, and runs the game:

```csharp
using Dreambit;

using var game = new Core(1280, 720, "My Dreambit Game");
Scene.SetNextScene(new MainScene());
game.Run();
```

Do not call `Input.Init`, frame hooks, render passes, or physics ticks yourself;
`Core` owns those lifecycle calls.

## Content references

If you use Dreambit's shared effects and fonts, also reference the existing
`Dreambit.Content` project and import its `BuildContent.targets`. For XML UI and
your own assets, create a content project following
[Project and content layout](project-structure.md).

