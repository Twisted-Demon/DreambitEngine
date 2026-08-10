using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Dreambit.AssetEditor.Core;
using Newtonsoft.Json.Linq;

namespace Dreambit.AssetEditor.Controls;

internal sealed class GenericAssetEditorView : UserControl
{
    public GenericAssetEditorView(AssetEditorProject project, Type assetType, JObject json)
    {
        Background = EditorTheme.WindowBackground;

        var stack = new StackPanel { Spacing = 14 };
        stack.Children.Add(new StackPanel
        {
            Spacing = 4,
            Children =
            {
                EditorTheme.Title(assetType.Name, 20),
                EditorTheme.Caption(assetType.FullName ?? assetType.Name)
            }
        });

        var members = ReflectionHelpers.GetAssetMembers(assetType);
        if (members.Count == 0)
        {
            stack.Children.Add(EditorTheme.Card(EditorTheme.Caption("No editable JSON members were discovered for this asset type.")));
        }
        else
        {
            var fields = new StackPanel { Spacing = 11 };
            foreach (var member in members)
            {
                var token = json.TryGetValue(member.JsonName, StringComparison.OrdinalIgnoreCase, out var existing)
                    ? existing
                    : ReflectionHelpers.CreateDefaultToken(member.ValueType);

                var editor = JsonMemberEditor.Create(
                    project,
                    member.ValueType,
                    token,
                    value =>
                    {
                        json[member.JsonName] = value;
                        Changed?.Invoke(this, EventArgs.Empty);
                    },
                    $"{assetType.Name}.{member.DisplayName}");

                fields.Children.Add(CreateField(member.DisplayName, editor));
            }
            stack.Children.Add(EditorTheme.Card(fields));
        }

        Content = new ScrollViewer
        {
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
            Content = stack
        };
    }

    public event EventHandler? Changed;

    private static Control CreateField(string label, Control editor)
    {
        var grid = new Grid
        {
            ColumnDefinitions = ColumnDefinitions.Parse("210,*"),
            ColumnSpacing = 18
        };
        grid.Children.Add(EditorTheme.FieldLabel(label));
        Grid.SetColumn(editor, 1);
        grid.Children.Add(editor);
        return grid;
    }
}
