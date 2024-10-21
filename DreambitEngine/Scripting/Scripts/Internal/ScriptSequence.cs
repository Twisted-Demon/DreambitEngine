using System.Collections.Generic;

namespace Dreambit.Scripting;

public class ScriptSequence
{
    private List<ScriptActionGroup> _scriptGroups = [];

    public Queue<ScriptActionGroup> GetScriptGroupQueue()
    {
        var queue = new Queue<ScriptActionGroup>();

        foreach (var scriptGroup in _scriptGroups)
        {
            queue.Enqueue(scriptGroup);

            foreach (var script in scriptGroup.Scripts)
            {
                script.IsComplete = false;
                script.IsStarted = false;
            }
        }
        
        return queue;
    }

    public void RegisterGroups(List<ScriptActionGroup> groups)
    {
        _scriptGroups = groups;
    }
}