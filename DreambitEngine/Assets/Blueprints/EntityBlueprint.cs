using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace Dreambit;

public class EntityBlueprint : DreambitAsset
{
    [JsonProperty("name", Required = Required.Always)]
    public string Name { get; set; } = string.Empty;

    [JsonProperty("guid")] public Guid Guid { get; set; } = Guid.NewGuid();

    [JsonProperty("tags")] public HashSet<string> Tags { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    [JsonProperty("enabled")] public bool Enabled { get; set; } = true;

    [JsonProperty("position")]
    [JsonConverter(typeof(Vector3Converter))]
    public Vector3 Position { get; set; } = Vector3.Zero;

    [JsonProperty("rotation")]
    [JsonConverter(typeof(Vector3Converter))]
    public Vector3 Rotation { get; set; } = Vector3.Zero;

    [JsonProperty("scale")]
    [JsonConverter(typeof(Vector3Converter))]
    public Vector3 Scale { get; set; } = Vector3.One;

    [JsonProperty("components")] public List<ComponentBlueprint> Components { get; set; } = [];

    [JsonProperty("children")] public List<EntityBlueprint> Children { get; set; } = [];

    /// <summary>
    /// When present, this entity is an instance of an external Entity Blueprint. The scene stores
    /// only the instance root transform and source identity; components and children are resolved
    /// from the source Blueprint every time the scene is loaded.
    /// </summary>
    [JsonProperty("blueprint_instance", NullValueHandling = NullValueHandling.Ignore)]
    public BlueprintInstanceReference BlueprintInstance { get; set; }

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

    public static EntityBlueprint LoadBlueprint(string path)
    {
        return Resources.LoadAsset<EntityBlueprint>(path);
    }
}
