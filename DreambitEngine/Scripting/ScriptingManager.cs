using System;
using System.Collections.Generic;
using System.Linq;

namespace Dreambit.Scripting;

public class ScriptingManager
{
    private readonly Logger<ScriptingManager> _logger = new();
    public Action OnScriptingEnd;
    public Action OnScriptingStart;
    private Queue<ScriptActionGroup> _groupQueue = [];

    public ScriptingManager()
    {
        IsCutsceneActive = false;
    }

    public static ScriptingManager Instance => Scene.Instance.ScriptingManager;
    public static bool IsCutsceneActive { get; internal set; }

    /// <summary>
    ///     Loads and starts a cutscene asset through <see cref="Resources"/>.
    /// </summary>
    public bool StartCutscene(string assetName)
    {
        if (IsCutsceneActive || _groupQueue.Count != 0)
        {
            _logger.Warn("Unable to start cutscene {0}; another cutscene is already active", assetName);
            return false;
        }

        var cutscene = Resources.LoadAsset<Cutscene>(assetName);
        if (cutscene is not null)
            return StartCutscene(cutscene);

        _logger.Warn("Unable to load cutscene asset {0}", assetName);
        return false;
    }

    /// <summary>
    ///     Starts an already-loaded cutscene asset.
    /// </summary>
    public bool StartCutscene(Cutscene cutscene)
    {
        ArgumentNullException.ThrowIfNull(cutscene);

        var cutsceneName = string.IsNullOrWhiteSpace(cutscene.AssetName)
            ? "<unregistered cutscene>"
            : cutscene.AssetName;

        if (IsCutsceneActive || _groupQueue.Count != 0)
        {
            _logger.Warn("Unable to start cutscene {0}; another cutscene is already active", cutsceneName);
            return false;
        }

        try
        {
            _groupQueue = CreateRuntimeQueue(cutscene);
            IsCutsceneActive = true;
            OnScriptingStart?.Invoke();
            return true;
        }
        catch (Exception exception)
        {
            _groupQueue.Clear();
            _logger.Warn("Unable to start cutscene {0}", cutsceneName);
            _logger.Error(exception.ToString());
            return false;
        }
    }

    private static Queue<ScriptActionGroup> CreateRuntimeQueue(Cutscene cutscene)
    {
        var queue = new Queue<ScriptActionGroup>(cutscene.Groups.Count);
        foreach (var groupDefinition in cutscene.Groups)
        {
            var group = new ScriptActionGroup();
            foreach (var actionDefinition in groupDefinition.Actions)
                group.Scripts.Add(ScriptFactory.CreateScript(actionDefinition));
            queue.Enqueue(group);
        }

        return queue;
    }

    public void Update()
    {
        if (!IsCutsceneActive || _groupQueue.Count == 0)
            return;

        var currentGroup = _groupQueue.Peek();

        foreach (var script in currentGroup.Scripts
                     .Where(script => !script.IsComplete))
            script.Update();

        if (currentGroup.Completed())
        {
            _logger.Debug("Script Group Completed");
            foreach (var script in currentGroup.Scripts) script.OnGroupEnd();
            _groupQueue.Dequeue();
        }

        if (_groupQueue.Count != 0) return;
        IsCutsceneActive = false;
        OnScriptingEnd?.Invoke();
    }

    internal void CleanUp()
    {
        _groupQueue.Clear();
        IsCutsceneActive = false;
        OnScriptingStart = null;
        OnScriptingEnd = null;
    }
}
