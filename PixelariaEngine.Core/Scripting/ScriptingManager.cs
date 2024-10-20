using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace PixelariaEngine.Scripting;

public class ScriptingManager
{
    private Queue<ScriptActionGroup> _groupQueue = [];
    private readonly Logger<ScriptingManager> _logger = new();
    private static readonly Dictionary<string, ScriptSequence> PreloadedGroups = [];
    public static ScriptingManager Instance => Scene.Instance.ScriptingManager;
    public Action OnScriptingStart;
    public Action OnScriptingEnd;
    public static bool IsCutsceneActive { get; internal set; }

    public ScriptingManager()
    {
        IsCutsceneActive = false;
    }

    public void StartCutscene(string cutsceneName, string fileExtension = ".yaml")
    {
        if (IsCutsceneActive || _groupQueue.Count != 0)
        {
            _logger.Warn("Unable to Start Cutscene {0}, Another cutscene is alreadya active", cutsceneName);
            return;
        }

        try
        {
            // check if we have the scene pre-loaded
            if (PreloadedGroups.ContainsKey(cutsceneName + fileExtension))
            {
                var preloadedSequence = PreloadedGroups[cutsceneName + fileExtension];
                _groupQueue = preloadedSequence.GetScriptGroupQueue();
            }
            else
            {
                var sequence = LoadSequenceFromFile(cutsceneName, fileExtension);
                _groupQueue = sequence.GetScriptGroupQueue();
            }
            
            IsCutsceneActive = true;
            OnScriptingStart?.Invoke();
            
        }
        catch (Exception e)
        {
            _logger.Warn("Unable to Start Cutscene {0}", cutsceneName);
            _logger.Error(e.Message);
        }
    }

    public static ScriptSequence LoadSequenceFromFile(string cutsceneName, string fileExtension = ".yaml")
    {
        var yamlText = File.ReadAllText("Content/" + cutsceneName + fileExtension);
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
                
        var yamlData = deserializer.Deserialize<List<dynamic>>(yamlText);
    
        var sequence = new ScriptSequence();
        var groups = new List<ScriptActionGroup>();

        foreach (var groupData in yamlData)
        {
            var group = new ScriptActionGroup();

            var scriptList = groupData["scriptGroup"] as List<object>;

            foreach (var scriptData in scriptList!)
            {
                var scriptDict = ConvertToDictionary(scriptData as Dictionary<object, object>);

                if (scriptDict != null)
                {
                    var script = ScriptFactory.CreateScript(scriptDict);
                    if (script == null) continue;
                    group.Scripts.Add(script);
                }
            }

            groups.Add(group);
        }

        sequence.RegisterGroups(groups);
        
        //register the sequence
        if (!PreloadedGroups.ContainsKey(cutsceneName + fileExtension))
        {
            PreloadedGroups.Add(cutsceneName + fileExtension, sequence);
        }

        return sequence;
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
            foreach (var script in currentGroup.Scripts)
            {
                script.OnGroupEnd();
            }
            _groupQueue.Dequeue();
        }

        if (_groupQueue.Count != 0) return;
        IsCutsceneActive = false;
        OnScriptingEnd?.Invoke();
    }

    internal void CleanUp()
    {
        OnScriptingStart = null;
        OnScriptingEnd = null;
    }

    // Helper method to convert Dictionary<object, object> to Dictionary<string, object>
    private static Dictionary<string, object> ConvertToDictionary(Dictionary<object, object> original)
    {
        if (original == null) return null;

        return original.ToDictionary(
            entry => entry.Key.ToString(), // Convert keys to string
            entry => entry.Value // Keep values as object
        );
    }
}