using System.Collections;
using System.Globalization;
using System.Reflection;
using Dreambit.ECS;
using Dreambit.Editor.Assets;
using Dreambit.Editor.UI;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;
using Vector4 = System.Numerics.Vector4;

namespace Dreambit.Editor.Inspection;

internal readonly record struct InspectorValueDrawContext(
    string Id,
    InspectorMemberMetadata Metadata,
    bool Mixed,
    bool ReadOnly,
    int Depth = 0);

internal readonly record struct InspectorValueDrawResult(bool Changed, object? Value)
{
    public static InspectorValueDrawResult Unchanged(object? value)
    {
        return new InspectorValueDrawResult(false, value);
    }
}

internal interface IInspectorValueDrawer
{
    int Priority { get; }
    bool CanDraw(Type type);

    InspectorValueDrawResult Draw(
        InspectorValueDrawerRegistry registry,
        string label,
        Type type,
        object? value,
        InspectorValueDrawContext context);
}

internal sealed class InspectorValueDrawerRegistry
{
    private readonly List<IInspectorValueDrawer> _drawers = [];

    public InspectorValueDrawerRegistry(
        AssetDatabase? assets = null,
        EditorDragDropService? dragDrop = null,
        Func<Scene?>? sceneProvider = null)
    {
        if (assets is not null && dragDrop is not null && sceneProvider is not null)
            Register(new ObjectReferenceValueDrawer(assets, dragDrop, sceneProvider));
        Register(new NullableValueDrawer());
        Register(new BooleanValueDrawer());
        Register(new EnumValueDrawer());
        Register(new NumericValueDrawer());
        Register(new StringValueDrawer());
        Register(new VectorValueDrawer());
        Register(new ColorValueDrawer());
        Register(new DictionaryValueDrawer());
        Register(new CollectionValueDrawer());
        Register(new NestedObjectValueDrawer());
        Register(new UnsupportedValueDrawer());
    }

    public void Register(IInspectorValueDrawer drawer)
    {
        _drawers.Add(drawer);
        _drawers.Sort(static (left, right) => right.Priority.CompareTo(left.Priority));
    }

    public InspectorValueDrawResult Draw(
        string label,
        Type type,
        object? value,
        InspectorValueDrawContext context)
    {
        var drawer = _drawers.First(candidate => candidate.CanDraw(type));
        if (context.ReadOnly)
            ImGui.BeginDisabled();
        try
        {
            // Containers own a tree of child controls. Rendering one inside a
            // value cell would make every child start in that already-narrow
            // column, so draw their header full-width and let children begin
            // fresh property rows below it.
            var isContainer = drawer is DictionaryValueDrawer ||
                              drawer is CollectionValueDrawer ||
                              drawer is NestedObjectValueDrawer ||
                              drawer is NullableValueDrawer ||
                              drawer is EnumValueDrawer &&
                              type.GetCustomAttributes(typeof(FlagsAttribute), true).Length > 0;
            if (isContainer)
            {
                var result = drawer.Draw(this, label, type, value, context);
                if (!string.IsNullOrWhiteSpace(context.Metadata.Tooltip) && ImGui.IsItemHovered())
                    ImGui.SetTooltip(context.Metadata.Tooltip);
                return result;
            }

            // Keep generated properties in a predictable label/value layout.
            // Drawers get an ID-only label so controls do not add a second label.
            var availableWidth = MathF.Max(1f, ImGui.GetContentRegionAvail().X);
            var labelWidth = Math.Clamp(availableWidth * 0.35f, 120f, 190f);
            var flags = ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.NoSavedSettings;
            if (!ImGui.BeginTable($"##PropertyRow.{context.Id}", 2, flags))
                return drawer.Draw(this, $"##{context.Id}", type, value, context);

            try
            {
                ImGui.TableSetupColumn("##Label", ImGuiTableColumnFlags.WidthFixed, labelWidth);
                ImGui.TableSetupColumn("##Value", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);
                ImGui.AlignTextToFramePadding();
                ImGui.TextUnformatted(label);
                if (!string.IsNullOrWhiteSpace(context.Metadata.Tooltip) && ImGui.IsItemHovered())
                    ImGui.SetTooltip(context.Metadata.Tooltip);

                ImGui.TableSetColumnIndex(1);
                ImGui.SetNextItemWidth(-1f);
                var result = drawer.Draw(this, $"##{context.Id}", type, value, context);
                if (!string.IsNullOrWhiteSpace(context.Metadata.Tooltip) && ImGui.IsItemHovered())
                    ImGui.SetTooltip(context.Metadata.Tooltip);
                return result;
            }
            finally
            {
                ImGui.EndTable();
            }
        }
        finally
        {
            if (context.ReadOnly)
                ImGui.EndDisabled();
        }
    }

    private sealed class ObjectReferenceValueDrawer(
        AssetDatabase assets,
        EditorDragDropService dragDrop,
        Func<Scene?> sceneProvider) : IInspectorValueDrawer
    {
        private string _search = string.Empty;

        public int Priority => 95;

        public bool CanDraw(Type type)
        {
            return typeof(DreambitAsset).IsAssignableFrom(type) ||
                   type == typeof(Entity) ||
                   typeof(Component).IsAssignableFrom(type);
        }

        public InspectorValueDrawResult Draw(
            InspectorValueDrawerRegistry registry,
            string label,
            Type type,
            object? value,
            InspectorValueDrawContext context)
        {
            var display = value switch
            {
                DreambitAsset asset => asset.AssetName ?? asset.GetType().Name,
                Entity entity => entity.Name,
                Component component => $"{component.Entity.Name} ({component.GetType().Name})",
                _ => "None"
            };

            var changedValue = value;
            var changed = false;

            var clearWidth = ImGui.GetFrameHeight();
            var fieldWidth = MathF.Max(
                1f,
                ImGui.GetContentRegionAvail().X - clearWidth - ImGui.GetStyle().ItemSpacing.X);
            if (ImGui.Button($"{display}##{context.Id}", new Vector2(fieldWidth, 0f)))
            {
                _search = string.Empty;
                ImGui.OpenPopup($"Object Picker##{context.Id}");
            }

            // This must remain immediately after the field because ImGui uses
            // the previously drawn item as the drag-drop target.
            if (AcceptDrop(type, ref changedValue))
                changed = true;

            ImGui.SameLine();
            if (ImGui.SmallButton($"×##{context.Id}.Clear"))
            {
                changedValue = null;
                changed = true;
            }

            if (DrawPicker(type, context.Id, ref changedValue))
                changed = true;

            return new InspectorValueDrawResult(
                changed,
                changedValue);
        }

        private unsafe bool AcceptDrop(Type type, ref object? value)
        {
            if (!ImGui.BeginDragDropTarget())
                return false;
            var changed = false;
            try
            {
                if (typeof(DreambitAsset).IsAssignableFrom(type))
                {
                    var payload = ImGui.AcceptDragDropPayload(EditorDragDropService.ProjectItemPayloadType);
                    if (payload.NativePtr != null && dragDrop.ProjectItem is { IsFolder: false } item &&
                        assets.TryGetAsset(item.RelativePath, out var asset) &&
                        AssetTypeClassifier.IsCompatibleWith(asset!, type))
                    {
                        var loaded = Resources.LoadDreambitAsset(item.AssetId, asset!.LogicalAssetName, type);
                        if (loaded is not null && type.IsInstanceOfType(loaded))
                        {
                            value = loaded;
                            changed = true;
                        }

                        dragDrop.ClearProjectItem();
                    }
                }
                else
                {
                    var payload = ImGui.AcceptDragDropPayload(EditorDragDropService.HierarchyEntityPayloadType);
                    if (payload.NativePtr != null && dragDrop.HierarchyEntityId is { } id &&
                        sceneProvider()?.FindEntity(id) is { } entity)
                    {
                        object? candidate = type == typeof(Entity) ? entity : entity.GetComponent(type);
                        if (candidate is not null && type.IsInstanceOfType(candidate))
                        {
                            value = candidate;
                            changed = true;
                        }

                        dragDrop.ClearHierarchyEntity();
                    }
                }
            }
            finally
            {
                ImGui.EndDragDropTarget();
            }

            return changed;
        }

        private bool DrawPicker(Type type, string id, ref object? value)
        {
            if (!ImGui.BeginPopup($"Object Picker##{id}"))
                return false;
            var changed = false;
            try
            {
                ImGui.SetNextItemWidth(360f);
                ImGui.InputTextWithHint("##PickerSearch", "Search", ref _search, 128);
                ImGui.Separator();
                ImGui.BeginChild("##PickerItems", new Vector2(360f, 260f));
                try
                {
                    if (typeof(DreambitAsset).IsAssignableFrom(type))
                        foreach (var asset in assets.GetSnapshot().Assets)
                        {
                            if (!AssetTypeClassifier.IsCompatibleWith(asset, type))
                                continue;
                            if (!string.IsNullOrWhiteSpace(_search) &&
                                !asset.RelativePath.Contains(_search, StringComparison.OrdinalIgnoreCase))
                                continue;
                            if (!ImGui.Selectable(asset.RelativePath))
                                continue;
                            var loaded = Resources.LoadDreambitAsset(asset.Id, asset.LogicalAssetName, type);
                            if (loaded is not null && type.IsInstanceOfType(loaded))
                            {
                                value = loaded;
                                changed = true;
                                ImGui.CloseCurrentPopup();
                            }
                        }
                    else if (sceneProvider() is { } scene)
                        foreach (var entity in scene.GetAllEntities().Where(entity => !entity.IsEditorOnly))
                        {
                            if (!string.IsNullOrWhiteSpace(_search) &&
                                !entity.Name.Contains(_search, StringComparison.OrdinalIgnoreCase))
                                continue;
                            object? candidate = type == typeof(Entity) ? entity : entity.GetComponent(type);
                            if (candidate is null || !ImGui.Selectable(entity.Name))
                                continue;
                            value = candidate;
                            changed = true;
                            ImGui.CloseCurrentPopup();
                        }
                }
                finally
                {
                    ImGui.EndChild();
                }
            }
            finally
            {
                ImGui.EndPopup();
            }

            return changed;
        }
    }

    private sealed class NullableValueDrawer : IInspectorValueDrawer
    {
        public int Priority => 100;

        public bool CanDraw(Type type)
        {
            return Nullable.GetUnderlyingType(type) is not null;
        }

        public InspectorValueDrawResult Draw(
            InspectorValueDrawerRegistry registry,
            string label,
            Type type,
            object? value,
            InspectorValueDrawContext context)
        {
            var underlying = Nullable.GetUnderlyingType(type)!;
            var hasValue = value is not null;
            if (ImGui.Checkbox($"##{context.Id}.HasValue", ref hasValue))
                return new InspectorValueDrawResult(
                    true,
                    hasValue ? Activator.CreateInstance(underlying) : null);
            ImGui.SameLine();
            return hasValue
                ? registry.Draw(label, underlying, value, context with { Id = context.Id + ".Value" })
                : DrawNull(label, value);
        }

        private static InspectorValueDrawResult DrawNull(string label, object? value)
        {
            ImGui.TextDisabled($"{label}: null");
            return InspectorValueDrawResult.Unchanged(value);
        }
    }

    private sealed class BooleanValueDrawer : IInspectorValueDrawer
    {
        public int Priority => 90;

        public bool CanDraw(Type type)
        {
            return type == typeof(bool);
        }

        public InspectorValueDrawResult Draw(
            InspectorValueDrawerRegistry registry,
            string label,
            Type type,
            object? value,
            InspectorValueDrawContext context)
        {
            var current = value is true;
            return ImGui.Checkbox($"{label}##{context.Id}", ref current)
                ? new InspectorValueDrawResult(true, current)
                : InspectorValueDrawResult.Unchanged(value);
        }
    }

    private sealed class EnumValueDrawer : IInspectorValueDrawer
    {
        public int Priority => 85;

        public bool CanDraw(Type type)
        {
            return type.IsEnum;
        }

        public InspectorValueDrawResult Draw(
            InspectorValueDrawerRegistry registry,
            string label,
            Type type,
            object? value,
            InspectorValueDrawContext context)
        {
            value ??= Enum.GetValues(type).GetValue(0);
            if (type.GetCustomAttributes(typeof(FlagsAttribute), true).Length > 0)
            {
                var bits = Convert.ToUInt64(value, CultureInfo.InvariantCulture);
                var changed = false;
                if (ImGui.TreeNodeEx($"{label}##{context.Id}", ImGuiTreeNodeFlags.SpanAvailWidth))
                {
                    foreach (var option in Enum.GetValues(type))
                    {
                        var optionBits = Convert.ToUInt64(option, CultureInfo.InvariantCulture);
                        if (optionBits == 0)
                            continue;
                        var selected = (bits & optionBits) == optionBits;
                        if (ImGui.Checkbox($"{option}##{context.Id}.{optionBits}", ref selected))
                        {
                            bits = selected ? bits | optionBits : bits & ~optionBits;
                            changed = true;
                        }
                    }

                    ImGui.TreePop();
                }

                return changed
                    ? new InspectorValueDrawResult(true, Enum.ToObject(type, bits))
                    : InspectorValueDrawResult.Unchanged(value);
            }

            var names = Enum.GetNames(type);
            var currentName = Enum.GetName(type, value!) ?? value!.ToString() ?? string.Empty;
            if (!ImGui.BeginCombo($"{label}##{context.Id}", currentName))
                return InspectorValueDrawResult.Unchanged(value);
            var selectedValue = value;
            var changedValue = false;
            foreach (var name in names)
            {
                var selected = name == currentName;
                if (ImGui.Selectable(name, selected))
                {
                    selectedValue = Enum.Parse(type, name);
                    changedValue = true;
                }

                if (selected)
                    ImGui.SetItemDefaultFocus();
            }

            ImGui.EndCombo();
            return new InspectorValueDrawResult(changedValue, selectedValue);
        }
    }

    private sealed class NumericValueDrawer : IInspectorValueDrawer
    {
        private static readonly HashSet<Type> Types =
        [
            typeof(byte), typeof(sbyte), typeof(short), typeof(ushort), typeof(int), typeof(uint),
            typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal)
        ];

        public int Priority => 80;

        public bool CanDraw(Type type)
        {
            return Types.Contains(type);
        }

        public InspectorValueDrawResult Draw(
            InspectorValueDrawerRegistry registry,
            string label,
            Type type,
            object? value,
            InspectorValueDrawContext context)
        {
            var range = context.Metadata.Range;
            if (type == typeof(float))
            {
                var current = Convert.ToSingle(value, CultureInfo.InvariantCulture);
                var changed = range is null
                    ? ImGui.DragFloat($"{label}##{context.Id}", ref current, 0.1f)
                    : ImGui.DragFloat(
                        $"{label}##{context.Id}",
                        ref current,
                        0.1f,
                        (float)range.Minimum,
                        (float)range.Maximum,
                        "%.3f",
                        ImGuiSliderFlags.AlwaysClamp);
                DrawExactEntryTooltip();
                return changed
                    ? new InspectorValueDrawResult(true, current)
                    : InspectorValueDrawResult.Unchanged(value);
            }

            if (type == typeof(double) || type == typeof(decimal))
            {
                var current = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                var changed = ImGui.InputDouble($"{label}##{context.Id}", ref current, 0.1, 1.0, "%.6g");
                if (range is not null)
                    current = Math.Clamp(current, range.Minimum, range.Maximum);
                object converted = type == typeof(decimal) ? Convert.ToDecimal(current) : current;
                return changed
                    ? new InspectorValueDrawResult(true, converted)
                    : InspectorValueDrawResult.Unchanged(value);
            }

            if (type == typeof(int))
            {
                var current = Convert.ToInt32(value, CultureInfo.InvariantCulture);
                var changed = range is null
                    ? ImGui.DragInt($"{label}##{context.Id}", ref current, 1f)
                    : ImGui.DragInt(
                        $"{label}##{context.Id}",
                        ref current,
                        1f,
                        (int)Math.Ceiling(range.Minimum),
                        (int)Math.Floor(range.Maximum),
                        "%d",
                        ImGuiSliderFlags.AlwaysClamp);
                DrawExactEntryTooltip();
                return changed
                    ? new InspectorValueDrawResult(true, current)
                    : InspectorValueDrawResult.Unchanged(value);
            }

            var text = Convert.ToString(value, CultureInfo.InvariantCulture) ?? "0";
            if (!ImGui.InputText($"{label}##{context.Id}", ref text, 64, ImGuiInputTextFlags.CharsDecimal))
                return InspectorValueDrawResult.Unchanged(value);
            try
            {
                var converted = Convert.ChangeType(text, type, CultureInfo.InvariantCulture);
                return new InspectorValueDrawResult(true, converted);
            }
            catch
            {
                return InspectorValueDrawResult.Unchanged(value);
            }
        }

        private static void DrawExactEntryTooltip()
        {
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Drag to adjust. Double-click or Ctrl+click to type an exact value.");
        }
    }

    private sealed class StringValueDrawer : IInspectorValueDrawer
    {
        public int Priority => 75;

        public bool CanDraw(Type type)
        {
            return type == typeof(string) || type == typeof(char);
        }

        public InspectorValueDrawResult Draw(
            InspectorValueDrawerRegistry registry,
            string label,
            Type type,
            object? value,
            InspectorValueDrawContext context)
        {
            var current = value?.ToString() ?? string.Empty;
            if (!ImGui.InputText($"{label}##{context.Id}", ref current, 4096))
                return InspectorValueDrawResult.Unchanged(value);
            return new InspectorValueDrawResult(true, type == typeof(char) ? current.FirstOrDefault() : current);
        }
    }

    private sealed class VectorValueDrawer : IInspectorValueDrawer
    {
        public int Priority => 70;

        public bool CanDraw(Type type)
        {
            return type == typeof(Microsoft.Xna.Framework.Vector2) ||
                   type == typeof(Microsoft.Xna.Framework.Vector3) ||
                   type == typeof(Microsoft.Xna.Framework.Vector4) ||
                   type == typeof(Quaternion);
        }

        public InspectorValueDrawResult Draw(
            InspectorValueDrawerRegistry registry,
            string label,
            Type type,
            object? value,
            InspectorValueDrawContext context)
        {
            if (type == typeof(Microsoft.Xna.Framework.Vector2))
            {
                var source = value is Microsoft.Xna.Framework.Vector2 vector
                    ? vector
                    : Microsoft.Xna.Framework.Vector2.Zero;
                var current = new Vector2(source.X, source.Y);
                return ImGui.DragFloat2($"{label}##{context.Id}", ref current, 0.1f)
                    ? new InspectorValueDrawResult(true, new Microsoft.Xna.Framework.Vector2(current.X, current.Y))
                    : InspectorValueDrawResult.Unchanged(value);
            }

            if (type == typeof(Microsoft.Xna.Framework.Vector3))
            {
                var source = value is Microsoft.Xna.Framework.Vector3 vector
                    ? vector
                    : Microsoft.Xna.Framework.Vector3.Zero;
                var current = new Vector3(source.X, source.Y, source.Z);
                return ImGui.DragFloat3($"{label}##{context.Id}", ref current, 0.1f)
                    ? new InspectorValueDrawResult(true,
                        new Microsoft.Xna.Framework.Vector3(current.X, current.Y, current.Z))
                    : InspectorValueDrawResult.Unchanged(value);
            }

            var source4 = value switch
            {
                Microsoft.Xna.Framework.Vector4 vector => new Vector4(vector.X, vector.Y, vector.Z, vector.W),
                Quaternion quaternion => new Vector4(quaternion.X, quaternion.Y, quaternion.Z, quaternion.W),
                _ => Vector4.Zero
            };
            if (!ImGui.DragFloat4($"{label}##{context.Id}", ref source4, 0.01f))
                return InspectorValueDrawResult.Unchanged(value);
            return new InspectorValueDrawResult(
                true,
                type == typeof(Quaternion)
                    ? Quaternion.Normalize(new Quaternion(source4.X, source4.Y, source4.Z, source4.W))
                    : new Microsoft.Xna.Framework.Vector4(source4.X, source4.Y, source4.Z, source4.W));
        }
    }

    private sealed class ColorValueDrawer : IInspectorValueDrawer
    {
        public int Priority => 72;

        public bool CanDraw(Type type)
        {
            return type == typeof(Color);
        }

        public InspectorValueDrawResult Draw(
            InspectorValueDrawerRegistry registry,
            string label,
            Type type,
            object? value,
            InspectorValueDrawContext context)
        {
            var source = value is Color color ? color : Color.White;
            var current = new Vector4(source.R / 255f, source.G / 255f, source.B / 255f, source.A / 255f);
            return ImGui.ColorEdit4($"{label}##{context.Id}", ref current)
                ? new InspectorValueDrawResult(true, new Color(current))
                : InspectorValueDrawResult.Unchanged(value);
        }
    }

    private sealed class DictionaryValueDrawer : IInspectorValueDrawer
    {
        public int Priority => 65;

        public bool CanDraw(Type type) => GetDictionaryTypes(type) is not null;

        public InspectorValueDrawResult Draw(
            InspectorValueDrawerRegistry registry,
            string label,
            Type type,
            object? value,
            InspectorValueDrawContext context)
        {
            var types = GetDictionaryTypes(type)!.Value;
            var entries = value is IEnumerable enumerable
                ? enumerable.Cast<object?>().Select(entry => new DictionaryEntryValue(
                    entry?.GetType().GetProperty("Key")?.GetValue(entry),
                    entry?.GetType().GetProperty("Value")?.GetValue(entry))).ToList()
                : [];
            var changed = false;

            if (!ImGui.TreeNodeEx($"{label} ({entries.Count})##{context.Id}", ImGuiTreeNodeFlags.SpanAvailWidth))
                return InspectorValueDrawResult.Unchanged(value);

            try
            {
                int? removeIndex = null;
                for (var index = 0; index < entries.Count; index++)
                {
                    ImGui.PushID($"{context.Id}.{index}");
                    try
                    {
                        var entry = entries[index];
                        var keyResult = registry.Draw("Key", types.Key, entry.Key, context with
                        {
                            Id = $"{context.Id}.{index}.Key", Mixed = false, Depth = context.Depth + 1
                        });
                        var valueResult = registry.Draw("Value", types.Value, entry.Value, context with
                        {
                            Id = $"{context.Id}.{index}.Value", Mixed = false, Depth = context.Depth + 1
                        });
                        if (keyResult.Changed || valueResult.Changed)
                        {
                            entries[index] = new DictionaryEntryValue(
                                keyResult.Changed ? keyResult.Value : entry.Key,
                                valueResult.Changed ? valueResult.Value : entry.Value);
                            changed = true;
                        }

                        if (ImGui.SmallButton("Remove") && !context.ReadOnly)
                            removeIndex = index;
                    }
                    finally
                    {
                        ImGui.PopID();
                    }
                }

                if (removeIndex.HasValue)
                {
                    entries.RemoveAt(removeIndex.Value);
                    changed = true;
                }

                if (ImGui.SmallButton($"+ Add##{context.Id}") && !context.ReadOnly)
                {
                    entries.Add(new DictionaryEntryValue(CreateKey(types.Key, entries), CreateDefault(types.Value)));
                    changed = true;
                }
            }
            finally
            {
                ImGui.TreePop();
            }

            return changed
                ? new InspectorValueDrawResult(true, BuildDictionary(type, types, entries))
                : InspectorValueDrawResult.Unchanged(value);
        }

        private static (Type Key, Type Value)? GetDictionaryTypes(Type type)
        {
            var dictionary = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IDictionary<,>)
                ? type
                : type.GetInterfaces().FirstOrDefault(candidate =>
                    candidate.IsGenericType && candidate.GetGenericTypeDefinition() == typeof(IDictionary<,>));
            return dictionary is null ? null : (dictionary.GetGenericArguments()[0], dictionary.GetGenericArguments()[1]);
        }

        private static object? CreateKey(Type type, IReadOnlyList<DictionaryEntryValue> entries)
        {
            if (type != typeof(string))
                return CreateDefault(type);
            var existing = entries.Select(entry => entry.Key as string).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var key = "New Key";
            for (var suffix = 2; existing.Contains(key); suffix++)
                key = $"New Key {suffix}";
            return key;
        }

        private static object? CreateDefault(Type type) => type == typeof(string)
            ? string.Empty
            : type.IsValueType || type.GetConstructor(Type.EmptyTypes) is not null
                ? Activator.CreateInstance(type)
                : null;

        private static object BuildDictionary(
            Type targetType,
            (Type Key, Type Value) types,
            IReadOnlyList<DictionaryEntryValue> entries)
        {
            var concreteType = targetType.IsInterface || targetType.IsAbstract
                ? typeof(Dictionary<,>).MakeGenericType(types.Key, types.Value)
                : targetType;
            var dictionary = Activator.CreateInstance(concreteType)
                             ?? throw new InvalidOperationException($"Could not create dictionary '{concreteType.FullName}'.");
            var add = concreteType.GetMethod("Add", [types.Key, types.Value])
                      ?? typeof(IDictionary<,>).MakeGenericType(types.Key, types.Value).GetMethod("Add")!;
            foreach (var entry in entries)
                add.Invoke(dictionary, [entry.Key, entry.Value]);
            return dictionary;
        }

        private readonly record struct DictionaryEntryValue(object? Key, object? Value);
    }

    private sealed class CollectionValueDrawer : IInspectorValueDrawer
    {
        public int Priority => 60;

        public bool CanDraw(Type type)
        {
            return TryGetElementType(type, out _);
        }

        public InspectorValueDrawResult Draw(
            InspectorValueDrawerRegistry registry,
            string label,
            Type type,
            object? value,
            InspectorValueDrawContext context)
        {
            if (context.Depth >= 8)
            {
                ImGui.TextDisabled($"{label}: maximum nesting depth reached");
                return InspectorValueDrawResult.Unchanged(value);
            }

            TryGetElementType(type, out var elementType);
            var items = value is IEnumerable enumerable
                ? enumerable.Cast<object?>().ToList()
                : [];
            var changed = false;
            if (ImGui.TreeNodeEx(
                    $"{label} ({items.Count})##{context.Id}",
                    ImGuiTreeNodeFlags.SpanAvailWidth))
            try
            {
                int? removeIndex = null;
                for (var index = 0; index < items.Count; index++)
                {
                    ImGui.PushID($"{context.Id}.{index}");
                    var result = registry.Draw(
                        $"Element {index}",
                        elementType,
                        items[index],
                        context with
                        {
                            Id = $"{context.Id}.{index}",
                            Mixed = false,
                            Depth = context.Depth + 1
                        });
                    if (result.Changed)
                    {
                        items[index] = result.Value;
                        changed = true;
                    }

                    // Keep destructive actions beneath the element instead of
                    // attempting to append them after a full-width row.
                    if (ImGui.SmallButton("Remove") && !context.ReadOnly)
                        removeIndex = index;
                    ImGui.PopID();
                }

                if (removeIndex.HasValue)
                {
                    items.RemoveAt(removeIndex.Value);
                    changed = true;
                }

                if (ImGui.SmallButton($"+ Add##{context.Id}") && !context.ReadOnly)
                {
                    items.Add(CreateDefault(elementType));
                    changed = true;
                }

            }
            finally { ImGui.TreePop(); }

            return changed
                ? new InspectorValueDrawResult(true, BuildCollection(type, elementType, items))
                : InspectorValueDrawResult.Unchanged(value);
        }

        private static bool TryGetElementType(Type type, out Type elementType)
        {
            if (type.IsArray)
            {
                elementType = type.GetElementType()!;
                return true;
            }

            if (type == typeof(string))
            {
                elementType = null!;
                return false;
            }

            var collectionType = type.IsGenericType &&
                                 type.GetGenericArguments().Length == 1 &&
                                 typeof(IEnumerable<>).MakeGenericType(type.GetGenericArguments()[0])
                                     .IsAssignableFrom(type)
                ? type
                : type.GetInterfaces().FirstOrDefault(candidate =>
                    candidate.IsGenericType &&
                    candidate.GetGenericTypeDefinition() == typeof(IEnumerable<>));
            if (collectionType is null)
            {
                elementType = null!;
                return false;
            }

            elementType = collectionType.GetGenericArguments()[0];
            return true;
        }

        private static object? CreateDefault(Type type)
        {
            if (type == typeof(string))
                return string.Empty;
            if (type.IsValueType || type.GetConstructor(Type.EmptyTypes) is not null)
                return Activator.CreateInstance(type);
            return null;
        }

        private static object BuildCollection(Type targetType, Type elementType, IReadOnlyList<object?> items)
        {
            if (targetType.IsArray)
            {
                var array = Array.CreateInstance(elementType, items.Count);
                for (var index = 0; index < items.Count; index++)
                    array.SetValue(items[index], index);
                return array;
            }

            var concreteType = targetType.IsInterface || targetType.IsAbstract
                ? typeof(List<>).MakeGenericType(elementType)
                : targetType;
            var collection = Activator.CreateInstance(concreteType)
                             ?? throw new InvalidOperationException(
                                 $"Could not create collection '{concreteType.FullName}'.");
            var add = concreteType.GetMethod("Add", [elementType]) ??
                      typeof(ICollection<>).MakeGenericType(elementType).GetMethod("Add")!;
            foreach (var item in items)
                add.Invoke(collection, [item]);
            return collection;
        }
    }

    private sealed class NestedObjectValueDrawer : IInspectorValueDrawer
    {
        public int Priority => 10;

        public bool CanDraw(Type type)
        {
            return type != typeof(object) &&
                   !type.IsPrimitive &&
                   !type.IsPointer &&
                   !type.IsByRef &&
                   type.Namespace != "System";
        }

        public InspectorValueDrawResult Draw(
            InspectorValueDrawerRegistry registry,
            string label,
            Type type,
            object? value,
            InspectorValueDrawContext context)
        {
            if (value is null)
            {
                ImGui.TextDisabled($"{label}: null");
                ImGui.SameLine();
                if (!context.ReadOnly && ImGui.SmallButton($"Create##{context.Id}"))
                    try
                    {
                        return new InspectorValueDrawResult(true, Activator.CreateInstance(type));
                    }
                    catch
                    {
                        ImGui.SetTooltip($"{type.Name} needs a parameterless constructor.");
                    }

                return InspectorValueDrawResult.Unchanged(value);
            }

            if (context.Depth >= 8)
            {
                ImGui.TextDisabled($"{label}: maximum nesting depth reached");
                return InspectorValueDrawResult.Unchanged(value);
            }

            if (!ImGui.TreeNodeEx($"{label}##{context.Id}", ImGuiTreeNodeFlags.SpanAvailWidth))
                return InspectorValueDrawResult.Unchanged(value);

            var editable = value;
            var cloned = false;
            var changed = false;
            try
            {
                foreach (var member in DiscoverMembers(type))
                {
                    var memberValue = member.GetValue(editable);
                    var result = registry.Draw(
                        member.DisplayName,
                        member.ValueType,
                        memberValue,
                        context with
                        {
                            Id = $"{context.Id}.{member.SerializedName}",
                            Metadata = member,
                            Mixed = false,
                            ReadOnly = context.ReadOnly || member.IsReadOnly,
                            Depth = context.Depth + 1
                        });
                    if (!result.Changed || member.IsReadOnly)
                        continue;
                    if (!cloned)
                    {
                        editable = Clone(value, type);
                        cloned = true;
                    }

                    member.SetValue(editable!, result.Value);
                    changed = true;
                }
            }
            finally
            {
                ImGui.TreePop();
            }
            return changed
                ? new InspectorValueDrawResult(true, editable)
                : InspectorValueDrawResult.Unchanged(value);
        }

        private static object Clone(object value, Type type)
        {
            return DreambitJson.FromToken(DreambitJson.ToToken(value), type)
                   ?? throw new InvalidOperationException($"Could not clone nested value '{type.FullName}'.");
        }

        private static IEnumerable<InspectorMemberMetadata> DiscoverMembers(Type type)
        {
            var serializedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var serializedMembers = DreambitSerializationRules.GetSerializableMembers(type);
            foreach (var property in serializedMembers.OfType<PropertyInfo>())
            {
                if (property.GetCustomAttribute<HideInInspectorAttribute>() is not null)
                    continue;
                var json = property.GetCustomAttribute<JsonPropertyAttribute>();
                if (json is null && property.SetMethod?.IsPublic != true)
                    continue;
                var serializedName = DreambitSerializationRules.GetSerializedName(property);
                if (!serializedNames.Add(serializedName))
                    continue;
                yield return CreateMetadata(
                    serializedName,
                    property.Name,
                    property.PropertyType,
                    property,
                    property.SetMethod is not null);
            }

            foreach (var field in serializedMembers.OfType<FieldInfo>())
            {
                if (field.GetCustomAttribute<HideInInspectorAttribute>() is not null)
                    continue;
                var serializedName = DreambitSerializationRules.GetSerializedName(field);
                if (!serializedNames.Add(serializedName))
                    continue;
                yield return CreateMetadata(
                    serializedName,
                    field.Name,
                    field.FieldType,
                    field,
                    !field.IsInitOnly);
            }
        }

        private static InspectorMemberMetadata CreateMetadata(
            string serializedName,
            string displayName,
            Type valueType,
            MemberInfo member,
            bool canWrite)
        {
            return new InspectorMemberMetadata(
                serializedName,
                displayName,
                valueType,
                member,
                canWrite,
                !canWrite || member.GetCustomAttribute<ReadOnlyInInspectorAttribute>() is not null,
                member.GetCustomAttribute<RangeAttribute>(),
                member.GetCustomAttribute<HeaderAttribute>()?.Text,
                member.GetCustomAttribute<TooltipAttribute>()?.Text);
        }
    }

    private sealed class UnsupportedValueDrawer : IInspectorValueDrawer
    {
        public int Priority => int.MinValue;

        public bool CanDraw(Type type)
        {
            return true;
        }

        public InspectorValueDrawResult Draw(
            InspectorValueDrawerRegistry registry,
            string label,
            Type type,
            object? value,
            InspectorValueDrawContext context)
        {
            ImGui.TextDisabled($"{label}: {value ?? "null"} ({type.Name})");
            return InspectorValueDrawResult.Unchanged(value);
        }
    }
}
