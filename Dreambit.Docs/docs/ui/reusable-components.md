# Reusable UI components

File-backed components let several layouts reuse one authored subtree. Declare
component sources before the visual tree:

```xml
<Ui>
  <Ui.Components>
    <Component name="NavigationBar" source="Components/navigation.xml" />
  </Ui.Components>

  <NavigationBar id-prefix="nav." />
</Ui>
```

The source file must compose to exactly one visual root. `id-prefix` namespaces
every authored ID so multiple instances do not collide. Attributes on the
component instance override attributes on the source root.

You can also create a detached instance at runtime:

```csharp
var badge = frame.CreateComponent(
    "Ui/Components/status-badge.xml",
    "network.");

frame.Layout.GetRequired<UiContainer>("status-host").AddChild(badge);
```

Adding the subtree validates IDs and attaches it to the destination layout.
Use `GetRequired<T>("network.label")` after attachment.

Source paths are resolved inside the content root. Composition rejects paths
that escape that root and reports recursive/cyclic component references.

