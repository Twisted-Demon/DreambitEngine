# Rendering

Each scene owns a render pipeline and drawable repository. The default pipeline
runs 2D lighting, debug drawing, post-processing, and UI in that order.

Attach drawable components for normal gameplay rendering:

- `SpriteDrawer` for sprite assets
- `RectDrawer` and `CircleDrawer` for primitives
- `ParticleSystemDrawer` for particles
- `PointLight2D` and `AmbientLight2D` for lighting
- `UiFrame` for retained UI

The scene's `MainCamera` transforms world drawing; `UiCamera` transforms UI.
Configure sampling and blending through `Scene.RenderingOptions`.

