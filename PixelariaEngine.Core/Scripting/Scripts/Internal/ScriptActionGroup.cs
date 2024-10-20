using System.Collections.Generic;
using System.Linq;

namespace PixelariaEngine.Scripting;

public class ScriptActionGroup
{
    public readonly List<ScriptAction> Scripts = [];

    public bool Completed()
    {
        return Scripts.All(action => action.IsComplete);
    }
}