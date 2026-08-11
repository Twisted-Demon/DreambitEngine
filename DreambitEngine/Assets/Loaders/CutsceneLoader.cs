using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dreambit.Scripting;
using YamlDotNet.Serialization;

namespace Dreambit;

/// <summary>
///     Loads cutscenes from baked YAML assets (.yamlb), including from pak files.
/// </summary>
public sealed class CutsceneLoader : AssetLoaderBase
{
    public override string Extension { get; } = ".yamlb";
    public override bool AddToDisposableList { get; } = true;
    public override Type TargetType { get; } = typeof(Cutscene);

    public override object Load(string assetName, string pakName, bool usePak, string contentDirectory)
    {
        using var stream = GetStream(GetPath(assetName), pakName, usePak, contentDirectory);
        var yaml = YmlbLoader.GetYamlString(stream);

        try
        {
            var sourceGroups = new DeserializerBuilder()
                .Build()
                .Deserialize<List<CutsceneGroupSource>>(yaml);

            if (sourceGroups is null || sourceGroups.Count == 0)
                throw new InvalidDataException("The cutscene does not contain any script groups.");

            var groups = sourceGroups
                .Select((group, groupIndex) => CreateGroup(group, groupIndex))
                .ToArray();

            return new Cutscene(groups) { AssetName = assetName };
        }
        catch (InvalidDataException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new InvalidDataException($"Cutscene '{assetName}' contains invalid YAML.", exception);
        }
    }

    private static CutsceneGroup CreateGroup(CutsceneGroupSource source, int groupIndex)
    {
        if (source?.ScriptGroup is null || source.ScriptGroup.Count == 0)
            throw new InvalidDataException($"Cutscene group {groupIndex + 1} does not contain any actions.");

        var actions = source.ScriptGroup
            .Select((action, actionIndex) => CreateAction(action, groupIndex, actionIndex))
            .ToArray();

        return new CutsceneGroup(actions);
    }

    private static CutsceneAction CreateAction(
        Dictionary<string, object> source,
        int groupIndex,
        int actionIndex)
    {
        if (source is null ||
            !source.TryGetValue("script", out var scriptValue) ||
            string.IsNullOrWhiteSpace(scriptValue?.ToString()))
        {
            throw new InvalidDataException(
                $"Cutscene group {groupIndex + 1}, action {actionIndex + 1} is missing 'script'.");
        }

        var arguments = source
            .Where(pair => !pair.Key.Equals("script", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);

        return new CutsceneAction(scriptValue.ToString(), arguments);
    }

    private sealed class CutsceneGroupSource
    {
        [YamlMember(Alias = "scriptGroup")]
        public List<Dictionary<string, object>> ScriptGroup { get; set; } = [];
    }
}
