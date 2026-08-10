# Converting LDtk values to MonoGame

Import `Dreambit.LDtk` to use the conversion extension methods:

```csharp
using Dreambit.LDtk;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
```

The raw LDtk models are not changed. Conversions are explicit so pixel, grid,
normalized, and Dreambit world coordinates cannot be confused silently.

## Primitive values

| LDtk value | MonoGame result | Helper |
| --- | --- | --- |
| `LdtkPoint` | `Point` | `value.ToPoint()` |
| `LdtkPoint` | `Vector2` | `value.ToVector2()` |
| `LdtkPoint` | `Vector3` | `value.ToVector3(z)` |
| `LdtkPoint` pixels | `Vector2` world units | `value.ToWorldVector2(pixelsPerUnit)` |
| `LdtkPoint` pixels | `Vector3` world units | `value.ToWorldVector3(pixelsPerUnit, z)` |
| `LdtkVector2` | `Vector2` | `value.ToVector2()` |
| `LdtkVector2` | `Vector3` | `value.ToVector3(z)` |
| `LdtkColor` | `Color` | `value.ToColor()` |
| `TilesetRectangle` | `Rectangle` | `value.ToRectangle()` |

Nullable `LdtkPoint`, `LdtkVector2`, and `LdtkColor` values have nullable
counterpart overloads.

```csharp
Point pixel = instance.Px.ToPoint();
Vector2 world = instance.Px.ToWorldVector2(pixelsPerUnit: 16f);
Vector2 pivot = instance._Pivot.ToVector2();
Color tint = instance._SmartColor.ToColor();
Rectangle? tile = instance._Tile?.ToRectangle();
```

`ToVector2` preserves the original numbers. Only `ToWorldVector2` and
`ToWorldVector3` divide pixel coordinates by `pixelsPerUnit`.

## Entity-instance shortcuts

The most common `EntityInstance` values can be converted directly:

```csharp
var converted = new LDtkEntity
{
    Identifier = instance._Identifier,
    Iid = instance.Iid,
    Uid = instance.DefUid,
    Position = instance.ToPositionVector2(),
    Size = instance.ToSizeVector2(),
    Pivot = instance.ToPivotVector2(),
    Tile = instance.ToTileRectangle() ?? Rectangle.Empty,
    SmartColor = instance.ToSmartColor(),
};
```

Use `ToWorldPositionVector2(pixelsPerUnit)` and
`ToWorldSizeVector2(pixelsPerUnit)` when the destination stores Dreambit world
units instead of LDtk pixels. Entity positions are level-local; add the owning
layer's total offset and parent the runtime entity to the loaded level root when
placing it in a scene.

## LDtk Point fields

LDtk custom Point fields use `GridPoint`, whose coordinates are grid cells rather
than pixels:

```csharp
GridPoint waypoint = field.GetValue<GridPoint>()
    ?? throw new InvalidOperationException("Waypoint is null.");

Point cell = waypoint.ToPoint();
Point pixel = waypoint.ToPixelPoint(gridSize: 16);
Vector2 world = waypoint.ToWorldVector2(
    gridSize: 16f,
    pixelsPerUnit: 16f);
```

Keeping these conversions separate prevents a cell coordinate such as `(3, 4)`
from being mistaken for pixel position `(3, 4)`.

## Tile instances

Tile helpers handle LDtk's position, source location, opacity, and two flip bits:

```csharp
Point layerPixel = tile.ToPositionPoint();
Vector2 layerPosition = tile.ToPositionVector2();
Rectangle source = tile.ToSourceRectangle(layer._GridSize);
SpriteEffects effects = tile.ToSpriteEffects();
Color tint = tile.ToTint(layer._Opacity);
```

`ToSpriteEffects` maps bit zero to `FlipHorizontally` and bit one to
`FlipVertically`. `ToTint` combines tile alpha and layer opacity and clamps the
result to the range accepted by `Color`.

Background crop data can be converted to the integer source rectangle expected
by `SpriteBatch`:

```csharp
if (level._BgPos is { } backgroundPosition)
{
    Rectangle source = backgroundPosition.ToCropRectangle();
}
```

`ToCropRectangle` rounds LDtk's four floating-point crop values and throws if
the array is missing or invalid.

## Custom field shortcuts

Classic LDtk field values already deserialize directly to their CLR types with
`GetValue<T>()`: `int`, `float`, `bool`, `string`, enum identifiers, and file
paths need no MonoGame conversion.

Color, Point, and Tile fields have shortcuts for both scalar and array values:

```csharp
Color? color = colorField.GetMonoGameColor();
IReadOnlyList<Color> colors = colorsField.GetMonoGameColors();

Point? point = pointField.GetMonoGamePoint();
IReadOnlyList<Point> points = pointsField.GetMonoGamePoints();

Rectangle? tile = tileField.GetMonoGameRectangle();
IReadOnlyList<Rectangle> tiles = tileArrayField.GetMonoGameRectangles();
```

A null scalar field returns `null`. A null array field returns an empty list.
Calling a helper for an incompatible LDtk field type throws a JSON conversion
exception, which helps expose incorrect field mappings during development.
