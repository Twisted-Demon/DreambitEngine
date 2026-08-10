using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Dreambit;
using Dreambit.AssetEditor.Core;
using Dreambit.AssetEditor.Dialogs;
using Dreambit.ECS;
using Newtonsoft.Json.Linq;

namespace Dreambit.AssetEditor.Controls;

internal sealed class BlueprintEditorView : UserControl
{
    private readonly AssetCatalog _catalog;
    private readonly AssetEditorProject _project;
    private readonly JObject _document;
    private readonly bool _sceneMode;
    private readonly TreeView _tree;
    private readonly ScrollViewer _details;

    public BlueprintEditorView(AssetCatalog catalog, AssetEditorProject project, JObject document, bool sceneMode)
    {
        _catalog = catalog;
        _project = project;
        _document = document;
        _sceneMode = sceneMode;
        Background = EditorTheme.WindowBackground;

        _tree = new TreeView
        {
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0)
        };
        _tree.SelectionChanged += (_, _) => RefreshDetails();

        _details = new ScrollViewer
        {
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            Background = Brushes.Transparent
        };

        var root = new Grid
        {
            ColumnDefinitions = ColumnDefinitions.Parse("310,6,*")
        };

        var navigator = BuildNavigator();
        root.Children.Add(navigator);

        var splitter = new GridSplitter
        {
            Background = EditorTheme.Border,
            ResizeDirection = GridResizeDirection.Columns,
            ResizeBehavior = GridResizeBehavior.PreviousAndNext,
            HorizontalAlignment = AvaloniaHorizontalAlignment.Stretch,
            VerticalAlignment = AvaloniaVerticalAlignment.Stretch
        };
        Grid.SetColumn(splitter, 1);
        root.Children.Add(splitter);

        var detailChrome = new Border
        {
            Margin = new Thickness(14, 0, 0, 0),
            Background = Brushes.Transparent,
            Child = _details
        };
        Grid.SetColumn(detailChrome, 2);
        root.Children.Add(detailChrome);

        Content = root;
        BuildTree();
    }

    public event EventHandler? Changed;

    private Control BuildNavigator()
    {
        var rows = _sceneMode ? "Auto,Auto,*,Auto" : "Auto,*,Auto";
        var grid = new Grid
        {
            RowDefinitions = RowDefinitions.Parse(rows),
            RowSpacing = 12
        };

        grid.Children.Add(new StackPanel
        {
            Spacing = 3,
            Children =
            {
                EditorTheme.SectionTitle(_sceneMode ? "Scene Hierarchy" : "Entity Hierarchy"),
                EditorTheme.Caption(_sceneMode ? "Select an entity to edit its blueprint data." : "Children are stored directly in the EntityBlueprint.")
            }
        });

        var row = 1;
        if (_sceneMode)
        {
            var sceneName = EditorTheme.TextBox(_document.Value<string>("name") ?? string.Empty, "Scene name");
            sceneName.PropertyChanged += (_, args) =>
            {
                if (args.Property != TextBox.TextProperty)
                    return;
                _document["name"] = sceneName.Text ?? string.Empty;
                Changed?.Invoke(this, EventArgs.Empty);
            };
            Grid.SetRow(sceneName, row++);
            grid.Children.Add(sceneName);
        }

        var treeCard = EditorTheme.SubtleCard(_tree, new Thickness(6));
        Grid.SetRow(treeCard, row++);
        grid.Children.Add(treeCard);

        var actions = new Grid
        {
            RowDefinitions = _sceneMode
                ? RowDefinitions.Parse("Auto,Auto,Auto,Auto")
                : RowDefinitions.Parse("Auto,Auto,Auto"),
            RowSpacing = 8
        };

        var actionRow = 0;
        if (_sceneMode)
        {
            var addRoot = EditorTheme.Button("+ Add Root Entity", EditorTheme.ButtonTone.Primary);
            addRoot.HorizontalAlignment = AvaloniaHorizontalAlignment.Stretch;
            addRoot.Click += (_, _) => AddRoot();
            actions.Children.Add(addRoot);
            actionRow++;
        }

        var addChild = EditorTheme.Button("+ Add Child");
        addChild.HorizontalAlignment = AvaloniaHorizontalAlignment.Stretch;
        addChild.Click += (_, _) => AddChild();
        Grid.SetRow(addChild, actionRow++);
        actions.Children.Add(addChild);

        var duplicate = EditorTheme.Button("Duplicate");
        duplicate.HorizontalAlignment = AvaloniaHorizontalAlignment.Stretch;
        duplicate.Click += async (_, _) => await DuplicateSelectedAsync();
        Grid.SetRow(duplicate, actionRow++);
        actions.Children.Add(duplicate);

        var delete = EditorTheme.Button("Delete", EditorTheme.ButtonTone.Danger);
        delete.HorizontalAlignment = AvaloniaHorizontalAlignment.Stretch;
        delete.Click += async (_, _) => await DeleteSelectedAsync();
        Grid.SetRow(delete, actionRow);
        actions.Children.Add(delete);

        Grid.SetRow(actions, row);
        grid.Children.Add(actions);

        return EditorTheme.Card(grid, new Thickness(16));
    }

    private void BuildTree(JObject? selectEntity = null)
    {
        var nodes = new List<TreeViewItem>();
        if (_sceneMode)
        {
            foreach (var entity in EnsureArray(_document, "entities").OfType<JObject>())
                nodes.Add(CreateNode(entity));
        }
        else
        {
            EnsureEntityDefaults(_document);
            nodes.Add(CreateNode(_document));
        }

        _tree.ItemsSource = nodes;

        TreeViewItem? selected = selectEntity is null ? nodes.FirstOrDefault() : FindNode(nodes, selectEntity);
        if (selected is not null)
            selected.IsSelected = true;
        else
            RefreshDetails();
    }

    private TreeViewItem CreateNode(JObject entity)
    {
        EnsureEntityDefaults(entity);
        var node = new TreeViewItem
        {
            Header = entity.Value<string>("name") ?? "Entity",
            Tag = entity,
            IsExpanded = true,
            Foreground = EditorTheme.Text
        };
        node.ItemsSource = EnsureArray(entity, "children").OfType<JObject>().Select(CreateNode).ToArray();
        return node;
    }

    private void RefreshDetails()
    {
        if ((_tree.SelectedItem as TreeViewItem)?.Tag is not JObject entity)
        {
            _details.Content = EditorTheme.Card(new StackPanel
            {
                Spacing = 6,
                Children =
                {
                    EditorTheme.Title("Nothing selected", 18),
                    EditorTheme.Caption(_sceneMode ? "Add or select an entity in the hierarchy." : "Select an entity in the hierarchy.")
                }
            });
            return;
        }

        var name = entity.Value<string>("name") ?? "Entity";
        var guid = entity.Value<string>("guid") ?? string.Empty;
        var selectionTitle = EditorTheme.Title(name, 18);

        var stack = new StackPanel { Spacing = 14 };

        var heading = new Grid { ColumnDefinitions = ColumnDefinitions.Parse("*,Auto") };
        var headingText = new StackPanel
        {
            Spacing = 4,
            Children =
            {
                selectionTitle,
                EditorTheme.Caption(guid.Length == 0 ? "Entity" : $"GUID  {guid}")
            }
        };
        heading.Children.Add(headingText);
        stack.Children.Add(heading);

        var fields = new StackPanel { Spacing = 10 };
        AddTextField(fields, "Name", name, value =>
        {
            entity["name"] = value;
            selectionTitle.Text = string.IsNullOrWhiteSpace(value) ? "Entity" : value;
            if (_tree.SelectedItem is TreeViewItem selected)
                selected.Header = selectionTitle.Text;
        });
        AddTextField(fields, "GUID", guid, value =>
        {
            if (Guid.TryParse(value, out _))
                entity["guid"] = value;
        }, "00000000-0000-0000-0000-000000000000");
        AddBoolField(fields, "Enabled", entity.Value<bool?>("enabled") ?? true, value => entity["enabled"] = value);
        AddTextField(fields, "Tags", string.Join(", ", EnsureArray(entity, "tags").Values<string>()), value =>
        {
            entity["tags"] = new JArray(value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        }, "enemy, flying, boss");
        AddTokenField(fields, "Position", typeof(Microsoft.Xna.Framework.Vector3), entity["position"] ?? new JArray(0, 0, 0), value => entity["position"] = value);
        AddTokenField(fields, "Rotation", typeof(Microsoft.Xna.Framework.Vector3), entity["rotation"] ?? new JArray(0, 0, 0), value => entity["rotation"] = value);
        AddTokenField(fields, "Scale", typeof(Microsoft.Xna.Framework.Vector3), entity["scale"] ?? new JArray(1, 1, 1), value => entity["scale"] = value);

        stack.Children.Add(EditorTheme.Card(fields));

        stack.Children.Add(new StackPanel
        {
            Spacing = 3,
            Margin = new Thickness(0, 6, 0, 0),
            Children =
            {
                EditorTheme.SectionTitle("Components"),
                EditorTheme.Caption("Only members marked with [DreambitSerialize] are exposed by the inspector.")
            }
        });
        stack.Children.Add(CreateComponentArea(entity));

        _details.Content = stack;
    }

    private Control CreateComponentArea(JObject entity)
    {
        var components = EnsureArray(entity, "components");
        var componentItems = components.OfType<JObject>()
            .Select(json => new ComponentItem(json, json.Value<string>("type") ?? "Unknown Component"))
            .ToArray();

        var list = new ListBox
        {
            ItemsSource = componentItems,
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0)
        };

        var left = new Grid
        {
            RowDefinitions = RowDefinitions.Parse("*,Auto"),
            RowSpacing = 10
        };
        left.Children.Add(EditorTheme.SubtleCard(list, new Thickness(4)));

        var add = EditorTheme.Button("+ Add Component", EditorTheme.ButtonTone.Primary);
        var remove = EditorTheme.Button("Remove", EditorTheme.ButtonTone.Danger);
        var actions = new Grid { ColumnDefinitions = ColumnDefinitions.Parse("*,Auto"), ColumnSpacing = 8 };
        actions.Children.Add(add);
        Grid.SetColumn(remove, 1);
        actions.Children.Add(remove);
        Grid.SetRow(actions, 1);
        left.Children.Add(actions);

        var propertyScroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
            Background = Brushes.Transparent
        };
        var propertyHost = new Border
        {
            Background = Brushes.Transparent,
            Child = propertyScroll
        };

        void RefreshProperties()
        {
            propertyScroll.Content = list.SelectedItem is ComponentItem selected
                ? CreateComponentProperties(selected.Json)
                : EditorTheme.SubtleCard(EditorTheme.Caption("Select a component to edit its properties."));
        }

        list.SelectionChanged += (_, _) => RefreshProperties();
        add.Click += async (_, _) =>
        {
            if (TopLevel.GetTopLevel(this) is not AvaloniaWindow owner)
                return;
            var selectedType = await new TypePickerDialog("Add Component", _catalog.ComponentTypes, "component")
                .ShowDialog<Type?>(owner);
            if (selectedType is null)
                return;

            try
            {
                AddComponentWithRequirements(entity, selectedType);
                Changed?.Invoke(this, EventArgs.Empty);
                RefreshDetails();
            }
            catch (Exception exception)
            {
                await MessageDialog.ShowAsync(owner, "Unable to Add Component", exception.Message);
            }
        };
        remove.Click += (_, _) =>
        {
            if (list.SelectedItem is not ComponentItem selected)
                return;
            components.Remove(selected.Json);
            Changed?.Invoke(this, EventArgs.Empty);
            RefreshDetails();
        };

        var root = new Grid
        {
            ColumnDefinitions = ColumnDefinitions.Parse("260,6,*"),
            Height = 500
        };
        root.Children.Add(EditorTheme.Card(left, new Thickness(12)));

        var splitter = new GridSplitter
        {
            Background = EditorTheme.Border,
            ResizeDirection = GridResizeDirection.Columns,
            ResizeBehavior = GridResizeBehavior.PreviousAndNext
        };
        Grid.SetColumn(splitter, 1);
        root.Children.Add(splitter);
        Grid.SetColumn(propertyHost, 2);
        propertyHost.Margin = new Thickness(12, 0, 0, 0);
        root.Children.Add(propertyHost);

        if (componentItems.Length > 0)
            list.SelectedIndex = 0;
        else
            RefreshProperties();

        return root;
    }

    private Control CreateComponentProperties(JObject component)
    {
        var typeId = component.Value<string>("type") ?? string.Empty;
        var componentType = ResolveComponentType(typeId);

        var stack = new StackPanel { Spacing = 12 };
        stack.Children.Add(new StackPanel
        {
            Spacing = 3,
            Children =
            {
                EditorTheme.Title(componentType?.Name ?? typeId, 17),
                EditorTheme.Caption(typeId)
            }
        });

        AddBoolField(stack, "Enabled", component.Value<bool?>("enabled") ?? true, value => component["enabled"] = value);

        if (componentType is null)
        {
            stack.Children.Add(new Border
            {
                Background = new SolidColorBrush(Color.Parse("#33282A")),
                BorderBrush = EditorTheme.Danger,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12),
                Child = new TextBlock
                {
                    Text = "This component type is not loaded. Load the DLL that defines it to expose its [DreambitSerialize] properties.",
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = EditorTheme.Text
                }
            });
            return EditorTheme.Card(stack);
        }

        var properties = component["properties"] as JObject;
        if (properties is null)
        {
            properties = new JObject();
            component["properties"] = properties;
        }

        object? instance = null;
        try { instance = Activator.CreateInstance(componentType); } catch { }

        var members = ReflectionHelpers.GetBlueprintMembers(componentType);
        foreach (var member in members)
        {
            var token = properties.TryGetValue(member.JsonName, StringComparison.OrdinalIgnoreCase, out var existing)
                ? existing
                : ReflectionHelpers.CreateDefaultToken(member.ValueType, instance, member.Member);
            AddTokenField(stack, member.DisplayName, member.ValueType, token, value => properties[member.JsonName] = value,
                $"{componentType.Name}.{member.DisplayName}");
        }

        if (members.Count == 0)
            stack.Children.Add(EditorTheme.Caption("This component has no [DreambitSerialize] members."));

        var unsupported = properties.Properties()
            .Where(p => members.All(m => !m.JsonName.Equals(p.Name, StringComparison.OrdinalIgnoreCase)))
            .Select(p => p.Name)
            .ToArray();
        if (unsupported.Length > 0)
        {
            stack.Children.Add(new TextBlock
            {
                Text = $"Preserved but hidden non-[DreambitSerialize] properties: {string.Join(", ", unsupported)}",
                TextWrapping = TextWrapping.Wrap,
                Foreground = EditorTheme.Warning,
                FontSize = 12
            });
        }

        return EditorTheme.Card(stack);
    }

    private void AddTextField(StackPanel host, string label, string value, Action<string> setter, string? watermark = null)
    {
        var editor = EditorTheme.TextBox(value, watermark);
        editor.PropertyChanged += (_, args) =>
        {
            if (args.Property != TextBox.TextProperty)
                return;
            setter(editor.Text ?? string.Empty);
            Changed?.Invoke(this, EventArgs.Empty);
        };
        host.Children.Add(CreateField(label, editor));
    }

    private void AddBoolField(StackPanel host, string label, bool value, Action<bool> setter)
    {
        var check = new CheckBox
        {
            IsChecked = value,
            Foreground = EditorTheme.Text,
            VerticalAlignment = AvaloniaVerticalAlignment.Center
        };
        check.IsCheckedChanged += (_, _) =>
        {
            setter(check.IsChecked == true);
            Changed?.Invoke(this, EventArgs.Empty);
        };
        host.Children.Add(CreateField(label, check));
    }

    private void AddTokenField(StackPanel host, string label, Type type, JToken value, Action<JToken> setter, string? title = null)
    {
        var editor = JsonMemberEditor.Create(_project, type, value, token =>
        {
            setter(token);
            Changed?.Invoke(this, EventArgs.Empty);
        }, title ?? label);
        host.Children.Add(CreateField(label, editor));
    }

    private static Control CreateField(string label, Control editor)
    {
        var grid = new Grid
        {
            ColumnDefinitions = ColumnDefinitions.Parse("170,*"),
            ColumnSpacing = 16
        };
        grid.Children.Add(EditorTheme.FieldLabel(label));
        Grid.SetColumn(editor, 1);
        grid.Children.Add(editor);
        return grid;
    }

    private JObject AddComponentWithRequirements(JObject entity, Type componentType)
    {
        var components = EnsureArray(entity, "components");
        var existingTypes = components.OfType<JObject>()
            .Select(x => ResolveComponentType(x.Value<string>("type") ?? string.Empty))
            .Where(x => x is not null)
            .Cast<Type>()
            .ToHashSet();
        var resolving = new HashSet<Type>();

        JObject AddRecursive(Type type)
        {
            if (existingTypes.Contains(type))
                return components.OfType<JObject>().First(x => ResolveComponentType(x.Value<string>("type") ?? string.Empty) == type);

            if (!resolving.Add(type))
                throw new InvalidOperationException($"Component requirement cycle detected at {type.FullName ?? type.Name}.");

            try
            {
                foreach (var requirement in type.GetCustomAttributes<RequireAttribute>(true).SelectMany(a => a.RequiredTypes))
                    AddRecursive(requirement);

                var json = CreateComponentJson(type);
                components.Add(json);
                existingTypes.Add(type);
                return json;
            }
            finally
            {
                resolving.Remove(type);
            }
        }

        return AddRecursive(componentType);
    }

    private static JObject CreateComponentJson(Type componentType)
    {
        var properties = new JObject();
        object? instance = null;
        try { instance = Activator.CreateInstance(componentType); } catch { }

        if (instance is not null)
        {
            foreach (var member in ReflectionHelpers.GetBlueprintMembers(componentType))
            {
                try
                {
                    object? value = member.Member switch
                    {
                        PropertyInfo property => property.GetValue(instance),
                        FieldInfo field => field.GetValue(instance),
                        _ => null
                    };
                    if (value is not null)
                        properties[member.JsonName] = DreambitJson.ToToken(value);
                }
                catch
                {
                    // The property remains available and will get its default token on first edit.
                }
            }
        }

        return new JObject
        {
            ["type"] = ReflectionHelpers.ComponentTypeId(componentType),
            ["enabled"] = true,
            ["properties"] = properties
        };
    }

    private Type? ResolveComponentType(string typeId)
    {
        var direct = BlueprintResolver.ResolveComponentType(typeId);
        if (direct is not null)
            return direct;

        return _catalog.ComponentTypes.FirstOrDefault(type =>
            ReflectionHelpers.ComponentTypeId(type).Equals(typeId, StringComparison.OrdinalIgnoreCase) ||
            type.FullName?.Equals(typeId, StringComparison.OrdinalIgnoreCase) == true ||
            type.Name.Equals(typeId, StringComparison.OrdinalIgnoreCase));
    }

    private void AddRoot()
    {
        if (!_sceneMode)
            return;
        var entity = CreateEntity("Entity");
        EnsureArray(_document, "entities").Add(entity);
        Changed?.Invoke(this, EventArgs.Empty);
        BuildTree(entity);
    }

    private void AddChild()
    {
        if ((_tree.SelectedItem as TreeViewItem)?.Tag is not JObject parent)
            return;
        var child = CreateEntity("Child Entity");
        EnsureArray(parent, "children").Add(child);
        Changed?.Invoke(this, EventArgs.Empty);
        BuildTree(child);
    }

    private async Task DuplicateSelectedAsync()
    {
        if ((_tree.SelectedItem as TreeViewItem)?.Tag is not JObject selected)
            return;

        var clone = (JObject)selected.DeepClone();
        RegenerateGuids(clone);
        clone["name"] = (clone.Value<string>("name") ?? "Entity") + " Copy";

        var selectedNode = (TreeViewItem)_tree.SelectedItem!;
        if (FindParentEntity(selectedNode, out var parent))
            EnsureArray(parent!, "children").Add(clone);
        else if (_sceneMode)
            EnsureArray(_document, "entities").Add(clone);
        else
        {
            if (TopLevel.GetTopLevel(this) is AvaloniaWindow owner)
                await MessageDialog.ShowAsync(owner, "Duplicate Entity", "The root EntityBlueprint cannot be duplicated beside itself. Duplicate a child entity instead.");
            return;
        }

        Changed?.Invoke(this, EventArgs.Empty);
        BuildTree(clone);
    }

    private async Task DeleteSelectedAsync()
    {
        if ((_tree.SelectedItem as TreeViewItem)?.Tag is not JObject selected)
            return;

        var selectedNode = (TreeViewItem)_tree.SelectedItem!;
        if (FindParentEntity(selectedNode, out var parent))
            EnsureArray(parent!, "children").Remove(selected);
        else if (_sceneMode)
            EnsureArray(_document, "entities").Remove(selected);
        else
        {
            if (TopLevel.GetTopLevel(this) is AvaloniaWindow owner)
                await MessageDialog.ShowAsync(owner, "Delete Entity", "The root entity of an EntityBlueprint cannot be deleted.");
            return;
        }

        Changed?.Invoke(this, EventArgs.Empty);
        BuildTree();
    }

    private bool FindParentEntity(TreeViewItem childNode, out JObject? parentEntity)
    {
        foreach (var root in (_tree.ItemsSource as IEnumerable<TreeViewItem>) ?? [])
        {
            if (TryFindParent(root, childNode, out parentEntity))
                return true;
        }
        parentEntity = null;
        return false;
    }

    private static bool TryFindParent(TreeViewItem current, TreeViewItem target, out JObject? parentEntity)
    {
        foreach (var child in (current.ItemsSource as IEnumerable<TreeViewItem>) ?? [])
        {
            if (ReferenceEquals(child, target))
            {
                parentEntity = current.Tag as JObject;
                return parentEntity is not null;
            }
            if (TryFindParent(child, target, out parentEntity))
                return true;
        }
        parentEntity = null;
        return false;
    }

    private static TreeViewItem? FindNode(IEnumerable<TreeViewItem> nodes, JObject entity)
    {
        foreach (var node in nodes)
        {
            if (ReferenceEquals(node.Tag, entity))
                return node;
            var child = FindNode((node.ItemsSource as IEnumerable<TreeViewItem>) ?? [], entity);
            if (child is not null)
                return child;
        }
        return null;
    }

    private static JObject CreateEntity(string name) => new()
    {
        ["name"] = name,
        ["guid"] = Guid.NewGuid().ToString(),
        ["tags"] = new JArray(),
        ["enabled"] = true,
        ["position"] = new JArray(0f, 0f, 0f),
        ["rotation"] = new JArray(0f, 0f, 0f),
        ["scale"] = new JArray(1f, 1f, 1f),
        ["components"] = new JArray(),
        ["children"] = new JArray()
    };

    private static void EnsureEntityDefaults(JObject entity)
    {
        entity["name"] ??= "Entity";
        entity["guid"] ??= Guid.NewGuid().ToString();
        entity["tags"] ??= new JArray();
        entity["enabled"] ??= true;
        entity["position"] ??= new JArray(0f, 0f, 0f);
        entity["rotation"] ??= new JArray(0f, 0f, 0f);
        entity["scale"] ??= new JArray(1f, 1f, 1f);
        entity["components"] ??= new JArray();
        entity["children"] ??= new JArray();
    }

    private static JArray EnsureArray(JObject obj, string name)
    {
        if (obj[name] is JArray array)
            return array;
        array = new JArray();
        obj[name] = array;
        return array;
    }

    private static void RegenerateGuids(JObject entity)
    {
        entity["guid"] = Guid.NewGuid().ToString();
        foreach (var child in EnsureArray(entity, "children").OfType<JObject>())
            RegenerateGuids(child);
    }

    private sealed record ComponentItem(JObject Json, string Label)
    {
        public override string ToString() => Label;
    }
}
