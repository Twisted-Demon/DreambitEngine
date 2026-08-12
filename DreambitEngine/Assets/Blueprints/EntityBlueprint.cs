using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Dreambit.ECS;
using Newtonsoft.Json;

namespace Dreambit;

[DreambitAssetType("dreambit.blueprint.entity")]
public class EntityBlueprint : DreambitAsset
{
    [DreambitSerialize]
    [JsonProperty("name", Required = Required.Always)]
    public string Name { get; set; } = string.Empty;

    [DreambitSerialize]
    [JsonProperty("guid")] public Guid Guid { get; set; } = Guid.NewGuid();

    [DreambitSerialize]
    [JsonProperty("tags")] public HashSet<string> Tags { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    [DreambitSerialize]
    [JsonProperty("enabled")] public bool Enabled { get; set; } = true;

    [DreambitSerialize]
    [JsonProperty("position")]
    [JsonConverter(typeof(Vector3Converter))]
    public Vector3 Position { get; set; } = Vector3.Zero;

    [DreambitSerialize]
    [JsonProperty("rotation")]
    [JsonConverter(typeof(Vector3Converter))]
    public Vector3 Rotation { get; set; } = Vector3.Zero;

    [DreambitSerialize]
    [JsonProperty("scale")]
    [JsonConverter(typeof(Vector3Converter))]
    public Vector3 Scale { get; set; } = Vector3.One;

    [DreambitSerialize]
    [JsonProperty("components")] public List<ComponentBlueprint> Components { get; set; } = [];

    [DreambitSerialize]
    [JsonProperty("children")] public List<EntityBlueprint> Children { get; set; } = [];

    /// <summary>
    /// When present, this entity is an instance of an external Entity Blueprint. The scene stores
    /// only the instance root transform and source identity; components and children are resolved
    /// from the source Blueprint every time the scene is loaded.
    /// </summary>
    [DreambitSerialize]
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
