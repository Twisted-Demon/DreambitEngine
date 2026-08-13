using Dreambit.ECS;
using Dreambit.Editor.Persistence;
using Dreambit.Editor.Scenes;
using ImGuiNET;
using Vector2 = System.Numerics.Vector2;

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

    public Microsoft.Xna.Framework.Vector2 MouseWorld => Camera.ScreenToWorld(
        new Microsoft.Xna.Framework.Vector2(MouseLocal.X, MouseLocal.Y));

    public Vector2 WorldToCanvas(Microsoft.Xna.Framework.Vector2 world)
    {
        var screen = Camera.WorldToScreen(world);
        return CanvasPosition + new Vector2(screen.X, screen.Y);
    }
}

/// <summary>
/// Draws and coordinates the fixed set of interactive component gizmos supported by the
/// editor. Adding another built-in gizmo changes this list, never a viewport panel.
/// </summary>
internal sealed class EditorComponentGizmoSystem : IDisposable
{
    private readonly IEditorComponentGizmo[] _gizmos;
    private readonly Action<string, Exception?>? _reportError;
    private IEditorComponentGizmoInteraction? _activeInteraction;
    private string? _lastReportedFailure;
    private bool _disposed;

    public EditorComponentGizmoSystem(
        EditorWorkspaceState workspace,
        Action<string, Exception?>? reportError = null)
    {
        ArgumentNullException.ThrowIfNull(workspace);

        _reportError = reportError;
        _gizmos =
        [
            new BoxColliderEditorGizmo(workspace),
            new PointLight2DEditorGizmo(workspace),
            new Camera2DEditorGizmo()
        ];
    }

    public bool HasActiveInteraction => _activeInteraction is not null;

    public string? LastError { get; private set; }

    /// <summary>
    /// Draws all supported gizmos for the document selection and advances at most one
    /// active interaction. A true result means the left-button input belongs to a
    /// component gizmo and must not also be used for transforms or picking.
    /// </summary>
    public bool DrawAndHandle(EditorComponentGizmoFrame frame)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(frame.Document);
        ArgumentNullException.ThrowIfNull(frame.Camera);

        LastError = null;
        var failed = false;
        var consumed = AdvanceActiveInteraction(frame, ref failed);
        if (failed)
            return consumed;

        IEditorComponentGizmoInteraction? startedInteraction = null;

        foreach (var entity in frame.Document.Selection.Resolve(frame.Document.Scene))
        {
            // A materialized Blueprint hierarchy is source-owned. Even its root only
            // permits placement changes; component values remain read-only until unboxed.
            if (!CanEditComponents(frame.Document, entity))
                continue;

            foreach (var gizmo in _gizmos)
            {
                try
                {
                    gizmo.Draw(
                        frame,
                        entity,
                        !consumed && _activeInteraction is null && startedInteraction is null,
                        ref startedInteraction);
                }
                catch (Exception exception)
                {
                    failed = true;
                    ReportFailure($"Could not draw the {gizmo.DisplayName}.", exception);
                }
            }
        }

        if (startedInteraction is not null)
        {
            _activeInteraction = startedInteraction;
            consumed = true;
            AdvanceStartedInteraction(frame, ref failed);
        }

        if (!failed)
            _lastReportedFailure = null;

        return consumed;
    }

    internal static bool CanEditComponents(SceneDocument document, Entity entity) =>
        !document.TryGetBlueprintInstanceRoot(entity, out _, out _);

    /// <summary>Rolls back the active drag while its document is still live.</summary>
    public void CancelActiveInteraction()
    {
        var interaction = TakeActiveInteraction();
        if (interaction is null)
            return;

        try
        {
            interaction.Cancel();
        }
        catch (Exception exception)
        {
            interaction.Abandon();
            ReportFailure($"Could not cancel the {interaction.DisplayName}.", exception);
        }
    }

    /// <summary>
    /// Forgets the active drag without touching its document. Use this when document
    /// disposal or assembly reload has already invalidated the live scene.
    /// </summary>
    public void AbandonActiveInteraction()
    {
        var interaction = TakeActiveInteraction();
        if (interaction is not null)
            interaction.Abandon();
    }

    private bool AdvanceActiveInteraction(
        EditorComponentGizmoFrame frame,
        ref bool failed)
    {
        var interaction = _activeInteraction;
        if (interaction is null)
            return false;

        if (!ReferenceEquals(interaction.Document, frame.Document) ||
            interaction.SceneGeneration != frame.Document.SceneGeneration)
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
                ReportFailure($"Could not commit the {interaction.DisplayName}.", exception);
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
            TryCancelAfterFailure(interaction, exception);
        }
        return true;
    }

    private void AdvanceStartedInteraction(
        EditorComponentGizmoFrame frame,
        ref bool failed)
    {
        var interaction = _activeInteraction;
        if (interaction is null || !ImGui.IsMouseDown(ImGuiMouseButton.Left))
            return;

        try
        {
            interaction.Update(frame);
        }
        catch (Exception exception)
        {
            failed = true;
            _activeInteraction = null;
            TryCancelAfterFailure(interaction, exception);
        }
    }

    private void TryCancelAfterFailure(
        IEditorComponentGizmoInteraction interaction,
        Exception failure)
    {
        try
        {
            interaction.Cancel();
            ReportFailure($"The {interaction.DisplayName} failed and was cancelled.", failure);
        }
        catch (Exception cancellationFailure)
        {
            interaction.Abandon();
            ReportFailure(
                $"The {interaction.DisplayName} failed and its transaction could not be restored.",
                new AggregateException(failure, cancellationFailure));
        }
    }

    private IEditorComponentGizmoInteraction? TakeActiveInteraction()
    {
        var interaction = _activeInteraction;
        _activeInteraction = null;
        return interaction;
    }

    private void ReportFailure(string message, Exception exception)
    {
        LastError = $"{message} {exception.Message}";
        var failure = $"{message}\n{exception}";
        if (string.Equals(_lastReportedFailure, failure, StringComparison.Ordinal))
            return;

        _lastReportedFailure = failure;
        try
        {
            _reportError?.Invoke(message, exception);
        }
        catch (Exception reportingFailure)
        {
            // Fault isolation includes the logging boundary. A custom error sink must
            // not turn a recoverable component-gizmo failure into a viewport crash.
            try
            {
                Console.Error.WriteLine(
                    $"{message} {exception}{Environment.NewLine}" +
                    $"Reporting the editor error also failed: {reportingFailure}");
            }
            catch
            {
                // Error reporting is best-effort at this isolation boundary.
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
}

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
/// Active interactions retain stable entity IDs and value snapshots only. In particular,
/// they must not retain component instances or collectible-assembly Type objects.
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
