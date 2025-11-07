using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using LDtk;

namespace Dreambit;

public static class LdtkLoader
{
    public static LDtkFile DreambitFomJson(string json, string filePath)
    {
        var file = JsonSerializer.Deserialize<LDtkFile>(json, Constants.JsonSourceGenerator.LDtkFile);
        if(file is null)
            throw new LDtkException($"Failed to Deserialize LDtk File");
        
        file.ValidateLDtkFile();

        file.FilePath = filePath;
        file.Content = Core.Instance.Content;
        
        return file;
    }

    public static LDtkWorld DreambitLoadWorld(this LDtkFile file, Guid iid)
    {
        foreach (var world in file.Worlds)
        {
            if (world.Iid != iid)
                continue;
            
            world.FilePath = file.FilePath;

            foreach (LDtkLevel level in world.Levels)
            {
                level.FilePath = Path.Join(Path.GetDirectoryName(file.FilePath), level.ExternalRelPath);
                level.FilePath = level.FilePath.Replace(".ldtkl", "");
                level.ExternalRelPath = level.ExternalRelPath?.Replace(".ldtkl", "");

                level.WorldFilePath = world.FilePath;
            }

            world.Content = Core.Instance.Content;
            return world;
        }

        return null;
    }

    public static LDtkLevel DreambitLoadLevel(this LDtkWorld world, Guid iid)
    {
        foreach (var level in world.Levels)
        {
            if (level.Iid != iid)
                continue;

            return world.DreambitLoadLevel(level);
        }

        throw new Exception($"No level with iid: {iid} found in this world");
    }
    
    public static LDtkLevel DreambitLoadLevel(this LDtkWorld world, string identifier)
    {
        foreach (var level in world.Levels)
        {
            if (level.Identifier != identifier)
                continue;

            return world.DreambitLoadLevel(level);
        }

        throw new Exception($"No level with identifier: {identifier} found in this world");
    }

    public static LDtkLevel DreambitLoadLevel(this LDtkWorld world, LDtkLevel rawLevel)
    {
        if (rawLevel.ExternalRelPath is null)
        {
            rawLevel.FilePath = world.FilePath;
            return rawLevel;
        }
        else
        {
            var path = Path.Join(Path.GetDirectoryName(world.FilePath), rawLevel.ExternalRelPath);
            
            var level = Resources.LoadAsset<LDtkLevel>(path);
            
            if(level is null)
                throw new Exception($"Failed to Load LDtk Level {path}");

            level.ExternalRelPath = rawLevel.ExternalRelPath;
            level.WorldFilePath = world.FilePath;
            level.FilePath = level.ExternalRelPath;

            SetLevelToLoaded(level);

            return level;
        }
    }

    private static void SetLevelToLoaded(LDtkLevel level)
    {
        var propInfo = level.GetType().GetProperty("Loaded", BindingFlags.Instance | BindingFlags.NonPublic);

        propInfo?.SetValue(level, true, null);
    }
    
    public static void ValidateLDtkFile(this LDtkFile file)
    {
        if (Version.Parse(file.JsonVersion) < Version.Parse(Constants.SupportedLDtkVersion))
        {
            throw new LDtkException("LDtk File Version is Not Supported. Please update your LDtk version");
        }

        if (file.Flags == null)
            throw new LDtkException(
                "LDtk file is missing required flags. Please enable them in the ldtk file flags in the UI");

        if (!file.Flags.Contains(Flag.MultiWorlds))
            throw new LDtkException("LDtk file is not a multiworld file. Please enable Multiworlds in the editor");
        
    }
}