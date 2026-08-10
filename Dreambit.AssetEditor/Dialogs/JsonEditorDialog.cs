using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Dreambit.AssetEditor.Dialogs;

internal sealed class JsonEditorDialog : AvaloniaWindow
{
    private readonly TextBox _editor;

    public JsonEditorDialog(string title, JToken token)
    {
        Title = title;
        Width = 920;
        Height = 720;
        MinWidth = 640;
        MinHeight = 480;
        ShowInTaskbar = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = EditorTheme.WindowBackground;

        _editor = new TextBox
        {
            Text = token.ToString(Formatting.Indented),
            AcceptsReturn = true,
            AcceptsTab = true,
            TextWrapping = TextWrapping.NoWrap,
            FontFamily = new FontFamily("Consolas"),
            FontSize = 13,
            Background = EditorTheme.Panel,
            Foreground = EditorTheme.Text,
            BorderBrush = EditorTheme.Border,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(12)
        };
        ScrollViewer.SetVerticalScrollBarVisibility(_editor, ScrollBarVisibility.Auto);
        ScrollViewer.SetHorizontalScrollBarVisibility(_editor, ScrollBarVisibility.Auto);

        var format = EditorTheme.Button("Format");
        format.Click += async (_, _) =>
        {
            if (TryParse(out var parsed, out var error))
                _editor.Text = parsed.ToString(Formatting.Indented);
            else
                await MessageDialog.ShowAsync(this, "Invalid JSON", error, tone: MessageTone.Error);
        };

        var cancel = EditorTheme.Button("Cancel");
        cancel.Click += (_, _) => Close(null);

        var apply = EditorTheme.Button("Apply", EditorTheme.ButtonTone.Primary);
        apply.Click += async (_, _) =>
        {
            if (!TryParse(out var parsed, out var error))
            {
                await MessageDialog.ShowAsync(this, "Invalid JSON", error, tone: MessageTone.Error);
                return;
            }
            Close(parsed);
        };

        var toolbar = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = AvaloniaHorizontalAlignment.Right,
            Spacing = 8,
            Children = { format, cancel, apply }
        };

        var grid = new Grid
        {
            RowDefinitions = RowDefinitions.Parse("Auto,*,Auto"),
            RowSpacing = 14
        };

        grid.Children.Add(new StackPanel
        {
            Spacing = 4,
            Children =
            {
                EditorTheme.Title("Advanced JSON", 20),
                EditorTheme.Caption("Direct JSON editing is intended as an escape hatch for converter-defined shapes. Blueprint root JSON remains protected.")
            }
        });

        Grid.SetRow(_editor, 1);
        grid.Children.Add(_editor);
        Grid.SetRow(toolbar, 2);
        grid.Children.Add(toolbar);

        Content = new Border
        {
            Margin = new Thickness(18),
            Padding = new Thickness(20),
            Background = EditorTheme.Surface,
            BorderBrush = EditorTheme.Border,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Child = grid
        };
    }

    private bool TryParse(out JToken token, out string error)
    {
        try
        {
            token = JToken.Parse(_editor.Text ?? string.Empty);
            error = string.Empty;
            return true;
        }
        catch (Exception exception)
        {
            token = JValue.CreateNull();
            error = exception.Message;
            return false;
        }
    }
}
