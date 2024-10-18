using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace PixelariaEngine.Sandbox;

public class CutsceneAction
{
    [JsonProperty("action")]
    public string Action { get; set; }
    
    [JsonProperty("entity")]
    public string Entity { get; set; }
    
    [JsonProperty("duration")]
    public float Duration { get; set; }
    
    [JsonProperty("text")]
    public string Text { get; set; }
    
    [JsonProperty("waitForCompletion")]
    public bool WaitForCompletion { get; set; }
    
    [JsonProperty("destination")]
    public int[] Destination { get; set; }
    
    [JsonProperty("moveSpeed")]
    public float MoveSpeed { get; set; }
    
}