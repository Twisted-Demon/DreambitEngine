using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PixelariaEngine;

public class Core : Game
{
    private readonly Logger<Core> _logger = new();

    public Core(string title, int width = 800, int height = 600)
    {
        GraphicsDeviceManager = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        Instance = this;

        GameName = title;
        PixelariaEngine.Window.SetTitle(title);
        PixelariaEngine.Window.SetSize(width, height);

        TargetElapsedTime = TimeSpan.FromSeconds((double)1 / 120); //set Target fps to 120

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
    }

    protected override void Update(GameTime gameTime)
    {
        Time.Update(gameTime);

        UpdateDebug();

        Input.PreUpdate();
        {
            if (NextScene != null)
                ChangeScenes();

            CurrentScene.Tick();
        }
        Input.PostUpdate();

        base.Update(gameTime);
    }

    private void UpdateDebug()
    {
#if DEBUG
        UpdateTitle();
#endif
    }

    protected override void Draw(GameTime gameTime)
    {
        CurrentScene.OnDraw();
        base.Draw(gameTime);
    }

    protected override void OnExiting(object sender, ExitingEventArgs args)
    {
        CurrentScene?.Terminate();
        base.OnExiting(sender, args);
    }

    private void ChangeScenes()
    {
        _logger.Info("Changing Scenes");
        CurrentScene?.Terminate();
        CurrentScene = NextScene;
        NextScene = null;
    }

    internal void SetNextScene(Scene scene)
    {
        NextScene = scene;
    }

#if DEBUG
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