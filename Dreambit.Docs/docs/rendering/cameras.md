# Cameras and coordinates

Dreambit separates world scale, user zoom, and resolution zoom:

```csharp
MainCamera.PixelsPerUnit = 16;       // 16 pixels per world unit
MainCamera.Zoom = 1.5f;              // player-controlled magnification
MainCamera.SetTargetVerticalResolution(720); // resolution scaling reference
```

`Scale = PixelsPerUnit * Zoom * ResolutionZoom`. `WorldUnitsPerTexturePixel` is
`1 / PixelsPerUnit` and is useful for pixel-sized debug lines in world space.

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

