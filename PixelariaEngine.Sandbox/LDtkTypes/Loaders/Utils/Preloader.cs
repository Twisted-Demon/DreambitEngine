using LDtk;
using PixelariaEngine.Sandbox.Utils;

namespace PixelariaEngine.Sandbox;

public partial class Preloader : LDtkEntity<Preloader>
{
    protected override void SetUp(LDtkLevel level)
    {
        var e = CreateEntity(this);

        var preloader = e.AttachComponent<AssetPreloader>();
        
        preloader.RegisterTextures(Textures);
        preloader.RegisterAudios(Audios);
        preloader.RegisterScripts(Scripts);
    }
}