# Component lifecycle

The common runtime sequence is:

1. Construction
2. Required-component setup and field/property injection
3. `OnCreated`
4. Attachment to the entity and `OnAddedToEntity`
5. `OnUpdate` each game frame and `OnPhysicsUpdate` each physics step
6. Optional `OnDisabled` / `OnEnabled`
7. `OnRemovedFromEntity`
8. `OnDestroyed`

Blueprint-created components additionally receive `OnBeforeDeserialize` and
`OnAfterDeserialize` around property population.

Use `OnCreated` to initialize your component once dependencies are available.
Use `OnAddedToEntity` for registration that specifically depends on attachment.
Unregister symmetrically in both `OnRemovedFromEntity` and `OnDestroyed` when a
service cannot tolerate stale entries.

An exception in a callback is logged and faults the owning entity. Avoid hiding
exceptions inside update loops; the quarantine information makes the original
failure much easier to diagnose.

