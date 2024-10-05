using System;
using System.Diagnostics;
using System.Xml;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PixelariaEngine.Graphics;

namespace PixelariaEngine;

public class Core : Game
{
    public Core(string title, int width = 800, int height = 600)
    {
        GraphicsDeviceManager = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        Instance = this;
        
        GameName = title;
        PixelariaEngine.Window.SetTitle(title);
        PixelariaEngine.Window.SetSize(width, height);

        GraphicsDeviceManager.SynchronizeWithVerticalRetrace = false;
        GraphicsDeviceManager.ApplyChanges();
        TargetElapsedTime = TimeSpan.FromSeconds((double)1/120);
        
        Input.Init();
    }
    
    public static GraphicsDeviceManager GraphicsDeviceManager { get; private set; }
    public static SpriteBatch SpriteBatch { get; private set; }
    public Scene CurrentScene { get; private set; }
    public Scene NextScene { get; private set; }

    public static Core Instance { get; private set; }

    private readonly Logger<Core> _logger = new();
    private static string GameName { get; set; }

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
    
    #if DEBUG
    private float _debugElapsedTime = 0f;
    private void UpdateTitle()
    {
        _debugElapsedTime += Time.DeltaTime;
        
        if (_debugElapsedTime < 1f)
            return;
        _debugElapsedTime = 0f;
        var memory = Process.GetCurrentProcess().PrivateMemorySize64;
        var megabytes = (double) memory / (1024 * 1024);
        megabytes = Math.Round(megabytes, 2);
        PixelariaEngine.Window.SetTitle($"{GameName} {Time.FrameRate}fps | memory: {megabytes}MB");
    }
    #endif

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
}