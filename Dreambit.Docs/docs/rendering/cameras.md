# Cameras and coordinates

Dreambit separates world scale, user zoom, and viewport scaling:

```csharp
MainCamera.Zoom = 1.5f;              // player-controlled magnification
MainCamera.SetTargetVerticalResolution(45f); // visible world units at Zoom 1
```

`Scale` is the final number of screen pixels per world unit. Sprite assets own
their pixels-per-unit value, which determines how texture pixels convert to
world units. `WorldUnitsPerScreenPixel` is useful for pixel-sized debug lines.

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

