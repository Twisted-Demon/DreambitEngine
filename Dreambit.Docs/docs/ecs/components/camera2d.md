# Camera2D

`Camera2D` converts between world and screen coordinates and controls scale,
zoom, rotation, and following. Every scene already supplies `MainCamera` and
`UiCamera`; attach another only for a deliberate secondary view.

```csharp
MainCamera.Zoom = 2f;
MainCamera.SetTargetVerticalResolution(45f);
MainCamera.PixelPerfectPixelsPerUnit = 32f;
MainCamera.PixelSnap = true; // Point-sampled pixel art
MainCamera.ForcePosition(new Vector3(20, 12, 0));
```

`TargetVerticalResolution` is the number of world units visible vertically at
Zoom 1. Sprite assets define how many texture pixels represent one world unit.
`Scale` is the final screen-pixels-per-world-unit value.

For following, assign `TransformToFollow`, select `CameraFollowBehavior`, set
`LerpSpeed`, and leave `IsFollowing` true. `ForcePosition` is appropriate for
teleports and scene initialization.

Use `WorldToScreen` / `ScreenToWorld` for viewport coordinates and
`WorldToCameraLocal` / `CameraLocalToWorld` for camera-local UI-style space. See
[Cameras and coordinates](../../rendering/cameras.md) for examples.

