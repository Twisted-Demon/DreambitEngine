using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;

namespace Dreambit;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public class UIRenderPass : RenderPass
{
    private static UIRenderPass _activePass;
    private bool _batchActive;
    private RasterizerState _scissorRasterizerState;

    public override void Initialize()
    {
        base.Initialize();
        Order = 2;
        _scissorRasterizerState = new RasterizerState
        {
            CullMode = CullMode.None,
            ScissorTestEnable = true
        };
    }

    private void DrawUIComponents()
    {
        var drawLayers = Scene.Drawables.GetDrawLayers();
        var layerOrder = drawLayers.Keys.OrderBy(x => x).ToList();

        Device.SetRenderTarget(null);

        for (var i = 0; i < layerOrder.Count; i++)
        {
            _activePass = this;
            BeginBatch();
            try
            {
                foreach (var drawable in drawLayers[layerOrder[i]])
                {
                    if (!drawable.Enabled || !drawable.Entity.Enabled)
                        continue;

                    drawable.DrawUi();
                }
            }
            finally
            {
                EndBatch();
                _activePass = null;
            }
        }
    }

    /// <summary>
    ///     Flushes queued UI sprites before a draw context changes GPU scissor
    ///     state. A new deferred batch is opened immediately with identical state.
    /// </summary>
    internal static void FlushForScissorChange()
    {
        _activePass?.RestartBatch();
    }

    private void BeginBatch()
    {
        Core.SpriteBatch.Begin(
            transformMatrix: Scene.UiCamera.TopLeftTransformMatrix,
            sortMode: SpriteSortMode.Deferred,
            samplerState: Scene.RenderingOptions.UISamplerState,
            blendState: BlendState.AlphaBlend,
            rasterizerState: _scissorRasterizerState);
        _batchActive = true;
    }

    private void EndBatch()
    {
        if (!_batchActive)
            return;

        Core.SpriteBatch.End();
        _batchActive = false;
    }

    private void RestartBatch()
    {
        if (!_batchActive)
            return;

        EndBatch();
        BeginBatch();
    }

    public override void OnDraw()
    {
        DrawUIComponents();
    }

    protected override void OnDisposing()
    {
        _scissorRasterizerState?.Dispose();
        _scissorRasterizerState = null;
        base.OnDisposing();
    }
}