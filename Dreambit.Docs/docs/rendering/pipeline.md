# Render pipeline and post-processing

The default scene pipeline is configured in `Scene.SetUpRenderPipeLine`:

1. `Basic2dLightingRenderPass`
2. `DebugRenderPass`
3. `PostProcessRenderPass`
4. `UIRenderPass`

The protected setup method can be overridden to opt into or out of the base
pipeline:

```csharp
protected override void SetUpRenderPipeLine()
{
    base.SetUpRenderPipeLine();
}
```

Custom passes derive from `RenderPass`, set `Order`, initialize resources in
`Initialize`, draw in `OnDraw`, and dispose owned resources through the base
lifecycle. `IsActive` toggles a pass.

!!! warning "Current extension limit"
    Although `SetUpRenderPipeLine` is overridable and `RenderPipeline` can add
    passes, the scene's pipeline field is private and no protected add/get API is
    exposed. A derived scene cannot currently add a custom pass through its
    override. Add a protected pipeline accessor/registration method to `Scene`
    before treating custom passes as a supported game-facing extension point.

## Post-processing

Configure the current built-in color controls on the scene:

```csharp
PostProcessSettings.HueShift = 0.08f;
PostProcessSettings.Saturation = 0.8f;
PostProcessSettings.TintColor = new Color(230, 240, 255);
```

The pipeline renders the scene through a render target created at back-buffer
size. Recreate custom render targets when the window changes, and dispose them
with the owning pass.
