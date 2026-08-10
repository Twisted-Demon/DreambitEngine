using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Dreambit.AssetEditor;

internal static class EditorTheme
{
    public static readonly Color WindowBackgroundColor = Color.Parse("#0F131A");
    public static readonly Color PanelColor = Color.Parse("#151B24");
    public static readonly Color SurfaceColor = Color.Parse("#1A2230");
    public static readonly Color SurfaceRaisedColor = Color.Parse("#202A39");
    public static readonly Color BorderColor = Color.Parse("#2A3647");
    public static readonly Color TextColor = Color.Parse("#E8EDF5");
    public static readonly Color MutedTextColor = Color.Parse("#91A0B5");
    public static readonly Color AccentColor = Color.Parse("#6EA8FE");
    public static readonly Color AccentStrongColor = Color.Parse("#3D7FE8");
    public static readonly Color DangerColor = Color.Parse("#E66A73");
    public static readonly Color WarningColor = Color.Parse("#E8B45E");
    public static readonly Color SuccessColor = Color.Parse("#70C891");

    public static readonly IBrush WindowBackground = new SolidColorBrush(WindowBackgroundColor);
    public static readonly IBrush Panel = new SolidColorBrush(PanelColor);
    public static readonly IBrush Surface = new SolidColorBrush(SurfaceColor);
    public static readonly IBrush SurfaceRaised = new SolidColorBrush(SurfaceRaisedColor);
    public static readonly IBrush Border = new SolidColorBrush(BorderColor);
    public static readonly IBrush Text = new SolidColorBrush(TextColor);
    public static readonly IBrush MutedText = new SolidColorBrush(MutedTextColor);
    public static readonly IBrush Accent = new SolidColorBrush(AccentColor);
    public static readonly IBrush AccentStrong = new SolidColorBrush(AccentStrongColor);
    public static readonly IBrush Danger = new SolidColorBrush(DangerColor);
    public static readonly IBrush Warning = new SolidColorBrush(WarningColor);
    public static readonly IBrush Success = new SolidColorBrush(SuccessColor);

    public static Border Card(Control content, Thickness? padding = null)
        => new()
        {
            Background = Surface,
            BorderBrush = Border,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Padding = padding ?? new Thickness(18),
            Child = content
        };

    public static Border SubtleCard(Control content, Thickness? padding = null)
        => new()
        {
            Background = Panel,
            BorderBrush = Border,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = padding ?? new Thickness(14),
            Child = content
        };

    public static TextBlock Title(string text, double size = 22)
        => new()
        {
            Text = text,
            FontSize = size,
            FontWeight = FontWeight.SemiBold,
            Foreground = Text,
            TextTrimming = TextTrimming.CharacterEllipsis
        };

    public static TextBlock SectionTitle(string text)
        => new()
        {
            Text = text,
            FontSize = 14,
            FontWeight = FontWeight.SemiBold,
            Foreground = Text
        };

    public static TextBlock Caption(string text)
        => new()
        {
            Text = text,
            FontSize = 12,
            Foreground = MutedText,
            TextWrapping = TextWrapping.Wrap
        };

    public static TextBlock FieldLabel(string text)
        => new()
        {
            Text = text,
            FontSize = 12.5,
            Foreground = MutedText,
            VerticalAlignment = AvaloniaVerticalAlignment.Center
        };

    public static Button Button(string text, ButtonTone tone = ButtonTone.Secondary)
    {
        var button = new Button
        {
            Content = text,
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(14, 8),
            MinHeight = 36,
            HorizontalContentAlignment = AvaloniaHorizontalAlignment.Center,
            VerticalContentAlignment = AvaloniaVerticalAlignment.Center,
            FontWeight = FontWeight.Medium,
            BorderThickness = new Thickness(1)
        };

        switch (tone)
        {
            case ButtonTone.Primary:
                button.Background = AccentStrong;
                button.BorderBrush = AccentStrong;
                button.Foreground = Brushes.White;
                break;
            case ButtonTone.Danger:
                button.Background = new SolidColorBrush(Color.Parse("#4A2228"));
                button.BorderBrush = new SolidColorBrush(Color.Parse("#6C323A"));
                button.Foreground = new SolidColorBrush(Color.Parse("#FFC7CC"));
                break;
            case ButtonTone.Ghost:
                button.Background = Brushes.Transparent;
                button.BorderBrush = Brushes.Transparent;
                button.Foreground = MutedText;
                break;
            default:
                button.Background = SurfaceRaised;
                button.BorderBrush = Border;
                button.Foreground = Text;
                break;
        }

        return button;
    }

    public static TextBox TextBox(string? text = null, string? watermark = null)
        => new()
        {
            Text = text ?? string.Empty,
            PlaceholderText = watermark,
            Background = Panel,
            Foreground = Text,
            BorderBrush = Border,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(7),
            Padding = new Thickness(10, 7),
            MinHeight = 36
        };

    public static ComboBox ComboBox()
        => new()
        {
            Background = Panel,
            Foreground = Text,
            BorderBrush = Border,
            BorderThickness = new Thickness(1),
            MinHeight = 36,
            HorizontalAlignment = AvaloniaHorizontalAlignment.Stretch
        };

    public static Separator Separator()
        => new()
        {
            Background = Border,
            Margin = new Thickness(0, 6)
        };

    public static Border VerticalDivider()
        => new()
        {
            Width = 1,
            Background = Border,
            Margin = new Thickness(6, 3)
        };

    public enum ButtonTone
    {
        Primary,
        Secondary,
        Danger,
        Ghost
    }
}
