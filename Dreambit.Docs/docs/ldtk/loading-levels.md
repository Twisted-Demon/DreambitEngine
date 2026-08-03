# Loading worlds and levels

Set up the LDtk project after `Core` has initialized resources and before
switching to an LDtk scene:

```csharp
LDtkManager.Instance.SetUp("Worlds/game");
LDtkManager.Instance.LoadWorld(worldIid);
Scene.SetNextLDtkScene(levelIid);
```

The project must use LDtk's multi-world flag and a supported JSON version.
External levels are resolved relative to the world file and loaded through
Dreambit resources. Tileset relative `.png` paths are converted into runtime
sprite sheets using each tileset's grid size.

`LDtkScene` loads the requested level, creates an always-updating
`LDtkRenderer`, asks the manager to set up mapped entities, and adopts the LDtk
background color.

!!! warning "Use IIDs in current builds"
    `LDtkManager.LoadLDtkLevel(string identifier)` currently throws
    `NotImplementedException`. Although identifier-based scene helpers exist,
    choose the GUID/IID overload until that manager method is implemented.

The manager holds loaded levels and sprite sheets for the current project.
Set up a new manager/project deliberately when changing LDtk projects.

!!! warning "Current renderer attachment order"
    `LDtkRenderer.OnAddedToEntity` prerenders its `Level`, but `LDtkScene`
    currently assigns `Level` after attaching the component. If the bundled
    renderer does not tolerate null, assign the level before the component's
    attachment lifecycle (or move prerendering into the `Level` setter) before
    relying on the automatic scene path.
