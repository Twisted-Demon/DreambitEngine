using Microsoft.Xna.Framework;

namespace PixelariaEngine;

public class Core : Game
{
    public Core(string title, int width = 800, int height = 600)
    {
        GraphicsDeviceManager = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        Instance = this;
        
        GameWindow.SetTitle(title);
        GameWindow.SetSize(width, height);
        Input.Init();
    }

    public static GraphicsDeviceManager GraphicsDeviceManager { get; private set; }
    public Scene CurrentScene { get; private set; }
    public Scene NextScene { get; private set; }

    public static Core Instance { get; private set; }

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

    public void SetNextScene(Scene scene)
    {
        NextScene = scene;
    }
}