using Dreambit.ECS;
using Dreambit.Editor.Inspection;
using Dreambit.Editor.Assets;
using Dreambit.Editor.Graphics;
using Dreambit.Editor.Logging;
using Dreambit.EditorApi;
using Dreambit.Editor.Scenes;
using Dreambit.LDtk;
using Dreambit.Tiled;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Vector3 = System.Numerics.Vector3;
using Vector4 = System.Numerics.Vector4;

namespace Dreambit.Editor.UI.Panels;

internal sealed class InspectorPanel : EditorPanel
{
    private readonly EditorDocumentContext _documentContext;
    private readonly InspectorMetadataCache _metadata;
    private readonly EditorTypeRegistry _types;
    private readonly AssetEditingService _assetEditing;
    private readonly AssetDatabase _assets;
    private readonly AssetPreviewService _previews;
    private readonly CustomEditorRegistry _customEditors;
    private readonly EditorLogService _logs;
    private readonly InspectorValueDrawerRegistry _drawers;
    private string _componentSearch = string.Empty;
    private string _blueprintReferenceSearch = string.Empty;
    private string? _error;
    private string? _lastUnhandledFailure;

    public InspectorPanel(
        EditorDocumentContext documentContext,
        InspectorMetadataCache metadata,
        EditorTypeRegistry types,
        AssetEditingService assetEditing,
        AssetDatabase assets,
        EditorDragDropService dragDrop,
        AssetPreviewService previews,
        CustomEditorRegistry customEditors,
        EditorLogService logs)
        : base(EditorPanelIds.Inspector, "Inspector")
    {
        _documentContext = documentContext;
        _metadata = metadata;
        _types = types;
        _assetEditing = assetEditing;
        _assets = assets;
        _drawers = new InspectorValueDrawerRegistry(assets, dragDrop, () => _documentContext.Current?.Scene);
        _previews = previews;
        _customEditors = customEditors;
        _logs = logs;
    }

    protected override void DrawContents()
    {
        try
        {
            DrawContentsCore();
            _lastUnhandledFailure = null;
        }
        catch (Exception exception)
        {
            var failure = exception.ToString();
            _error = $"Inspector could not draw this selection: {exception.Message}";
            if (!string.Equals(_lastUnhandledFailure, failure, StringComparison.Ordinal))
            {
                _logs.Error(
                    "Inspector",
                    "An entity or asset could not be inspected. The Editor is still running.",
                    exception);
                _lastUnhandledFailure = failure;
            }
        }
        finally
        {
            // A merge key is valid only for one continuous ImGui interaction.
            // Once no item is active, a later edit of the same property must create
            // a separate undo step.
            if (!ImGui.IsAnyItemActive())
            {
                _documentContext.Current?.Undo.EndMergeGroup();
                _assetEditing.Current?.Undo.EndMergeGroup();
            }
        }
    }

    private void DrawContentsCore()
    {
        var document = _documentContext.Current;
        var entities = document?.Selection.Resolve(document.Scene) ?? [];
        var inspectBlueprintEntity = _documentContext.IsBlueprint && entities.Count > 0;
        var inspectAsset = _documentContext.IsAsset ||
                           (_documentContext.IsBlueprint && entities.Count == 0);

        if (inspectAsset && !inspectBlueprintEntity && _assetEditing.Current is { } assetDocument)
        {
            DrawAssetDocument(assetDocument);
            return;
        }
        if (_documentContext.IsAsset && !inspectBlueprintEntity &&
            _assetEditing.Selected is { } selectedAsset)
        {
            ImGui.TextUnformatted(selectedAsset.Name);
            ImGui.TextDisabled(selectedAsset.RelativePath);
            ImGui.Spacing();
            try
            {
                if (_previews.TryGetTexture(selectedAsset, out var texture, out var width, out var height))
                {
                    var available = ImGui.GetContentRegionAvail();
                    var previewWidth = MathF.Min(available.X, width);
                    var previewHeight = previewWidth * height / MathF.Max(1, width);
                    if (previewHeight > available.Y)
                    {
                        previewHeight = available.Y;
                        previewWidth = previewHeight * width / MathF.Max(1, height);
                    }
                    ImGui.Image(texture, new System.Numerics.Vector2(previewWidth, previewHeight));
                    ImGui.TextDisabled($"{width} × {height}");
                    return;
                }
            }
            catch (Exception exception)
            {
                ImGui.TextColored(new Vector4(0.96f, 0.34f, 0.36f, 1f), exception.Message);
            }
            ImGui.TextWrapped(
                selectedAsset.Kind == AssetKind.Scene
                    ? "Double-click this scene to open it."
                    : "No loaded Dreambit asset type is available for this file. Its data remains untouched.");
            return;
        }

        if (document is null)
        {
            ImGui.TextDisabled("Nothing selected");
            ImGui.Spacing();
            ImGui.TextWrapped("Select an entity in the Hierarchy or Scene view to inspect it.");
            return;
        }

        if (document.LDtkReference is not null)
            DrawLDtkImportOptions(document);
        if (document.TiledReference is not null)
            DrawTiledImportOptions(document);

        if (entities.Count == 0)
        {
            ImGui.TextDisabled("No entity selected");
            ImGui.Spacing();
            ImGui.TextWrapped("Select an entity in the Hierarchy or Scene view to inspect it.");
            return;
        }

        if (entities.Count == 1 &&
            document.TryGetBlueprintInstanceRoot(entities[0], out var instanceRoot, out var instance))
        {
            ImGui.TextUnformatted(entities[0].Name);
            ImGui.Separator();

            ImGui.TextColored(new Vector4(0.35f, 0.72f, 1f, 1f), "Boxed Blueprint Instance");

            ImGui.TextWrapped(instance.AssetName);
            ImGui.TextDisabled("Source changes update this instance automatically.");
            ImGui.TextDisabled("Right-click it in the Hierarchy and choose Unbox to edit its contents.");
            ImGui.Separator();

            if (ReferenceEquals(entities[0], instanceRoot))
                DrawTransform(document, entities);
            else
                ImGui.TextDisabled("Linked child values are read-only.");

            DrawComponents(document, entities, readOnly: true);

            return;
        }
        if (entities.Count > 1 &&
            entities.Any(entity => document.TryGetBlueprintInstanceRoot(entity, out _, out _)))
        {
            ImGui.TextUnformatted($"{entities.Count} Entities");
            ImGui.Separator();
            ImGui.TextDisabled("A boxed Blueprint instance is included in this selection.");
            ImGui.TextDisabled("Unbox it before editing this selection together.");
            return;
        }

        DrawEntityHeader(document, entities);
        DrawEntityTags(document, entities);
        if (entities.Any(entity => entity.IsImportedMapGenerated))
        {
            var sourceLabel = entities.All(entity => entity.IsTiledGenerated)
                ? "Tiled-generated visualization"
                : entities.All(entity => entity.IsLDtkGenerated)
                    ? "LDtk-generated visualization"
                    : "Imported map visualization";
            ImGui.TextColored(new Vector4(0.42f, 0.78f, 1f, 1f), sourceLabel);
            ImGui.TextDisabled("Value changes are stored as Dreambit overrides and survive reimport.");
            ImGui.TextDisabled("Hierarchy structure and components remain owned by the source map.");
        }
        ImGui.Separator();

        DrawTransform(document, entities);
        DrawComponents(document, entities);
        if (entities.All(entity => !entity.IsImportedMapGenerated))
            DrawAddComponent(document, entities);

        if (!string.IsNullOrWhiteSpace(_error))
        {
            ImGui.Spacing();
            ImGui.TextColored(new Vector4(0.96f, 0.34f, 0.36f, 1f), _error);
        }
    }

    private void DrawLDtkImportOptions(SceneDocument document)
    {
        if (!ImGui.CollapsingHeader("LDtk Import Options", ImGuiTreeNodeFlags.DefaultOpen))
            return;

        var current = document.LDtkReference!.ImportOptions ?? new LDtkImportOptions();
        var edited = current.Clone();
        var pixelsPerUnit = edited.PixelsPerUnit;
        var baseDrawLayer = edited.BaseDrawLayer;
        var drawLayerStep = edited.DrawLayerStep;
        var worldDepthStride = edited.WorldDepthDrawLayerStride;
        var renderBackgroundColor = edited.RenderLevelBackgroundColor;
        var renderBackgroundImage = edited.RenderLevelBackgroundImage;
        var includeInvisibleLayers = edited.IncludeInvisibleLayers;
        var changed = false;
        string? mergeKey = null;
        DrawPropertyRow("LDtk.PixelsPerUnit", "Pixels Per Unit", () =>
        {
            if (ImGui.DragFloat("##Value", ref pixelsPerUnit, 0.1f, 0.001f, 100000f))
                (changed, mergeKey) = (true, "LDtk.PixelsPerUnit");
        });
        DrawPropertyRow("LDtk.BaseDrawLayer", "Base Draw Layer", () =>
        {
            if (ImGui.DragInt("##Value", ref baseDrawLayer, 1f))
                (changed, mergeKey) = (true, "LDtk.BaseDrawLayer");
        });
        DrawPropertyRow("LDtk.DrawLayerStep", "Draw Layer Step", () =>
        {
            if (ImGui.DragInt("##Value", ref drawLayerStep, 1f, 1, 100000))
                (changed, mergeKey) = (true, "LDtk.DrawLayerStep");
        });
        DrawPropertyRow("LDtk.WorldDepthStride", "World Depth Stride", () =>
        {
            if (ImGui.DragInt("##Value", ref worldDepthStride, 1f, 1, int.MaxValue))
                (changed, mergeKey) = (true, "LDtk.WorldDepthStride");
        });
        DrawPropertyRow("LDtk.RenderBackgroundColor", "Render Background Color", () =>
        {
            if (ImGui.Checkbox("##Value", ref renderBackgroundColor))
                (changed, mergeKey) = (true, "LDtk.RenderBackgroundColor");
        });
        DrawPropertyRow("LDtk.RenderBackgroundImage", "Render Background Image", () =>
        {
            if (ImGui.Checkbox("##Value", ref renderBackgroundImage))
                (changed, mergeKey) = (true, "LDtk.RenderBackgroundImage");
        });
        DrawPropertyRow("LDtk.IncludeInvisibleLayers", "Include Invisible Layers", () =>
        {
            if (ImGui.Checkbox("##Value", ref includeInvisibleLayers))
                (changed, mergeKey) = (true, "LDtk.IncludeInvisibleLayers");
        });

        edited.PixelsPerUnit = pixelsPerUnit;
        edited.BaseDrawLayer = baseDrawLayer;
        edited.DrawLayerStep = drawLayerStep;
        edited.WorldDepthDrawLayerStride = worldDepthStride;
        edited.RenderLevelBackgroundColor = renderBackgroundColor;
        edited.RenderLevelBackgroundImage = renderBackgroundImage;
        edited.IncludeInvisibleLayers = includeInvisibleLayers;

        if (changed)
        {
            try
            {
                document.UpdateLDtkImportOptions("Change LDtk Import Options", options =>
                {
                    options.PixelsPerUnit = edited.PixelsPerUnit;
                    options.BaseDrawLayer = edited.BaseDrawLayer;
                    options.DrawLayerStep = edited.DrawLayerStep;
                    options.WorldDepthDrawLayerStride = edited.WorldDepthDrawLayerStride;
                    options.RenderLevelBackgroundColor = edited.RenderLevelBackgroundColor;
                    options.RenderLevelBackgroundImage = edited.RenderLevelBackgroundImage;
                    options.IncludeInvisibleLayers = edited.IncludeInvisibleLayers;
                }, mergeKey);
                _error = null;
            }
            catch (Exception exception)
            {
                _error = exception.Message;
            }
        }

        if (ImGui.Button("Reimport LDtk Now", new System.Numerics.Vector2(-1f, 0f)))
        {
            try
            {
                document.ReimportLDtk();
                _error = null;
            }
            catch (Exception exception)
            {
                _error = exception.Message;
            }
        }
        ImGui.TextDisabled("Live sync watches the .ldtk project and its external level files.");
        ImGui.Separator();
    }

    private void DrawTiledImportOptions(SceneDocument document)
    {
        if (!ImGui.CollapsingHeader("Tiled Import Options", ImGuiTreeNodeFlags.DefaultOpen))
            return;

        var current = document.TiledReference!.ImportOptions ?? new TiledImportOptions();
        var edited = current.Clone();
        var pixelsPerUnit = edited.PixelsPerUnit;
        var baseDrawLayer = edited.BaseDrawLayer;
        var drawLayerStep = edited.DrawLayerStep;
        var worldDepth = edited.WorldDepth;
        var worldDepthStride = edited.WorldDepthDrawLayerStride;
        var renderBackgroundColor = edited.RenderMapBackgroundColor;
        var includeInvisibleLayers = edited.IncludeInvisibleLayers;
        var changed = false;
        string? mergeKey = null;
        DrawPropertyRow("Tiled.PixelsPerUnit", "Pixels Per Unit", () =>
        {
            if (ImGui.DragFloat("##Value", ref pixelsPerUnit, 0.1f, 0.001f, 100000f))
                (changed, mergeKey) = (true, "Tiled.PixelsPerUnit");
        });
        DrawPropertyRow("Tiled.BaseDrawLayer", "Base Draw Layer", () =>
        {
            if (ImGui.DragInt("##Value", ref baseDrawLayer, 1f))
                (changed, mergeKey) = (true, "Tiled.BaseDrawLayer");
        });
        DrawPropertyRow("Tiled.DrawLayerStep", "Draw Layer Step", () =>
        {
            if (ImGui.DragInt("##Value", ref drawLayerStep, 1f, 1, 100000))
                (changed, mergeKey) = (true, "Tiled.DrawLayerStep");
        });
        DrawPropertyRow("Tiled.WorldDepth", "World Depth", () =>
        {
            if (ImGui.DragInt("##Value", ref worldDepth, 1f))
                (changed, mergeKey) = (true, "Tiled.WorldDepth");
        });
        DrawPropertyRow("Tiled.WorldDepthStride", "World Depth Stride", () =>
        {
            if (ImGui.DragInt("##Value", ref worldDepthStride, 1f, 1, int.MaxValue))
                (changed, mergeKey) = (true, "Tiled.WorldDepthStride");
        });
        DrawPropertyRow("Tiled.RenderBackgroundColor", "Render Background Color", () =>
        {
            if (ImGui.Checkbox("##Value", ref renderBackgroundColor))
                (changed, mergeKey) = (true, "Tiled.RenderBackgroundColor");
        });
        DrawPropertyRow("Tiled.IncludeInvisibleLayers", "Include Invisible Layers", () =>
        {
            if (ImGui.Checkbox("##Value", ref includeInvisibleLayers))
                (changed, mergeKey) = (true, "Tiled.IncludeInvisibleLayers");
        });

        edited.PixelsPerUnit = pixelsPerUnit;
        edited.BaseDrawLayer = baseDrawLayer;
        edited.DrawLayerStep = drawLayerStep;
        edited.WorldDepth = worldDepth;
        edited.WorldDepthDrawLayerStride = worldDepthStride;
        edited.RenderMapBackgroundColor = renderBackgroundColor;
        edited.IncludeInvisibleLayers = includeInvisibleLayers;

        if (changed)
        {
            try
            {
                document.UpdateTiledImportOptions("Change Tiled Import Options", options =>
                {
                    options.PixelsPerUnit = edited.PixelsPerUnit;
                    options.BaseDrawLayer = edited.BaseDrawLayer;
                    options.DrawLayerStep = edited.DrawLayerStep;
                    options.WorldDepth = edited.WorldDepth;
                    options.WorldDepthDrawLayerStride = edited.WorldDepthDrawLayerStride;
                    options.RenderMapBackgroundColor = edited.RenderMapBackgroundColor;
                    options.IncludeInvisibleLayers = edited.IncludeInvisibleLayers;
                }, mergeKey);
                _error = null;
            }
            catch (Exception exception)
            {
                _error = exception.Message;
            }
        }

        if (ImGui.Button("Reimport Tiled Now", new System.Numerics.Vector2(-1f, 0f)))
        {
            try
            {
                document.ReimportTiled();
                _error = null;
            }
            catch (Exception exception)
            {
                _error = exception.Message;
            }
        }
        ImGui.TextDisabled("Live sync watches the .tmx map and referenced .tsx tilesets.");
        ImGui.TextDisabled("Object and image layers are intentionally ignored.");
        ImGui.Separator();
    }

    private void DrawAssetDocument(DreambitAssetDocument document)
    {
        ImGui.TextUnformatted(document.Asset.Name + (document.IsDirty ? " *" : string.Empty));
        ImGui.TextDisabled(document.Asset.RelativePath);
        ImGui.Separator();
        if (_customEditors.TryGet(document.AssetType, out var customEditor))
        {
            var context = new CustomInspectorContext(
                [document.Instance],
                () => DrawDefaultAssetDocument(document),
                (name, mutation) => document.Apply(name, _ => mutation()),
                LogExtension);
            try
            {
                customEditor!.Draw(context);
            }
            catch (Exception exception)
            {
                _logs.Error("Game Editor", $"Custom asset Editor for '{document.AssetType.FullName}' failed.", exception);
                ImGui.TextColored(new Vector4(0.96f, 0.34f, 0.36f, 1f), exception.Message);
                DrawDefaultAssetDocument(document);
            }
            return;
        }
        DrawDefaultAssetDocument(document);
    }

    private void DrawDefaultAssetDocument(DreambitAssetDocument document)
    {
        if (document.Instance is EntityBlueprint blueprint)
        {
            DrawBlueprintDocument(document, blueprint);
            return;
        }

        foreach (var member in _metadata.Get(document.AssetType, InspectorTargetKind.Asset))
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(member.Header))
                {
                    ImGui.Spacing();
                    ImGui.TextDisabled(member.Header);
                }
                var value = member.GetValue(document.Instance);
                var result = _drawers.Draw(
                    member.DisplayName,
                    member.ValueType,
                    value,
                    new InspectorValueDrawContext(
                        $"Asset.{document.Asset.Id}.{member.SerializedName}",
                        member,
                        false,
                        member.IsReadOnly));
                if (result.Changed && !member.IsReadOnly)
                    document.Apply(
                        $"Change {member.DisplayName}",
                        asset => member.SetValue(asset, result.Value),
                        $"Asset.{document.Asset.Id}.{member.SerializedName}");
            }
            catch (Exception exception)
            {
                _error = $"{member.DisplayName}: {exception.Message}";
                ImGui.TextColored(new Vector4(0.96f, 0.34f, 0.36f, 1f), _error);
            }
        }
    }

    private void DrawBlueprintDocument(DreambitAssetDocument document, EntityBlueprint blueprint)
    {
        var name = blueprint.Name;
        DrawPropertyRow("Blueprint.Name", "Name", () =>
        {
            if (ImGui.InputText("##Value", ref name, 256))
                document.Apply(
                    "Rename Blueprint",
                    asset => ((EntityBlueprint)asset).Name = name,
                    $"Blueprint.{blueprint.Guid:N}.Name");
        });
        var enabled = blueprint.Enabled;
        DrawPropertyRow("Blueprint.Enabled", "Enabled", () =>
        {
            if (ImGui.Checkbox("##Value", ref enabled))
                document.Apply(
                    "Set Blueprint Enabled",
                    asset => ((EntityBlueprint)asset).Enabled = enabled,
                    $"Blueprint.{blueprint.Guid:N}.Enabled");
        });

        ImGui.TextDisabled("Drag to adjust. Double-click a number to type an exact value.");
        var position = new Vector3(blueprint.Position.X, blueprint.Position.Y, blueprint.Position.Z);
        DrawPropertyRow("Blueprint.Position", "Position", () =>
        {
            if (ImGui.DragFloat3("##Value", ref position, 0.1f))
                document.Apply(
                    "Change Blueprint Position",
                    asset => ((EntityBlueprint)asset).Position =
                        new Microsoft.Xna.Framework.Vector3(position.X, position.Y, position.Z),
                    $"Blueprint.{blueprint.Guid:N}.Position");
        });
        var rotation = new Vector3(blueprint.Rotation.X, blueprint.Rotation.Y, blueprint.Rotation.Z);
        DrawPropertyRow("Blueprint.Rotation", "Rotation", () =>
        {
            if (ImGui.DragFloat3("##Value", ref rotation, 0.01f))
                document.Apply(
                    "Change Blueprint Rotation",
                    asset => ((EntityBlueprint)asset).Rotation =
                        new Microsoft.Xna.Framework.Vector3(rotation.X, rotation.Y, rotation.Z),
                    $"Blueprint.{blueprint.Guid:N}.Rotation");
        });
        var scale = new Vector3(blueprint.Scale.X, blueprint.Scale.Y, blueprint.Scale.Z);
        DrawPropertyRow("Blueprint.Scale", "Scale", () =>
        {
            if (ImGui.DragFloat3("##Value", ref scale, 0.01f))
                document.Apply(
                    "Change Blueprint Scale",
                    asset => ((EntityBlueprint)asset).Scale =
                        new Microsoft.Xna.Framework.Vector3(scale.X, scale.Y, scale.Z),
                    $"Blueprint.{blueprint.Guid:N}.Scale");
        });

        ImGui.SeparatorText("Components");
        foreach (var component in blueprint.Components.ToArray())
        {
            ImGui.PushID(component.GetHashCode());
            try
            {
                var componentType = BlueprintResolver.ResolveComponentType(component.Type);
                var title = componentType?.Name ?? $"Missing: {component.Type}";
                var (open, removeRequested) = DrawRemovableHeader(title);
                if (removeRequested)
                {
                    document.Apply($"Remove {title}", asset => ((EntityBlueprint)asset).Components.Remove(component));
                    continue;
                }
                if (open)
                {
                    var componentEnabled = component.Enabled;
                    DrawPropertyRow("BlueprintComponent.Enabled", "Enabled", () =>
                    {
                        if (ImGui.Checkbox("##Value", ref componentEnabled))
                            document.Apply(
                                "Set Component Enabled",
                                _ => component.Enabled = componentEnabled,
                                $"Blueprint.{blueprint.Guid:N}.{component.Type}.Enabled");
                    });
                    if (componentType is null)
                    {
                        ImGui.TextColored(
                            new Vector4(1f, 0.68f, 0.28f, 1f),
                            "Type unavailable. Serialized properties are preserved.");
                    }
                    else
                    {
                        DrawBlueprintComponentMembers(document, blueprint, component, componentType);
                    }
                }
            }
            finally
            {
                ImGui.PopID();
            }
        }

        if (ImGui.Button("Add Component", new System.Numerics.Vector2(-1, 0)))
            ImGui.OpenPopup("Add Blueprint Component##Inspector");
        if (ImGui.BeginPopup("Add Blueprint Component##Inspector"))
        {
            ImGui.SetNextItemWidth(300);
            ImGui.InputTextWithHint("##Search", "Search components", ref _componentSearch, 128);
            foreach (var type in _types.ComponentTypes)
            {
                if (!string.IsNullOrWhiteSpace(_componentSearch) &&
                    !type.Name.Contains(_componentSearch, StringComparison.OrdinalIgnoreCase))
                    continue;
                var alreadyExists = blueprint.Components.Any(component =>
                    BlueprintResolver.ResolveComponentType(component.Type) == type);
                if (alreadyExists) ImGui.BeginDisabled();
                if (ImGui.Selectable(type.FullName ?? type.Name))
                {
                    document.Apply($"Add {type.Name}", asset =>
                    {
                        ((EntityBlueprint)asset).Components.Add(new ComponentBlueprint
                        {
                            Type = type.GetCustomAttributes(typeof(BlueprintTypeAttribute), true)
                                .OfType<BlueprintTypeAttribute>()
                                .FirstOrDefault()?.Id ?? $"{type.Assembly.GetName().Name}.{type.Name}"
                        });
                    });
                    ImGui.CloseCurrentPopup();
                }
                if (alreadyExists) ImGui.EndDisabled();
            }
            ImGui.EndPopup();
        }
        if (blueprint.Children.Count > 0)
            ImGui.TextDisabled($"{blueprint.Children.Count} child blueprint(s) are preserved in this asset.");
    }

    private void DrawBlueprintComponentMembers(
        DreambitAssetDocument document,
        EntityBlueprint blueprint,
        ComponentBlueprint component,
        Type componentType)
    {
        Component? defaultComponent = null;
        try
        {
            defaultComponent = Activator.CreateInstance(componentType) as Component;
        }
        catch
        {
            // Components with special constructors still retain their serialized data.
        }

        foreach (var member in _metadata.Get(componentType, InspectorTargetKind.Component))
        {
            if (member.ValueType == typeof(Entity) ||
                typeof(Component).IsAssignableFrom(member.ValueType))
            {
                DrawBlueprintReferenceMember(document, blueprint, component, member);
                continue;
            }

            if (typeof(DreambitAsset).IsAssignableFrom(member.ValueType))
            {
                DrawBlueprintAssetReferenceMember(document, component, member);
                continue;
            }

            object? value = null;
            if (component.Properties.TryGetValue(member.SerializedName, out var token))
            {
                try
                {
                    value = DreambitJson.FromToken(token, member.ValueType);
                }
                catch
                {
                    ImGui.TextColored(
                        new Vector4(1f, 0.68f, 0.28f, 1f),
                        $"{member.DisplayName}: serialized reference/value retained");
                    continue;
                }
            }
            else if (defaultComponent is not null)
            {
                value = member.GetValue(defaultComponent);
            }
            else if (member.ValueType.IsValueType)
            {
                value = Activator.CreateInstance(member.ValueType);
            }
            var result = _drawers.Draw(
                member.DisplayName,
                member.ValueType,
                value,
                new InspectorValueDrawContext(
                    $"Blueprint.{component.Type}.{member.SerializedName}",
                    member,
                    false,
                    member.IsReadOnly));
            if (result.Changed && !member.IsReadOnly)
                document.Apply(
                    $"Change {member.DisplayName}",
                    _ => component.Properties[member.SerializedName] = DreambitJson.ToToken(result.Value),
                    $"Blueprint.{blueprint.Guid:N}.{component.Type}.{member.SerializedName}");
        }
    }

    private void DrawBlueprintAssetReferenceMember(
        DreambitAssetDocument document,
        ComponentBlueprint component,
        InspectorMemberMetadata member)
    {
        component.Properties.TryGetValue(member.SerializedName, out var token);
        var snapshot = _assets.GetSnapshot();
        AssetRecord? selected = null;
        string? fallbackPath = null;

        if (DreambitAssetReferenceToken.TryRead(token, out var assetId, out fallbackPath))
            selected = snapshot.Assets.FirstOrDefault(asset => asset.Id == assetId);
        else if (token?.Type == Newtonsoft.Json.Linq.JTokenType.String)
        {
            fallbackPath = (string?)token;
            selected = snapshot.Assets.FirstOrDefault(asset =>
                string.Equals(
                    asset.LogicalAssetName,
                    fallbackPath,
                    StringComparison.OrdinalIgnoreCase));
        }

        var display = selected?.RelativePath ??
                      (!string.IsNullOrWhiteSpace(fallbackPath)
                          ? $"Missing ({fallbackPath})"
                          : token is null || token.Type == Newtonsoft.Json.Linq.JTokenType.Null
                              ? "None"
                              : "Invalid inline asset");
        var pickerId = $"BlueprintAsset.{component.Type}.{member.SerializedName}";

        DrawBlueprintReferenceRow(
            pickerId,
            member.DisplayName,
            display,
            token is null || member.IsReadOnly,
            () =>
            {
                _blueprintReferenceSearch = string.Empty;
                ImGui.OpenPopup($"Blueprint Asset Picker##{pickerId}");
            },
            () => document.Apply($"Clear {member.DisplayName}", _ =>
                component.Properties.Remove(member.SerializedName)));

        if (!ImGui.BeginPopup($"Blueprint Asset Picker##{pickerId}"))
            return;
        try
        {
            ImGui.TextDisabled($"Select a {member.ValueType.Name} asset.");
            ImGui.SetNextItemWidth(420f);
            ImGui.InputTextWithHint(
                "##BlueprintAssetSearch",
                $"Search {member.ValueType.Name} assets",
                ref _blueprintReferenceSearch,
                128);
            ImGui.Separator();
            ImGui.BeginChild(
                "##BlueprintAssetItems",
                new System.Numerics.Vector2(420f, 260f));
            try
            {
                foreach (var candidate in snapshot.Assets)
                {
                    if (!AssetTypeClassifier.IsCompatibleWith(candidate, member.ValueType))
                        continue;
                    if (!string.IsNullOrWhiteSpace(_blueprintReferenceSearch) &&
                        !candidate.RelativePath.Contains(
                            _blueprintReferenceSearch,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (!ImGui.Selectable(
                            $"{candidate.RelativePath}##{candidate.Id}",
                            candidate.Id == selected?.Id) || member.IsReadOnly)
                    {
                        continue;
                    }

                    document.Apply($"Change {member.DisplayName}", _ =>
                        component.Properties[member.SerializedName] =
                            DreambitAssetReferenceToken.Create(
                                candidate.Id,
                                candidate.LogicalAssetName));
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
    }

    private void DrawBlueprintReferenceMember(
        DreambitAssetDocument document,
        EntityBlueprint blueprint,
        ComponentBlueprint component,
        InspectorMemberMetadata member)
    {
        component.Properties.TryGetValue(member.SerializedName, out var token);
        var referencedGuid = Guid.Empty;
        var hasReference = token?.Type == Newtonsoft.Json.Linq.JTokenType.String &&
                           Guid.TryParse((string?)token, out referencedGuid);
        var candidates = GetBlueprintReferenceCandidates(blueprint, member.ValueType);
        var selected = hasReference
            ? candidates.FirstOrDefault(candidate => candidate.Guid == referencedGuid)
            : null;
        var display = selected is not null
            ? member.ValueType == typeof(Entity)
                ? selected.Name
                : $"{selected.Name} ({member.ValueType.Name})"
            : hasReference
                ? $"Missing ({referencedGuid.ToString()[..8]})"
                : "None";
        var pickerId = $"BlueprintReference.{component.Type}.{member.SerializedName}";

        DrawBlueprintReferenceRow(
            pickerId,
            member.DisplayName,
            display,
            !hasReference || member.IsReadOnly,
            () =>
            {
                _blueprintReferenceSearch = string.Empty;
                ImGui.OpenPopup($"Blueprint Reference Picker##{pickerId}");
            },
            () => document.Apply($"Clear {member.DisplayName}", _ =>
                component.Properties.Remove(member.SerializedName)));

        if (!ImGui.BeginPopup($"Blueprint Reference Picker##{pickerId}"))
            return;
        try
        {
            ImGui.TextDisabled(
                member.ValueType == typeof(Entity)
                    ? "Select an entity from this Blueprint."
                    : $"Select an entity in this Blueprint containing {member.ValueType.Name}.");
            ImGui.SetNextItemWidth(360f);
            ImGui.InputTextWithHint(
                "##BlueprintReferenceSearch",
                "Search Blueprint entities",
                ref _blueprintReferenceSearch,
                128);
            ImGui.Separator();
            ImGui.BeginChild(
                "##BlueprintReferenceItems",
                new System.Numerics.Vector2(360f, 260f));
            try
            {
                foreach (var candidate in candidates)
                {
                    if (!string.IsNullOrWhiteSpace(_blueprintReferenceSearch) &&
                        !candidate.Name.Contains(
                            _blueprintReferenceSearch,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var label = $"{candidate.Name}##{candidate.Guid:N}";
                    if (!ImGui.Selectable(label, candidate.Guid == referencedGuid) || member.IsReadOnly)
                        continue;
                    document.Apply($"Change {member.DisplayName}", _ =>
                        component.Properties[member.SerializedName] =
                            new Newtonsoft.Json.Linq.JValue(candidate.Guid.ToString()));
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
    }

    private static void DrawPropertyRow(string id, string label, Action drawValue)
    {
        var availableWidth = MathF.Max(1f, ImGui.GetContentRegionAvail().X);
        var labelWidth = Math.Clamp(availableWidth * 0.35f, 120f, 190f);
        var flags = ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.NoSavedSettings;
        if (!ImGui.BeginTable($"##PropertyRow.{id}", 2, flags))
            return;

        try
        {
            ImGui.TableSetupColumn("##Label", ImGuiTableColumnFlags.WidthFixed, labelWidth);
            ImGui.TableSetupColumn("##Value", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableNextRow();
            ImGui.TableSetColumnIndex(0);
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(label);
            ImGui.TableSetColumnIndex(1);
            ImGui.SetNextItemWidth(-1f);
            drawValue();
        }
        finally
        {
            ImGui.EndTable();
        }
    }

    private static void DrawBlueprintReferenceRow(
        string id,
        string label,
        string value,
        bool clearDisabled,
        Action select,
        Action clear)
    {
        var availableWidth = MathF.Max(1f, ImGui.GetContentRegionAvail().X);
        var labelWidth = Math.Clamp(availableWidth * 0.35f, 120f, 190f);
        var flags = ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.NoSavedSettings;
        if (!ImGui.BeginTable($"##BlueprintPropertyRow.{id}", 3, flags))
            return;

        try
        {
            ImGui.TableSetupColumn("##Label", ImGuiTableColumnFlags.WidthFixed, labelWidth);
            ImGui.TableSetupColumn("##Value", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("##Clear", ImGuiTableColumnFlags.WidthFixed, ImGui.GetFrameHeight());
            ImGui.TableNextRow();
            ImGui.TableSetColumnIndex(0);
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(label);
            ImGui.TableSetColumnIndex(1);
            if (ImGui.Button($"{value}##{id}", new System.Numerics.Vector2(-1f, 0f)))
                select();
            ImGui.TableSetColumnIndex(2);
            ImGui.BeginDisabled(clearDisabled);
            if (ImGui.SmallButton($"×##{id}.Clear"))
                clear();
            ImGui.EndDisabled();
        }
        finally
        {
            ImGui.EndTable();
        }
    }

    internal static IReadOnlyList<EntityBlueprint> GetBlueprintReferenceCandidates(
        EntityBlueprint blueprint,
        Type referenceType)
    {
        ArgumentNullException.ThrowIfNull(blueprint);
        ArgumentNullException.ThrowIfNull(referenceType);
        return blueprint.FlattenedHierarchy()
            .Where(entity => referenceType == typeof(Entity) ||
                             typeof(Component).IsAssignableFrom(referenceType) &&
                             entity.Components.Any(component =>
                             {
                                 var candidateType = BlueprintResolver.ResolveComponentType(component.Type);
                                 return candidateType is not null &&
                                        referenceType.IsAssignableFrom(candidateType);
                             }))
            .ToArray();
    }

    private static void DrawMixedLabel(string label, bool mixed)
    {
        if (mixed)
        {
            ImGui.SameLine();
            ImGui.TextDisabled($"{label}: —");
        }
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
            Apply(document, "Set Enabled", "Entity.Enabled", () =>
            {
                foreach (var entity in entities)
                {
                    entity.Enabled = enabled;
                    document.RecordGeneratedEntityEnabled(entity);
                }
            });
    }

    private void DrawEntityTags(SceneDocument document, IReadOnlyList<Entity> entities)
    {
        var firstTags = entities[0].Tags.OrderBy(tag => tag, StringComparer.OrdinalIgnoreCase).ToArray();
        var mixed = entities.Skip(1).Any(entity =>
            !entity.Tags.SetEquals(entities[0].Tags));
        var tags = mixed ? string.Empty : string.Join(", ", firstTags);

        DrawPropertyRow("Entity.Tags", "Tags", () =>
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
                Apply(document, "Change Entity Tags", "Entity.Tags", () =>
                {
                    foreach (var entity in entities)
                    {
                        entity.Tags.Clear();
                        entity.Tags.UnionWith(updatedTags);
                        document.RecordGeneratedEntityTags(entity);
                    }
                });
            }

            if (mixed)
            {
                ImGui.SameLine();
                ImGui.TextDisabled("Multiple values");
            }
        });
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
            DrawPropertyRow("Transform.Position", "Position", () =>
            {
                if (ImGui.DragFloat3("##Value", ref position, 0.1f))
                    Apply(document, "Change Position", "Transform.Position", () =>
                    {
                        var value = new Microsoft.Xna.Framework.Vector3(position.X, position.Y, position.Z);
                        foreach (var entity in entities)
                        {
                            entity.Transform.Position = value;
                            document.RecordGeneratedPosition(entity);
                        }
                    });
                DrawMixedLabel("Position", mixedPosition);
            });

            var rotation = MathHelper.ToDegrees(first.Rotation2D);
            var mixedRotation = entities.Skip(1).Any(entity =>
                MathF.Abs(entity.Transform.Rotation2D - first.Rotation2D) > 0.0001f);
            DrawPropertyRow("Transform.Rotation", "Rotation", () =>
            {
                if (ImGui.DragFloat("##Value", ref rotation, 0.25f))
                    Apply(document, "Change Rotation", "Transform.Rotation", () =>
                    {
                        var value = MathHelper.ToRadians(rotation);
                        foreach (var entity in entities)
                        {
                            entity.Transform.Rotation2D = value;
                            document.RecordGeneratedRotation(entity);
                        }
                    });
                DrawMixedLabel("Rotation", mixedRotation);
            });

            var scale = new Vector3(first.Scale.X, first.Scale.Y, first.Scale.Z);
            var mixedScale = entities.Skip(1).Any(entity => entity.Transform.Scale != first.Scale);
            DrawPropertyRow("Transform.Scale", "Scale", () =>
            {
                if (ImGui.DragFloat3("##Value", ref scale, 0.01f))
                    Apply(document, "Change Scale", "Transform.Scale", () =>
                    {
                        var value = new Microsoft.Xna.Framework.Vector3(scale.X, scale.Y, scale.Z);
                        foreach (var entity in entities)
                        {
                            entity.Transform.Scale = value;
                            document.RecordGeneratedScale(entity);
                        }
                    });
                DrawMixedLabel("Scale", mixedScale);
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
        .Where(type =>
            entities.Skip(1).All(entity =>
                entity.GetComponent(type) is not null))
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

            var allowRemove = !readOnly && !generated;
            var statusText = readOnly
                ? "Boxed"
                : generated
                    ? "LDtk"
                    : null;

            var (open, removeRequested) = DrawRemovableHeader(
                componentType.Name,
                allowRemove,
                statusText);

            if (removeRequested)
            {
                Apply(
                    document,
                    $"Remove {componentType.Name}",
                    () =>
                    {
                        foreach (var entity in entities)
                        {
                            if (entity.GetComponent(componentType) is { } component)
                                entity.DetachComponent(component);
                        }
                    });

                continue;
            }

            if (!open)
                continue;

            if (readOnly)
            {
                // Game-defined custom editors currently have a mutation API
                // but no read-only contract. Use the default inspector while
                // displaying source-owned boxed Blueprint component data.
                DrawComponentMembers(
                    document,
                    components,
                    readOnly: true);

                continue;
            }

            if (_customEditors.TryGet(componentType, out var customEditor))
            {
                var context = new CustomInspectorContext(
                    components.Cast<object>().ToArray(),
                    () => DrawComponentMembers(document, components),
                    (name, mutation) => document.Apply(name, _ =>
                    {
                        mutation();
                        RecordGeneratedComponentValues(document, components);
                    }),
                    LogExtension);

                try
                {
                    customEditor!.Draw(context);
                }
                catch (Exception exception)
                {
                    _logs.Error(
                        "Game Editor",
                        $"Custom Component Editor for '{componentType.FullName}' failed.",
                        exception);

                    ImGui.TextColored(
                        new Vector4(0.96f, 0.34f, 0.36f, 1f),
                        exception.Message);

                    DrawComponentMembers(document, components);
                }
            }
            else
            {
                DrawComponentMembers(document, components);
            }
        }
        finally
        {
            ImGui.PopID();
        }
    }

    var partialCount =
        entities[0].GetAllComponents().Count - commonTypes.Length;

    if (entities.Count > 1 && partialCount != 0)
    {
        ImGui.TextDisabled(
            "Components not shared by every selected entity are hidden.");
    }
}

    private static (bool Open, bool RemoveRequested) DrawRemovableHeader(
        string title,
        bool allowRemove = true,
        string? statusText = null)
    {
        var open = false;
        var removeRequested = false;

        var flags =
            ImGuiTableFlags.SizingStretchProp |
            ImGuiTableFlags.NoSavedSettings;

        if (!ImGui.BeginTable("##RemovableHeader", 2, flags))
            return (false, false);

        try
        {
            ImGui.TableSetupColumn(
                "##Title",
                ImGuiTableColumnFlags.WidthStretch);

            ImGui.TableSetupColumn(
                "##Remove",
                ImGuiTableColumnFlags.WidthFixed,
                ImGui.GetFrameHeight());

            ImGui.TableNextRow();

            ImGui.TableSetColumnIndex(0);

            open = ImGui.CollapsingHeader(
                title,
                ImGuiTreeNodeFlags.DefaultOpen);

            ImGui.TableSetColumnIndex(1);

            if (allowRemove)
            {
                if (ImGui.SmallButton("×"))
                    removeRequested = true;

                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip($"Remove {title}");
            }
            else if (!string.IsNullOrWhiteSpace(statusText))
            {
                ImGui.TextDisabled(statusText);
            }
        }
        finally
        {
            ImGui.EndTable();
        }

        return (open, removeRequested);
    }

    private void DrawComponentMembers(
    SceneDocument document,
    IReadOnlyList<Component> components,
    bool readOnly = false)
{
    var componentType = components[0].GetType();
    var members = _metadata.Get(
        componentType,
        InspectorTargetKind.Component);

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

            var mixed = components
                .Skip(1)
                .Any(component =>
                    !ValuesEqual(
                        first,
                        member.GetValue(component)));

            var id =
                $"{componentType.FullName}.{member.SerializedName}";

            var effectiveReadOnly =
                readOnly || member.IsReadOnly;

            var result = _drawers.Draw(
                member.DisplayName,
                member.ValueType,
                first,
                new InspectorValueDrawContext(
                    id,
                    member,
                    mixed,
                    effectiveReadOnly));

            // Keep this explicit even though disabled ImGui controls should
            // not report changes. It protects the Blueprint ownership
            // invariant if a drawer behaves unexpectedly.
            if (!result.Changed || effectiveReadOnly)
                continue;

            Apply(
                document,
                $"Change {member.DisplayName}",
                id,
                () =>
                {
                    foreach (var component in components)
                    {
                        member.SetValue(
                            component,
                            result.Value);

                        component.AcknowledgeEditorSerializationFailure(
                            member.SerializedName);

                        document.RecordGeneratedComponentMember(
                            component,
                            member.SerializedName,
                            result.Value);

                        if (result.Value is null &&
                            (typeof(DreambitAsset).IsAssignableFrom(member.ValueType) ||
                             member.ValueType == typeof(Entity) ||
                             typeof(Component).IsAssignableFrom(member.ValueType)))
                        {
                            document.MarkReferenceCleared(
                                component.Entity,
                                componentType,
                                member.SerializedName);
                        }
                    }
                });
        }
        catch (Exception exception)
        {
            _error =
                $"{componentType.Name}.{member.DisplayName}: {exception.Message}";

            ImGui.TextColored(
                new Vector4(0.96f, 0.34f, 0.36f, 1f),
                _error);
        }
    }
}

    private void RecordGeneratedComponentValues(
        SceneDocument document,
        IReadOnlyList<Component> components)
    {
        foreach (var component in components)
            foreach (var member in _metadata.Get(component.GetType(), InspectorTargetKind.Component))
                if (!member.IsReadOnly)
                    document.RecordGeneratedComponentMember(
                        component,
                        member.SerializedName,
                        member.GetValue(component));
    }

    private void DrawAddComponent(SceneDocument document, IReadOnlyList<Entity> entities)
    {
        ImGui.Spacing();
        if (ImGui.Button("Add Component", new System.Numerics.Vector2(-1f, 0f)))
        {
            _componentSearch = string.Empty;
            ImGui.OpenPopup("Add Component##Inspector");
        }
        if (!ImGui.BeginPopup("Add Component##Inspector"))
            return;
        ImGui.SetNextItemWidth(320f);
        ImGui.InputTextWithHint("##ComponentSearch", "Search components", ref _componentSearch, 128);
        ImGui.Separator();
        ImGui.BeginChild("##ComponentList", new System.Numerics.Vector2(320, 280));
        foreach (var type in _types.ComponentTypes)
        {
            if (!string.IsNullOrWhiteSpace(_componentSearch) &&
                !type.Name.Contains(_componentSearch, StringComparison.OrdinalIgnoreCase) &&
                !(type.FullName?.Contains(_componentSearch, StringComparison.OrdinalIgnoreCase) ?? false))
                continue;
            var alreadyOnAll = entities.All(entity => entity.GetComponent(type) is not null);
            if (alreadyOnAll)
                ImGui.BeginDisabled();
            if (ImGui.Selectable(type.FullName ?? type.Name))
            {
                Apply(document, $"Add {type.Name}", () =>
                {
                    foreach (var entity in entities)
                        entity.AttachComponent(type);
                });
                ImGui.CloseCurrentPopup();
            }
            if (alreadyOnAll)
                ImGui.EndDisabled();
        }
        ImGui.EndChild();
        ImGui.EndPopup();
    }

    private void Apply(SceneDocument document, string name, string mergeKey, Action mutation)
    {
        try
        {
            document.Apply(
                name,
                _ => mutation(),
                BuildSceneMergeKey(document, mergeKey));
            _error = null;
        }
        catch (Exception exception)
        {
            _error = exception.Message;
        }
    }

    private void Apply(SceneDocument document, string name, Action mutation)
    {
        try
        {
            document.Apply(name, _ => mutation());
            _error = null;
        }
        catch (Exception exception)
        {
            _error = exception.Message;
        }
    }

    private static string BuildSceneMergeKey(SceneDocument document, string propertyKey)
    {
        var selection = string.Join(
            ",",
            document.Selection.EntityIds
                .OrderBy(static id => id)
                .Select(static id => id.ToString("N")));
        return $"{propertyKey}|{selection}";
    }

    private static bool ValuesEqual(object? left, object? right) =>
        ReferenceEquals(left, right) || (left?.Equals(right) ?? right is null);

    protected override void DisposeCore() => _previews.Dispose();

    private void LogExtension(EditorExtensionLogLevel level, string message, Exception? exception)
    {
        switch (level)
        {
            case EditorExtensionLogLevel.Information:
                _logs.Info("Game Editor", message);
                break;
            case EditorExtensionLogLevel.Warning:
                _logs.Warning("Game Editor", message);
                break;
            case EditorExtensionLogLevel.Error:
                _logs.Error("Game Editor", message, exception);
                break;
        }
    }

    private sealed class CustomInspectorContext(
        IReadOnlyList<object> targets,
        Action drawDefault,
        Action<string, Action> recordChange,
        Action<EditorExtensionLogLevel, string, Exception?> log) : IEditorInspectorContext
    {
        public object? ActiveTarget => targets.Count == 0 ? null : targets[0];
        public IReadOnlyList<object> Targets => targets;
        public void DrawDefaultInspector() => drawDefault();
        public void RecordChange(string name, Action mutation) => recordChange(name, mutation);
        public void Log(EditorExtensionLogLevel level, string message, Exception? exception = null) =>
            log(level, message, exception);
    }
}
