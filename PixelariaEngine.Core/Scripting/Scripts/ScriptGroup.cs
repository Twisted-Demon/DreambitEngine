using System.Collections.Generic;
using System.Linq;

namespace PixelariaEngine.Scripting;

public class ScriptGroup
{
    public readonly List<Script> Scripts = [];

    public bool Completed()
    {
        return Scripts.All(action => action.IsComplete);
    }
}