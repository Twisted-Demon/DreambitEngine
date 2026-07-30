using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace Dreambit;

public class EntityBlueprint : DreambitAsset
{
    [JsonProperty("name", Required = Required.Always)]
    public string Name { get; set; } = string.Empty;
    
    [JsonProperty("guid")]
    public Guid Guid { get; set; } = Guid.NewGuid();
    
    [JsonProperty("tags")]
    public HashSet<string> Tags { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    
    [JsonProperty("enabled")]
    public bool Enabled { get; set; } = true;
    
    [JsonProperty("position")]
    [JsonConverter(typeof(Vector3Converter))]
    public Vector3 Position { get; set; } = Vector3.Zero;
    
    [JsonProperty("rotation")]
    [JsonConverter(typeof(Vector3Converter))]
    public Vector3 Rotation { get; set; } = Vector3.Zero;
    
    [JsonProperty("scale")]
    [JsonConverter(typeof(Vector3Converter))]
    public Vector3 Scale { get; set; } = Vector3.One;

    [JsonProperty("components")] 
    public List<ComponentBlueprint> Components { get; set; } = [];
    
    [JsonProperty("children")]
    public List<EntityBlueprint> Children { get; set; } = [];

    public IEnumerable<EntityBlueprint> FlattenedHierarchy()
    {
        var stack = new Stack<EntityBlueprint>();
        stack.Push(this);

        while (stack.TryPop(out var entity))
        {
            yield return entity;
            
            for (var i = entity.Children.Count - 1; i >= 0; i--)
                stack.Push(entity.Children[i]);
        }
    }
}