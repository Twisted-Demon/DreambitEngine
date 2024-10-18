using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using PixelariaEngine.ECS;

namespace PixelariaEngine.Sandbox;

public partial class HolyBell
{
    public static Entity CreateInstance(Vector3 createAt)
    {
        var e = Entity.Create(nameof(HolyBell), ["spell"], createAt: createAt);
        var spellEffect = e.AttachComponent<SpellEffect>();
        spellEffect.SpellAnimationPath = "Animations/holybell";
        spellEffect.SoundEffectPath = "Sounds/arthasSpell";
        
        return e;
    }
}