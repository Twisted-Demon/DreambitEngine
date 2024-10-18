using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PixelariaEngine;

public class Core : Game
{
    private readonly Logger<Core> _logger = new();
    public static LogLevel LogLevel = LogLevel.Debug;

    public Core(string title, int width = 800, int height = 600)
    {
        GraphicsDeviceManager = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        Instance = this;

        GameName = title;
        PixelariaEngine.Window.Init();
        PixelariaEngine.Window.SetTitle(title);
        PixelariaEngine.Window.SetSize(width, height);
        
        //SetFixedTimeStep(false);

        TargetElapsedTime = TimeSpan.FromSeconds((double)1 / 120); //set Target fps to 120
        
    }

    protected override void Initialize()
    {
        base.Initialize();
        //PixelariaEngine.Window.SetBorderlessFullscreen(true);
        GraphicsDevice.BlendState = BlendState.NonPremultiplied;
        Input.Init();
    }

    public static Core Instance { get; private set; }
    public static GraphicsDeviceManager GraphicsDeviceManager { get; private set; }
    public static SpriteBatch SpriteBatch { get; private set; }
    public Scene CurrentScene { get; private set; }
    public Scene NextScene { get; private set; }
    private static string GameName { get; set; }

    public static void SetFixedTimeStep(bool value)
    {
        Instance.IsFixedTimeStep = value;
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

    private const float FixedPhysicsStep = 1f / 120f;
    private float accumulatedPhysicsTime = 0f;

    private void HandlePhysics()
    {
        accumulatedPhysicsTime += Time.DeltaTime;

        if (accumulatedPhysicsTime >= FixedPhysicsStep)
        {
            //handle physics here;
            accumulatedPhysicsTime = 0f;
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
        _logger.Info("Changing Scenes");
        CurrentScene?.Terminate();
        CurrentScene = NextScene;
        NextScene = null;
        accumulatedPhysicsTime = 0f;

        PhysicsSystem.Instance.CleanUp();
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
        PixelariaEngine.Window.SetTitle($"{GameName} {Time.FrameRate}fps | memory: {megabytes}MB");
    }
#endif
}