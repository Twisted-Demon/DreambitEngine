using Dreambit.Editor.Scenes;
using Dreambit.EditorApi;
using Microsoft.Xna.Framework;
using Vector4 = System.Numerics.Vector4;

namespace Dreambit.Editor.UI.Panels;

internal sealed class SceneSettingsPanel(EditorDocumentContext documentContext)
    : EditorPanel(EditorPanelIds.SceneSettings, "Scene Settings")
{
    private static readonly string[] ToneMappingNames =
    [
        "None",
        "Reinhard",
        "Reinhard Extended",
        "Hable",
        "ACES",
        "Lottes",
        "Uchimura",
        "AgX"
    ];

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
        var ambientColor = ToNumerics(edited.AmbientLightColor);
        var ambientIntensity = edited.AmbientLightIntensity;
        var exposure = edited.Exposure;

        var postProcessing = edited.PostProcessing;
        var tintColor = ToNumerics(postProcessing.TintColor);
        var hueShift = postProcessing.HueShift;
        var saturation = postProcessing.Saturation;
        var bloomEnabled = postProcessing.BloomEnabled;
        var bloomThreshold = postProcessing.BloomThreshold;
        var bloomSoftKnee = postProcessing.BloomSoftKnee;
        var bloomIntensity = postProcessing.BloomIntensity;
        int toneMap = (int)postProcessing.ToneMappingType;

        var changed = false;
        string? mergeKey = null;

        EditorGui.Header("Scene Settings", document.DisplayName);
        using (var ambientSection = EditorGui.Section("SceneSettings.Ambient", "Ambient Light"))
        {
            if (ambientSection.IsOpen)
            {
                if (EditorGui.Property(
                        "SceneSettings.AmbientLightIntensity", "Intensity", ref ambientIntensity,
                        speed: 0.01f, min: 0f, max: 100f))
                    (changed, mergeKey) = (true, "SceneSettings.AmbientLightIntensity");
                if (EditorGui.ColorProperty("SceneSettings.AmbientLightColor", "Color", ref ambientColor))
                    (changed, mergeKey) = (true, "SceneSettings.AmbientLightColor");
                if (EditorGui.Property(
                        "SceneSettings.Exposure", "Exposure", ref exposure,
                        speed: 0.01f, min: 0f, max: 5f))
                    (changed, mergeKey) = (true, "SceneSettings.Exposure");
            }
        }

        using (var postSection = EditorGui.Section("SceneSettings.PostProcessing", "Post Processing"))
        {
            if (postSection.IsOpen)
            {
                if (EditorGui.ChoiceProperty(
                        "SceneSettings.ToneMappingType", "Tone Mapping", ref toneMap, ToneMappingNames))
                    (changed, mergeKey) = (true, "SceneSettings.ToneMappingType");
                if (EditorGui.Property(
                        "SceneSettings.PostProcessing.HueShift", "Hue Shift", ref hueShift,
                        speed: 0.01f, min: -1f, max: 1f))
                    (changed, mergeKey) = (true, "SceneSettings.PostProcessing.HueShift");
                if (EditorGui.Property(
                        "SceneSettings.PostProcessing.Saturation", "Saturation", ref saturation,
                        speed: 0.01f, min: 0f, max: 4f))
                    (changed, mergeKey) = (true, "SceneSettings.PostProcessing.Saturation");
                if (EditorGui.ColorProperty(
                        "SceneSettings.PostProcessing.TintColor", "Tint Color", ref tintColor))
                    (changed, mergeKey) = (true, "SceneSettings.PostProcessing.TintColor");
                if (EditorGui.Property(
                        "SceneSettings.PostProcessing.BloomEnabled", "Bloom Enabled", ref bloomEnabled))
                    (changed, mergeKey) = (true, "SceneSettings.PostProcessing.BloomEnabled");
                if (EditorGui.Property(
                        "SceneSettings.PostProcessing.BloomIntensity", "Bloom Intensity", ref bloomIntensity,
                        speed: 0.01f, min: 0f, max: 10f))
                    (changed, mergeKey) = (true, "SceneSettings.PostProcessing.BloomThreshold");
                if (EditorGui.Property(
                        "SceneSettings.PostProcessing.BloomThreshold", "Bloom Threshold", ref bloomThreshold,
                        speed: 0.01f, min: 0f, max: 10f))
                    (changed, mergeKey) = (true, "SceneSettings.PostProcessing.BloomThreshold");
                if (EditorGui.Property(
                        "SceneSettings.PostProcessing.BloomSoftKnee", "Bloom Soft Knee", ref bloomSoftKnee,
                        speed: 0.01f, min: 0f, max: 10f))
                    (changed, mergeKey) = (true, "SceneSettings.PostProcessing.BloomSoftKnee");
            }
        }

        edited.AmbientLightIntensity = ambientIntensity;
        edited.AmbientLightColor = new Color(ambientColor.X, ambientColor.Y, ambientColor.Z, ambientColor.W);
        edited.Exposure = exposure;
        postProcessing.HueShift = hueShift;
        postProcessing.Saturation = saturation;
        postProcessing.TintColor = new Color(tintColor.X, tintColor.Y, tintColor.Z, tintColor.W);
        postProcessing.BloomEnabled = bloomEnabled;
        postProcessing.BloomThreshold = bloomThreshold;
        postProcessing.BloomSoftKnee = bloomSoftKnee;
        postProcessing.BloomIntensity = bloomIntensity;
        postProcessing.ToneMappingType = (ToneMappingType)toneMap;

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
            EditorGui.Error(_error);
    }

    private static Vector4 ToNumerics(Color color) =>
        new(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f);
}
