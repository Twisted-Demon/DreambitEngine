# Dreambit Asset Editor

`Dreambit.AssetEditor` is a modern Avalonia desktop authoring tool for Dreambit's JSON-backed assets and blueprints. It is intentionally code-only Avalonia UI because the inspector is generated dynamically from Dreambit's runtime types and serialization metadata.

## What it supports

- Creates any `DreambitAsset` that has a registered `.jsonb` `IAssetLoader`.
- Opens and saves readable `.json` source or Dreambit's JSNB-backed `.jsonb` format.
- Uses Dreambit's real `DreambitJson` converter stack instead of maintaining a second serializer.
- Loads an external game/asset DLL and discovers its `.jsonb` assets, components, and property converters.
- Lets you choose a project asset root and browse its `.json` / `.jsonb` files in a folder tree.
- Opens project assets by double-clicking them in the explorer and refreshes the tree after saves.
- Supports dragging explorer files onto `DreambitAsset` and blueprint reference fields; paths are stored relative to the project root with the final extension removed.
- Provides a dedicated `EntityBlueprint` / `SceneBlueprint` hierarchy and component inspector.
- Only exposes component members marked with `[DreambitSerialize]`.
- Automatically adds component dependencies declared with `[Require]`.
- Preserves legacy/unrecognized blueprint properties while keeping them hidden from editing.
- Provides structured editors for primitive values, enums, vectors, colors, rectangles, nested objects, lists, dictionaries, and converter-defined JSON shapes.
- Validates blueprints through `BlueprintValidator` and other assets through the Dreambit conversion stack.
- Supports dragging `.json`, `.jsonb`, and `.dll` files onto the editor window.

## UI

The editor uses Avalonia's Fluent theme with a custom dark Dreambit palette. The main blueprint workspace has a resizable hierarchy panel, a scrollable inspector, a resizable component list/property area, modern modal pickers, and responsive layouts that do not depend on fixed WinForms control positions.

## Build

From the DreambitEngine repository root:

```powershell
dotnet restore Dreambit.AssetEditor/Dreambit.AssetEditor.csproj
dotnet build Dreambit.AssetEditor/Dreambit.AssetEditor.csproj
```

Run it with:

```powershell
dotnet run --project Dreambit.AssetEditor/Dreambit.AssetEditor.csproj
```

Choose **Set Root** in the Project Explorer and select the asset folder used by your game's content build. For example, dropping `characters/player.blueprint.json` into a blueprint reference field stores `characters/player.blueprint`.
