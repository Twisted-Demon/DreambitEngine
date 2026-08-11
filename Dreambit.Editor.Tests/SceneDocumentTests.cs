using Dreambit.ECS;
using Dreambit.Editor.Scenes;
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
        public void Line(Microsoft.Xna.Framework.Vector2 from, Microsoft.Xna.Framework.Vector2 to, Microsoft.Xna.Framework.Color color, float thickness = 1) { }
        public void Circle(Microsoft.Xna.Framework.Vector2 center, float radius, Microsoft.Xna.Framework.Color color, float thickness = 1) { }
        public void Rectangle(RectangleF rectangle, Microsoft.Xna.Framework.Color color, float thickness = 1) { }
        public void Label(Microsoft.Xna.Framework.Vector2 position, string text, Microsoft.Xna.Framework.Color color) { }
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
}
