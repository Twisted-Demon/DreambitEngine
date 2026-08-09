#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Dreambit.LDtk;

public static class LdtkJson
{
    public const string SupportedVersion = "1.5.3";

    public static JsonSerializerOptions Options { get; } = CreateOptions();

    public static LDtkFile DeserializeProject(string json)
    {
        var project = JsonSerializer.Deserialize<LDtkFile>(json, Options);
        return project ?? throw new LdtkException("The LDtk project JSON was empty or invalid.");
    }

    public static LDtkLevel DeserializeLevel(string json)
    {
        var level = JsonSerializer.Deserialize<LDtkLevel>(json, Options);
        return level ?? throw new LdtkException("The external LDtk level JSON was empty or invalid.");
    }

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            AllowTrailingCommas = true,
            PropertyNameCaseInsensitive = false,
            ReadCommentHandling = JsonCommentHandling.Skip,
        };

        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}

public class LdtkException : Exception
{
    public LdtkException(string message) : base(message)
    {
    }

    public LdtkException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

public sealed class LdtkWorldSelectionRequiredException : LdtkException
{
    public LdtkWorldSelectionRequiredException(IReadOnlyList<LDtkWorld> worlds)
        : base($"This LDtk project contains {worlds.Count} worlds. Select one by identifier or IID: " +
               string.Join(", ", worlds.Select(world => $"{world.Identifier} ({world.Iid})")))
    {
        Worlds = worlds;
    }

    public IReadOnlyList<LDtkWorld> Worlds { get; }
}
