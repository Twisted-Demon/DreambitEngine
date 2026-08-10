# Cameras and coordinates

Dreambit separates world scale, user zoom, and viewport scaling:

```csharp
MainCamera.Zoom = 1.5f;              // player-controlled magnification
MainCamera.SetTargetVerticalResolution(45f); // visible world units at Zoom 1
```

`Scale` is the final number of screen pixels per world unit. Sprite assets own
their pixels-per-unit value, which determines how texture pixels convert to
world units. `WorldUnitsPerScreenPixel` is useful for pixel-sized debug lines.

For point-sampled pixel art, set `PixelPerfectPixelsPerUnit` to the source
pixels represented by one world unit and enable `PixelSnap`:

```csharp
MainCamera.PixelPerfectPixelsPerUnit = 32f;
MainCamera.PixelSnap = true;
```

The first setting quantizes camera scale so every source pixel occupies an
integer number of screen pixels after a resize. The second rounds the final
render translation to whole screen pixels. Pixel-perfect rendering requires an
unrotated camera; when the viewport is not an exact multiple of the target
resolution, the visible world area changes slightly rather than using a
fractional texel scale.

## Conversions

```csharp
Vector2 screen = MainCamera.WorldToScreen(world);
Vector2 world = MainCamera.ScreenToWorld(screen);

Vector2 cameraLocal = MainCamera.WorldToCameraLocal(world);
Vector2 worldAgain = MainCamera.CameraLocalToWorld(cameraLocal);
```

Normal screen coordinates center the camera in the viewport. Camera-local
coordinates map the camera's top-left view origin to `(0,0)` and back the UI
camera's layout.

For a mouse world point, convert client to back buffer first, then use
`ScreenToWorld`.

`BoundsF` is the axis-aligned world envelope of the rotated visible viewport and
is the right value for culling. `BoundsNoPosition` is only a size rectangle.

