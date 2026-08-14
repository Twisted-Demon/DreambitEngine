using Dreambit.ECS;
using Dreambit.Editor.Graphics;
using Dreambit.Editor.Scenes;
using Dreambit.Editor.UI;
using Dreambit.Editor.UI.Viewport;
using Dreambit.LDtk;
using Microsoft.Xna.Framework;
using Newtonsoft.Json.Linq;

namespace Dreambit.Editor.Tests;

public sealed class SceneDocumentTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(),
        "Dreambit.Editor.SceneDocumentTests",
        Guid.NewGuid().ToString("N"));

    public SceneDocumentTests() => Directory.CreateDirectory(_root);

    [Fact]
    public void SingleRootDocumentCapturesBlueprintHierarchyEdits()
    {
        var root = new EntityBlueprint
        {
            Name = "Tree",
            Guid = Guid.NewGuid(),
            Children =
            [
                new EntityBlueprint { Name = "Leaves", Guid = Guid.NewGuid() }
            ]
        };
        var selection = new SelectionService();
        using var document = new SceneDocument(
            new SceneBlueprint { Name = "Tree", Entities = [root] },
            null,
            selection);
        var changed = 0;
        document.Changed += _ => changed++;
        var liveRoot = Assert.Single(
            document.Scene!.GetAllEntities(),
            entity => entity.Parent is null);

        document.CreateEmpty("Shadow", liveRoot);
        var captured = document.CaptureSingleRoot();

        Assert.Equal(1, changed);
        Assert.Equal(new[] { "Leaves", "Shadow" }, captured.Children.Select(child => child.Name));
    }

    [Fact]
    public void DuplicateOnlyRemapsTypedEntityReferences()
    {
        var referencedId = Guid.NewGuid();
        var root = new EntityBlueprint
        {
            Name = "Root",
            Guid = referencedId,
            Children =
            [
                new EntityBlueprint
                {
                    Name = "Child",
                    Guid = Guid.NewGuid(),
                    Components =
                    [
                        new ComponentBlueprint
                        {
                            Type = $"{typeof(EditorReloadSafetyComponent).Assembly.GetName().Name}." +
                                   nameof(EditorReloadSafetyComponent),
                            Properties = new Dictionary<string, JToken>
                            {
                                [nameof(EditorReloadSafetyComponent.Target)] = referencedId.ToString(),
                                [nameof(EditorReloadSafetyComponent.StableGuid)] = referencedId.ToString(),
                                [nameof(EditorReloadSafetyComponent.Label)] = referencedId.ToString()
                            }
                        }
                    ]
                }
            ]
        };

        var duplicate = SceneDocumentSerializer.CloneAndRemap(root);
        var component = Assert.Single(Assert.Single(duplicate.Children).Components);

        Assert.Equal(duplicate.Guid.ToString(), component.Properties[nameof(EditorReloadSafetyComponent.Target)]!.Value<string>());
        Assert.Equal(referencedId.ToString(), component.Properties[nameof(EditorReloadSafetyComponent.StableGuid)]!.Value<string>());
        Assert.Equal(referencedId.ToString(), component.Properties[nameof(EditorReloadSafetyComponent.Label)]!.Value<string>());
    }

    [Fact]
    public void NewSpriteDrawerDefaultsToOpaqueWhiteAndSerializesThoseDefaults()
    {
        var root = new EntityBlueprint
        {
            Name = "Sprite",
            Guid = Guid.NewGuid(),
            Components = [new ComponentBlueprint { Type = nameof(SpriteDrawer) }]
        };
        using var document = new SceneDocument(
            new SceneBlueprint { Name = "Sprite", Entities = [root] },
            null,
            new SelectionService());

        var entity = Assert.Single(document.Scene!.GetAllEntities());
        var drawer = Assert.IsType<SpriteDrawer>(entity.GetComponent<SpriteDrawer>());
        Assert.Equal(Microsoft.Xna.Framework.Color.White, drawer.Tint);
        Assert.Equal(1f, drawer.Opacity);

        var serialized = Assert.Single(document.CaptureSingleRoot().Components).Properties;
        Assert.Equal(
            new[] { 255, 255, 255, 255 },
            serialized[nameof(SpriteDrawer.Tint)]!.Values<int>());
        Assert.Equal(1f, serialized[nameof(SpriteDrawer.Opacity)]!.Value<float>());
    }

    [Fact]
    public void SpriteDrawerMembersWithNonPublicSettersRoundTripThroughBlueprints()
    {
        var root = new EntityBlueprint
        {
            Name = "Sprite",
            Guid = Guid.NewGuid(),
            Components =
            [
                new ComponentBlueprint
                {
                    Type = nameof(SpriteDrawer),
                    Properties = new Dictionary<string, JToken>
                    {
                        [nameof(SpriteDrawer.Pivot)] = new JArray(24f, 41f),
                        [nameof(SpriteDrawer.PivotType)] = (int)PivotType.Custom
                    }
                }
            ]
        };
        using var document = new SceneDocument(
            new SceneBlueprint { Name = "Sprite", Entities = [root] },
            null,
            new SelectionService());

        var entity = Assert.Single(document.Scene!.GetAllEntities());
        var drawer = Assert.IsType<SpriteDrawer>(entity.GetComponent<SpriteDrawer>());
        Assert.Equal(new Microsoft.Xna.Framework.Vector2(24f, 41f), drawer.Pivot);
        Assert.Equal(PivotType.Custom, drawer.PivotType);

        var serialized = Assert.Single(document.CaptureSingleRoot().Components).Properties;
        Assert.Equal(new[] { 24f, 41f }, serialized[nameof(SpriteDrawer.Pivot)]!.Values<float>());
        Assert.Equal((int)PivotType.Custom, serialized[nameof(SpriteDrawer.PivotType)]!.Value<int>());
    }

    [Fact]
    public void SpriteDrawerSaveMigratesLegacyPathToStableSpriteReference()
    {
        var entityId = Guid.NewGuid();
        var spriteId = AssetId.New();
        var source = new SceneBlueprint
        {
            Name = "Legacy Sprite",
            Entities =
            [
                new EntityBlueprint
                {
                    Name = "Sprite",
                    Guid = entityId,
                    Components =
                    [
                        new ComponentBlueprint
                        {
                            Type = nameof(SpriteDrawer),
                            Properties = new Dictionary<string, JToken>
                            {
                                ["SpritePath"] = "sprites/tree"
                            }
                        }
                    ]
                }
            ]
        };
        using var scene = new TestEditorScene();
        var entity = scene.CreateEntity("Sprite", guidOverride: entityId);
        entity.AttachComponent<SpriteDrawer>().Sprite = new Sprite
        {
            AssetId = spriteId,
            AssetName = "sprites/tree"
        };

        var captured = SceneDocumentSerializer.Capture(scene, source, source.Name);
        var properties = Assert.Single(Assert.Single(captured.Entities).Components).Properties;

        Assert.DoesNotContain("SpritePath", properties.Keys);
        Assert.True(DreambitAssetReferenceToken.TryRead(
            properties[nameof(SpriteDrawer.Sprite)],
            out var capturedId,
            out var capturedPath));
        Assert.Equal(spriteId, capturedId);
        Assert.Equal("sprites/tree", capturedPath);
    }

    [Fact]
    public void BlueprintRootLookupIgnoresTheParentlessEditorCamera()
    {
        var root = new EntityBlueprint { Name = "Tree", Guid = Guid.NewGuid() };
        using var document = new SceneDocument(
            new SceneBlueprint { Name = "Tree", Entities = [root] },
            null,
            new SelectionService());
        document.Scene!.EnsureEditorCamera();

        Assert.Equal(
            2,
            document.Scene.GetAllEntities().Count(entity => entity.Parent is null));
        var authoredRoot = BlueprintEditingService.FindAuthoredRoot(document, root.Guid);

        Assert.NotNull(authoredRoot);
        Assert.Equal(root.Guid, authoredRoot.Id);
        Assert.False(authoredRoot.IsEditorOnly);
    }

    [Fact]
    public void EditorSceneFlushesRealComponentsWithoutGameplayCallbacks()
    {
        EditorLifecycleTestComponent.Reset();
        using (var scene = new TestEditorScene())
        {
            var entity = scene.CreateEntity("editor entity");
            entity.AttachComponent<EditorLifecycleTestComponent>();
            scene.FlushStructuralChanges();
            scene.EditorTick();

            Assert.Equal(0, EditorLifecycleTestComponent.GameCreated);
            Assert.Equal(0, EditorLifecycleTestComponent.GameAdded);
            Assert.Equal(0, EditorLifecycleTestComponent.GameUpdated);
            Assert.Equal(1, EditorLifecycleTestComponent.EditorCreated);
            Assert.Equal(1, EditorLifecycleTestComponent.EditorUpdated);
            Assert.Single(entity.GetAllAttachedComponents());
        }

        Assert.Equal(1, EditorLifecycleTestComponent.EditorDestroyed);
        Assert.Equal(0, EditorLifecycleTestComponent.GameDestroyed);
    }

    [Fact]
    public void ReparentCanPreserveWorldTransformAndRejectsCycles()
    {
        using var scene = new TestEditorScene();
        var parent = scene.CreateEntity("parent", createAt: new Microsoft.Xna.Framework.Vector3(10, 20, 0));
        var child = scene.CreateEntity("child", createAt: new Microsoft.Xna.Framework.Vector3(3, 4, 0));
        scene.FlushStructuralChanges();
        var before = child.Transform.WorldPosition;

        child.SetParent(parent, preserveWorldTransform: true);

        Assert.Equal(before, child.Transform.WorldPosition);
        Assert.Throws<InvalidOperationException>(() => parent.SetParent(child, true));
    }

    [Fact]
    public void EditorSceneInvokesAlwaysAndSelectedGizmosWithoutGameplayDraw()
    {
        EditorLifecycleTestComponent.Reset();
        using var scene = new TestEditorScene();
        var entity = scene.CreateEntity("gizmo entity");
        entity.AttachComponent<EditorLifecycleTestComponent>();
        scene.FlushStructuralChanges();

        scene.DrawEditorGizmos(new RecordingGizmoContext(), new HashSet<Guid> { entity.Id });

        Assert.Equal(1, EditorLifecycleTestComponent.GizmosDrawn);
        Assert.Equal(1, EditorLifecycleTestComponent.SelectedGizmosDrawn);
    }

    [Fact]
    public void PointLightRadiusHandleUsesWorldDistanceAndOptionalSnapping()
    {
        var center = new Microsoft.Xna.Framework.Vector2(2f, 3f);
        var handle = new Microsoft.Xna.Framework.Vector2(5f, 7f);

        Assert.Equal(5f, PointLight2DEditorGizmo.CalculateRadius(center, handle, false, 1f));
        Assert.Equal(6f, PointLight2DEditorGizmo.CalculateRadius(center, handle, true, 3f));
    }

    [Fact]
    public void BoxColliderResizeUsesEntityLocalSpaceThroughTransformedParent()
    {
        using var scene = new TestEditorScene();
        var parent = scene.CreateEntity(
            "parent",
            createAt: new Microsoft.Xna.Framework.Vector3(8f, -3f, 0f),
            eulerRotation: new Microsoft.Xna.Framework.Vector3(0f, 0f, MathF.PI / 2f),
            scale: new Microsoft.Xna.Framework.Vector3(2f, 3f, 1f));
        var entity = scene.CreateEntity("collider");
        entity.SetParent(parent, preserveWorldTransform: false);
        scene.FlushStructuralChanges();

        var oppositeLocal = new Microsoft.Xna.Framework.Vector2(1f, 1f);
        var cursorWorld = entity.Transform.TransformPoint2D(
            new Microsoft.Xna.Framework.Vector2(-3f, -1f));
        var resized = BoxColliderEditorGizmo.CalculateResizedShape(
            entity.Transform,
            oppositeLocal,
            cursorWorld,
            false,
            1f);

        Assert.InRange(resized.TopLeft.X, -3.001f, -2.999f);
        Assert.InRange(resized.TopLeft.Y, -1.001f, -0.999f);
        Assert.InRange(resized.BottomRight.X, 0.999f, 1.001f);
        Assert.InRange(resized.BottomRight.Y, 0.999f, 1.001f);
    }

    [Fact]
    public void TransformSelectionExcludesDescendantsWhoseAncestorIsSelected()
    {
        var parentId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        using var document = new SceneDocument(
            new SceneBlueprint
            {
                Name = "Hierarchy",
                Entities =
                [
                    new EntityBlueprint
                    {
                        Name = "Parent",
                        Guid = parentId,
                        Children = [new EntityBlueprint { Name = "Child", Guid = childId }]
                    }
                ]
            },
            null,
            new SelectionService());
        var parent = document.Scene!.FindEntity(parentId)!;
        var child = document.Scene.FindEntity(childId)!;
        document.Selection.Set(child);
        document.Selection.Set(parent, additive: true);

        var states = EditorTransformGizmo.CaptureEditableSelection(document);

        var state = Assert.Single(states);
        Assert.Equal(parentId, state.Id);
    }

    [Fact]
    public void TransformManipulationAnchorMatchesSelectedAncestorFiltering()
    {
        using var scene = new TestEditorScene();
        var root = scene.CreateEntity("Root");
        var parent = scene.CreateEntity("Parent");
        parent.SetParent(root, preserveWorldTransform: false);
        var child = scene.CreateEntity("Child");
        child.SetParent(parent, preserveWorldTransform: false);
        scene.FlushStructuralChanges();

        var anchor = EditorTransformGizmo.ResolveManipulationAnchor(
            child,
            new HashSet<Guid> { root.Id, parent.Id, child.Id });

        Assert.Same(root, anchor);
    }

    [Fact]
    public void TransformSelectionAllowsBoxedRootButNotItsMaterializedChild()
    {
        var sourceRootId = Guid.NewGuid();
        var sourceChildId = Guid.NewGuid();
        var instanceId = Guid.NewGuid();
        var source = new EntityBlueprint
        {
            Name = "Source",
            Guid = sourceRootId,
            Children = [new EntityBlueprint { Name = "Source Child", Guid = sourceChildId }]
        };
        using var document = new SceneDocument(
            new SceneBlueprint
            {
                Name = "Scene",
                Entities =
                [
                    new EntityBlueprint
                    {
                        Name = "Instance",
                        Guid = instanceId,
                        BlueprintInstance = new BlueprintInstanceReference
                        {
                            AssetId = Guid.NewGuid(),
                            AssetName = "source"
                        }
                    }
                ]
            },
            null,
            new SelectionService(),
            blueprintInstanceResolver: _ => source);
        var instance = document.Scene!.FindEntity(instanceId)!;
        var child = Assert.Single(instance.Children);
        document.Selection.Set(child);

        Assert.Empty(EditorTransformGizmo.CaptureEditableSelection(document));
        Assert.False(EditorComponentGizmoSystem.CanEditComponents(document, child));

        document.Selection.Set(instance);
        Assert.Equal(instanceId, Assert.Single(EditorTransformGizmo.CaptureEditableSelection(document)).Id);
        Assert.False(EditorComponentGizmoSystem.CanEditComponents(document, instance));
    }

    [Fact]
    public void ParentAndChildSelectionAppliesMoveOnlyOnceToChild()
    {
        var parentId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        using var document = new SceneDocument(
            new SceneBlueprint
            {
                Name = "Hierarchy",
                Entities =
                [
                    new EntityBlueprint
                    {
                        Name = "Parent",
                        Guid = parentId,
                        Children =
                        [
                            new EntityBlueprint
                            {
                                Name = "Child",
                                Guid = childId,
                                Position = new Microsoft.Xna.Framework.Vector3(2f, 0f, 0f)
                            }
                        ]
                    }
                ]
            },
            null,
            new SelectionService());
        var parent = document.Scene!.FindEntity(parentId)!;
        var child = document.Scene.FindEntity(childId)!;
        document.Selection.Set(child);
        document.Selection.Set(parent, additive: true);
        var states = EditorTransformGizmo.CaptureEditableSelection(document);

        EditorTransformGizmo.ApplyMove(
            document,
            document.Scene,
            states,
            new Microsoft.Xna.Framework.Vector2(3f, 0f));

        Assert.Equal(3f, parent.Transform.WorldPosition.X);
        Assert.Equal(5f, child.Transform.WorldPosition.X);
    }

    [Fact]
    public void PointLightGizmoMutationUsesDocumentUndoPath()
    {
        var entityId = Guid.NewGuid();
        using var document = new SceneDocument(
            new SceneBlueprint
            {
                Name = "Light",
                Entities =
                [
                    new EntityBlueprint
                    {
                        Name = "Light",
                        Guid = entityId,
                        Components = [new ComponentBlueprint { Type = nameof(PointLight2D) }]
                    }
                ]
            },
            null,
            new SelectionService());
        var transaction = document.BeginTransaction("Resize Point Light");
        transaction.Update(scene => PointLight2DEditorGizmo.ApplyResize(
            document,
            scene,
            entityId,
            new Microsoft.Xna.Framework.Vector2(7f, 0f),
            false,
            1f));
        transaction.Commit();

        Assert.Equal(7f, document.Scene!.FindEntity(entityId)!.GetComponent<PointLight2D>()!.Radius);
        Assert.True(document.Undo.CanUndo);

        document.Undo.Undo();
        Assert.Equal(0f, document.Scene.FindEntity(entityId)!.GetComponent<PointLight2D>()!.Radius);
    }

    [Fact]
    public void SelectedPointLightDrawsItsRadiusCircle()
    {
        using var scene = new TestEditorScene();
        var entity = scene.CreateEntity("point light");
        entity.AttachComponent<PointLight2D>().Radius = 4.5f;
        scene.FlushStructuralChanges();
        var context = new RecordingGizmoContext();

        scene.DrawEditorGizmos(context, new HashSet<Guid> { entity.Id });

        Assert.Equal(1, context.CircleCount);
        Assert.Equal(4.5f, context.LastCircleRadius);
    }

    [Fact]
    public void SceneDocumentUndoAndSavePreserveMissingComponentPayload()
    {
        var scenePath = Path.Combine(_root, "level.scene.json");
        var entityId = Guid.NewGuid();
        var source = new SceneBlueprint
        {
            Name = "Level",
            Entities =
            [
                new EntityBlueprint
                {
                    Name = "Original",
                    Guid = entityId,
                    Components =
                    [
                        new ComponentBlueprint
                        {
                            Type = "Removed.GameComponent",
                            Properties = new Dictionary<string, JToken>
                            {
                                ["UnrecoverableData"] = new JObject { ["answer"] = 42 }
                            }
                        }
                    ]
                }
            ]
        };
        File.WriteAllText(scenePath, DreambitJson.Serialize(source));
        var selection = new SelectionService();
        using var document = SceneDocument.Open(scenePath, selection);
        var entity = document.Scene!.FindEntity(entityId)!;

        document.Rename(entity, "Renamed");
        Assert.Equal("Renamed", document.Scene.FindEntity(entityId)!.Name);
        Assert.True(document.Undo.Undo());
        Assert.Equal("Original", document.Scene.FindEntity(entityId)!.Name);
        Assert.True(document.Undo.Redo());

        document.Save();
        var saved = SceneDocumentSerializer.Deserialize(File.ReadAllText(scenePath));
        var missing = Assert.Single(Assert.Single(saved.Entities).Components);
        Assert.Equal("Removed.GameComponent", missing.Type);
        Assert.Equal(42, missing.Properties["UnrecoverableData"]!["answer"]!.Value<int>());
    }

    [Fact]
    public void EditorPreservesInvalidKnownComponentMembersUntilDeliberatelyChanged()
    {
        var scenePath = Path.Combine(_root, "reload-safe.scene.json");
        var entityId = Guid.NewGuid();
        var missingTarget = Guid.NewGuid();
        var componentType = $"{typeof(EditorReloadSafetyComponent).Assembly.GetName().Name}." +
                            nameof(EditorReloadSafetyComponent);
        File.WriteAllText(scenePath, DreambitJson.Serialize(new SceneBlueprint
        {
            Name = "Reload Safety",
            Entities =
            [
                new EntityBlueprint
                {
                    Name = "Player",
                    Guid = entityId,
                    Components =
                    [
                        new ComponentBlueprint
                        {
                            Type = componentType,
                            Properties = new Dictionary<string, JToken>
                            {
                                [nameof(EditorReloadSafetyComponent.Count)] = new JValue("not-a-number"),
                                [nameof(EditorReloadSafetyComponent.Target)] = new JValue(missingTarget),
                                ["RetiredMember"] = new JObject { ["stillHere"] = true }
                            }
                        }
                    ]
                }
            ]
        }));

        var selection = new SelectionService();
        using var document = SceneDocument.Open(scenePath, selection);
        var component = document.Scene!.FindEntity(entityId)!
            .GetComponent<EditorReloadSafetyComponent>()!;
        Assert.Contains(nameof(EditorReloadSafetyComponent.Count), component.EditorSerializationFailures);
        Assert.Contains(nameof(EditorReloadSafetyComponent.Target), component.EditorSerializationFailures);
        Assert.Contains("RetiredMember", component.EditorSerializationFailures);

        document.Apply("Replace invalid count", _ =>
        {
            component.Count = 7;
            component.AcknowledgeEditorSerializationFailure(nameof(EditorReloadSafetyComponent.Count));
        });
        document.Save();

        var saved = SceneDocumentSerializer.Deserialize(File.ReadAllText(scenePath));
        var properties = Assert.Single(Assert.Single(saved.Entities).Components).Properties;
        Assert.Equal(7, properties[nameof(EditorReloadSafetyComponent.Count)]!.Value<int>());
        Assert.Equal(missingTarget.ToString(), properties[nameof(EditorReloadSafetyComponent.Target)]!.Value<string>());
        Assert.True(properties["RetiredMember"]!["stillHere"]!.Value<bool>());
    }

    [Fact]
    public void EditorPreservesKnownComponentPayloadWhenConstructionFails()
    {
        var entityId = Guid.NewGuid();
        var componentType = SceneDocumentSerializer.GetComponentTypeId(
            typeof(EditorConstructionFailureComponent));
        EditorConstructionFailureComponent.FailConstruction = true;
        try
        {
            using var document = new SceneDocument(
                new SceneBlueprint
                {
                    Name = "Construction Failure",
                    Entities =
                    [
                        new EntityBlueprint
                        {
                            Name = "Before",
                            Guid = entityId,
                            Components =
                            [
                                new ComponentBlueprint
                                {
                                    Type = componentType,
                                    Properties = new Dictionary<string, JToken>
                                    {
                                        [nameof(EditorConstructionFailureComponent.Value)] = new JValue(42)
                                    }
                                }
                            ]
                        }
                    ]
                },
                null,
                new SelectionService());
            var entity = document.Scene!.FindEntity(entityId)!;
            Assert.Null(entity.GetComponent<EditorConstructionFailureComponent>());

            document.Rename(entity, "After");

            var captured = document.CaptureSingleRoot();
            var preserved = Assert.Single(captured.Components);
            Assert.Equal(componentType, preserved.Type);
            Assert.Equal(42, preserved.Properties[nameof(EditorConstructionFailureComponent.Value)]!.Value<int>());
        }
        finally
        {
            EditorConstructionFailureComponent.FailConstruction = false;
        }
    }

    [Fact]
    public void DeliberatelyDetachedKnownComponentIsNotResurrected()
    {
        var entityId = Guid.NewGuid();
        var componentType = SceneDocumentSerializer.GetComponentTypeId(
            typeof(EditorConstructionFailureComponent));
        using var document = new SceneDocument(
            new SceneBlueprint
            {
                Name = "Deliberate Removal",
                Entities =
                [
                    new EntityBlueprint
                    {
                        Name = "Entity",
                        Guid = entityId,
                        Components =
                        [
                            new ComponentBlueprint
                            {
                                Type = componentType,
                                Properties = new Dictionary<string, JToken>
                                {
                                    [nameof(EditorConstructionFailureComponent.Value)] = new JValue(7)
                                }
                            }
                        ]
                    }
                ]
            },
            null,
            new SelectionService());
        var entity = document.Scene!.FindEntity(entityId)!;
        var component = entity.GetComponent<EditorConstructionFailureComponent>()!;

        document.Apply("Remove Component", _ => entity.DetachComponent(component));

        Assert.Empty(document.CaptureSingleRoot().Components);
        Assert.True(document.Undo.Undo());
        Assert.NotNull(document.Scene!.FindEntity(entityId)!
            .GetComponent<EditorConstructionFailureComponent>());
        Assert.Single(document.CaptureSingleRoot().Components);
        Assert.True(document.Undo.Redo());
        Assert.Empty(document.CaptureSingleRoot().Components);
    }

    [Fact]
    public void CaptureDropsDuplicateKnownComponentsWhenOneLiveComponentMatches()
    {
        var componentType = SceneDocumentSerializer.GetComponentTypeId(
            typeof(EditorConstructionFailureComponent));
        using var document = new SceneDocument(
            new SceneBlueprint
            {
                Name = "Duplicate Cleanup",
                Entities =
                [
                    new EntityBlueprint
                    {
                        Name = "Entity",
                        Guid = Guid.NewGuid(),
                        Components =
                        [
                            new ComponentBlueprint
                            {
                                Type = componentType,
                                Properties = new Dictionary<string, JToken>
                                {
                                    [nameof(EditorConstructionFailureComponent.Value)] = new JValue(1)
                                }
                            },
                            new ComponentBlueprint
                            {
                                Type = componentType,
                                Properties = new Dictionary<string, JToken>
                                {
                                    [nameof(EditorConstructionFailureComponent.Value)] = new JValue(2)
                                }
                            }
                        ]
                    }
                ]
            },
            null,
            new SelectionService());

        var captured = document.CaptureSingleRoot();
        var component = Assert.Single(captured.Components);
        Assert.Equal(2, component.Properties[nameof(EditorConstructionFailureComponent.Value)]!.Value<int>());
    }

    [Fact]
    public void AssemblyReloadRehydratesTheSceneAndRestoresSelectionByStableId()
    {
        var selection = new SelectionService();
        using var document = SceneDocument.CreateNew("Reload", selection);
        var entity = document.CreateEmpty("Selected");
        var id = entity.Id;
        Assert.True(selection.Contains(entity));

        document.BeforeAssemblyReload();
        Assert.False(document.HasLiveScene);
        document.AfterAssemblyReload();

        var restored = document.Scene!.FindEntity(id);
        Assert.NotNull(restored);
        Assert.True(selection.Contains(restored!));
    }

    [Fact]
    public void BoxedBlueprintTracksSourceUntilItIsUnboxed()
    {
        var source = new EntityBlueprint
        {
            AssetId = AssetId.New(),
            AssetName = "actors/hero.blueprint",
            Name = "Hero",
            Guid = Guid.NewGuid(),
            Position = new Microsoft.Xna.Framework.Vector3(2, 3, 0)
        };
        var selection = new SelectionService();
        using var document = SceneDocument.CreateNew(
            "Linked",
            selection,
            blueprintInstanceResolver: _ => source);

        var instance = document.InstantiateBlueprint(
            source,
            new Microsoft.Xna.Framework.Vector3(20, 30, 0));
        var instanceId = instance.Id;
        Assert.True(document.IsBlueprintInstanceRoot(instance));
        Assert.Equal(new Microsoft.Xna.Framework.Vector3(20, 30, 0), instance.Transform.WorldPosition);

        var scenePath = Path.Combine(_root, "boxed.scene.json");
        document.Save(scenePath);
        var boxedSource = Assert.Single(SceneDocumentSerializer.Deserialize(File.ReadAllText(scenePath)).Entities);
        Assert.NotNull(boxedSource.BlueprintInstance);
        Assert.Equal(source.AssetId.Value, boxedSource.BlueprintInstance.AssetId);
        Assert.Empty(boxedSource.Components);
        Assert.Empty(boxedSource.Children);

        var duplicate = document.Duplicate(instance);
        Assert.True(document.IsBlueprintInstanceRoot(duplicate));
        Assert.NotEqual(instance.Id, duplicate.Id);
        document.Delete([duplicate]);

        var childSourceId = Guid.NewGuid();
        source.Name = "Hero Updated";
        source.Children.Add(new EntityBlueprint
        {
            Name = "New Source Child",
            Guid = childSourceId
        });

        document.RefreshBlueprintInstances();
        var refreshed = document.Scene!.FindEntity(instanceId)!;
        var childId = Assert.Single(refreshed.Children).Id;
        Assert.Equal("Hero Updated", refreshed.Name);
        Assert.Equal(new Microsoft.Xna.Framework.Vector3(20, 30, 0), refreshed.Transform.WorldPosition);

        document.BeforeAssemblyReload();
        document.AfterAssemblyReload();
        refreshed = document.Scene!.FindEntity(instanceId)!;
        Assert.Equal(childId, Assert.Single(refreshed.Children).Id);

        document.UnboxBlueprint(refreshed);
        Assert.False(document.IsBlueprintInstanceRoot(refreshed));
        source.Name = "Future Source Name";
        source.Children.Clear();
        document.BeforeAssemblyReload();
        document.AfterAssemblyReload();

        var unboxed = document.Scene!.FindEntity(instanceId)!;
        Assert.Equal("Hero Updated", unboxed.Name);
        Assert.Equal(childId, Assert.Single(unboxed.Children).Id);
    }

    [Fact]
    public void BlueprintMaterializationOnlyRemapsTypedEntityReferences()
    {
        var sourceRootId = Guid.NewGuid();
        var source = new EntityBlueprint
        {
            AssetId = AssetId.New(),
            AssetName = "actors/reference-test.blueprint",
            Name = "Reference Test",
            Guid = sourceRootId,
            Children =
            [
                new EntityBlueprint
                {
                    Name = "Child",
                    Guid = Guid.NewGuid(),
                    Components =
                    [
                        new ComponentBlueprint
                        {
                            Type = $"{typeof(EditorReloadSafetyComponent).Assembly.GetName().Name}." +
                                   nameof(EditorReloadSafetyComponent),
                            Properties = new Dictionary<string, JToken>
                            {
                                [nameof(EditorReloadSafetyComponent.Target)] = sourceRootId.ToString(),
                                [nameof(EditorReloadSafetyComponent.StableGuid)] = sourceRootId.ToString(),
                                [nameof(EditorReloadSafetyComponent.Label)] = sourceRootId.ToString()
                            }
                        }
                    ]
                }
            ]
        };
        using var document = SceneDocument.CreateNew(
            "References",
            new SelectionService(),
            blueprintInstanceResolver: _ => source);

        var instance = document.InstantiateBlueprint(source);
        var component = Assert.Single(instance.Children)
            .GetComponent<EditorReloadSafetyComponent>()!;

        Assert.Same(instance, component.Target);
        Assert.Equal(sourceRootId, component.StableGuid);
        Assert.Equal(sourceRootId.ToString(), component.Label);
    }

    [Fact]
    public void DirtyStateTracksTheLastSuccessfulSceneSaveAcrossUndoAndRedo()
    {
        var scenePath = Path.Combine(_root, "dirty-baseline.scene.json");
        var entityId = Guid.NewGuid();
        File.WriteAllText(scenePath, DreambitJson.Serialize(new SceneBlueprint
        {
            Name = "Dirty Baseline",
            Entities = [new EntityBlueprint { Name = "Before", Guid = entityId }]
        }));
        using var document = SceneDocument.Open(scenePath, new SelectionService());

        document.Rename(document.Scene!.FindEntity(entityId)!, "After");
        Assert.True(document.IsDirty);
        document.Save();
        Assert.False(document.IsDirty);

        Assert.True(document.Undo.Undo());
        Assert.True(document.IsDirty);
        Assert.Equal("Before", document.Scene!.FindEntity(entityId)!.Name);

        Assert.True(document.Undo.Redo());
        Assert.False(document.IsDirty);
        Assert.Equal("After", document.Scene!.FindEntity(entityId)!.Name);
    }

    [Fact]
    public void FailedSceneMutationRestoresThePreviousSnapshotWithoutHistory()
    {
        var entityId = Guid.NewGuid();
        using var document = new SceneDocument(
            new SceneBlueprint
            {
                Name = "Atomic Mutation",
                Entities = [new EntityBlueprint { Name = "Before", Guid = entityId }]
            },
            null,
            new SelectionService());
        var changed = 0;
        document.Changed += _ => changed++;

        Assert.Throws<InvalidOperationException>(() => document.Apply("Fail", scene =>
        {
            scene.FindEntity(entityId)!.Name = "Partial";
            throw new InvalidOperationException("Mutation failed.");
        }));

        Assert.Equal("Before", document.Scene!.FindEntity(entityId)!.Name);
        Assert.False(document.IsDirty);
        Assert.False(document.Undo.CanUndo);
        Assert.Equal(0, changed);
    }

    [Fact]
    public void NoOpSceneTransactionDoesNotDirtyOrRecordHistory()
    {
        var entityId = Guid.NewGuid();
        using var document = new SceneDocument(
            new SceneBlueprint
            {
                Name = "No-op Transaction",
                Entities = [new EntityBlueprint { Name = "Original", Guid = entityId }]
            },
            null,
            new SelectionService());
        var entity = document.Scene!.FindEntity(entityId)!;
        var changed = 0;
        document.Changed += _ => changed++;

        using (var transaction = document.BeginTransaction("Temporary Rename"))
        {
            transaction.Update(_ => entity.Name = "Temporary");
            transaction.Update(_ => entity.Name = "Original");
        }

        Assert.False(document.IsDirty);
        Assert.False(document.Undo.CanUndo);
        Assert.Equal(0, changed);
    }

    [Fact]
    public void AssemblyReloadRollsBackActiveTransactionBeforeCapturingSource()
    {
        var entityId = Guid.NewGuid();
        using var document = new SceneDocument(
            new SceneBlueprint
            {
                Name = "Reload During Gesture",
                Entities = [new EntityBlueprint { Name = "Before", Guid = entityId }]
            },
            null,
            new SelectionService());
        document.Selection.Set(document.Scene!.FindEntity(entityId));
        var changed = 0;
        document.Changed += _ => changed++;
        using var transaction = document.BeginTransaction("Rename Gesture");
        transaction.Update(scene => scene.FindEntity(entityId)!.Name = "Uncommitted");
        Assert.Equal("Uncommitted", document.Scene.FindEntity(entityId)!.Name);

        document.BeforeAssemblyReload();
        Assert.Null(document.Scene);
        Assert.False(document.IsDirty);
        Assert.False(document.Undo.CanUndo);
        Assert.Equal(0, changed);

        document.AfterAssemblyReload();
        Assert.Equal("Before", document.Scene!.FindEntity(entityId)!.Name);
        Assert.Equal(entityId, Assert.Single(document.Selection.EntityIds));
        Assert.False(document.IsDirty);
        Assert.False(document.Undo.CanUndo);
        Assert.Equal(0, changed);

        // The stale interaction was finished and unregistered by the document, so a
        // fresh viewport interaction can begin after reload.
        using var next = document.BeginTransaction("Next Gesture");
        next.Abandon();
    }

    [Fact]
    public void SceneDocumentOwnsAtMostOneActiveTransactionAndFinishPathsReleaseIt()
    {
        using var document = SceneDocument.CreateNew("Transactions", new SelectionService());
        var first = document.BeginTransaction("First");
        Assert.Throws<InvalidOperationException>(() => document.BeginTransaction("Overlapping"));
        first.Abandon();

        var second = document.BeginTransaction("Second");
        second.Cancel();
        var third = document.BeginTransaction("Third");
        third.Commit();
        var fourth = document.BeginTransaction("Fourth");

        document.Dispose();
        fourth.Abandon();
        fourth.Dispose();
    }

    [Fact]
    public void ContinuousSceneAppliesWithSameKeyUndoToTheFirstSnapshot()
    {
        var entityId = Guid.NewGuid();
        using var document = new SceneDocument(
            new SceneBlueprint
            {
                Name = "Continuous Scene Edit",
                Entities = [new EntityBlueprint { Name = "Entity", Guid = entityId }]
            },
            null,
            new SelectionService());

        foreach (var x in new[] { 1f, 2f, 3f })
        {
            document.Apply(
                "Change Position",
                scene => scene.FindEntity(entityId)!.Transform.Position =
                    new Microsoft.Xna.Framework.Vector3(x, 0f, 0f),
                "Transform.Position");
        }

        Assert.True(document.Undo.Undo());
        Assert.Equal(
            Microsoft.Xna.Framework.Vector3.Zero,
            document.Scene!.FindEntity(entityId)!.Transform.Position);
        Assert.False(document.Undo.CanUndo);
        Assert.True(document.Undo.Redo());
        Assert.Equal(
            new Microsoft.Xna.Framework.Vector3(3f, 0f, 0f),
            document.Scene!.FindEntity(entityId)!.Transform.Position);
    }

    [Fact]
    public void ExternallyOwnedSceneDocumentPublishesChangesWithoutOwningDirtyOrUndoState()
    {
        var entityId = Guid.NewGuid();
        using var document = new SceneDocument(
            new SceneBlueprint
            {
                Name = "Blueprint Host",
                Entities = [new EntityBlueprint { Name = "Before", Guid = entityId }]
            },
            null,
            new SelectionService(),
            historyOwnership: SceneDocumentHistoryOwnership.External);
        var changed = 0;
        document.Changed += _ => changed++;

        document.Rename(document.Scene!.FindEntity(entityId)!, "After");

        Assert.Equal("After", document.CaptureSingleRoot().Name);
        Assert.False(document.IsDirty);
        Assert.False(document.Undo.CanUndo);
        Assert.Equal(1, changed);
    }

    [Fact]
    public void FailedUndoKeepsTheExistingLiveSceneAndUndoEntry()
    {
        var instanceId = Guid.NewGuid();
        var linked = new EntityBlueprint
        {
            AssetId = AssetId.New(),
            AssetName = "linked",
            Name = "Linked",
            Guid = Guid.NewGuid()
        };
        var failResolution = false;
        using var document = new SceneDocument(
            new SceneBlueprint
            {
                Name = "Atomic Restore",
                Entities =
                [
                    new EntityBlueprint
                    {
                        Name = "Instance",
                        Guid = instanceId,
                        BlueprintInstance = new BlueprintInstanceReference
                        {
                            AssetId = linked.AssetId.Value,
                            AssetName = linked.AssetName
                        }
                    }
                ]
            },
            null,
            new SelectionService(),
            blueprintInstanceResolver: _ => failResolution
                ? throw new InvalidOperationException("Resolver unavailable.")
                : linked);
        document.Apply("Move Instance", scene =>
            scene.FindEntity(instanceId)!.Transform.Position =
                new Microsoft.Xna.Framework.Vector3(4, 5, 0));
        var workingScene = document.Scene;
        failResolution = true;

        Assert.Throws<InvalidOperationException>(() => document.Undo.Undo());

        Assert.Same(workingScene, document.Scene);
        Assert.Equal(
            new Microsoft.Xna.Framework.Vector3(4, 5, 0),
            document.Scene!.FindEntity(instanceId)!.Transform.Position);
        Assert.True(document.Undo.CanUndo);
        Assert.False(document.Undo.CanRedo);
    }

    [Fact]
    public void UnboxingOuterBlueprintKeepsNestedInstanceAuthoredAndBoxed()
    {
        var innerId = AssetId.New();
        var outerId = AssetId.New();
        var inner = new EntityBlueprint
        {
            AssetId = innerId,
            AssetName = "blueprints/inner",
            Name = "Inner",
            Guid = Guid.NewGuid(),
            Children = [new EntityBlueprint { Name = "Materialized Inner Child", Guid = Guid.NewGuid() }]
        };
        var nestedSourceId = Guid.NewGuid();
        var outer = new EntityBlueprint
        {
            AssetId = outerId,
            AssetName = "blueprints/outer",
            Name = "Outer",
            Guid = Guid.NewGuid(),
            Children =
            [
                new EntityBlueprint
                {
                    Name = "Nested Inner",
                    Guid = nestedSourceId,
                    BlueprintInstance = new BlueprintInstanceReference
                    {
                        AssetId = innerId.Value,
                        AssetName = inner.AssetName
                    }
                }
            ]
        };
        using var document = SceneDocument.CreateNew(
            "Nested Unbox",
            new SelectionService(),
            blueprintInstanceResolver: reference => reference.AssetId == outerId.Value ? outer : inner);
        var instance = document.InstantiateBlueprint(outer);
        var nestedLive = Assert.Single(instance.Children);
        Assert.Single(nestedLive.Children);

        document.UnboxBlueprint(instance);
        var authored = document.CaptureSingleRoot();
        var nested = Assert.Single(authored.Children);

        Assert.Null(authored.BlueprintInstance);
        Assert.NotNull(nested.BlueprintInstance);
        Assert.Equal(innerId.Value, nested.BlueprintInstance.AssetId);
        Assert.Equal(inner.AssetName, nested.BlueprintInstance.AssetName);
        Assert.Empty(nested.Children);
        Assert.Equal(nestedLive.Id, nested.Guid);
        Assert.Equal(
            authored.FlattenedHierarchy().Count(),
            authored.FlattenedHierarchy().Select(entity => entity.Guid).Distinct().Count());
    }

    [Fact]
    public void LDtkSceneLinkSurvivesCaptureWhileGeneratedEntitiesStayOutOfTheSceneFile()
    {
        var assetId = Guid.NewGuid();
        var worldId = Guid.NewGuid();
        var source = new SceneBlueprint
        {
            Name = "LDtk World",
            LDtk = new LDtkSceneReference
            {
                AssetId = assetId,
                AssetName = "maps/world",
                WorldIid = worldId,
                PixelsPerUnit = 16f
            }
        };
        using var scene = new TestEditorScene();
        scene.EnsureEditorCamera();
        scene.CreateEntity("Dreambit Placed");

        var captured = SceneDocumentSerializer.Capture(scene, source, source.Name);
        var restored = SceneDocumentSerializer.Deserialize(SceneDocumentSerializer.Serialize(captured));

        Assert.NotNull(restored.LDtk);
        Assert.Equal(assetId, restored.LDtk.AssetId);
        Assert.Equal("maps/world", restored.LDtk.AssetName);
        Assert.Equal(worldId, restored.LDtk.WorldIid);
        Assert.Equal(16f, restored.LDtk.PixelsPerUnit);
        Assert.Equal("Dreambit Placed", Assert.Single(restored.Entities).Name);

        var legacy = SceneDocumentSerializer.Deserialize("""
        {
          "name": "Legacy LDtk",
          "entities": [],
          "ldtk": {
            "asset": "maps/world",
            "pixels_per_unit": 24
          }
        }
        """);
        Assert.Equal(24f, legacy.LDtk!.ImportOptions.PixelsPerUnit);
    }

    [Fact]
    public void LDtkSourceLoaderProducesRuntimeLogicalAssetNames()
    {
        var contentRoot = Path.Combine(_root, "Assets");
        var maps = Path.Combine(contentRoot, "maps");
        Directory.CreateDirectory(maps);
        var path = Path.Combine(maps, "world.ldtk");
        File.WriteAllText(path, "{\"jsonVersion\":\"1.5.3\",\"levels\":[]}");

        var project = LDtkFile.FromContentFile(path, "maps/world", contentRoot);

        Assert.Equal("maps/world", project.SourcePath);
        Assert.Equal("textures/tiles", project.ResolveAssetName("../textures/tiles.png"));
        Assert.Empty(project.LoadWorld().Levels);
    }

    [Fact]
    public void LDtkSceneLoadsExternalLevelFilesAndRendersTheirTransientDrawables()
    {
        var contentRoot = Path.Combine(_root, "Assets");
        var maps = Path.Combine(contentRoot, "maps");
        var levels = Path.Combine(maps, "Levels");
        Directory.CreateDirectory(levels);
        var worldId = Guid.NewGuid();
        var levelId = Guid.NewGuid();
        var layerId = Guid.NewGuid();
        var projectPath = Path.Combine(maps, "world.ldtk");
        var levelPath = Path.Combine(levels, "Forest.ldtkl");
        File.WriteAllText(projectPath, $$"""
        {
          "jsonVersion": "1.5.3",
          "externalLevels": true,
          "worlds": [{
            "identifier": "ForestWorld",
            "iid": "{{worldId}}",
            "worldLayout": "Free",
            "worldGridWidth": 32,
            "worldGridHeight": 32,
            "levels": [{
              "__bgColor": "#123456",
              "identifier": "Forest",
              "iid": "{{levelId}}",
              "uid": 1,
              "pxWid": 32,
              "pxHei": 32,
              "worldX": 0,
              "worldY": 0,
              "worldDepth": 0,
              "externalRelPath": "Levels/Forest.ldtkl",
              "fieldInstances": [],
              "__neighbours": [],
              "layerInstances": null
            }]
          }]
        }
        """);
        File.WriteAllText(levelPath, $$"""
        {
          "__bgColor": "#123456",
          "identifier": "Forest",
          "iid": "{{levelId}}",
          "uid": 1,
          "pxWid": 32,
          "pxHei": 32,
          "worldX": 0,
          "worldY": 0,
          "worldDepth": 0,
          "externalRelPath": "Levels/Forest.ldtkl",
          "fieldInstances": [],
          "__neighbours": [],
          "layerInstances": [{
            "__identifier": "GameplayMarkers",
            "iid": "{{layerId}}",
            "__cHei": 1,
            "__cWid": 1,
            "__gridSize": 16,
            "__opacity": 1,
            "__pxTotalOffsetX": 0,
            "__pxTotalOffsetY": 0,
            "__tilesetDefUid": null,
            "__tilesetRelPath": null,
            "__type": "Entities",
            "autoLayerTiles": [],
            "entityInstances": [],
            "gridTiles": [],
            "intGridCsv": [],
            "layerDefUid": 1,
            "levelId": 1,
            "pxOffsetX": 0,
            "pxOffsetY": 0,
            "visible": true
          }]
        }
        """);

        var selection = new SelectionService();
        using var document = SceneDocument.CreateNew(
            "External LDtk",
            selection,
            ldtkProjectResolver: _ => LDtkFile.FromContentFile(
                projectPath,
                "maps/world",
                contentRoot),
            ldtk: new LDtkSceneReference
            {
                AssetName = "maps/world",
                WorldIid = worldId
            });

        var generated = document.Scene!.GetAllEntities()
            .Where(entity => entity.IsEditorOnly)
            .ToArray();
        Assert.Contains(generated, entity => entity.Name == "LDtk Level: Forest");
        Assert.Contains(generated, entity => entity.Name.Contains("GameplayMarkers"));
        var background = Assert.Single(
            generated.SelectMany(entity => entity.GetAllComponents()).OfType<FilledRectDrawer>());
        Assert.True(SceneViewportRenderer.ShouldRenderDrawable(background));
        Assert.All(generated, entity => Assert.True(entity.IsLDtkGenerated));

        var placed = document.CreateEmpty("Dreambit Placed");
        var placedId = placed.Id;
        document.Apply("Move Dreambit Entity", _ =>
            placed.Transform.Position = new Microsoft.Xna.Framework.Vector3(12, 34, 0));
        document.Apply("Override LDtk Background", _ =>
        {
            background.Entity.Transform.Position = new Microsoft.Xna.Framework.Vector3(5, 7, 0);
            background.Entity.Tags.Clear();
            background.Entity.Tags.Add("editor-override");
            background.Width = 99f;
            document.RecordLDtkPosition(background.Entity);
            document.RecordLDtkEntityTags(background.Entity);
            document.RecordLDtkComponentMember(background, nameof(FilledRectDrawer.Width), background.Width);
        });

        File.WriteAllText(levelPath, File.ReadAllText(levelPath)
            .Replace("\"identifier\": \"Forest\"", "\"identifier\": \"Forest Updated\"")
            .Replace("\"pxWid\": 32", "\"pxWid\": 64"));
        document.ReimportLDtk();

        var preserved = document.Scene!.FindEntity(placedId);
        Assert.NotNull(preserved);
        Assert.Equal(
            new Microsoft.Xna.Framework.Vector3(12, 34, 0),
            preserved.Transform.Position);
        Assert.Contains(
            document.Scene.GetAllEntities(),
            entity => entity.Name == "LDtk Level: Forest Updated");
        var reimportedBackground = Assert.Single(
            document.Scene.GetAllEntities()
                .SelectMany(entity => entity.GetAllComponents())
                .OfType<FilledRectDrawer>());
        Assert.Equal(new Microsoft.Xna.Framework.Vector3(5, 7, 0), reimportedBackground.Entity.Transform.Position);
        Assert.Contains("editor-override", reimportedBackground.Entity.Tags);
        Assert.Equal(99f, reimportedBackground.Width);

        document.UpdateLDtkImportOptions("Disable LDtk Background", options =>
        {
            options.PixelsPerUnit = 16f;
            options.RenderLevelBackgroundColor = false;
        });
        Assert.Equal(16f, document.LDtkReference!.ImportOptions.PixelsPerUnit);
        Assert.Empty(document.Scene.GetAllEntities()
            .SelectMany(entity => entity.GetAllComponents())
            .OfType<FilledRectDrawer>());
        Assert.NotNull(document.Scene.FindEntity(placedId));

        var validExternalLevel = File.ReadAllText(levelPath);
        var workingScene = document.Scene;
        File.WriteAllText(levelPath, "{ incomplete LDtk save");
        Assert.ThrowsAny<Exception>(() => document.ReimportLDtk());
        Assert.Same(workingScene, document.Scene);
        Assert.NotNull(document.Scene.FindEntity(placedId));
        File.WriteAllText(levelPath, validExternalLevel);
        document.ReimportLDtk();

        var captured = SceneDocumentSerializer.Capture(
            document.Scene,
            new SceneBlueprint
            {
                Name = "External LDtk",
                LDtk = document.LDtkReference
            },
            "External LDtk");
        Assert.Single(captured.Entities);
        Assert.Equal("Dreambit Placed", captured.Entities[0].Name);
        Assert.NotEmpty(captured.LDtk!.EntityOverrides);
        var roundTripped = SceneDocumentSerializer.Deserialize(SceneDocumentSerializer.Serialize(captured));
        Assert.Equal(16f, roundTripped.LDtk!.ImportOptions.PixelsPerUnit);
        Assert.Contains(
            roundTripped.LDtk.EntityOverrides.Values,
            item => item.Position == new Microsoft.Xna.Framework.Vector3(5, 7, 0));
        Assert.Contains(
            roundTripped.LDtk.EntityOverrides.Values,
            item => item.Tags?.Contains("editor-override") == true);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_root))
                Directory.Delete(_root, true);
        }
        catch (IOException)
        {
        }
    }

    private sealed class TestEditorScene : Scene
    {
        public TestEditorScene() : base(SceneExecutionMode.Editor)
        {
        }
    }

    private sealed class RecordingGizmoContext : IEditorGizmoContext
    {
        public int CircleCount { get; private set; }
        public float LastCircleRadius { get; private set; }

        public void Line(Microsoft.Xna.Framework.Vector2 from, Microsoft.Xna.Framework.Vector2 to, Microsoft.Xna.Framework.Color color, float thickness = 1) { }
        public void Circle(Microsoft.Xna.Framework.Vector2 center, float radius, Microsoft.Xna.Framework.Color color, float thickness = 1)
        {
            CircleCount++;
            LastCircleRadius = radius;
        }
        public void Rectangle(RectangleF rectangle, Microsoft.Xna.Framework.Color color, float thickness = 1) { }
        public void Label(Microsoft.Xna.Framework.Vector2 position, string text, Microsoft.Xna.Framework.Color color) { }
        public void ShowIcon(string icon, Vector2 position, Color color, float size = 24)
        {
            
        }

        public void RadiusHandle(Component component, string memberName, Vector2 center, Color color, float thickness = 1)
        {
            
        }

        public void BoxHandle(Component component, string memberName, Color color, float thickness = 1)
        {
            
        }
    }
}

public sealed class EditorLifecycleTestComponent : Component
{
    public static int GameCreated { get; private set; }
    public static int GameAdded { get; private set; }
    public static int GameUpdated { get; private set; }
    public static int GameDestroyed { get; private set; }
    public static int EditorCreated { get; private set; }
    public static int EditorUpdated { get; private set; }
    public static int EditorDestroyed { get; private set; }
    public static int GizmosDrawn { get; private set; }
    public static int SelectedGizmosDrawn { get; private set; }

    public static void Reset() =>
        (GameCreated, GameAdded, GameUpdated, GameDestroyed,
            EditorCreated, EditorUpdated, EditorDestroyed,
            GizmosDrawn, SelectedGizmosDrawn) = (0, 0, 0, 0, 0, 0, 0, 0, 0);

    public override void OnCreated() => GameCreated++;
    public override void OnAddedToEntity() => GameAdded++;
    public override void OnUpdate() => GameUpdated++;
    public override void OnDestroyed() => GameDestroyed++;
    public override void OnEditorCreated() => EditorCreated++;
    public override void OnEditorUpdate() => EditorUpdated++;
    public override void OnEditorDestroyed() => EditorDestroyed++;
    public override void OnEditorDrawGizmos(IEditorGizmoContext context) => GizmosDrawn++;
    public override void OnEditorDrawGizmosSelected(IEditorGizmoContext context) => SelectedGizmosDrawn++;
}

public sealed class EditorReloadSafetyComponent : Component
{
    [DreambitSerialize]
    public int Count { get; set; }

    [DreambitSerialize]
    public Entity? Target { get; set; }

    [DreambitSerialize]
    public Guid StableGuid { get; set; }

    [DreambitSerialize]
    public string Label { get; set; } = string.Empty;
}

public sealed class EditorConstructionFailureComponent : Component
{
    public static bool FailConstruction { get; set; }

    public EditorConstructionFailureComponent()
    {
        if (FailConstruction)
            throw new InvalidOperationException("Intentional editor construction failure.");
    }

    [DreambitSerialize]
    public int Value { get; set; }
}
