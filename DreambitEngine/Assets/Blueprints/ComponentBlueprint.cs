using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Dreambit;

public class ComponentBlueprint
{
    [JsonProperty("type", Required = Required.Always)]
    public string Type { get; set; } = string.Empty;

    [JsonProperty("enabled")] public bool Enabled { get; set; } = true;

    [JsonProperty("properties")]
    public Dictionary<string, JToken> Properties { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}