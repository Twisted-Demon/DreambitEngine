namespace Dreambit;

/// <summary>
/// Defines whether a scene is being run as gameplay or hosted by an authoring tool.
/// Editor scenes maintain real ECS state without invoking gameplay callbacks.
/// </summary>
public enum SceneExecutionMode
{
    Runtime,
    Editor
}
