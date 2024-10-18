using System.Threading;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using PixelariaEngine.Graphics;

namespace PixelariaEngine.Sandbox.Scenes;

public class InitialLoadingScreen : Scene
{
    private AssetQueue<Texture2D> _textureQueue = new();
    private AssetQueue<SpriteSheetAnimation> _spriteSheetAnimationCache = new();
    private AssetQueue<SpriteSheet> _spriteSheetCache = new();   
    private readonly AssetQueue<SoundEffect> _soundEffectCache = new();
    
    private Task _spriteSheetLoaderTask;
    private Task _spriteAnimationLoaderTask;
    private Task _soundEffectLoaderTask;
    
    protected override void OnInitialize()
    {
        // Load Initial assets here
        
        //LoadData();
    }

    private void LoadData()
    {
        var mainthread = Thread.CurrentThread;
        
        _soundEffectCache.Enqueue("Sounds", [
            "arthasSpell"
        ]);
        
        _soundEffectLoaderTask = Task.Run(() => Resources.LoadAssetQueueAsync(_soundEffectCache));
        
        _spriteSheetCache.Enqueue("SpriteSheets", [
            "aria",
            "arthas",
            "darion",
            "holybell",
            "keyboard"
        ]);
        
        _spriteSheetLoaderTask = Task.Run(() => Resources.LoadAssetQueueAsync(_spriteSheetCache));
        
        _spriteSheetAnimationCache.Enqueue("Animations", [
            "aria_idle",
            "aria_run",
            "arthas_cast",
            "arthas_idle",
            "arthas_lick",
            "arthas_run",
            "arthas_sleep",
            "darion_idle",
            "darion_lick",
            "darion_run",
            "darion_sleep",
            "holybell",
        ]);
        
        _spriteAnimationLoaderTask = Task.Run(() => Resources.LoadAssetQueueAsync(_spriteSheetAnimationCache));
    }

    protected override void OnUpdate()
    {
        
        LDtkManager.Instance.SetUp("AriaWorld");

        var scene = SetNextLDtkScene(Worlds.AriaWorld.EntityTest);
        scene.AddRenderer<DefaultRenderer>();
        
    }
}