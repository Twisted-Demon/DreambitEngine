using System;
using Newtonsoft.Json;

namespace Dreambit;

public abstract class PropertyConverter<T> : JsonConverter<T>, IPropertyConverterMarker
{
    public Type TargetType { get; } = typeof(T);
}