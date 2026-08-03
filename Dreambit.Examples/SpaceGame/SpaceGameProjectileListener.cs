using System;
using Dreambit.ECS;

namespace Dreambit.Examples.SpaceGame;

public class SpaceGameProjectileListener : Component
{
    public Action OnProjectileHit;
    
    public void ProjectileHit()
    {
        OnProjectileHit?.Invoke();
    }
}