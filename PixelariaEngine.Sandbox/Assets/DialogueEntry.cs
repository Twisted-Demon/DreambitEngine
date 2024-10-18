using Newtonsoft.Json;

namespace PixelariaEngine.Sandbox;

public class DialogueEntry
{
    [JsonProperty("speaker")]
    public string Speaker { get; set; }
    
    [JsonProperty("text")]
    public string Text { get; set; }
}