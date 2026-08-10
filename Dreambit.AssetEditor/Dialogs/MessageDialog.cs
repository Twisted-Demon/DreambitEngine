using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Dreambit.AssetEditor.Dialogs;

internal enum MessageDialogResult
{
    None,
    Ok,
    Yes,
    No,
    Cancel
}

internal enum MessageDialogButtons
{
    Ok,
    YesNo,
    YesNoCancel
}

internal sealed class MessageDialog : AvaloniaWindow
{
    private MessageDialog(string title, string message, MessageDialogButtons buttons, MessageTone tone)
    {
        Title = title;
        Width = 520;
        MinWidth = 420;
        MaxWidth = 700;
        SizeToContent = SizeToContent.Height;
        CanResize = false;
        ShowInTaskbar = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = EditorTheme.WindowBackground;

        var accent = tone switch
        {
            MessageTone.Error => EditorTheme.Danger,
            MessageTone.Warning => EditorTheme.Warning,
            MessageTone.Success => EditorTheme.Success,
            _ => EditorTheme.Accent
        };

        var icon = new Border
        {
            Width = 42,
            Height = 42,
            CornerRadius = new CornerRadius(21),
            Background = CreateTint(accent),
            BorderBrush = accent,
            BorderThickness = new Thickness(1),
            Child = new TextBlock
            {
                Text = tone switch
                {
                    MessageTone.Error => "!",
                    MessageTone.Warning => "!",
                    MessageTone.Success => "✓",
                    _ => "i"
                },
                Foreground = accent,
                FontSize = 20,
                FontWeight = FontWeight.Bold,
                HorizontalAlignment = AvaloniaHorizontalAlignment.Center,
                VerticalAlignment = AvaloniaVerticalAlignment.Center
            }
        };

        var messageText = new TextBlock
        {
            Text = message,
            Foreground = EditorTheme.Text,
            TextWrapping = TextWrapping.Wrap,
            FontSize = 13,
            LineHeight = 20,
            MaxWidth = 560
        };

        var messageScroll = new ScrollViewer
        {
            MaxHeight = 460,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
            Content = messageText
        };

        var contentGrid = new Grid
        {
            ColumnDefinitions = ColumnDefinitions.Parse("Auto,*"),
            ColumnSpacing = 16
        };
        contentGrid.Children.Add(icon);
        Grid.SetColumn(messageScroll, 1);
        contentGrid.Children.Add(messageScroll);

        var buttonRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = AvaloniaHorizontalAlignment.Right,
            Spacing = 8,
            Margin = new Thickness(0, 20, 0, 0)
        };

        switch (buttons)
        {
            case MessageDialogButtons.YesNoCancel:
                buttonRow.Children.Add(CreateResultButton("Cancel", MessageDialogResult.Cancel));
                buttonRow.Children.Add(CreateResultButton("No", MessageDialogResult.No));
                buttonRow.Children.Add(CreateResultButton("Yes", MessageDialogResult.Yes, EditorTheme.ButtonTone.Primary));
                break;
            case MessageDialogButtons.YesNo:
                buttonRow.Children.Add(CreateResultButton("No", MessageDialogResult.No));
                buttonRow.Children.Add(CreateResultButton("Yes", MessageDialogResult.Yes, EditorTheme.ButtonTone.Primary));
                break;
            default:
                buttonRow.Children.Add(CreateResultButton("OK", MessageDialogResult.Ok, EditorTheme.ButtonTone.Primary));
                break;
        }

        var stack = new StackPanel
        {
            Spacing = 0,
            Children = { contentGrid, buttonRow }
        };

        Content = new Border
        {
            Padding = new Thickness(22),
            Background = EditorTheme.Surface,
            BorderBrush = EditorTheme.Border,
            BorderThickness = new Thickness(1),
            Child = stack
        };
    }

    public static Task<MessageDialogResult> ShowAsync(
        AvaloniaWindow owner,
        string title,
        string message,
        MessageDialogButtons buttons = MessageDialogButtons.Ok,
        MessageTone tone = MessageTone.Info)
        => new MessageDialog(title, message, buttons, tone).ShowDialog<MessageDialogResult>(owner);

    private static IBrush CreateTint(IBrush brush)
    {
        var color = brush is SolidColorBrush solid ? solid.Color : EditorTheme.AccentColor;
        return new SolidColorBrush(Color.FromArgb(42, color.R, color.G, color.B));
    }

    private Button CreateResultButton(string text, MessageDialogResult result, EditorTheme.ButtonTone tone = EditorTheme.ButtonTone.Secondary)
    {
        var button = EditorTheme.Button(text, tone);
        button.MinWidth = 88;
        button.Click += (_, _) => Close(result);
        return button;
    }
}

internal enum MessageTone
{
    Info,
    Success,
    Warning,
    Error
}
