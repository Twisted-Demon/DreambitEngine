using System.Reflection;
using Dreambit.ECS;
using Dreambit.Editor.Persistence;
using Dreambit.Editor.Scenes;
using ImGuiNET;
using Vector2 = System.Numerics.Vector2;
using Vector4 = System.Numerics.Vector4;
using XnaColor = Microsoft.Xna.Framework.Color;
using XnaVector2 = Microsoft.Xna.Framework.Vector2;

namespace Dreambit.Editor.UI.Viewport;

/// <summary>
/// Immutable view of the state an editor component gizmo needs for one viewport frame.
/// The document is the mutation boundary; handlers must never mutate components outside
/// a <see cref="SceneDocument.SceneEditTransaction"/>.
/// </summary>
internal readonly record struct EditorComponentGizmoFrame(
    SceneDocument Document,
    Camera2D Camera,
    ImDrawListPtr DrawList,
    Vector2 CanvasPosition,
    Vector2 MouseLocal,
    bool Hovered)
{
    public Vector2 MouseScreen => CanvasPosition + MouseLocal;

    public XnaVector2 MouseWorld => Camera.ScreenToWorld(
        new XnaVector2(MouseLocal.X, MouseLocal.Y));

    public Vector2 WorldToCanvas(XnaVector2 world)
    {
        var screen = Camera.WorldToScreen(world);

        return CanvasPosition +
               new Vector2(screen.X, screen.Y);
    }
}

/// <summary>
/// Coordinates reusable interactive component handles.
///
/// Components declare handles through IEditorGizmoContext. Handle declarations retain
/// stable entity/component/member identity only. Active interactions never retain a
/// component instance or collectible-assembly Type.
/// </summary>
internal sealed class EditorComponentGizmoSystem : IDisposable
{
    private const float RadiusHandleRadius = 6f;
    private const float RadiusHandleOutlineRadius = 9f;
    private const float RadiusHitRadius = 12f;

    private const float BoxHandleHalfSize = 4f;
    private const float BoxHitRadius = 9f;

    private readonly EditorWorkspaceState _workspace;
    private readonly Action<string, Exception?>? _reportError;

    // Camera2DEditorGizmo has not yet been converted to the reusable handle API.
    private readonly IEditorComponentGizmo[] _legacyGizmos;

    private readonly List<RadiusHandleRequest> _radiusHandles = [];
    private readonly List<BoxHandleRequest> _boxHandles = [];

    private IEditorComponentGizmoInteraction? _activeInteraction;
    private string? _lastReportedFailure;
    private bool _disposed;

    public EditorComponentGizmoSystem(
        EditorWorkspaceState workspace,
        Action<string, Exception?>? reportError = null)
    {
        ArgumentNullException.ThrowIfNull(workspace);

        _workspace = workspace;
        _reportError = reportError;

        _legacyGizmos =
        [
            new Camera2DEditorGizmo()
        ];
    }

    public bool HasActiveInteraction =>
        _activeInteraction is not null;

    public string? LastError { get; private set; }

    /// <summary>
    /// Clears the immediate-mode handle declarations from the previous viewport frame.
    /// Active drag state is intentionally preserved.
    /// </summary>
    public void BeginFrame()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        ClearFrameRequests();
    }

    internal void RegisterRadiusHandle(
        Component component,
        string memberName,
        XnaVector2 center,
        XnaColor color,
        float thickness)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(component);
        ArgumentException.ThrowIfNullOrWhiteSpace(memberName);

        if (component.Entity is null)
            return;

        _radiusHandles.Add(
            new RadiusHandleRequest(
                CreateBinding(component, memberName),
                center,
                color,
                thickness));
    }

    internal void RegisterBoxHandle(
        Component component,
        string memberName,
        XnaColor color,
        float thickness)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(component);
        ArgumentException.ThrowIfNullOrWhiteSpace(memberName);

        if (component.Entity is null)
            return;

        _boxHandles.Add(
            new BoxHandleRequest(
                CreateBinding(component, memberName),
                color,
                thickness));
    }

    /// <summary>
    /// Draws all declared handles and advances at most one active interaction.
    ///
    /// A true result means left-button input belongs to a component gizmo and must
    /// not also be used for transforms or picking.
    /// </summary>
    public bool DrawAndHandle(EditorComponentGizmoFrame frame)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(frame.Document);
        ArgumentNullException.ThrowIfNull(frame.Camera);

        LastError = null;

        var failed = false;

        try
        {
            // Update an existing interaction first. The generic handle drawing below
            // then resolves the newly-mutated member value, avoiding one-frame visual lag.
            var consumed =
                AdvanceActiveInteraction(frame, ref failed);

            if (failed)
                return consumed;

            IEditorComponentGizmoInteraction? startedInteraction = null;

            DrawLegacyGizmos(
                frame,
                consumed,
                ref failed,
                ref startedInteraction);

            DrawRadiusHandles(
                frame,
                consumed,
                ref failed,
                ref startedInteraction);

            DrawBoxHandles(
                frame,
                consumed,
                ref failed,
                ref startedInteraction);

            if (startedInteraction is not null)
            {
                _activeInteraction = startedInteraction;
                consumed = true;

                AdvanceStartedInteraction(
                    frame,
                    ref failed);
            }

            if (!failed)
                _lastReportedFailure = null;

            return consumed;
        }
        finally
        {
            ClearFrameRequests();
        }
    }

    private void DrawLegacyGizmos(
        EditorComponentGizmoFrame frame,
        bool consumed,
        ref bool failed,
        ref IEditorComponentGizmoInteraction? startedInteraction)
    {
        foreach (var entity in frame.Document.Selection.Resolve(frame.Document.Scene))
        {
            if (!CanEditComponents(frame.Document, entity))
                continue;

            foreach (var gizmo in _legacyGizmos)
            {
                try
                {
                    gizmo.Draw(
                        frame,
                        entity,
                        !consumed &&
                        _activeInteraction is null &&
                        startedInteraction is null,
                        ref startedInteraction);
                }
                catch (Exception exception)
                {
                    failed = true;

                    ReportFailure(
                        $"Could not draw the {gizmo.DisplayName}.",
                        exception);
                }
            }
        }
    }

    private void DrawRadiusHandles(
        EditorComponentGizmoFrame frame,
        bool consumed,
        ref bool failed,
        ref IEditorComponentGizmoInteraction? startedInteraction)
    {
        foreach (var request in _radiusHandles)
        {
            try
            {
                DrawRadiusHandle(
                    frame,
                    request,
                    !consumed &&
                    _activeInteraction is null &&
                    startedInteraction is null,
                    ref startedInteraction);
            }
            catch (Exception exception)
            {
                failed = true;

                ReportFailure(
                    $"Could not draw the " +
                    $"{request.Binding.ComponentDisplayName} radius handle.",
                    exception);
            }
        }
    }

    private void DrawBoxHandles(
        EditorComponentGizmoFrame frame,
        bool consumed,
        ref bool failed,
        ref IEditorComponentGizmoInteraction? startedInteraction)
    {
        foreach (var request in _boxHandles)
        {
            try
            {
                DrawBoxHandle(
                    frame,
                    request,
                    !consumed &&
                    _activeInteraction is null &&
                    startedInteraction is null,
                    ref startedInteraction);
            }
            catch (Exception exception)
            {
                failed = true;

                ReportFailure(
                    $"Could not draw the " +
                    $"{request.Binding.ComponentDisplayName} box handle.",
                    exception);
            }
        }
    }

    private void DrawRadiusHandle(
        EditorComponentGizmoFrame frame,
        RadiusHandleRequest request,
        bool allowInteraction,
        ref IEditorComponentGizmoInteraction? startedInteraction)
    {
        if (!TryResolveHandleTarget(
                frame.Document,
                request.Binding,
                out var entity,
                out var component))
        {
            return;
        }

        var radius =
            NormalizeRadius(
                ReadFloatMember(
                    component,
                    request.Binding.MemberName));

        var center = request.Center;

        if (!IsFinite(center))
            return;

        var handleWorld =
            center + XnaVector2.UnitX * radius;

        var centerScreen =
            frame.WorldToCanvas(center);

        var handleScreen =
            frame.WorldToCanvas(handleWorld);

        var color =
            ColorU32(request.Color);

        var thickness =
            NormalizeThickness(request.Thickness);

        // Visualization remains visible even when the selected component belongs
        // to a read-only linked Blueprint.
        frame.DrawList.AddCircle(
            centerScreen,
            MathF.Abs(radius * frame.Camera.Scale),
            color,
            48,
            thickness);

        frame.DrawList.AddLine(
            centerScreen,
            handleScreen,
            color,
            thickness);

        if (!CanEditComponents(frame.Document, entity))
            return;

        frame.DrawList.AddCircleFilled(
            handleScreen,
            RadiusHandleRadius,
            color,
            20);

        frame.DrawList.AddCircle(
            handleScreen,
            RadiusHandleOutlineRadius,
            color,
            24,
            1.5f);

        if (!allowInteraction ||
            startedInteraction is not null ||
            !frame.Hovered ||
            Vector2.Distance(frame.MouseScreen, handleScreen) > RadiusHitRadius ||
            !ImGui.IsMouseClicked(ImGuiMouseButton.Left))
        {
            return;
        }

        startedInteraction =
            new RadiusResizeInteraction(
                frame.Document,
                request.Binding,
                center,
                _workspace);
    }

    private void DrawBoxHandle(
        EditorComponentGizmoFrame frame,
        BoxHandleRequest request,
        bool allowInteraction,
        ref IEditorComponentGizmoInteraction? startedInteraction)
    {
        if (!TryResolveHandleTarget(
                frame.Document,
                request.Binding,
                out var entity,
                out var component))
        {
            return;
        }

        var box =
            ReadBoxMember(
                component,
                request.Binding.MemberName);

        var vertices =
            box.TransformPolygon(component.Transform).Vertices;

        if (vertices is null || vertices.Length != 4)
        {
            throw new InvalidOperationException(
                $"{request.Binding.ComponentDisplayName}.{request.Binding.MemberName} " +
                "did not produce exactly four transformed vertices.");
        }

        var color =
            ColorU32(request.Color);

        var thickness =
            NormalizeThickness(request.Thickness);

        // Draw the box outline regardless of whether the component is read-only.
        for (var index = 0; index < vertices.Length; index++)
        {
            var current =
                frame.WorldToCanvas(vertices[index]);

            var next =
                frame.WorldToCanvas(
                    vertices[(index + 1) % vertices.Length]);

            frame.DrawList.AddLine(
                current,
                next,
                color,
                thickness);
        }

        if (!CanEditComponents(frame.Document, entity))
            return;

        for (var index = 0; index < vertices.Length; index++)
        {
            var handle =
                frame.WorldToCanvas(vertices[index]);

            frame.DrawList.AddRectFilled(
                handle - new Vector2(BoxHandleHalfSize),
                handle + new Vector2(BoxHandleHalfSize),
                color);

            if (!allowInteraction ||
                startedInteraction is not null ||
                !frame.Hovered ||
                Vector2.Distance(frame.MouseScreen, handle) > BoxHitRadius ||
                !ImGui.IsMouseClicked(ImGuiMouseButton.Left))
            {
                continue;
            }

            startedInteraction =
                new BoxResizeInteraction(
                    frame.Document,
                    request.Binding,
                    GetOppositeCorner(box, index),
                    _workspace);
        }
    }

    internal static bool CanEditComponents(
        SceneDocument document,
        Entity entity) =>
        !document.TryGetBlueprintInstanceRoot(
            entity,
            out _,
            out _);

    internal static float CalculateRadius(
        XnaVector2 center,
        XnaVector2 cursorWorld,
        bool snapEnabled,
        float snapSize)
    {
        var radius =
            XnaVector2.Distance(
                center,
                cursorWorld);

        if (snapEnabled)
        {
            var snap =
                NormalizeSnap(snapSize);

            radius =
                MathF.Round(radius / snap) * snap;
        }

        return NormalizeRadius(radius);
    }

    internal static XnaVector2 GetOppositeCorner(
        Box2D box,
        int cornerIndex) =>
        cornerIndex switch
        {
            0 => box.BottomRight,
            1 => box.BottomLeft,
            2 => box.TopLeft,
            3 => box.TopRight,

            _ => throw new ArgumentOutOfRangeException(
                nameof(cornerIndex))
        };

    internal static Box2D CalculateResizedShape(
        Transform transform,
        XnaVector2 oppositeLocal,
        XnaVector2 cursorWorld,
        bool snapEnabled,
        float snapSize)
    {
        ArgumentNullException.ThrowIfNull(transform);

        var cursorLocal =
            transform.InverseTransformPoint2D(cursorWorld);

        if (snapEnabled)
        {
            var snap =
                NormalizeSnap(snapSize);

            cursorLocal.X =
                MathF.Round(cursorLocal.X / snap) * snap;

            cursorLocal.Y =
                MathF.Round(cursorLocal.Y / snap) * snap;
        }

        var center =
            (cursorLocal + oppositeLocal) * 0.5f;

        var halfWidth =
            MathF.Max(
                0.001f,
                MathF.Abs(
                    cursorLocal.X - oppositeLocal.X) * 0.5f);

        var halfHeight =
            MathF.Max(
                0.001f,
                MathF.Abs(
                    cursorLocal.Y - oppositeLocal.Y) * 0.5f);

        return Box2D.CreateRectangle(
            center,
            halfWidth,
            halfHeight);
    }

    private static void ApplyRadiusResize(
        SceneDocument document,
        EditorScene scene,
        ComponentMemberBinding binding,
        XnaVector2 center,
        XnaVector2 cursorWorld,
        bool snapEnabled,
        float snapSize)
    {
        var component =
            ResolveComponent(
                scene,
                binding);

        var radius =
            CalculateRadius(
                center,
                cursorWorld,
                snapEnabled,
                snapSize);

        var storedValue =
            SetMemberValue(
                component,
                binding.MemberName,
                radius);

        document.RecordLDtkComponentMember(
            component,
            binding.MemberName,
            storedValue);
    }

    private static void ApplyBoxResize(
        SceneDocument document,
        EditorScene scene,
        ComponentMemberBinding binding,
        XnaVector2 oppositeLocal,
        XnaVector2 cursorWorld,
        bool snapEnabled,
        float snapSize)
    {
        var component =
            ResolveComponent(
                scene,
                binding);

        var box =
            CalculateResizedShape(
                component.Transform,
                oppositeLocal,
                cursorWorld,
                snapEnabled,
                snapSize);

        var storedValue =
            SetMemberValue(
                component,
                binding.MemberName,
                box);

        document.RecordLDtkComponentMember(
            component,
            binding.MemberName,
            storedValue);
    }

    private static ComponentMemberBinding CreateBinding(
        Component component,
        string memberName)
    {
        var componentType =
            component.GetType();

        return new ComponentMemberBinding(
            component.Entity.Id,
            SceneDocumentSerializer.GetComponentTypeId(componentType),
            componentType.Name,
            memberName);
    }

    private static bool TryResolveHandleTarget(
        SceneDocument document,
        ComponentMemberBinding binding,
        out Entity entity,
        out Component component)
    {
        entity = null!;
        component = null!;

        var scene =
            document.Scene;

        if (scene is null)
            return false;

        var resolvedEntity =
            scene.FindEntity(binding.EntityId);

        if (resolvedEntity is null)
            return false;

        var resolvedComponent =
            FindComponent(
                resolvedEntity,
                binding.ComponentTypeId);

        if (resolvedComponent is null)
            return false;

        entity = resolvedEntity;
        component = resolvedComponent;

        return true;
    }

    private static Component ResolveComponent(
        EditorScene scene,
        ComponentMemberBinding binding)
    {
        var entity =
            scene.FindEntity(binding.EntityId)
            ?? throw new InvalidOperationException(
                $"Entity '{binding.EntityId}' no longer exists.");

        return FindComponent(
                   entity,
                   binding.ComponentTypeId)
               ?? throw new InvalidOperationException(
                   $"{binding.ComponentDisplayName} no longer exists on entity " +
                   $"'{entity.Name}'.");
    }

    private static Component? FindComponent(
        Entity entity,
        string componentTypeId)
    {
        foreach (var component in entity.GetAllComponents())
        {
            var candidateTypeId =
                SceneDocumentSerializer.GetComponentTypeId(
                    component.GetType());

            if (string.Equals(
                    candidateTypeId,
                    componentTypeId,
                    StringComparison.OrdinalIgnoreCase))
            {
                return component;
            }
        }

        return null;
    }

    private static float ReadFloatMember(
        Component component,
        string memberName)
    {
        var member =
            ResolveMember(
                component,
                memberName);

        var value =
            GetMemberValue(
                component,
                member);

        if (value is float result)
            return result;

        throw new InvalidOperationException(
            $"{component.GetType().Name}.{memberName} must be a float " +
            $"to use {nameof(IEditorGizmoContext.RadiusHandle)}.");
    }

    private static Box2D ReadBoxMember(
        Component component,
        string memberName)
    {
        var member =
            ResolveMember(
                component,
                memberName);

        var value =
            GetMemberValue(
                component,
                member);

        if (value is Box2D box)
            return box;

        throw new InvalidOperationException(
            $"{component.GetType().Name}.{memberName} must currently contain " +
            $"a {nameof(Box2D)} to use {nameof(IEditorGizmoContext.BoxHandle)}.");
    }

    private static object? SetMemberValue(
        Component component,
        string memberName,
        object value)
    {
        var member =
            ResolveMember(
                component,
                memberName);

        var memberType =
            GetMemberType(member);

        if (!memberType.IsInstanceOfType(value))
        {
            throw new InvalidOperationException(
                $"{component.GetType().Name}.{memberName} is a " +
                $"{memberType.FullName}, which cannot accept " +
                $"{value.GetType().FullName}.");
        }

        switch (member)
        {
            case PropertyInfo property:
            {
                if (!property.CanWrite)
                {
                    throw new InvalidOperationException(
                        $"{component.GetType().Name}.{memberName} is read-only.");
                }

                property.SetValue(
                    component,
                    value);

                break;
            }

            case FieldInfo field:
            {
                if (field.IsInitOnly)
                {
                    throw new InvalidOperationException(
                        $"{component.GetType().Name}.{memberName} is readonly.");
                }

                field.SetValue(
                    component,
                    value);

                break;
            }

            default:
                throw new InvalidOperationException(
                    $"Unsupported editable member {member.Name}.");
        }

        // If this member previously failed editor deserialization, an explicit gizmo
        // edit means the newly-authored value should replace that stale source payload.
        component.AcknowledgeEditorSerializationFailure(memberName);

        return GetMemberValue(
            component,
            member);
    }

    private static MemberInfo ResolveMember(
        Component component,
        string memberName)
    {
        const BindingFlags flags =
            BindingFlags.Instance |
            BindingFlags.Public;

        var componentType =
            component.GetType();

        if (componentType.GetProperty(
                memberName,
                flags) is { } property)
        {
            return property;
        }

        if (componentType.GetField(
                memberName,
                flags) is { } field)
        {
            return field;
        }

        throw new InvalidOperationException(
            $"{componentType.Name} does not contain a public member named " +
            $"'{memberName}'.");
    }

    private static Type GetMemberType(MemberInfo member) =>
        member switch
        {
            PropertyInfo property => property.PropertyType,
            FieldInfo field => field.FieldType,

            _ => throw new InvalidOperationException(
                $"Unsupported editable member {member.Name}.")
        };

    private static object? GetMemberValue(
        Component component,
        MemberInfo member) =>
        member switch
        {
            PropertyInfo property =>
                property.GetValue(component),

            FieldInfo field =>
                field.GetValue(component),

            _ => throw new InvalidOperationException(
                $"Unsupported editable member {member.Name}.")
        };

    private static bool IsFinite(XnaVector2 value) =>
        float.IsFinite(value.X) &&
        float.IsFinite(value.Y);

    private static float NormalizeRadius(float radius) =>
        float.IsFinite(radius)
            ? MathF.Max(0f, radius)
            : 0f;

    private static float NormalizeSnap(float value) =>
        float.IsFinite(value)
            ? MathF.Max(0.001f, value)
            : 0.001f;

    private static float NormalizeThickness(float value) =>
        float.IsFinite(value)
            ? MathF.Max(1f, value)
            : 1f;

    private static uint ColorU32(XnaColor color) =>
        ImGui.GetColorU32(
            new Vector4(
                color.R / 255f,
                color.G / 255f,
                color.B / 255f,
                color.A / 255f));

    /// <summary>
    /// Rolls back the active drag while its document is still live.
    /// </summary>
    public void CancelActiveInteraction()
    {
        ClearFrameRequests();

        var interaction =
            TakeActiveInteraction();

        if (interaction is null)
            return;

        try
        {
            interaction.Cancel();
        }
        catch (Exception exception)
        {
            interaction.Abandon();

            ReportFailure(
                $"Could not cancel the {interaction.DisplayName}.",
                exception);
        }
    }

    /// <summary>
    /// Forgets the active drag without touching its document. Use this when document
    /// disposal or assembly reload has already invalidated the live scene.
    /// </summary>
    public void AbandonActiveInteraction()
    {
        ClearFrameRequests();

        var interaction =
            TakeActiveInteraction();

        if (interaction is not null)
            interaction.Abandon();
    }

    private bool AdvanceActiveInteraction(
        EditorComponentGizmoFrame frame,
        ref bool failed)
    {
        var interaction =
            _activeInteraction;

        if (interaction is null)
            return false;

        if (!ReferenceEquals(
                interaction.Document,
                frame.Document) ||
            interaction.SceneGeneration !=
            frame.Document.SceneGeneration)
        {
            AbandonActiveInteraction();
            return false;
        }

        if (!ImGui.IsMouseDown(ImGuiMouseButton.Left))
        {
            _activeInteraction = null;

            try
            {
                interaction.Commit();
            }
            catch (Exception exception)
            {
                failed = true;

                interaction.Abandon();

                ReportFailure(
                    $"Could not commit the {interaction.DisplayName}.",
                    exception);
            }

            return true;
        }

        try
        {
            interaction.Update(frame);
        }
        catch (Exception exception)
        {
            failed = true;
            _activeInteraction = null;

            TryCancelAfterFailure(
                interaction,
                exception);
        }

        return true;
    }

    private void AdvanceStartedInteraction(
        EditorComponentGizmoFrame frame,
        ref bool failed)
    {
        var interaction =
            _activeInteraction;

        if (interaction is null ||
            !ImGui.IsMouseDown(ImGuiMouseButton.Left))
        {
            return;
        }

        try
        {
            interaction.Update(frame);
        }
        catch (Exception exception)
        {
            failed = true;
            _activeInteraction = null;

            TryCancelAfterFailure(
                interaction,
                exception);
        }
    }

    private void TryCancelAfterFailure(
        IEditorComponentGizmoInteraction interaction,
        Exception failure)
    {
        try
        {
            interaction.Cancel();

            ReportFailure(
                $"The {interaction.DisplayName} failed and was cancelled.",
                failure);
        }
        catch (Exception cancellationFailure)
        {
            interaction.Abandon();

            ReportFailure(
                $"The {interaction.DisplayName} failed and its transaction " +
                "could not be restored.",
                new AggregateException(
                    failure,
                    cancellationFailure));
        }
    }

    private IEditorComponentGizmoInteraction? TakeActiveInteraction()
    {
        var interaction =
            _activeInteraction;

        _activeInteraction = null;

        return interaction;
    }

    private void ClearFrameRequests()
    {
        _radiusHandles.Clear();
        _boxHandles.Clear();
    }

    private void ReportFailure(
        string message,
        Exception exception)
    {
        LastError =
            $"{message} {exception.Message}";

        var failure =
            $"{message}\n{exception}";

        if (string.Equals(
                _lastReportedFailure,
                failure,
                StringComparison.Ordinal))
        {
            return;
        }

        _lastReportedFailure = failure;

        try
        {
            _reportError?.Invoke(
                message,
                exception);
        }
        catch (Exception reportingFailure)
        {
            try
            {
                Console.Error.WriteLine(
                    $"{message} {exception}{Environment.NewLine}" +
                    $"Reporting the editor error also failed: {reportingFailure}");
            }
            catch
            {
                // Diagnostics are best-effort at this isolation boundary.
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        AbandonActiveInteraction();

        _disposed = true;
    }

    private readonly record struct ComponentMemberBinding(
        Guid EntityId,
        string ComponentTypeId,
        string ComponentDisplayName,
        string MemberName);

    private readonly record struct RadiusHandleRequest(
        ComponentMemberBinding Binding,
        XnaVector2 Center,
        XnaColor Color,
        float Thickness);

    private readonly record struct BoxHandleRequest(
        ComponentMemberBinding Binding,
        XnaColor Color,
        float Thickness);

    private sealed class RadiusResizeInteraction
        : IEditorComponentGizmoInteraction
    {
        private readonly ComponentMemberBinding _binding;
        private readonly XnaVector2 _center;
        private readonly EditorWorkspaceState _workspace;
        private readonly SceneDocument.SceneEditTransaction _transaction;

        public RadiusResizeInteraction(
            SceneDocument document,
            ComponentMemberBinding binding,
            XnaVector2 center,
            EditorWorkspaceState workspace)
        {
            Document = document;
            SceneGeneration = document.SceneGeneration;

            _binding = binding;
            _center = center;
            _workspace = workspace;

            _transaction =
                document.BeginTransaction(
                    $"Resize {binding.ComponentDisplayName} {binding.MemberName}");
        }

        public string DisplayName =>
            $"{_binding.ComponentDisplayName} radius resize";

        public SceneDocument Document { get; }

        public int SceneGeneration { get; }

        public void Update(EditorComponentGizmoFrame frame)
        {
            var cursorWorld =
                frame.MouseWorld;

            _transaction.Update(
                scene =>
                    ApplyRadiusResize(
                        Document,
                        scene,
                        _binding,
                        _center,
                        cursorWorld,
                        _workspace.SnapEnabled,
                        _workspace.MoveSnap));
        }

        public void Commit() =>
            _transaction.Commit();

        public void Cancel() =>
            _transaction.Cancel();

        public void Abandon() =>
            _transaction.Abandon();
    }

    private sealed class BoxResizeInteraction
        : IEditorComponentGizmoInteraction
    {
        private readonly ComponentMemberBinding _binding;
        private readonly XnaVector2 _oppositeLocal;
        private readonly EditorWorkspaceState _workspace;
        private readonly SceneDocument.SceneEditTransaction _transaction;

        public BoxResizeInteraction(
            SceneDocument document,
            ComponentMemberBinding binding,
            XnaVector2 oppositeLocal,
            EditorWorkspaceState workspace)
        {
            Document = document;
            SceneGeneration = document.SceneGeneration;

            _binding = binding;
            _oppositeLocal = oppositeLocal;
            _workspace = workspace;

            _transaction =
                document.BeginTransaction(
                    $"Resize {binding.ComponentDisplayName} {binding.MemberName}");
        }

        public string DisplayName =>
            $"{_binding.ComponentDisplayName} box resize";

        public SceneDocument Document { get; }

        public int SceneGeneration { get; }

        public void Update(EditorComponentGizmoFrame frame)
        {
            var cursorWorld =
                frame.MouseWorld;

            _transaction.Update(
                scene =>
                    ApplyBoxResize(
                        Document,
                        scene,
                        _binding,
                        _oppositeLocal,
                        cursorWorld,
                        _workspace.SnapEnabled,
                        _workspace.MoveSnap));
        }

        public void Commit() =>
            _transaction.Commit();

        public void Cancel() =>
            _transaction.Cancel();

        public void Abandon() =>
            _transaction.Abandon();
    }
}

/// <summary>
/// Legacy component-specific editor gizmo interface.
///
/// Camera2DEditorGizmo still uses this until it is moved to the generic
/// IEditorGizmoContext API.
/// </summary>
internal interface IEditorComponentGizmo
{
    string DisplayName { get; }

    void Draw(
        EditorComponentGizmoFrame frame,
        Entity entity,
        bool allowInteraction,
        ref IEditorComponentGizmoInteraction? startedInteraction);
}

/// <summary>
/// Active interactions retain stable entity IDs and value snapshots only.
/// In particular, they must not retain component instances or collectible-assembly Type objects.
/// </summary>
internal interface IEditorComponentGizmoInteraction
{
    string DisplayName { get; }

    SceneDocument Document { get; }

    int SceneGeneration { get; }

    void Update(EditorComponentGizmoFrame frame);

    void Commit();

    void Cancel();

    void Abandon();
}