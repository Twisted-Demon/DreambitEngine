using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dreambit.ECS;
using LDtk;
using LDtk.Renderer;

namespace Dreambit;

public class LDtkManager : Singleton<LDtkManager>
{
    private readonly Dictionary<Guid, Entity> _entityRefs = new();
    public readonly Dictionary<Guid, LDtkLevel> LoadedLevels = new();
    public readonly Dictionary<int, SpriteSheet> SpriteSheets = new();
    public LDtkLevel CurrentLevel;
    public LDtkWorld CurrentWorld;
    public LDtkFile LDtkFile;
    public ExampleRenderer LDtkRenderer;

    public void SetUp(string ldtkFilePath)
    {
        LoadFile(ldtkFilePath);
        SetUpSpriteSheets();
    }

    public void Init()
    {
        LDtkRenderer = new ExampleRenderer(Core.SpriteBatch, Core.Instance.Content);
    }

    public static void SetUpEntities(LDtkLevel ldtkLevel)
    {
        var types = GetDerivedTypes<LDtkEntity>();

        foreach (var type in types) InvokeSetUpEntitiesForType(type, ldtkLevel);
    }

    public static void RegisterEntity(Guid iid, Entity entity)
    {
        if (!Instance._entityRefs.ContainsKey(iid))
            return;

        Instance._entityRefs.Add(iid, entity);
    }

    public static void DeregisterEntity(Guid iid)
    {
        Instance._entityRefs.Remove(iid);
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

    private void LoadFile(string ldtkFilePath)
    {
        LDtkFile = Resources.LoadAsset<LDtkFile>(ldtkFilePath);
    }

    public void LoadWorld(Guid iid)
    {
        CurrentWorld = LDtkFile.DreambitLoadWorld(iid);
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

    public LDtkLevel LoadLDtkLevel(Guid iid)
    {
        if (LoadedLevels.TryGetValue(iid, out var level))
        {
            CurrentLevel = level;
            return level;
        }

        level = CurrentWorld.DreambitLoadLevel(iid);

        LoadedLevels.Add(iid, level);

        CurrentLevel = level;

        return level;
    }

    public LDtkLevel LoadLDtkLevel(string identifier)
    {
        throw new NotImplementedException("Not implemented");
    }

    private static List<Type> GetDerivedTypes<TBase>() where TBase : new()
    {
        return AppDomain.CurrentDomain
            .GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type.IsClass && !type.IsAbstract && type.IsSubclassOf(typeof(TBase)))
            .ToList();
    }
}