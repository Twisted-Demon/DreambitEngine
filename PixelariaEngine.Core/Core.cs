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
        
        PixelariaEngine.Window.SetTitle(title);
        PixelariaEngine.Window.SetSize(width, height);
        Input.Init();
    }

    public static GraphicsDeviceManager GraphicsDeviceManager { get; private set; }
    public static SpriteBatch SpriteBatch { get; private set; }
    public Scene CurrentScene { get; private set; }
    public Scene NextScene { get; private set; }

    public static Core Instance { get; private set; }

    protected override void LoadContent()
    {
        base.LoadContent();
        
        SpriteBatch = new SpriteBatch(GraphicsDevice);
    }

    protected override void Update(GameTime gameTime)
    {
        Time.Update(gameTime);

        Input.PreUpdate();
        {
            if (NextScene != null)
                ChangeScenes();

            CurrentScene.Tick();
        }
        Input.PostUpdate();

        base.Update(gameTime);
        
        
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
        Log.Info("Changing Scenes");
        CurrentScene?.Terminate();
        CurrentScene = NextScene;
        NextScene = null;
    }

    internal void SetNextScene(Scene scene)
    {
        NextScene = scene;
    }
}