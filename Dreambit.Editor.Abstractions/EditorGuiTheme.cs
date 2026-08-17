using System.Numerics;

namespace Dreambit.EditorApi;

/// <summary>
/// Immutable visual tokens shared by Dreambit Editor surfaces and game-defined custom editors.
/// </summary>
public static class EditorGuiTheme
{
    public static Vector4 WindowBackground => new(0.043f, 0.067f, 0.094f, 1f);
    public static Vector4 PanelBackground => new(0.067f, 0.098f, 0.145f, 1f);
    public static Vector4 SurfaceBackground => new(0.090f, 0.130f, 0.170f, 1f);
    public static Vector4 ElevatedSurface => new(0.106f, 0.149f, 0.200f, 1f);
    public static Vector4 HoveredSurface => new(0.114f, 0.204f, 0.255f, 1f);
    public static Vector4 ActiveSurface => new(0.055f, 0.263f, 0.314f, 1f);
    public static Vector4 SelectedBackground => new(0.063f, 0.300f, 0.353f, 1f);
    public static Vector4 Border => new(0.153f, 0.208f, 0.271f, 0.92f);
    public static Vector4 StrongBorder => new(0.200f, 0.275f, 0.353f, 1f);

    public static Vector4 PrimaryAccent => new(0.082f, 0.785f, 0.906f, 1f);
    public static Vector4 PrimaryAccentHovered => new(0.180f, 0.875f, 0.965f, 1f);
    public static Vector4 PrimaryAccentActive => new(0.040f, 0.635f, 0.760f, 1f);
    public static Vector4 SecondaryAccent => new(0.590f, 0.420f, 0.920f, 1f);

    public static Vector4 PrimaryText => new(0.886f, 0.910f, 0.940f, 1f);
    public static Vector4 MutedText => new(0.600f, 0.655f, 0.720f, 1f);
    public static Vector4 DisabledText => new(0.360f, 0.430f, 0.515f, 1f);
    public static Vector4 Error => new(0.965f, 0.360f, 0.420f, 1f);
    public static Vector4 Warning => new(0.985f, 0.700f, 0.275f, 1f);
    public static Vector4 Success => new(0.330f, 0.820f, 0.560f, 1f);
    public static Vector4 ViewportBackground => new(0.030f, 0.050f, 0.065f, 1f);
    public static Vector4 Grid => new(0.500f, 0.720f, 0.810f, 0.14f);
    public static Vector4 GridAxis => new(0.082f, 0.785f, 0.906f, 0.40f);
    public static Vector4 ErrorBackground => new(0.290f, 0.070f, 0.090f, 0.94f);
    public static Vector4 GizmoAxisX => new(0.950f, 0.280f, 0.300f, 1f);
    public static Vector4 GizmoAxisY => new(0.320f, 0.860f, 0.420f, 1f);
    public static Vector4 GizmoRotation => new(1f, 0.780f, 0.220f, 1f);
    public static Vector4 GizmoScale => new(0.105f, 0.690f, 0.745f, 1f);

    public static Vector2 WindowPadding => new(10f, 9f);
    public static Vector2 FramePadding => new(9f, 5.5f);
    public static Vector2 SectionHeaderPadding => new(10f, 7f);
    public static Vector2 CellPadding => new(8f, 5f);
    public static Vector2 ItemSpacing => new(9f, 7f);
    public static Vector2 ItemInnerSpacing => new(7f, 5f);
    public static Vector2 BreadcrumbSpacing => new(4f, 5f);

    public const float FontSize = 15f;
    public const float ControlHeight = 29f;
    public const float ToolbarIconButtonSize = 30f;
    public const float PropertyLabelWidth = 176f;
    public const float MinimumPropertyLabelWidth = 92f;
    public const float PropertyLabelRatio = 0.36f;
    public const float VectorAxisLabelSpacing = 4f;
    public const float MinimumVectorComponentWidth = 32f;
    public const float PropertyIndent = 12f;
    public const float CompactSpacing = 4f;
    public const float NormalSpacing = 8f;
    public const float SectionSpacing = 14f;
    public const float WindowRounding = 4f;
    public const float SurfaceRounding = 4f;
    public const float FrameRounding = 4f;
    public const float PopupRounding = 5f;
    public const float ScrollbarRounding = 7f;
    public const float BorderThickness = 1f;
    public const float ScrollbarSize = 13f;
    public const float GrabMinimumSize = 9f;
}
