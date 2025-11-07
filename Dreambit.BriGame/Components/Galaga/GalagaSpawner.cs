using System.Collections.Generic;
using Dreambit.ECS;
using Microsoft.Xna.Framework;

namespace Dreambit.BriGame.Components.Galaga;

public static class GalagaSpawner
{
    private const float SpawnCd = 0.0f;
    private static float _spawnTick = 0.0f;
    private static bool _isSpawning = false;

    private static readonly Stack<GalagaEnemy> _enemiesToSpawn = [];
    
    public static GalagaEnemy SpawnBee(FormationSlot formationSlot, Vector3 spawnPosition)
    {
        var beeEntity = ECS.Entity.Create("bee", ["enemy", "bee"], createAt: spawnPosition);
        var beeComponent = beeEntity.AttachComponent<GalagaEnemy>();
        beeComponent.SetFormationSlot(formationSlot);
        beeEntity.Enabled = false;
        
        
        _enemiesToSpawn.Push(beeComponent);

        return beeComponent;
    }

    public static void SpawnRange(GalagaEnemy[] enemies)
    {
        foreach (var enemy in enemies)
        {
            _enemiesToSpawn.Push(enemy);
        }
    }

    public static void UpdateSpawner()
    {
        if (_enemiesToSpawn.Count == 0) return;
        
        if (_isSpawning)
        {
            _spawnTick += Time.DeltaTime;
            if (_spawnTick >= SpawnCd)
            {
                _isSpawning = false;
                _spawnTick = 0.0f;
            }
            else
            {
                return;
            }
        }

        _isSpawning = true;
        
        var nextEnemy = _enemiesToSpawn.Pop();
        nextEnemy.Entity.Enabled = true;
    }
}