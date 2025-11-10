using Microsoft.Xna.Framework;

namespace Dreambit;

public record struct FloatRange(float Min, float Max);

public record struct IntRange(int Min, int Max);

public record struct Vector2Range(Vector2 Min, Vector2 Max);

public record struct Vector3Range(Vector3 Min, Vector3 Max);

public record struct Vector4Range(Vector4 Min, Vector4 Max);