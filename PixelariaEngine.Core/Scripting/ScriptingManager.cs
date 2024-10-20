using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace PixelariaEngine.Scripting;

public class ScriptingManager
{
    private readonly Queue<ScriptGroup> _groupQueue = [];
    private readonly Logger<ScriptingManager> _logger = new();
    public static bool IsCutsceneActive { get; set; }

    public void StartCutscene(string cutsceneName, string fileExtension = ".yaml")
    {
        if (IsCutsceneActive || _groupQueue.Count != 0)
        {
            _logger.Warn("Unable to Start Cutscene {0}, Another cutscene is alreadya active", cutsceneName);
            return;
        }

        try
        {
            var yamlText = File.ReadAllText("Content/" + cutsceneName + fileExtension);
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            var yamlData = deserializer.Deserialize<List<dynamic>>(yamlText);

            var groups = new List<ScriptGroup>();

            foreach (var groupData in yamlData)
            {
                var group = new ScriptGroup();

                var scriptList = groupData["scriptGroup"] as List<object>;

                foreach (var scriptData in scriptList!)
                {
                    var scriptDict = ConvertToDictionary(scriptData as Dictionary<object, object>);

                    if (scriptDict != null)
                    {
                        var script = ScriptFactory.CreateScript(scriptDict);
                        group.Scripts.Add(script);
                    }
                }

                groups.Add(group);
            }

            foreach (var group in groups) _groupQueue.Enqueue(group);

            IsCutsceneActive = true;
        }
        catch (Exception e)
        {
            _logger.Warn("Unable to Start Cutscene {0}", cutsceneName);
            _logger.Error(e.Message);
        }
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

        if (_groupQueue.Count == 0)
            IsCutsceneActive = false;
    }

    // Helper method to convert Dictionary<object, object> to Dictionary<string, object>
    private Dictionary<string, object> ConvertToDictionary(Dictionary<object, object> original)
    {
        if (original == null) return null;

        return original.ToDictionary(
            entry => entry.Key.ToString(), // Convert keys to string
            entry => entry.Value // Keep values as object
        );
    }
}