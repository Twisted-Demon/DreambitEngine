# Camera2D

`Camera2D` converts between world and screen coordinates and controls scale,
zoom, rotation, and following. Every scene already supplies `MainCamera` and
`UiCamera`; attach another only for a deliberate secondary view.

```csharp
MainCamera.PixelsPerUnit = 16;
MainCamera.Zoom = 2f;
MainCamera.SetTargetVerticalResolution(720);
MainCamera.ForcePosition(new Vector3(20, 12, 0));
```

`PixelsPerUnit` defines how many texture pixels represent one world unit before
zoom. `SetTargetVerticalResolution` adds resolution scaling. `Scale` is the final
pixels-per-world-unit value.

For following, assign `TransformToFollow`, select `CameraFollowBehavior`, set
`LerpSpeed`, and leave `IsFollowing` true. `ForcePosition` is appropriate for
teleports and scene initialization.

Use `WorldToScreen` / `ScreenToWorld` for viewport coordinates and
`WorldToCameraLocal` / `CameraLocalToWorld` for camera-local UI-style space. See
[Cameras and coordinates](../../rendering/cameras.md) for examples.

