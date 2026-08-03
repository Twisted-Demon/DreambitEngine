# UiContainer

`UiContainer` is the base for elements that can own multiple children. It
provides `AddChild`, `RemoveChild`, and `ClearChildren` while maintaining parent,
layout, dependency, ID, focus, and pointer-capture state.

```csharp
var host = layout.GetRequired<UiContainer>("runtime-host");
host.AddChild(new UiText { Text = "Connected" });
```

Use a more specific panel when you need automatic layout. The base container's
children keep their own common layout values.

Do not modify the public `Children` list directly. The container methods perform
the attachment bookkeeping and reject duplicate IDs or invalid parent cycles.

