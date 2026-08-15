using Dreambit.ECS;
using Dreambit.Editor.Scenes;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Vector3 = System.Numerics.Vector3;
using Vector4 = System.Numerics.Vector4;

namespace Dreambit.Editor.Inspection;

internal sealed class SceneEntityInspector(
    InspectorMetadataCache metadata,
    EditorTypeRegistry types,
    InspectorValueDrawerRegistry drawers,
    ComponentTypePicker componentPicker,
    CustomInspectorHost customInspectors)
{
    private string? _error;

    public void Draw(SceneDocument document, IReadOnlyList<Entity> entities)
    {
        DrawEntityHeader(document, entities);
        DrawEntityTags(document, entities);
        DrawImportedMapNotice(entities);
        ImGui.Separator();
        DrawTransform(document, entities);
        DrawComponents(document, entities);
        if (entities.All(entity => !entity.IsImportedMapGenerated))
            DrawAddComponent(document, entities);
        DrawError();
    }

    public void DrawBoxedBlueprintInstance(
        SceneDocument document,
        Entity entity,
        Entity instanceRoot,
        BlueprintInstanceReference instance)
    {
        ImGui.TextUnformatted(entity.Name);
        ImGui.Separator();
        ImGui.TextColored(new Vector4(0.35f, 0.72f, 1f, 1f), "Boxed Blueprint Instance");
        ImGui.TextWrapped(instance.AssetName);
        ImGui.TextDisabled("Source changes update this instance automatically.");
        ImGui.TextDisabled("Right-click it in the Hierarchy and choose Unbox to edit its contents.");
        ImGui.Separator();

        if (ReferenceEquals(entity, instanceRoot))
            DrawTransform(document, [entity]);
        else
            ImGui.TextDisabled("Linked child values are read-only.");

        DrawComponents(document, [entity], readOnly: true);
        DrawError();
    }

    public static void DrawSelectionContainingBoxedBlueprint(int entityCount)
    {
        ImGui.TextUnformatted($"{entityCount} Entities");
        ImGui.Separator();
        ImGui.TextDisabled("A boxed Blueprint instance is included in this selection.");
        ImGui.TextDisabled("Unbox it before editing this selection together.");
    }

    private void DrawEntityHeader(SceneDocument document, IReadOnlyList<Entity> entities)
    {
        if (entities.Count == 1)
        {
            var entity = entities[0];
            var name = entity.Name;
            ImGui.SetNextItemWidth(-1f);
            if (ImGui.InputText("##EntityName", ref name, 256, ImGuiInputTextFlags.EnterReturnsTrue))
                document.Rename(entity, name);
        }
        else
        {
            ImGui.TextUnformatted($"{entities.Count} Entities");
        }

        var enabled = entities[0].LocallyEnabled;
        var mixedEnabled = entities.Skip(1).Any(entity => entity.LocallyEnabled != enabled);
        if (ImGui.Checkbox("Enabled", ref enabled))
            TryMutation(() => document.SetEntityEnabled(
                entities,
                enabled,
                BuildSceneMergeKey(document, "Entity.Enabled")));
        InspectorUi.MixedValueIndicator("Enabled", mixedEnabled);
    }

    private void DrawEntityTags(SceneDocument document, IReadOnlyList<Entity> entities)
    {
        var firstTags = entities[0].Tags.OrderBy(tag => tag, StringComparer.OrdinalIgnoreCase).ToArray();
        var mixed = entities.Skip(1).Any(entity => !entity.Tags.SetEquals(entities[0].Tags));
        var tags = mixed ? string.Empty : string.Join(", ", firstTags);

        InspectorUi.PropertyRow("Entity.Tags", "Tags", () =>
        {
            if (ImGui.InputTextWithHint(
                    "##Value",
                    "Comma-separated tags",
                    ref tags,
                    1024,
                    ImGuiInputTextFlags.EnterReturnsTrue))
            {
                var updatedTags = tags
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                TryMutation(() => document.SetEntityTags(
                    entities,
                    updatedTags,
                    BuildSceneMergeKey(document, "Entity.Tags")));
            }

            if (mixed)
            {
                ImGui.SameLine();
                ImGui.TextDisabled("Multiple values");
            }
        });
    }

    private static void DrawImportedMapNotice(IReadOnlyList<Entity> entities)
    {
        if (!entities.Any(entity => entity.IsImportedMapGenerated))
            return;

        var sourceLabel = entities.All(entity => entity.IsTiledGenerated)
            ? "Tiled-generated visualization"
            : entities.All(entity => entity.IsLDtkGenerated)
                ? "LDtk-generated visualization"
                : "Imported map visualization";
        ImGui.TextColored(new Vector4(0.42f, 0.78f, 1f, 1f), sourceLabel);
        ImGui.TextDisabled("Value changes are stored as Dreambit overrides and survive reimport.");
        ImGui.TextDisabled("Hierarchy structure and components remain owned by the source map.");
    }

    private void DrawTransform(SceneDocument document, IReadOnlyList<Entity> entities)
    {
        if (!ImGui.CollapsingHeader("Transform", ImGuiTreeNodeFlags.DefaultOpen))
            return;
        ImGui.PushID("TransformInspector");
        try
        {
            ImGui.TextDisabled("Drag to adjust. Double-click a number to type an exact value.");
            var first = entities[0].Transform;

            var position = new Vector3(first.Position.X, first.Position.Y, first.Position.Z);
            var mixedPosition = entities.Skip(1).Any(entity => entity.Transform.Position != first.Position);
            InspectorUi.PropertyRow("Transform.Position", "Position", () =>
            {
                if (ImGui.DragFloat3("##Value", ref position, 0.1f))
                {
                    var value = new Microsoft.Xna.Framework.Vector3(position.X, position.Y, position.Z);
                    TryMutation(() => document.SetEntityPosition(
                        entities,
                        value,
                        BuildSceneMergeKey(document, "Transform.Position")));
                }
                InspectorUi.MixedValueIndicator("Position", mixedPosition);
            });

            var rotation = MathHelper.ToDegrees(first.Rotation2D);
            var mixedRotation = entities.Skip(1).Any(entity =>
                MathF.Abs(entity.Transform.Rotation2D - first.Rotation2D) > 0.0001f);
            InspectorUi.PropertyRow("Transform.Rotation", "Rotation", () =>
            {
                if (ImGui.DragFloat("##Value", ref rotation, 0.25f))
                    TryMutation(() => document.SetEntityRotation(
                        entities,
                        MathHelper.ToRadians(rotation),
                        BuildSceneMergeKey(document, "Transform.Rotation")));
                InspectorUi.MixedValueIndicator("Rotation", mixedRotation);
            });

            var scale = new Vector3(first.Scale.X, first.Scale.Y, first.Scale.Z);
            var mixedScale = entities.Skip(1).Any(entity => entity.Transform.Scale != first.Scale);
            InspectorUi.PropertyRow("Transform.Scale", "Scale", () =>
            {
                if (ImGui.DragFloat3("##Value", ref scale, 0.01f))
                {
                    var value = new Microsoft.Xna.Framework.Vector3(scale.X, scale.Y, scale.Z);
                    TryMutation(() => document.SetEntityScale(
                        entities,
                        value,
                        BuildSceneMergeKey(document, "Transform.Scale")));
                }
                InspectorUi.MixedValueIndicator("Scale", mixedScale);
            });
        }
        finally
        {
            ImGui.PopID();
        }
    }

    private void DrawComponents(
        SceneDocument document,
        IReadOnlyList<Entity> entities,
        bool readOnly = false)
    {
        var commonTypes = entities[0]
            .GetAllComponents()
            .Select(component => component.GetType())
            .Distinct()
            .Where(type => entities.Skip(1).All(entity => entity.GetComponent(type) is not null))
            .OrderBy(type => type.Name)
            .ToArray();

        foreach (var componentType in commonTypes)
        {
            var components = entities
                .Select(entity => entity.GetComponent(componentType)!)
                .ToArray();

            ImGui.PushID(componentType.FullName);
            try
            {
                var generated = entities.Any(entity => entity.IsImportedMapGenerated);
                var (open, removeRequested) = InspectorUi.RemovableHeader(
                    componentType.Name,
                    !readOnly && !generated,
                    GetComponentStatus(entities, readOnly));
                if (removeRequested)
                {
                    TryMutation(() => document.Apply($"Remove {componentType.Name}", _ =>
                    {
                        foreach (var entity in entities)
                            if (entity.GetComponent(componentType) is { } component)
                                entity.DetachComponent(component);
                    }));
                    continue;
                }

                if (!open)
                    continue;

                if (readOnly)
                {
                    DrawComponentMembers(document, components, readOnly: true);
                    continue;
                }

                if (customInspectors.TryDraw(
                        componentType,
                        components.Cast<object>().ToArray(),
                        () => DrawComponentMembers(document, components),
                        (name, mutation) => document.Apply(name, _ =>
                        {
                            mutation();
                            RecordGeneratedComponentValues(document, components);
                        }),
                        $"Custom Component Editor for '{componentType.FullName}' failed."))
                {
                    continue;
                }

                DrawComponentMembers(document, components);
            }
            finally
            {
                ImGui.PopID();
            }
        }

        if (entities.Count > 1 && HasPartialComponents(entities, commonTypes))
            ImGui.TextDisabled("Components not shared by every selected entity are hidden.");
    }

    internal static bool HasPartialComponents(
        IReadOnlyList<Entity> entities,
        IReadOnlyCollection<Type> commonTypes)
    {
        var union = new HashSet<Type>();
        foreach (var entity in entities)
            foreach (var component in entity.GetAllComponents())
                union.Add(component.GetType());
        return union.Count != commonTypes.Count || union.Any(type => !commonTypes.Contains(type));
    }

    internal static string? GetComponentStatus(IReadOnlyList<Entity> entities, bool readOnly)
    {
        return GetComponentStatus(
            readOnly,
            entities.Any(entity => entity.IsImportedMapGenerated),
            entities.All(entity => entity.IsLDtkGenerated),
            entities.All(entity => entity.IsTiledGenerated));
    }

    internal static string? GetComponentStatus(
        bool readOnly,
        bool hasGeneratedEntity,
        bool allLDtkGenerated,
        bool allTiledGenerated) =>
        readOnly
            ? "Boxed"
            : !hasGeneratedEntity
                ? null
                : allLDtkGenerated
                    ? "LDtk"
                    : allTiledGenerated
                        ? "Tiled"
                        : "Imported";

    private void DrawComponentMembers(
        SceneDocument document,
        IReadOnlyList<Component> components,
        bool readOnly = false)
    {
        var componentType = components[0].GetType();
        var members = metadata.Get(componentType, InspectorTargetKind.Component);
        if (members.Count == 0)
        {
            ImGui.TextDisabled("No [DreambitSerialize] members.");
            return;
        }

        foreach (var member in members)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(member.Header))
                {
                    ImGui.Spacing();
                    ImGui.TextDisabled(member.Header);
                }

                var first = member.GetValue(components[0]);
                var mixed = components.Skip(1).Any(component =>
                    !ValuesEqual(first, member.GetValue(component)));
                var id = $"{componentType.FullName}.{member.SerializedName}";
                var effectiveReadOnly = readOnly || member.IsReadOnly;
                var result = drawers.Draw(
                    member.DisplayName,
                    member.ValueType,
                    first,
                    new InspectorValueDrawContext(id, member, mixed, effectiveReadOnly));

                if (!result.Changed || effectiveReadOnly)
                    continue;

                TryMutation(() => document.SetComponentMember(
                    $"Change {member.DisplayName}",
                    components,
                    member.SerializedName,
                    member.ValueType,
                    result.Value,
                    (component, value) => member.SetValue(component, value),
                    BuildSceneMergeKey(document, id)));
            }
            catch (Exception exception)
            {
                _error = $"{componentType.Name}.{member.DisplayName}: {exception.Message}";
                ImGui.TextColored(new Vector4(0.96f, 0.34f, 0.36f, 1f), _error);
            }
        }
    }

    private void RecordGeneratedComponentValues(
        SceneDocument document,
        IReadOnlyList<Component> components)
    {
        foreach (var component in components)
            foreach (var member in metadata.Get(component.GetType(), InspectorTargetKind.Component))
                if (!member.IsReadOnly)
                    document.RecordGeneratedComponentMember(
                        component,
                        member.SerializedName,
                        member.GetValue(component));
    }

    private void DrawAddComponent(SceneDocument document, IReadOnlyList<Entity> entities)
    {
        ImGui.Spacing();
        var selectedType = componentPicker.Draw(
            "Add Component##Inspector",
            types.ComponentTypes,
            type => entities.All(entity => entity.GetComponent(type) is not null));
        if (selectedType is null)
            return;

        TryMutation(() => document.Apply($"Add {selectedType.Name}", _ =>
        {
            foreach (var entity in entities)
                if (entity.GetComponent(selectedType) is null)
                    entity.AttachComponent(selectedType);
        }));
    }

    private void TryMutation(Action mutation)
    {
        try
        {
            mutation();
            _error = null;
        }
        catch (Exception exception)
        {
            _error = exception.Message;
        }
    }

    private void DrawError()
    {
        if (string.IsNullOrWhiteSpace(_error))
            return;
        ImGui.Spacing();
        ImGui.TextColored(new Vector4(0.96f, 0.34f, 0.36f, 1f), _error);
    }

    private static string BuildSceneMergeKey(SceneDocument document, string propertyKey)
    {
        var selection = string.Join(",", document.Selection.EntityIds
            .OrderBy(static id => id)
            .Select(static id => id.ToString("N")));
        return $"{propertyKey}|{selection}";
    }

    private static bool ValuesEqual(object? left, object? right) =>
        ReferenceEquals(left, right) || (left?.Equals(right) ?? right is null);
}
