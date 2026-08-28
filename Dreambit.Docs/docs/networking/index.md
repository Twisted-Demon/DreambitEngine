# Networking

Dreambit networking is a server-authoritative networking foundation built around the existing
Dreambit ECS, `Scene`, Entity Blueprint, Scene Blueprint, asset, and lifecycle systems.

The networking layer is designed so game code can continue to be ordinary Dreambit gameplay code.
Networking provides transport, connection/session state, synchronized scene loading, network entity
identity, ownership metadata, replication, spawning/despawning, and typed messages. The game remains
responsible for rules such as movement, interaction, combat, inventory, validation, and presentation.

Related documentation: [Scenes](../core/scenes.md), [Entity blueprints](../ecs/blueprints.md),
[Scene/asset loading](../assets/resources.md), and [Entity Blueprint assets](../assets/blueprints.md).

!!! note "Implementation status"
    This page describes the current public networking APIs and protocol version 3. Direct IP is the
    available transport today; Steam P2P and other transports remain future integrations behind the
    same transport boundary.

## Architecture

The networking stack is divided into layers:

```text
┌──────────────────────────────────────────────┐
│                   Game                       │
│                                              │
│ Player input, interactions, inventory,       │
│ combat, enemies, game-specific messages      │
├──────────────────────────────────────────────┤
│               NetworkService                 │
│                                              │
│ Public Dreambit networking API               │
│ Spawn / Despawn / Send / ChangeScene / etc.  │
├──────────────────────────────────────────────┤
│               NetworkSession                 │
│                                              │
│ Handshake, peers, scene synchronization,     │
│ baselines, structural state, replication     │
├──────────────────────┬───────────────────────┤
│     NetworkWorld     │ Registries / Protocol │
│                      │                       │
│ Network Entity IDs   │ Message codecs        │
│ ownership            │ replication schemas   │
│ player mappings      │ packet encoding       │
├──────────────────────┴───────────────────────┤
│              INetworkTransport               │
│                                              │
│ DirectIpTransport today                      │
│ Other transports can be added later          │
└──────────────────────────────────────────────┘
```

`NetworkService` is the public game-facing API. `NetworkSession` owns the networking protocol and
connection lifecycle. `NetworkWorld` owns runtime networking identity for one synchronized `Scene`.
`INetworkTransport` isolates the session from the underlying connection technology.

This separation means game code should not need to know whether packets are ultimately being carried
over Direct IP, Steam P2P, or another future transport.

## Core concepts

Dreambit uses several distinct identifiers because they solve different problems.

| Type | Purpose |
| --- | --- |
| `NetworkPeerId` | Identifies a connected peer/player for the active session. |
| `NetworkEntityId` | Identifies one replicated Entity across machines. |
| `NetworkSceneEpoch` | Identifies the synchronized Scene generation in which network entities exist. |
| `NetworkStructuralRevision` | Orders reliable topology changes such as spawn, despawn, ownership, and player mappings. |
| `NetworkEntityRef` | Safe cross-machine Entity reference containing both the Scene epoch and network Entity ID. |

An ordinary Dreambit `Entity.Id` remains the local/runtime and serialized Scene identity. A
`NetworkEntityId` is the cross-machine identity used by the networking system.

For example, the server and client may materialize the same dynamic Blueprint as different local
Entities:

```text
Server:
    Entity.Id        = AAAAAAAA-....
    NetworkEntityId = 57

Client:
    Entity.Id        = BBBBBBBB-....
    NetworkEntityId = 57
```

The machines agree on `NetworkEntityId = 57`; they do not need matching runtime `Entity.Id` values
for dynamically spawned network entities.

### Scene-safe Entity references

A `NetworkEntityRef` combines the current Scene epoch with an Entity ID:

```csharp
var network = Core.Instance.Networking;

if (network.TryGetNetworkId(target, out var networkId))
{
    var reference = new NetworkEntityRef(
        network.SceneEpoch,
        networkId);
}
```

Resolve it later with:

```csharp
if (network.TryResolve(reference, out var entity))
{
    // The reference belongs to the current synchronized Scene.
}
```

A reference from an old Scene epoch does not resolve in the new Scene, even if a numeric
`NetworkEntityId` is later reused.

## Network roles

Dreambit defines four roles:

```csharp
NetworkRole.Offline
NetworkRole.Server
NetworkRole.Host
NetworkRole.Client
```

A **server** is authoritative and has no local client player.

A **host** is an authoritative listen server that also has a local logical peer.

A **client** connects to an authoritative server or host.

`Core.Instance.Networking` exposes convenient role checks:

```csharp
var network = Core.Instance.Networking;

if (network.IsServer)
{
    // Runs on dedicated servers and hosts.
}

if (network.IsClient)
{
    // Runs on remote clients and hosts.
}

if (network.IsHost)
{
    // Listen-server-specific behavior, if the game actually needs it.
}
```

Prefer `IsServer` when deciding who is allowed to change authoritative gameplay state.

## NetworkObject

An Entity participates in the network world by having a `NetworkObject` Component.

```csharp
public sealed class NetworkObject : Component
{
    [DreambitSerialize]
    public NetworkPresence Presence { get; set; }
}
```

`NetworkObject` is deliberately an inert authored marker. Runtime networking identity and ownership
are stored in `NetworkWorld`, not serialized into the Component.

This keeps session-specific values out of Scene and Entity Blueprint data.

### Presence

```csharp
NetworkPresence.Replicated
NetworkPresence.ServerOnly
NetworkPresence.ClientOnly
```

Their current meanings are:

| Presence | Server | Host | Remote client |
| --- | ---: | ---: | ---: |
| `Replicated` | yes | yes | yes |
| `ServerOnly` | yes | yes | no |
| `ClientOnly` | no | no | yes |

A host is authoritative, so `ClientOnly` objects are removed on a host just as they are on a
dedicated server.

## Configure networking before starting a session

Scene, message, and replication registrations are frozen while a session is active. Register the
game's networking contract first, then start the server/host/client.

`Core` creates its `NetworkService` before the first Scene is assigned. The service then survives
Scene transitions for the lifetime of the game. A project can therefore register its networking
contract during application boot, or idempotently from an initial menu Scene, without starting a
connection yet.

`NetworkOptions` are copied when a session starts. Edit them while offline; after `Stop()`, edit them
again before starting another session. An edit made during an active session does not reconfigure
that running session.

A game-level bootstrap can keep this configuration in one place:

```csharp
using Dreambit;
using Dreambit.Networking;
using Dreambit.Networking.Messaging;
using Dreambit.Networking.Transport;

public sealed class GameNetworking
{
    private readonly NetworkService _network;

    public GameNetworking()
    {
        _network = Core.Instance.Networking;
        Configure();
    }

    private void Configure()
    {
        _network.Options.GameBuildId = "my-game-0.1.0";
        _network.Options.ReplicationRate = 20;

        _network.Scenes.Register(
            "world",
            static () => new GameWorldScene());

        _network.Replication.Register<PlayerNetworkState>();

        _network.Messages.Register(
            messageId: 200,
            direction: NetworkMessageDirection.ClientToServer,
            maximumPayload: sizeof(float) * 2,
            codec: new PlayerInputMessageCodec(),
            handler: HandlePlayerInput);

        _network.PeerConnected += OnPeerConnected;
        _network.PeerDisconnected += OnPeerDisconnected;
        _network.ConnectionFailed += OnConnectionFailed;
    }

    private void HandlePlayerInput(
        NetworkMessageContext context,
        PlayerInputMessage message)
    {
        // Game-specific server handling.
    }

    private void OnPeerConnected(NetworkPeerId peer)
    {
    }

    private void OnPeerDisconnected(
        NetworkPeerId peer,
        TransportDisconnectReason reason,
        string? diagnostic)
    {
    }

    private void OnConnectionFailed(
        TransportDisconnectReason reason,
        string? diagnostic)
    {
    }
}
```

!!! warning
    Do not register new network Scenes, replicated Components, or typed messages after a session
    starts. The registries are intentionally frozen so every peer has one stable protocol contract.

### Menus and other local Scenes

Starting a network session does not turn the currently active Scene into a network Scene.

This supports the usual game flow:

```text
Core creates NetworkService
        ↓
local title/menu Scene starts
        ↓
player chooses offline, host, or join
        ↓
StartHost / StartServer / Connect
        ↓
the menu remains a local Scene
        ↓
server calls ChangeScene(key), or client receives SceneChange
        ↓
Core swaps to the catalog-created synchronized Scene
        ↓
NetworkWorld attaches and synchronization begins
```

While the menu remains local, `CurrentSceneKey` is `null`, `SceneEpoch` is `None`, and its Entities
do not receive network identities. A client may remain on its own connection screen until the server
requests a synchronized Scene. A host or server enters the first synchronized world explicitly with
`ChangeScene(key)`.

Once a session starts, direct Scene transitions are guarded even if the current Scene is still local.
This prevents one peer from silently leaving the authoritative flow. To cancel a connection or return
to ordinary local navigation, call `Stop()` first and then use `Scene.SetNextScene(...)`.

For an offline/local game, do not start a network session and continue using ordinary
`Scene.SetNextScene(...)` transitions.

## Starting a server or host

An authoritative session may start before the first Scene or while a local menu/bootstrap Scene is
already running. That local Scene remains outside the network world. The server or host chooses the
first synchronized Scene with `ChangeScene`.

For Direct IP:

```csharp
var network = Core.Instance.Networking;

network.StartHost(7777);
network.ChangeScene("world");
```

For a dedicated server:

```csharp
var network = Core.Instance.Networking;

network.StartServer(7777);
network.ChangeScene("world");
```

The order matters:

```text
configure registries
        ↓
local menu may already be running
        ↓
StartHost / StartServer
        ↓
Networking.ChangeScene("world")
        ↓
Core performs the Scene swap
        ↓
NetworkSession attaches a NetworkWorld
        ↓
Scene initialization and synchronization begin
```

The authoritative server controls synchronized Scene changes through
`NetworkService.ChangeScene`.

!!! warning
    `Scene.SetNextScene(...)` is intentionally rejected while any networking session is
    active. Use `Core.Instance.Networking.ChangeScene(key)` on the server/host. Clients follow the
    server's `SceneChange` message. Stop the session before returning to local-only Scene navigation.

## Connecting a client

The client configures the same Scene, message, and replication registrations, then connects. It may
do this while its local menu or connection Scene is already running:

```csharp
Core.Instance.Networking.Connect(
    "127.0.0.1",
    7777);
```

The client does **not** choose the synchronized Scene itself.

After the handshake, the server sends the client's Scene key and epoch. The client resolves that key
through its local `NetworkSceneCatalog`, constructs the matching `Scene`, synchronizes the world, and
only then allows the Scene to begin gameplay.

## Connection handshake

A new client does not immediately enter gameplay.

The current handshake validates:

- Dreambit networking protocol version.
- `NetworkOptions.GameBuildId`.
- baked content fingerprint, when one is available/configured.
- typed message schema hash.
- replicated Component schema hash.

Conceptually:

```text
CLIENT                                            SERVER
  |                                                  |
  |--------------- transport connected ------------>|
  |                                                  |
  |-------------------- Hello ---------------------->|
  |  protocol version                               |
  |  GameBuildId                                    |
  |  content fingerprint                            |
  |  message schema                                 |
  |  replication schema                             |
  |                                                  |
  |<------------------- Welcome ---------------------|
  |                   NetworkPeerId                  |
  |                                                  |
```

If the contracts do not match, the server rejects the client before it enters the synchronized
world.

This protects the game from situations such as one peer running an incompatible component schema or
different baked content.

## Synchronized Scene loading

Networking does not serialize a live `Scene` object across the network.

Instead, every game registers stable Scene keys:

```csharp
network.Scenes.Register(
    "village",
    static () => new VillageScene());

network.Scenes.Register(
    "mine",
    static () => new MineScene());
```

The server changes Scenes by key:

```csharp
network.ChangeScene("mine");
```

The server sends the key and a new `NetworkSceneEpoch`. Every client uses its own local factory to
construct the corresponding Scene.

This makes Scene construction local while keeping Scene **choice and timing** authoritative.

## Editor-authored Scene Blueprints

Dreambit's networking model works directly with Scenes constructed in Dreambit.Editor.

A `SceneBlueprint` is a `.scene` Dreambit asset containing:

- Scene name.
- serialized Entity Blueprints.
- optional LDtk reference.
- optional Tiled reference.
- Scene settings.

The recommended runtime architecture is:

```text
Dreambit.Editor
     ↓
Scenes/village.scene
     ↓
VillageScene.OnInitialize()
     ↓
LoadIntoSelf("Scenes/village")
     ↓
live Dreambit entities are materialized
     ↓
network authored-entity binding
     ↓
client baseline
     ↓
OnBegin()
```

The runtime `Scene` class supplies behavior and lifecycle. The `.scene` asset supplies the authored
data.

### Recommended Scene class

For an editor-authored Scene:

```csharp
using Dreambit;

public sealed class VillageScene : Scene
{
    private const string SceneAsset = "Scenes/village";

    protected override void OnInitialize()
    {
        LoadIntoSelf(SceneAsset);
    }

    protected override void OnBegin()
    {
        // On a synchronized client, the network baseline has already been
        // applied before OnBegin is allowed to run.
    }

    protected override void OnUpdate()
    {
    }

    protected override void OnPhysicsUpdate()
    {
    }

    protected override void OnEnd()
    {
    }
}
```

Register that runtime Scene class with the network catalog:

```csharp
network.Scenes.Register(
    "village",
    static () => new VillageScene());
```

Then the host starts with:

```csharp
network.StartHost(7777);
network.ChangeScene("village");
```

A client only connects:

```csharp
network.Connect("127.0.0.1", 7777);
```

The server tells the client to construct `"village"`.

### Why LoadIntoSelf belongs in OnInitialize

This placement is important.

A newly assigned network Scene first has a `NetworkWorld` attached to it. On its first `Tick`,
Dreambit executes:

```text
Scene.Created
    ↓
InitializeInternals()
    ↓
OnInitialize()
    ↓
Services.ActivateAll()
    ↓
Scene.Starting
    ↓
network startup gate
    ↓
OnBegin() only when the gate succeeds
    ↓
Scene.Running
```

Calling `LoadIntoSelf` in `OnInitialize` means the editor-authored entities exist **before** the
network startup gate tries to bind or synchronize them.

On the server:

```text
OnInitialize
    ↓
LoadIntoSelf("Scenes/village")
    ↓
all authored NetworkObjects now exist
    ↓
network startup gate
    ↓
BindServerAuthoredEntities
    ↓
OnBegin
```

On a remote client:

```text
OnInitialize
    ↓
LoadIntoSelf("Scenes/village")
    ↓
all local authored NetworkObjects now exist
    ↓
network startup gate
    ↓
send SceneLoaded
    ↓
wait for baseline
    ↓
bind authored network IDs
    ↓
apply replicated state
    ↓
send Ready
    ↓
startup gate opens
    ↓
OnBegin
```

!!! warning
    Do not defer `LoadIntoSelf` until `OnBegin` for a synchronized Scene. The networking startup gate
    needs the editor-authored entities to exist before `OnBegin` can be reached.

### What LoadIntoSelf actually does

The convenience overload:

```csharp
LoadIntoSelf("Scenes/village");
```

loads the baked asset:

```csharp
Resources.LoadAsset<SceneBlueprint>("Scenes/village");
```

and then uses:

```csharp
SceneBlueprintLoadOptions.Runtime
```

The runtime load path performs this order:

```text
optional LDtk materialization
        ↓
optional Tiled materialization
        ↓
Scene settings
        ↓
materialize boxed Entity Blueprint instances
        ↓
validate Blueprint/component structure
        ↓
create Entity hierarchies
        ↓
build Components
        ↓
deserialize Component data and references
        ↓
Component creation callbacks
```

If any Entity hierarchy fails to materialize, the Scene Blueprint spawn path rolls that hierarchy
back.

#### Runtime load options and Entity IDs

`SceneBlueprintLoadOptions.Runtime` preserves serialized Entity GUIDs by default:

```csharp
public bool PreserveEntityIds { get; init; } = true;
```

That behavior is essential for **authored network entities**.

The same editor-created Scene asset loaded by the server and client produces the same authored root
Entity IDs. Networking uses those stable IDs as the source locator during the baseline.

You normally do not need to provide custom load options:

```csharp
protected override void OnInitialize()
{
    LoadIntoSelf("Scenes/village");
}
```

is already the correct strict runtime path.

### How an editor-authored replicated Entity binds

Suppose the editor-authored Scene contains:

```text
Village
└── AncientDoor
    ├── GUID: 8a3f...
    ├── NetworkObject
    │   └── Presence = Replicated
    ├── DoorNetworkState
    ├── SpriteDrawer
    └── BoxCollider
```

Both machines load the same `.scene`.

The server sees the `NetworkObject` and assigns a runtime network identity:

```text
source Entity GUID 8a3f...  ->  NetworkEntityId 47
```

The server includes that authored binding in the client's baseline.

The client already has its own `AncientDoor`, loaded from the same Scene Blueprint with the same
serialized GUID:

```text
client Entity GUID 8a3f...
```

The client therefore registers its existing local Entity as:

```text
NetworkEntityId 47
```

No duplicate door is spawned. Networking binds the already-authored object to the server's runtime
network identity.

This is the central difference between an **authored network Entity** and a **dynamic network
spawn**.

### Boxed Entity Blueprint instances in an editor Scene

A Scene Blueprint may contain an instance of an external `EntityBlueprint`.

For example, the editor Scene can place three instances of:

```text
Blueprints/door.blueprint
```

instead of duplicating the full Component/child data into the Scene asset.

When `LoadIntoSelf` materializes a boxed Blueprint instance, Dreambit:

1. resolves and clones the source Entity Blueprint;
2. remaps the source root GUID to the **instance root GUID stored in the Scene**;
3. creates deterministic descendant GUIDs from the instance root and source child GUIDs;
4. remaps serialized Entity/Component references that point into the hierarchy;
5. applies the instance's root position, rotation, and scale.

That means multiple linked instances of the same source Blueprint can remain distinct and stable in
an editor-authored Scene.

If the source Blueprint root contains:

```text
NetworkObject
DoorNetworkState
```

each editor-placed instance can become a separate authored network root because each instance root
has its own stable Scene GUID.

For example:

```text
door.blueprint source
        |
        +------ editor instance A -> source GUID A -> NetworkEntityId 21
        |
        +------ editor instance B -> source GUID B -> NetworkEntityId 22
        |
        +------ editor instance C -> source GUID C -> NetworkEntityId 23
```

This is useful for authored doors, chests, NPCs, harvest nodes, world objects, and other reusable
Blueprint-based content.

### A reusable Blueprint-backed Scene class

If most game logic lives in Components and Scene services, a game can use one small runtime Scene
class for many editor-authored Scene assets:

```csharp
using Dreambit;

public sealed class BlueprintBackedScene : Scene
{
    private readonly string _sceneAssetName;

    public BlueprintBackedScene(string sceneAssetName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sceneAssetName);
        _sceneAssetName = sceneAssetName;
    }

    protected override void OnInitialize()
    {
        LoadIntoSelf(_sceneAssetName);
    }
}
```

Register different synchronized Scene keys:

```csharp
network.Scenes.Register(
    "village",
    static () => new BlueprintBackedScene("Scenes/village"));

network.Scenes.Register(
    "forest",
    static () => new BlueprintBackedScene("Scenes/forest"));

network.Scenes.Register(
    "mine",
    static () => new BlueprintBackedScene("Scenes/mine"));
```

The editor controls the Scene contents. The network key is the stable protocol-facing name.

If a particular Scene needs custom runtime lifecycle behavior, use a dedicated `Scene` subclass and
still call `LoadIntoSelf` from `OnInitialize`.

### Changing between editor-authored Scenes

The server/host changes Scene:

```csharp
Core.Instance.Networking.ChangeScene("forest");
```

The flow is:

```text
SERVER
    ChangeScene("forest")
        ↓
    catalog creates BlueprintBackedScene("Scenes/forest")
        ↓
    clients receive SceneChange("forest", new epoch)
        ↓
    Core swaps the server Scene
        ↓
    server Scene OnInitialize -> LoadIntoSelf
        ↓
    authored network objects bind
        ↓
    each client creates the same catalog Scene
        ↓
    client OnInitialize -> LoadIntoSelf
        ↓
    client sends SceneLoaded
        ↓
    server sends baseline
        ↓
    client binds authored GUIDs + dynamic entities + state
        ↓
    client Ready
        ↓
    client OnBegin
```

Every synchronized transition receives a new `NetworkSceneEpoch`, so delayed traffic from the old
Scene cannot be mistaken for state in the new Scene.

### LDtk and Tiled references inside a Scene Blueprint

`LoadIntoSelf` also supports `SceneBlueprint.LDtk` and `SceneBlueprint.Tiled`.

Their materialization happens before the ordinary `SceneBlueprint.Entities` collection is created.
After `OnInitialize` completes, networking sees the resulting live Scene and scans it for
`NetworkObject` Components.

Networking does not have a separate LDtk/Tiled network protocol. The synchronized unit remains the
resulting Dreambit `Scene`.

For ordinary editor-authored network gameplay entities, prefer Dreambit Scene entities or linked
Entity Blueprint instances whose serialized identity is under Dreambit's control. If imported map
generation is used to create network roots, those generated entities must also have stable,
matching source identity on every peer.

## Authored entities and dynamic entities

Dreambit supports two different creation models.

### Authored network entities

These already exist in the synchronized Scene Blueprint.

Examples:

- doors;
- chests;
- persistent NPCs;
- world switches;
- harvest nodes;
- authored enemies;
- interactable objects.

They are matched between server and client by stable source Entity GUID and then assigned a
`NetworkEntityId`.

### Dynamic network entities

These are created while the game is running.

Examples:

- connected player characters;
- projectiles;
- dropped items;
- spawned enemies;
- temporary effects that genuinely require network identity.

Dynamic network entities are spawned by the **server** from an `EntityBlueprint`:

```csharp
var player = network.Spawn(
    playerBlueprint,
    new NetworkSpawnOptions
    {
        Owner = peer,
        DestroyWithOwner = true,
        Position = spawnPosition
    });
```

The Blueprint must have a stable `AssetId`. The root must contain exactly one `NetworkObject` with
`NetworkPresence.Replicated`.

A dynamic network Blueprint cannot contain another `NetworkObject` in a descendant hierarchy. Spawn
each independent network root separately.

### Dynamic spawn synchronization

A live spawn is a small transaction:

```text
Spawn
    ↓
initial replicated Component state
    ↓
initial replicated Component state
    ↓
...
    ↓
SpawnReady
```

On a remote client, Dreambit materializes the Blueprint but suspends gameplay updates across the
hierarchy until all initial state has been applied.

Conceptually:

```text
FRAME 100
    Spawn arrives
    Entity Blueprint is materialized
    UpdatesSuspended = true

    Scene.Tick
        Entity.Update -> blocked
        Entity.PhysicsUpdate -> blocked

FRAME 101
    initial Component state arrives
    values are applied

    Scene.Tick
        still blocked

FRAME 102
    SpawnReady arrives
    all expected initial Components are present
    UpdatesSuspended = false

    Scene.Tick
        first gameplay update sees authoritative initial state
```

This prevents a newly spawned projectile/player/enemy from running one frame with stale Blueprint
defaults before its server state arrives.

## Component replication

Replication is intended for persistent authoritative state.

A Component opts in with `[NetworkReplicated]`, and individual members opt in with `[Replicated]`.

```csharp
using Dreambit.ECS;
using Dreambit.Networking.Replication;
using Microsoft.Xna.Framework;

[NetworkReplicated(100)]
public sealed class PlayerNetworkState : Component
{
    [Replicated(1)]
    public Vector2 Position { get; set; }

    [Replicated(2)]
    public Vector2 Velocity { get; set; }

    [Replicated(3)]
    public ushort Health { get; set; }

    [Replicated(4)]
    public PlayerFacing Facing { get; set; }
}

public enum PlayerFacing : byte
{
    Down,
    Left,
    Right,
    Up
}
```

Register it before starting networking:

```csharp
network.Replication.Register<PlayerNetworkState>();
```

The numeric IDs are part of the network schema. Treat them as stable protocol identifiers.

### Supported automatic member types

Automatic replication currently supports:

- `bool`;
- signed/unsigned integer primitives;
- `float`;
- `double`;
- `Guid`;
- bounded UTF-8 `string`;
- `AssetId`;
- `NetworkEntityRef`;
- `Vector2`;
- `Vector3`;
- `Vector4`;
- `Quaternion`;
- `Color`;
- enums.

For complex state, register a custom Component codec.

### Do not replicate raw ECS references

This is rejected:

```csharp
[Replicated(1)]
public Entity Target { get; set; }
```

Use:

```csharp
[Replicated(1)]
public NetworkEntityRef Target { get; set; }
```

Similarly, replicate an `AssetId` rather than a `DreambitAsset` object reference.

### Root-only Component replication

Version 1 replication binds registered replicated Components on the network root only.

Valid:

```text
Player
├── NetworkObject
├── PlayerNetworkState      <- replicated
├── PlayerMotor
├── Visual
│   └── SpriteDrawer
└── InteractionAnchor
    └── InteractionComponent
```

Invalid:

```text
Player
├── NetworkObject
└── Visual
    └── AppearanceNetworkState   <- registered [NetworkReplicated] Component
```

Dreambit rejects the invalid shape rather than silently ignoring the descendant Component.

Keep the network state on the same Entity as `NetworkObject`. Children can still contain ordinary
rendering, collider, interaction, animation, audio, and other non-replicated Components.

### Replication is server-authoritative

Ownership does **not** transfer replication authority to the client.

The normal flow is:

```text
CLIENT
    reads input
        ↓
    sends command/request
        ↓
SERVER
    validates request
        ↓
    updates authoritative ECS state
        ↓
    runs gameplay/physics
        ↓
    publishes replicated state
        ↓
CLIENT
    receives authoritative state
        ↓
    presents/interpolates it
```

A client owning an Entity is game metadata. It means things such as:

- this is the peer's player;
- this peer is allowed to request actions for this Entity;
- this Entity may be destroyed when the peer disconnects.

It does not mean the client can overwrite authoritative replicated state on the server.

## Snapshots

The server advances its network tick with the fixed 60 Hz physics simulation.

Replicated state snapshots are sent according to:

```csharp
network.Options.ReplicationRate
```

The default is 20 Hz.

Snapshots are full Component states sent as `UnreliableSequenced` traffic. If an older snapshot is
lost, the next full snapshot can heal the state.

For example:

```text
snapshot sequence 100 -> received
snapshot sequence 101 -> lost
snapshot sequence 102 -> received

client uses 102
```

There is no reason to retransmit snapshot 101 after 102 has already arrived.

Dreambit tracks snapshot sequence state per:

```text
NetworkEntityId + replicated Component ID
```

and ignores old/out-of-order state.

The current system does not yet perform delta compression, dirty masks, relevancy filtering,
prediction, or general-purpose transform interpolation. Those can be layered on later without
changing the session/world identity model.

## Structural revisions

Persistent topology changes must be ordered reliably.

Dreambit increments `NetworkStructuralRevision` for changes such as:

```text
Spawn
Despawn
Ownership
PlayerEntity mapping
```

Suppose the client understands world revision 15, and the server creates Entity 50 at revision 16.

An unreliable snapshot for Entity 50 can race ahead of the reliable Spawn:

```text
client topology: revision 15

UDP snapshot:
    revision 16
    Entity 50

reliable Spawn has not arrived yet
```

The client knows it does not understand topology revision 16 yet, so it drops the snapshot.

Later:

```text
reliable Spawn revision 16
    ↓
Entity 50 now exists
    ↓
next full snapshot arrives
    ↓
state heals automatically
```

This lets Dreambit use reliable traffic for topology and inexpensive lossy traffic for transient
state without applying state to entities that do not exist yet.

## Scene epochs

`NetworkSceneEpoch` applies the same protection across Scene transitions.

For example:

```text
Village
    epoch 7
    Entity 23 = sheep

transition

Mine
    epoch 8
```

A delayed packet from epoch 7 cannot be interpreted as state in epoch 8.

This is also why `NetworkEntityRef` contains both the epoch and Entity ID.

## Typed gameplay messages

Persistent state and gameplay intent are different problems.

Use replication for:

```text
health
door open/closed state
authoritative position
animation state
current item identity
world state
```

Use typed messages for:

```text
player input
interact request
craft request
inventory move request
dialogue choice
server notification
one-shot gameplay event
```

### Define a message

```csharp
public readonly record struct PlayerInputMessage(
    float X,
    float Y);
```

### Define its codec

```csharp
using Dreambit.Networking.Messaging;
using Dreambit.Networking.Protocol;

public sealed class PlayerInputMessageCodec
    : INetworkMessageCodec<PlayerInputMessage>
{
    public void Write(
        NetworkWriter writer,
        PlayerInputMessage message)
    {
        writer.WriteSingle(message.X);
        writer.WriteSingle(message.Y);
    }

    public PlayerInputMessage Read(
        ref NetworkReader reader)
    {
        return new PlayerInputMessage(
            reader.ReadSingle(),
            reader.ReadSingle());
    }
}
```

### Register it

```csharp
network.Messages.Register(
    messageId: 200,
    direction: NetworkMessageDirection.ClientToServer,
    maximumPayload: sizeof(float) * 2,
    codec: new PlayerInputMessageCodec(),
    handler: HandlePlayerInput);
```

The handler receives a `NetworkMessageContext` containing:

```csharp
context.Sender
context.SceneEpoch
context.ServerTick
```

`context.Sender` is particularly important on the authoritative server because it tells game code
which peer actually sent the request.

### Send continuous input

Movement input is normally disposable if a newer input sample already exists, so it can use
unreliable sequenced delivery:

```csharp
using Dreambit.Networking.Transport;

network.SendToServer(
    new PlayerInputMessage(move.X, move.Y),
    NetworkDelivery.UnreliableSequenced);
```

### Send discrete gameplay actions

A one-time action such as interaction is normally reliable:

```csharp
network.SendToServer(
    new InteractRequest(targetReference));
```

`ReliableOrdered` is the default.

### Current logical channels

The session currently uses:

| Channel | Delivery | Purpose |
| ---: | --- | --- |
| 0 | reliable ordered | protocol, handshake, Scene sync, baseline, spawn/despawn, initial state |
| 1 | reliable ordered | typed gameplay messages |
| 2 | unreliable sequenced | server-to-client typed messages and snapshots |
| 3 | unreliable sequenced | client-to-server typed messages |

Game code normally chooses delivery semantics, not raw channel numbers.

## Ownership and player entities

`NetworkSpawnOptions.Owner` associates an Entity with a peer.

```csharp
var player = network.Spawn(
    playerBlueprint,
    new NetworkSpawnOptions
    {
        Owner = peer,
        DestroyWithOwner = true,
        Position = spawnPosition
    });
```

`NetworkService.SetPlayerEntity` separately marks which network Entity represents the peer's actual
player character:

```csharp
network.SetPlayerEntity(peer, player);
```

These are related but different concepts.

```text
Owner
    "peer 5 owns this network object"

PlayerEntity mapping
    "this particular network object is peer 5's player character"
```

On the local client:

```csharp
var player = Core.Instance.Networking.LocalPlayerEntity;
```

can then resolve the local player's synchronized Entity.

Ownership checks are also available:

```csharp
if (network.IsOwnedByLocalPeer(entity))
{
    // Local-player presentation/input behavior.
}
```

### Game-side player coordinator

The engine does not define what a game's "player" means. A game-level coordinator can own that
mapping and spawn policy.

```csharp
public sealed class PlayerNetworkCoordinator
{
    private readonly NetworkService _network;
    private readonly EntityBlueprint _playerBlueprint;
    private readonly Dictionary<NetworkPeerId, Entity> _players = [];

    public PlayerNetworkCoordinator(
        NetworkService network,
        EntityBlueprint playerBlueprint)
    {
        _network = network;
        _playerBlueprint = playerBlueprint;
    }

    public Entity SpawnPlayer(
        NetworkPeerId peer,
        Vector3 position)
    {
        var player = _network.Spawn(
            _playerBlueprint,
            new NetworkSpawnOptions
            {
                Owner = peer,
                DestroyWithOwner = true,
                Position = position
            });

        _network.SetPlayerEntity(peer, player);
        _players.Add(peer, player);

        return player;
    }

    public bool TryGetPlayer(
        NetworkPeerId peer,
        out Entity? player)
    {
        return _players.TryGetValue(peer, out player);
    }
}
```

Game code can then route a client request through `context.Sender`:

```csharp
private void HandlePlayerInput(
    NetworkMessageContext context,
    PlayerInputMessage message)
{
    if (!network.IsServer)
        return;

    if (!players.TryGetPlayer(
            context.Sender,
            out var player) ||
        player is null)
    {
        return;
    }

    var input = player.GetComponent<PlayerInputState>();
    input.Movement = new Vector2(
        message.X,
        message.Y);
}
```

The networking layer delivers and identifies the request. The normal gameplay Component performs
the actual movement.

## Despawning and disconnect ownership

The authoritative server can explicitly despawn a network Entity:

```csharp
network.Despawn(entity);
```

The topology revision advances, remote clients receive the despawn, and each `NetworkWorld` removes
the network registration.

A registered `NetworkObject` also informs `NetworkWorld` when its Entity is destroyed, allowing
ordinary ECS destruction to be reconciled with network topology.

For intentional network gameplay removal, prefer the explicit networking API:

```csharp
network.Despawn(entity);
```

because it makes the authoritative intent clear.

### DestroyWithOwner

Dynamic spawns default to:

```csharp
DestroyWithOwner = true;
```

When a peer disconnects:

```text
DestroyWithOwner = true
    -> authoritative Entity is despawned

DestroyWithOwner = false
    -> Entity remains
    -> ownership is cleared
```

Examples:

| Object | Typical policy |
| --- | --- |
| player character | destroy |
| temporary owned projectile | destroy |
| dropped persistent item | keep |
| placed building | keep |
| world chest | usually authored/not peer-owned |

## NetworkTransform2D authority

`NetworkTransform2D` is registered by Dreambit automatically. Add it beside `NetworkObject` and
choose its serialized `Authority` value in the blueprint inspector:

| Authority | Pose source | Typical use |
| --- | --- | --- |
| `Server` | dedicated server or host | monsters, NPCs, world objects, strict authoritative movement |
| `Client` | the Entity's assigned owning peer | responsive player movement in cooperative/casual games |
| `Both` | server/host and assigned owning peer | deliberately shared control; latest server-processed pose wins |

`Server` is the safe default. `Client` does not allow every client to move the Entity. The server
accepts a client pose only when the sending peer matches the Entity's runtime owner from
`NetworkSpawnOptions.Owner` or `NetworkService.SetOwner`. It then applies that pose locally and
includes it in normal snapshots sent to the other clients.

```csharp
var player = Network.Spawn(
    playerBlueprint,
    new NetworkSpawnOptions
    {
        Owner = peerId
    });

player.GetComponent<NetworkTransform2D>().Authority =
    TransformAuthority.Client;
```

A listen host's locally owned Entity also works with `Client` authority without a transport
round-trip. An Entity using `Client` authority but having no assigned peer owner has no client pose
source, so assign ownership when spawning it.

The current component smooths remote position, rotation, and scale toward the newest server-relayed
pose and snaps errors larger than `SnapDistance`. `ApplyToLocalOwner = false` remains available for a
future predicted local controller using strict `Server` authority. Input history, prediction,
reconciliation, and buffered snapshot interpolation are not implemented yet.

## Example: interacting with an authored door

Assume the editor-authored `Village.scene` contains a door with:

```text
AncientDoor
├── NetworkObject (Replicated)
├── DoorNetworkState
├── DoorInteraction
├── SpriteDrawer
└── BoxCollider
```

The state Component:

```csharp
using Dreambit.ECS;
using Dreambit.Networking.Replication;

[NetworkReplicated(110)]
public sealed class DoorNetworkState : Component
{
    [Replicated(1)]
    public bool IsOpen { get; set; }
}
```

The client does not directly decide that the authoritative door is open. It sends intent.

```csharp
public readonly record struct InteractRequest(
    NetworkEntityRef Target);
```

A codec can serialize the safe reference:

```csharp
using Dreambit.Networking;
using Dreambit.Networking.Messaging;
using Dreambit.Networking.Protocol;

public sealed class InteractRequestCodec
    : INetworkMessageCodec<InteractRequest>
{
    public void Write(
        NetworkWriter writer,
        InteractRequest message)
    {
        writer.WriteUInt32(
            message.Target.SceneEpoch.Value);
        writer.WriteUInt64(
            message.Target.EntityId.Value);
    }

    public InteractRequest Read(
        ref NetworkReader reader)
    {
        return new InteractRequest(
            new NetworkEntityRef(
                new NetworkSceneEpoch(
                    reader.ReadUInt32()),
                new NetworkEntityId(
                    reader.ReadUInt64())));
    }
}
```

Register it:

```csharp
network.Messages.Register(
    messageId: 210,
    direction: NetworkMessageDirection.ClientToServer,
    maximumPayload: sizeof(uint) + sizeof(ulong),
    codec: new InteractRequestCodec(),
    handler: HandleInteractRequest);
```

Create a reference when the local interaction system chooses a target:

```csharp
private static NetworkEntityRef GetNetworkReference(
    NetworkService network,
    Entity entity)
{
    return network.TryGetNetworkId(
        entity,
        out var id)
        ? new NetworkEntityRef(
            network.SceneEpoch,
            id)
        : NetworkEntityRef.None;
}
```

Send it:

```csharp
var target = GetNetworkReference(
    network,
    interactableEntity);

if (target.IsValid)
{
    network.SendToServer(
        new InteractRequest(target));
}
```

Server handling should validate the request against ordinary gameplay rules:

```text
request from peer 3
    ↓
does the NetworkEntityRef resolve?
    ↓
does peer 3 have a valid player?
    ↓
is that player close enough?
    ↓
is the player facing the object?
    ↓
is the object currently interactable?
    ↓
execute the normal interaction
    ↓
DoorNetworkState.IsOpen = true
    ↓
replication distributes the authoritative result
```

This pattern is preferable to replicating a temporary `WantsToInteract` boolean every snapshot.

**Client requests intent. The server determines truth. Replication distributes truth.**

## Example: inventory

Inventory actions use the same separation.

Instead of trusting a client to send a new authoritative inventory array, send a request such as:

```text
MoveInventoryItemRequest
    source slot
    target slot
```

The server:

```text
receives request
    ↓
identifies sender
    ↓
validates ownership and slot rules
    ↓
mutates authoritative Inventory
    ↓
distributes resulting state
```

The resulting state can be represented by replicated Component state or by reliable server-to-client
messages, depending on the game's inventory architecture.

## Direct IP transport

The current Direct IP transport combines:

```text
TCP
    reliable ordered traffic

UDP
    unreliable sequenced traffic
```

TCP reliable frames preserve the logical Dreambit channel. UDP maintains a sequence per logical
channel so stale/out-of-order unreliable data can be rejected.

The TCP connection also provides a random association token used to associate the peer's UDP
endpoint with the same logical transport connection.

The current Direct IP implementation is IPv4-only.

It is a useful development/direct-connect transport, but it should not be treated as a complete
internet matchmaking, NAT traversal, account authentication, or anti-cheat solution.

## Main-thread integration

Transport worker threads do not directly mutate the ECS.

The important `Core.Update` order is:

```text
Time / Window
    ↓
Network.PollTransport
    ↓
Input pre-update / UI routing / input update
    ↓
Network.ApplyInbound
    ↓
pending Scene change
    ↓
fixed physics
    ↓
Scene.Tick
    ↓
Network.AfterSceneTick
    ↓
input post-update
```

This gives networking a predictable main-thread application boundary.

Incoming protocol work can create/update/despawn entities before the current gameplay `Scene.Tick`.
Live-spawn update suspension guarantees a newly materialized Entity cannot run gameplay until its
initial authoritative network state is committed.

## Initial Scene baseline

A client joining an already-running world receives a baseline before its `Scene.OnBegin`.

The baseline contains:

```text
Begin
    expected counts

AuthoredEntity records
    source Scene GUID -> NetworkEntityId

DynamicEntity records
    Blueprint AssetId
    owner
    transform
    enabled state
    destroy-with-owner

PlayerEntity records
    NetworkPeerId -> NetworkEntityId

ComponentState records
    replicated state for every registered network Entity

End
```

The server validates the baseline before transmitting it.

On the client:

```text
bind authored entities
    ↓
materialize dynamic entities
    ↓
restore player mappings
    ↓
apply Component states
    ↓
set StructuralRevision / ServerTick
    ↓
send Ready
    ↓
allow Scene.OnBegin
```

A late join therefore starts from one coherent authoritative world snapshot rather than trying to
replay the entire history of the session.

## Content compatibility

When no explicit `NetworkOptions.ContentFingerprint` is supplied, networking can use Dreambit's
active baked-content fingerprint.

PAK/blob content can therefore reject peers whose baked game content differs.

Loose-file content has no automatic baked fingerprint. During loose-file development, set
`NetworkOptions.ContentFingerprint` yourself if you need strict content compatibility between peers.

## Building a game on top of Dreambit networking

A useful game-level division is:

| Game layer | Responsibility |
| --- | --- |
| `GameNetworking` | Registers Scene keys, replication, messages, and networking options. |
| game network coordinator | Handles peer lifecycle and game-specific player/session policy. |
| `Scene` subclasses | Load editor-authored `.scene` assets with `LoadIntoSelf`. |
| replicated Components | Persistent server-authoritative state. |
| typed messages | Commands, requests, and one-shot notifications. |
| normal gameplay Components | Movement, combat, interaction, inventory, AI, etc. |
| presentation Components | Client interpolation and visual smoothing. |
| Entity/Scene Blueprints | Authorable content and reusable network-spawn sources. |

A useful design rule is:

> **Network state describes gameplay. It should not become the gameplay architecture.**

A player can remain an ordinary Dreambit Entity:

```text
Player
├── NetworkObject
├── PlayerNetworkState
├── PlayerMotor
├── PlayerInteraction
├── Inventory
├── Collider
├── Animator
└── SpriteDrawer
```

Networking should not replace `PlayerMotor`, `Inventory`, `Interaction`, or the other gameplay
systems. It supplies the authoritative data flow around them.

## Recommended end-to-end game flow

A small multiplayer game can use this sequence:

```text
GAME STARTUP
    configure Networking.Options
    register network Scenes
    register replicated Components
    register typed messages

HOST
    StartHost(port)
    ChangeScene("village")

CLIENT
    Connect(host, port)

HANDSHAKE
    protocol/build/content/schema validation
    assign NetworkPeerId

SCENE
    server sends "village" + SceneEpoch
    both sides create VillageScene
    OnInitialize -> LoadIntoSelf("Scenes/village")

INITIAL SYNC
    server binds editor-authored NetworkObjects
    client sends SceneLoaded
    server sends baseline
    client binds authored objects + dynamic objects + state
    client sends Ready
    client OnBegin executes

PLAYER JOIN
    server loads player EntityBlueprint
    server Networking.Spawn(...)
    server SetPlayerEntity(peer, player)
    client receives Spawn + initial state + SpawnReady

GAMEPLAY
    client sends input/requests
    server validates and simulates
    server sends snapshots
    clients interpolate/present

SCENE CHANGE
    server Networking.ChangeScene("forest")
    new SceneEpoch
    both sides OnInitialize -> LoadIntoSelf("Scenes/forest")
    new baseline

DISCONNECT
    server removes/retains owned objects according to DestroyWithOwner
```

## Practical rules

!!! important
    **A session can start from a live local menu.** The menu stays local. The server/host enters the
    first synchronized world with `Networking.ChangeScene(key)`; a client waits for that server
    instruction.

!!! important
    **Load editor-authored Scene Blueprints from `OnInitialize`.** Networking needs authored
    `NetworkObject` entities before the startup gate performs binding/synchronization.

!!! important
    **Use stable Scene keys.** Both peers must register the same key to a factory that constructs the
    corresponding local Scene.

!!! important
    **Use `Networking.Spawn` for runtime entities that must exist on all peers.** Do not use ordinary
    local `Entity.Create` and expect the network layer to reproduce it remotely.

!!! important
    **Keep `[NetworkReplicated]` Components on the `NetworkObject` root.** Version 1 explicitly
    rejects registered replicated Components on descendants.

!!! important
    **Use `NetworkEntityRef` instead of raw `Entity`/`Component` references in network state.**

!!! important
    **Treat the server/host as the replication authority.** Ordinary replicated Component state is
    server-authored. `NetworkTransform2D` is the explicit exception: `Client`/`Both` can accept an
    owning peer's pose, which the server validates and relays.

!!! important
    **Use messages for intent and replication for state.** Continuous input is often
    `UnreliableSequenced`; discrete actions are normally `ReliableOrdered`.

!!! important
    **Do not call `Scene.SetNextScene` during an active session.** The server/host calls
    `Networking.ChangeScene`; clients follow it. Call `Stop()` before resuming local-only transitions.

## Current limitations and next layers

The current foundation intentionally does not yet provide every higher-level multiplayer feature.

Useful next layers include:

- interpolation using buffered snapshots;
- optional client prediction/reconciliation for responsive movement;
- relevancy/interest management for large worlds;
- snapshot batching and/or dirty-state/delta optimization when profiling demonstrates the need;
- additional transports such as Steam P2P;
- matchmaking/lobbies/session discovery;
- production authentication/security appropriate to the game's deployment model.

These features can be added on top of the existing `NetworkService` / `NetworkSession` /
`NetworkWorld` architecture without changing the fundamental Scene and network-identity model.

### Recommended next implementation milestone

Before adding more networking infrastructure, build one small two-player vertical slice:

```text
editor-authored village.scene
    ↓
host + one client
    ↓
two dynamically spawned players
    ↓
server-authoritative movement
    ↓
one editor-authored replicated door/chest
    ↓
typed interaction request
    ↓
replicated open/closed state
    ↓
spawn and despawn one dropped item
    ↓
transition to a second editor-authored Scene
    ↓
disconnect/reconnect/late join
```

That slice exercises the important real-game paths:

```text
Scene Blueprint loading
authored Entity binding
dynamic Blueprint spawning
ownership
typed messages
replication
late join baseline
Scene epochs
structural revisions
disconnect cleanup
```

Once that works comfortably, the next high-value movement feature is snapshot buffering and
interpolation for `NetworkTransform2D`, followed by optional prediction and reconciliation for
strict `Server` authority.
