using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace Dreambit;

public class EntityBlueprint : DreambitAsset
{
    [JsonProperty("name", Required = Required.Always)]
    public string Name { get; set; } = string.Empty;
    
    [JsonProperty("guid")] public Guid Guid { get; set; } = Guid.NewGuid();

    [JsonProperty("tags")] public HashSet<string> Tags { get; set; } = [];

    [JsonProperty("enabled")] public bool Enabled { get; set; } = true;

    // Prefer a proper Vector3 with a converter instead of a split string.
    [JsonProperty("position")]
    [JsonConverter(typeof(Vector3Converter))]
    public Vector3 Position { get; set; } = new(0, 0, 0);

    // Optional: rotation/scale if you want
    [JsonProperty("rotation")]
    [JsonConverter(typeof(Vector3Converter))]
    public Vector3 Rotation { get; set; } = new(0, 0, 0);

    [JsonProperty("scale")]
    [JsonConverter(typeof(Vector3Converter))]
    public Vector3 Scale { get; set; } = new(1, 1, 1);

    [JsonProperty("components")] public List<ComponentBlueprint> Components { get; set; } = [];
    
    [JsonProperty("children")] public IEnumerable<EntityBlueprint> Children { get; set; } = [];

    public IEnumerable<EntityBlueprint> FlattenedHirearchy()
    {
        var stack = new Stack<EntityBlueprint>();
        stack.Push(this);

        while (stack.TryPop(out var ent))
        {
            yield return ent;
            foreach (var child in ent.Children.Reverse())
            {
                stack.Push(child);
            }
        }
    }
}