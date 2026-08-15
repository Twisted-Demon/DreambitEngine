using Dreambit.Editor.Scenes;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Vector4 = System.Numerics.Vector4;

namespace Dreambit.Editor.UI.Panels;

internal sealed class SceneSettingsPanel(EditorDocumentContext documentContext)
    : EditorPanel(EditorPanelIds.SceneSettings, "Scene Settings")
{
    private string? _error;

    public override bool IsAvailable =>
        !documentContext.IsAsset &&
        !documentContext.IsBlueprint &&
        documentContext.Current is not null;

    protected override void DrawContents()
    {
        var document = documentContext.Current;
        if (document is null)
            return;

        var edited = document.Settings.Clone();
        var postProcessing = edited.PostProcessing;
        var ambientColor = ToNumerics(edited.AmbientLightColor);
        var tintColor = ToNumerics(postProcessing.TintColor);
        var ambientIntensity = edited.AmbientLightIntensity;
        var hueShift = postProcessing.HueShift;
        var saturation = postProcessing.Saturation;
        var exposure = edited.Exposure;
        
        var changed = false;
        string? mergeKey = null;

        ImGui.TextDisabled(document.DisplayName);
        ImGui.Separator();
        ImGui.TextUnformatted("Ambient Light");
        if (ImGui.DragFloat("Intensity", ref ambientIntensity, 0.01f, 0f, 100f))
            (changed, mergeKey) = (true, "SceneSettings.AmbientLightIntensity");
        if (ImGui.ColorEdit4("Color", ref ambientColor))
            (changed, mergeKey) = (true, "SceneSettings.AmbientLightColor");
        if (ImGui.DragFloat("Exposure", ref exposure, 0.01f, v_min: 0, v_max: 5f))
            (changed, mergeKey) = (true, "SceneSettings.Exposure");

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.TextUnformatted("Post Processing");
        if (ImGui.DragFloat("Hue Shift", ref hueShift, 0.01f, -1f, 1f))
            (changed, mergeKey) = (true, "SceneSettings.PostProcessing.HueShift");
        if (ImGui.DragFloat("Saturation", ref saturation, 0.01f, 0f, 4f))
            (changed, mergeKey) = (true, "SceneSettings.PostProcessing.Saturation");
        if (ImGui.ColorEdit4("Tint Color", ref tintColor))
            (changed, mergeKey) = (true, "SceneSettings.PostProcessing.TintColor");
        

        edited.AmbientLightIntensity = ambientIntensity;
        edited.AmbientLightColor = new Color(ambientColor.X, ambientColor.Y, ambientColor.Z, ambientColor.W);
        edited.Exposure = exposure;
        postProcessing.HueShift = hueShift;
        postProcessing.Saturation = saturation;
        postProcessing.TintColor = new Color(tintColor.X, tintColor.Y, tintColor.Z, tintColor.W);

        if (!changed)
        {
            DrawError();
            return;
        }

        try
        {
            document.UpdateSceneSettings("Change Scene Settings", settings =>
            {
                settings.AmbientLightIntensity = edited.AmbientLightIntensity;
                settings.AmbientLightColor = edited.AmbientLightColor;
                settings.Exposure = edited.Exposure;
                settings.PostProcessing = edited.PostProcessing.Clone();
            }, mergeKey);
            _error = null;
        }
        catch (Exception exception)
        {
            _error = exception.Message;
        }

        DrawError();
    }

    private void DrawError()
    {
        if (!string.IsNullOrWhiteSpace(_error))
            ImGui.TextColored(new Vector4(0.96f, 0.34f, 0.36f, 1f), _error);
    }

    private static Vector4 ToNumerics(Color color) =>
        new(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f);
}
