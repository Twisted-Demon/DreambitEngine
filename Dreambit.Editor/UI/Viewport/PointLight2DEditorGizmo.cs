using Dreambit.ECS;
using Dreambit.Editor.Persistence;
using Dreambit.Editor.Scenes;
using ImGuiNET;
using Vector2 = System.Numerics.Vector2;
using Vector4 = System.Numerics.Vector4;
using XnaVector2 = Microsoft.Xna.Framework.Vector2;

namespace Dreambit.Editor.UI.Viewport;

internal sealed class PointLight2DEditorGizmo(EditorWorkspaceState workspace)
    : IEditorComponentGizmo
{
    private const float HandleRadius = 6f;
    private const float HitRadius = 12f;

    public string DisplayName => "Point Light gizmo";

    public void Draw(
        EditorComponentGizmoFrame frame,
        Entity entity,
        bool allowInteraction,
        ref IEditorComponentGizmoInteraction? startedInteraction)
    {
        if (entity.GetComponent<PointLight2D>() is not { } light)
            return;

        var handleWorld = light.Position + XnaVector2.UnitX * MathF.Max(0f, light.Radius);
        var handle = frame.WorldToCanvas(handleWorld);
        var color = ImGui.GetColorU32(new Vector4(1f, 0.77f, 0.25f, 0.95f));
        frame.DrawList.AddCircleFilled(handle, HandleRadius, color, 20);
        frame.DrawList.AddCircle(handle, 9f, color, 24, 1.5f);

        if (!allowInteraction || startedInteraction is not null || !frame.Hovered ||
            Vector2.Distance(frame.MouseScreen, handle) > HitRadius ||
            !ImGui.IsMouseClicked(ImGuiMouseButton.Left))
        {
            return;
        }

        startedInteraction = new ResizeInteraction(
            frame.Document,
            entity.Id,
            workspace);
    }

    internal static float CalculateRadius(
        XnaVector2 center,
        XnaVector2 handle,
        bool snapEnabled,
        float snapSize)
    {
        var radius = XnaVector2.Distance(center, handle);
        if (snapEnabled)
        {
            var snap = float.IsFinite(snapSize)
                ? MathF.Max(0.001f, snapSize)
                : 0.001f;
            radius = MathF.Round(radius / snap) * snap;
        }

        return float.IsFinite(radius) ? MathF.Max(0f, radius) : 0f;
    }

    internal static void ApplyResize(
        SceneDocument document,
        EditorScene scene,
        Guid entityId,
        XnaVector2 cursorWorld,
        bool snapEnabled,
        float snapSize)
    {
        var entity = scene.FindEntity(entityId)
                     ?? throw new InvalidOperationException("The Point Light entity no longer exists.");
        var light = entity.GetComponent<PointLight2D>()
                    ?? throw new InvalidOperationException("The Point Light no longer exists.");
        light.Radius = CalculateRadius(
            light.Position,
            cursorWorld,
            snapEnabled,
            snapSize);
        document.RecordLDtkComponentMember(light, nameof(PointLight2D.Radius), light.Radius);
    }

    private sealed class ResizeInteraction : IEditorComponentGizmoInteraction
    {
        private readonly Guid _entityId;
        private readonly EditorWorkspaceState _workspace;
        private readonly SceneDocument.SceneEditTransaction _transaction;

        public ResizeInteraction(
            SceneDocument document,
            Guid entityId,
            EditorWorkspaceState workspace)
        {
            Document = document;
            SceneGeneration = document.SceneGeneration;
            _entityId = entityId;
            _workspace = workspace;
            _transaction = document.BeginTransaction("Resize Point Light");
        }

        public string DisplayName => "Point Light resize";
        public SceneDocument Document { get; }
        public int SceneGeneration { get; }

        public void Update(EditorComponentGizmoFrame frame)
        {
            var cursorWorld = frame.MouseWorld;
            _transaction.Update(scene => ApplyResize(
                Document,
                scene,
                _entityId,
                cursorWorld,
                _workspace.SnapEnabled,
                _workspace.MoveSnap));
        }

        public void Commit() => _transaction.Commit();
        public void Cancel() => _transaction.Cancel();
        public void Abandon() => _transaction.Abandon();
    }
}
