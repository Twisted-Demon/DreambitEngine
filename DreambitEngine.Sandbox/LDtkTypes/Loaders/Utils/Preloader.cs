using Dreambit.Sandbox.Utils;
using LDtk;

namespace Dreambit.Sandbox;

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