using System;
using System.Collections.Generic;

namespace Dreambit.Scripting;

/// <summary>
///     Reusable cutscene data loaded through <see cref="Resources"/>.
/// </summary>
[DreambitAssetType("dreambit.cutscene", FileExtension = DreambitAssetFileExtensions.Cutscene)]
public sealed class Cutscene : DreambitAsset
{
    private readonly List<CutsceneGroup> _groups;

    public Cutscene(IEnumerable<CutsceneGroup> groups)
    {
        ArgumentNullException.ThrowIfNull(groups);
        _groups = [.. groups];

        if (_groups.Count == 0)
            throw new ArgumentException("A cutscene must contain at least one group.", nameof(groups));
    }

    /// <summary>
    ///     Ordered groups of actions. Actions in a group run in parallel.
    /// </summary>
    public IReadOnlyList<CutsceneGroup> Groups => _groups;
}

public sealed class CutsceneGroup
{
    private readonly List<CutsceneAction> _actions;

    public CutsceneGroup(IEnumerable<CutsceneAction> actions)
    {
        ArgumentNullException.ThrowIfNull(actions);
        _actions = [.. actions];

        if (_actions.Count == 0)
            throw new ArgumentException("A cutscene group must contain at least one action.", nameof(actions));
    }

    public IReadOnlyList<CutsceneAction> Actions => _actions;
}

public sealed class CutsceneAction
{
    private readonly Dictionary<string, object> _arguments;

    public CutsceneAction(string script, IReadOnlyDictionary<string, object> arguments = null)
    {
        if (string.IsNullOrWhiteSpace(script))
            throw new ArgumentException("A cutscene action must name a script type.", nameof(script));

        Script = script.Trim();
        _arguments = arguments is null
            ? new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, object>(arguments, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     Simple or fully-qualified name of a <see cref="ScriptAction"/> type.
    /// </summary>
    public string Script { get; }

    /// <summary>
    ///     Constructor arguments for the script action.
    /// </summary>
    public IReadOnlyDictionary<string, object> Arguments => _arguments;
}
