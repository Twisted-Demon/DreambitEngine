using Dreambit.Editor.Assets;
using Dreambit.Editor.Graphics;
using ImGuiNET;

namespace Dreambit.Editor.Inspection;

internal sealed class AssetPreviewInspector(AssetPreviewService previews)
{
    public void Draw(AssetRecord asset)
    {
        ImGui.TextUnformatted(asset.Name);
        ImGui.TextDisabled(asset.RelativePath);
        ImGui.Spacing();
        try
        {
            if (previews.TryGetTexture(asset, out var texture, out var width, out var height))
            {
                var available = ImGui.GetContentRegionAvail();
                var previewWidth = MathF.Min(available.X, width);
                var previewHeight = previewWidth * height / MathF.Max(1, width);
                if (previewHeight > available.Y)
                {
                    previewHeight = available.Y;
                    previewWidth = previewHeight * width / MathF.Max(1, height);
                }

                ImGui.Image(texture, new System.Numerics.Vector2(previewWidth, previewHeight));
                ImGui.TextDisabled($"{width} × {height}");
                return;
            }
        }
        catch (Exception exception)
        {
            ImGui.TextColored(
                new System.Numerics.Vector4(0.96f, 0.34f, 0.36f, 1f),
                exception.Message);
        }

        ImGui.TextWrapped(
            asset.Kind == AssetKind.Scene
                ? "Double-click this scene to open it."
                : "No loaded Dreambit asset type is available for this file. Its data remains untouched.");
    }
}
