using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;

namespace Dreambit.AssetEditor.Dialogs;

internal sealed class TypePickerDialog : AvaloniaWindow
{
    private readonly IReadOnlyList<Type> _types;
    private readonly TextBox _search;
    private readonly ListBox _list;

    public TypePickerDialog(string title, IEnumerable<Type> types, string noun = "type")
    {
        _types = types.OrderBy(t => t.Name).ThenBy(t => t.Namespace).ToArray();

        Title = title;
        Width = 680;
        Height = 700;
        MinWidth = 520;
        MinHeight = 480;
        ShowInTaskbar = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = EditorTheme.WindowBackground;

        _search = EditorTheme.TextBox(watermark: $"Search {noun}s...");
        _search.Margin = new Thickness(0, 16, 0, 12);
        _search.TextChanged += (_, _) => RefreshList();

        _list = new ListBox
        {
            Background = EditorTheme.Panel,
            BorderBrush = EditorTheme.Border,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8)
        };
        _list.DoubleTapped += (_, _) => AcceptSelection();
        _list.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                AcceptSelection();
                e.Handled = true;
            }
        };

        var cancel = EditorTheme.Button("Cancel");
        cancel.Click += (_, _) => Close(null);

        var select = EditorTheme.Button("Select", EditorTheme.ButtonTone.Primary);
        select.MinWidth = 96;
        select.Click += (_, _) => AcceptSelection();

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = AvaloniaHorizontalAlignment.Right,
            Spacing = 8,
            Children = { cancel, select }
        };

        var content = new Grid
        {
            RowDefinitions = RowDefinitions.Parse("Auto,Auto,*,Auto"),
            RowSpacing = 0
        };

        var heading = new StackPanel
        {
            Spacing = 4,
            Children =
            {
                EditorTheme.Title(title, 20),
                EditorTheme.Caption($"{_types.Count} available {noun}{(_types.Count == 1 ? string.Empty : "s")}. External game DLL types appear here after loading the assembly.")
            }
        };
        content.Children.Add(heading);

        Grid.SetRow(_search, 1);
        content.Children.Add(_search);
        Grid.SetRow(_list, 2);
        content.Children.Add(_list);
        buttons.Margin = new Thickness(0, 14, 0, 0);
        Grid.SetRow(buttons, 3);
        content.Children.Add(buttons);

        Content = new Border
        {
            Margin = new Thickness(18),
            Padding = new Thickness(20),
            Background = EditorTheme.Surface,
            BorderBrush = EditorTheme.Border,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Child = content
        };

        RefreshList();
        Opened += (_, _) => _search.Focus();
    }

    private void RefreshList()
    {
        var search = _search.Text?.Trim() ?? string.Empty;
        var items = _types
            .Select(type => new TypeItem(type))
            .Where(item => search.Length == 0 || item.SearchText.Contains(search, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        _list.ItemsSource = items;
        _list.SelectedIndex = items.Length > 0 ? 0 : -1;
    }

    private void AcceptSelection()
    {
        if (_list.SelectedItem is TypeItem item)
            Close(item.Type);
    }

    private sealed class TypeItem(Type type)
    {
        public Type Type { get; } = type;
        public string SearchText { get; } = $"{type.Name} {type.FullName} {type.Assembly.GetName().Name}";
        public override string ToString() => $"{Type.Name}    {Type.Namespace}  ·  {Type.Assembly.GetName().Name}";
    }
}
