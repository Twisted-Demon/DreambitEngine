using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Dreambit;

public class Core : Game
{
    private const float FixedPhysicsStep = 1f / 60f;
    public static readonly Logger<Core> Logger = new();
    private float _accumulatedPhysicsTime;

    public Core(int width = 800, int height = 600, string title = "Dreambit Engine")
    {
        GraphicsDeviceManager = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        Instance = this;

        GameName = title;

        Dreambit.Window.Init();
        Dreambit.Window.SetTitle(title);
        Dreambit.Window.SetSize(width, height);

        Resources.Instance.Init();

        TargetElapsedTime = TimeSpan.FromSeconds((double)1 / 120); //set Target fps to 120
    }

    public static LogLevel Level { get; set; } = LogLevel.Debug;

    public static Core Instance { get; private set; }
    public static GraphicsDeviceManager GraphicsDeviceManager { get; private set; }
    public static SpriteBatch SpriteBatch { get; private set; }
    public Scene CurrentScene { get; private set; }
    public Scene NextScene { get; private set; }
    private static string GameName { get; set; }


    protected override void Initialize()
    {
        base.Initialize();

        GraphicsDevice.BlendState = BlendState.NonPremultiplied;
        Input.Init();
    }

    public static void SetFixedTimeStep(bool value)
    {
        Instance.IsFixedTimeStep = value;
    }

    public static void SetTargetFps(int fps)
    {
        Instance.TargetElapsedTime = TimeSpan.FromSeconds((double)1 / fps);
    }

    protected override void LoadContent()
    {
        base.LoadContent();

        SpriteBatch = new SpriteBatch(GraphicsDevice);
        LDtkManager.Instance.Init();
    }

    protected override void Update(GameTime gameTime)
    {
        Time.Update(gameTime);
        Dreambit.Window.Tick(gameTime);

        UpdateDebug();

        Input.PreUpdate();
        {
            if (NextScene != null)
                ChangeScenes();

            HandlePhysics();
            CurrentScene.Tick();
        }
        Input.PostUpdate();

        base.Update(gameTime);
    }

    private void UpdateDebug()
    {
#if DEBUG || RELEASE
        UpdateTitle();
#endif
    }

    protected override void Draw(GameTime gameTime)
    {
        CurrentScene.OnDraw();
        base.Draw(gameTime);
    }

    private void HandlePhysics()
    {
        _accumulatedPhysicsTime += Time.DeltaTime;

        if (_accumulatedPhysicsTime >= FixedPhysicsStep)
        {
            CurrentScene.PhysicsTick();

            _accumulatedPhysicsTime = 0f;
        }
    }

    protected override void OnExiting(object sender, ExitingEventArgs args)
    {
        CurrentScene?.Terminate();
        SpriteBatch.Dispose();
        base.OnExiting(sender, args);
    }

    private void ChangeScenes()
    {
        Logger.Info("Changing Scenes");
        CurrentScene?.Terminate();
        CurrentScene = NextScene;
        NextScene = null;
        _accumulatedPhysicsTime = 0f;

        PhysicsSystem.Instance.CleanUp();
        AudioSystem.Instance.CleanUp();
        Time.SceneLoaded();
    }

    internal void SetNextScene(Scene scene)
    {
        NextScene = scene;
    }

#if DEBUG || RELEASE
    private float _debugElapsedTime;
    private void UpdateTitle()
    {
        _debugElapsedTime += Time.DeltaTime;

        if (_debugElapsedTime < 1f)
            return;
        _debugElapsedTime = 0f;
        var memory = Process.GetCurrentProcess().PrivateMemorySize64;
        var megabytes = (double)memory / (1024 * 1024);
        megabytes = Math.Round(megabytes, 2);

        var entities = CurrentScene.Entities.GetAllEntities().Count;

        Dreambit.Window.SetTitle($"{GameName} {Time.FrameRate}fps | memory: {megabytes}MB | entities: {entities}");
    }
#endif
}