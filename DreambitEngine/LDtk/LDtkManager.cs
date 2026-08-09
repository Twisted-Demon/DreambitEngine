using System;
using System.Collections.Generic;
using System.Reflection;
using Dreambit.ECS;
using LDtk;
using LDtk.Renderer;

namespace Dreambit.LDtk;

public class LDtkManager : Singleton<LDtkManager>
{
    private readonly Dictionary<Guid, Entity> _entityRefs = new();
    public readonly Dictionary<Guid, LDtkLevel> LoadedLevels = new();
    public readonly Dictionary<int, SpriteSheet> SpriteSheets = new();
    public LDtkLevel CurrentLevel;
    public LDtkWorld CurrentWorld;
    public LDtkFile LDtkFile;
    public ExampleRenderer LDtkRenderer;

    private bool _initialized = false;

    public void SetFile(string filePath)
    {
        LDtkFile = Resources.LoadAsset<LDtkFile>(filePath);
        SetUpSpriteSheets();
    }

    public void Initialize()
    {
        if (_initialized) return;
        LDtkRenderer = new ExampleRenderer(Core.SpriteBatch, Core.Instance.Content);
        _initialized = true;

    }

    public static void SetUpEntities(LDtkLevel level)
    {
        var types = ReflectionUtils.GetAllSubclasses(typeof(LDtkEntity<>));

        foreach (var type in types) InvokeSetUpEntitiesForType(type, level);
    }

    public void RegisterEntity(Guid iid, Entity entity)
    {
        if (!_entityRefs.ContainsKey(iid))
            return;

        _entityRefs[iid] = entity;
    }

    public void DeregisterEntity(Guid iid)
    {
        _entityRefs.Remove(iid);
    }

    private static void InvokeSetUpEntitiesForType(Type entityType, object ldtkLevel)
    {
        if (entityType.ContainsGenericParameters)
            return;

        var setUpMethod = entityType.GetMethod("SetUpEntities",
            BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);

        if (setUpMethod != null)
            setUpMethod.Invoke(null, [ldtkLevel]);
    }

    public void LoadWorld(Guid iid)
    {
        CurrentWorld = LDtkFile.LoadWorld(iid);
    }

    public LDtkLevel LoadLevel(Guid iid)
    {
        if (LoadedLevels.TryGetValue(iid, out var level))
        {
            CurrentLevel = level;
            return level;
        }

        level = CurrentWorld.LoadLevel(iid);

        LoadedLevels.Add(iid, level);

        CurrentLevel = level;

        return level;
    }

    private void SetUpSpriteSheets()
    {
        var defs = LDtkFile.Defs;
        foreach (var tileSet in defs.Tilesets)
        {
            if (string.IsNullOrEmpty(tileSet.RelPath)) continue;

            tileSet.RelPath = tileSet.RelPath.Replace(".png", "");

            var texturePath = tileSet.RelPath;
            var gridSize = tileSet.TileGridSize;

            var spriteSheet = SpriteSheet.Create(gridSize, texturePath);
            spriteSheet.AssetName = tileSet.Identifier;

            SpriteSheets.Add(tileSet.Uid, spriteSheet);
        }
    }
}
