using Dreambit.Editor.Graphics;
using Dreambit.Editor.Infrastructure;
using Dreambit.Editor.Persistence;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Dreambit.Editor;

internal sealed class DreambitEditorGame : Core
{
    private readonly EditorLaunchOptions _options;
    private readonly EditorPaths _paths;
    private readonly EditorStateStore _stateStore;
    private readonly EditorGlobalState _globalState;
    private readonly EditorWorkspaceState _workspaceState;
    private int _remainingSmokeFrames;

    private ImGuiRenderer? _imGuiRenderer;
    private EditorApplication? _application;

    public DreambitEditorGame(EditorLaunchOptions options)
        : base(
            EditorWorkspaceState.DefaultWindowWidth,
            EditorWorkspaceState.DefaultWindowHeight,
            "Dreambit Editor")
    {
        _options = options;
        _paths = EditorPaths.Create(options);
        _stateStore = new EditorStateStore(_paths);
        _globalState = _stateStore.LoadGlobalState();
        _workspaceState = _stateStore.LoadWorkspaceState();
        _remainingSmokeFrames = options.SmokeTest ? 12 : -1;

        GraphicsDeviceManager.GraphicsProfile = GraphicsProfile.HiDef;
        GraphicsDeviceManager.PreferredBackBufferWidth = _workspaceState.WindowWidth;
        GraphicsDeviceManager.PreferredBackBufferHeight = _workspaceState.WindowHeight;
        GraphicsDeviceManager.SynchronizeWithVerticalRetrace = true;
        IsFixedTimeStep = false;
        Window.AllowUserResizing = true;
    }

    protected override void LoadContent()
    {
        base.LoadContent();
        RestoreWindowPosition();
        _imGuiRenderer = new ImGuiRenderer(this, _paths.ImGuiLayoutPath);
        _application = new EditorApplication(
            _options,
            _paths,
            _stateStore,
            _globalState,
            _workspaceState,
            _imGuiRenderer,
            Exit);
    }

    protected override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        if (_remainingSmokeFrames <= 0)
            return;

        _remainingSmokeFrames--;
        if (_remainingSmokeFrames == 0)
            Exit();
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(18, 20, 24));

        if (_imGuiRenderer is null || _application is null)
            return;

        _imGuiRenderer.BeginLayout(gameTime);
        _application.Draw();
        _imGuiRenderer.EndLayout();
    }

    protected override void OnExiting(object sender, ExitingEventArgs args)
    {
        if (_application is not null)
        {
            var bounds = Window.ClientBounds;
            var position = Window.Position;
            _application.CaptureWindowBounds(
                position.X,
                position.Y,
                bounds.Width,
                bounds.Height);
        }

        _imGuiRenderer?.SaveLayout();
        _application?.Dispose();
        _imGuiRenderer?.Dispose();
        _application = null;
        _imGuiRenderer = null;

        base.OnExiting(sender, args);
    }

    private void RestoreWindowPosition()
    {
        var hasPosition = _workspaceState.HasWindowPosition || _globalState.HasWindowPosition;
        if (!hasPosition)
            return;
        var x = _workspaceState.HasWindowPosition ? _workspaceState.WindowX : _globalState.WindowX;
        var y = _workspaceState.HasWindowPosition ? _workspaceState.WindowY : _globalState.WindowY;
        try
        {
            Window.Position = new Point(x, y);
        }
        catch (PlatformNotSupportedException)
        {
        }
    }
}
