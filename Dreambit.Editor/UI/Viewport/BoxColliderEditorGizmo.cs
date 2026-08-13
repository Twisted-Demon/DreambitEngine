using Dreambit.ECS;
using Dreambit.Editor.Persistence;
using Dreambit.Editor.Scenes;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Vector2 = System.Numerics.Vector2;
using Vector4 = System.Numerics.Vector4;
using XnaVector2 = Microsoft.Xna.Framework.Vector2;

namespace Dreambit.Editor.UI.Viewport;

internal sealed class BoxColliderEditorGizmo(EditorWorkspaceState workspace)
    : IEditorComponentGizmo
{
    private const float HandleHalfSize = 4f;
    private const float HitRadius = 9f;

    public string DisplayName => "Box Collider gizmo";

    public void Draw(
        EditorComponentGizmoFrame frame,
        Entity entity,
        bool allowInteraction,
        ref IEditorComponentGizmoInteraction? startedInteraction)
    {
        if (entity.GetComponent<BoxCollider>() is not { Bounds: Box2D localBox } collider)
            return;

        var vertices = collider.WorldPolygon2D.Vertices;
        if (vertices is null || vertices.Length != 4)
            return;

        var color = ImGui.GetColorU32(new Vector4(0.32f, 0.92f, 0.55f, 0.95f));
        for (var index = 0; index < vertices.Length; index++)
        {
            var handle = frame.WorldToCanvas(vertices[index]);
            frame.DrawList.AddRectFilled(
                handle - new Vector2(HandleHalfSize),
                handle + new Vector2(HandleHalfSize),
                color);

            if (!allowInteraction || startedInteraction is not null || !frame.Hovered ||
                Vector2.Distance(frame.MouseScreen, handle) > HitRadius ||
                !ImGui.IsMouseClicked(ImGuiMouseButton.Left))
            {
                continue;
            }

            startedInteraction = new ResizeInteraction(
                frame.Document,
                entity.Id,
                GetOppositeCorner(localBox, index),
                workspace);
        }
    }

    internal static XnaVector2 GetOppositeCorner(Box2D box, int cornerIndex) =>
        cornerIndex switch
        {
            0 => box.BottomRight,
            1 => box.BottomLeft,
            2 => box.TopLeft,
            3 => box.TopRight,
            _ => throw new ArgumentOutOfRangeException(nameof(cornerIndex))
        };

    internal static Box2D CalculateResizedShape(
        Transform transform,
        XnaVector2 oppositeLocal,
        XnaVector2 cursorWorld,
        bool snapEnabled,
        float snapSize)
    {
        ArgumentNullException.ThrowIfNull(transform);

        var cursorLocal = transform.InverseTransformPoint2D(cursorWorld);
        if (snapEnabled)
        {
            var snap = NormalizeSnap(snapSize);
            cursorLocal.X = MathF.Round(cursorLocal.X / snap) * snap;
            cursorLocal.Y = MathF.Round(cursorLocal.Y / snap) * snap;
        }

        var center = (cursorLocal + oppositeLocal) * 0.5f;
        var halfWidth = MathF.Max(0.001f, MathF.Abs(cursorLocal.X - oppositeLocal.X) * 0.5f);
        var halfHeight = MathF.Max(0.001f, MathF.Abs(cursorLocal.Y - oppositeLocal.Y) * 0.5f);
        return Box2D.CreateRectangle(center, halfWidth, halfHeight);
    }

    internal static void ApplyResize(
        SceneDocument document,
        EditorScene scene,
        Guid entityId,
        XnaVector2 oppositeLocal,
        XnaVector2 cursorWorld,
        bool snapEnabled,
        float snapSize)
    {
        var entity = scene.FindEntity(entityId)
                     ?? throw new InvalidOperationException("The Box Collider entity no longer exists.");
        var collider = entity.GetComponent<BoxCollider>()
                       ?? throw new InvalidOperationException("The Box Collider no longer exists.");
        collider.SetShape(CalculateResizedShape(
            entity.Transform,
            oppositeLocal,
            cursorWorld,
            snapEnabled,
            snapSize));
        document.RecordLDtkComponentMember(collider, nameof(Collider.Bounds), collider.Bounds);
    }

    private static float NormalizeSnap(float value) =>
        float.IsFinite(value) ? MathF.Max(0.001f, value) : 0.001f;

    private sealed class ResizeInteraction : IEditorComponentGizmoInteraction
    {
        private readonly Guid _entityId;
        private readonly XnaVector2 _oppositeLocal;
        private readonly EditorWorkspaceState _workspace;
        private readonly SceneDocument.SceneEditTransaction _transaction;

        public ResizeInteraction(
            SceneDocument document,
            Guid entityId,
            XnaVector2 oppositeLocal,
            EditorWorkspaceState workspace)
        {
            Document = document;
            SceneGeneration = document.SceneGeneration;
            _entityId = entityId;
            _oppositeLocal = oppositeLocal;
            _workspace = workspace;
            _transaction = document.BeginTransaction("Resize Box Collider");
        }

        public string DisplayName => "Box Collider resize";
        public SceneDocument Document { get; }
        public int SceneGeneration { get; }

        public void Update(EditorComponentGizmoFrame frame)
        {
            var cursorWorld = frame.MouseWorld;
            _transaction.Update(scene => ApplyResize(
                Document,
                scene,
                _entityId,
                _oppositeLocal,
                cursorWorld,
                _workspace.SnapEnabled,
                _workspace.MoveSnap));
        }

        public void Commit() => _transaction.Commit();
        public void Cancel() => _transaction.Cancel();
        public void Abandon() => _transaction.Abandon();
    }
}
