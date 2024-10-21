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
    private readonly Dictionary<Guid, Entity> EntityRefs = new();
    public readonly Dictionary<Guid, LDtkLevel> LoadedWorlds = new();
    public readonly Dictionary<int, SpriteSheet> SpriteSheets = new();
    public LDtkLevel CurrentLevel;
    public LDtkFile LDtkFile;
    public ExampleRenderer LDtkRenderer;
    public LDtkWorld LDtkWorld;

    public Logger<LDtkManager> Logger { get; set; } = new();

    public void SetUp(string ldtkFilePath)
    {
        LoadFileAndWorld(ldtkFilePath);
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
        if (!Instance.EntityRefs.ContainsKey(iid))
            return;

        Instance.EntityRefs.Add(iid, entity);
    }

    public static void DeregisterEntity(Guid iid)
    {
        Instance.EntityRefs.Remove(iid);
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

    private void LoadFileAndWorld(string ldtkFilePath)
    {
        LDtkFile = LDtkFile.FromFile(ldtkFilePath, Core.Instance.Content);
        LDtkWorld = LDtkFile.LoadSingleWorld();
    }

    private void SetUpSpriteSheets()
    {
        var defs = LDtkFile.Defs;
        foreach (var tileSet in defs.Tilesets)
        {
            if (string.IsNullOrEmpty(tileSet.RelPath)) continue;

            var texturePath = tileSet.RelPath.Replace(".png", "");
            var gridSize = tileSet.TileGridSize;

            var spriteSheet = SpriteSheet.Create(gridSize, texturePath);
            spriteSheet.AssetName = tileSet.Identifier;

            SpriteSheets.Add(tileSet.Uid, spriteSheet);
        }
    }

    public LDtkLevel LoadLDtkLevel(Guid iid)
    {
        if (LoadedWorlds.TryGetValue(iid, out var level))
        {
            CurrentLevel = level;
            return level;
        }

        level = LDtkWorld.LoadLevel(iid);

        LoadedWorlds.Add(iid, level);

        CurrentLevel = level;

        return level;
    }

    public LDtkLevel LoadLDtkLevel(string identifier)
    {
        return LDtkWorld.LoadLevel(identifier);
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