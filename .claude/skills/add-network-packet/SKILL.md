---
name: add-network-packet
description: Add a new client↔server network packet (and its handler) to the Divine Ascension mod. Use when adding any new request/response message on the "divineascension" channel, wiring a server handler, or surfacing a server push to the UI. Encodes the 4-touchpoint registration that is easy to do incompletely.
---

# Adding a network packet

All netcode runs on one channel, `NETWORK_CHANNEL` = `"divineascension"`. A new
message is **not** functional until it is touched in every place below. Missing
one produces a silent no-op or a runtime "message type not registered" error.

Server is authoritative. Never trust the client for permissions — re-check
founder/role/membership server-side even if the UI already hid the control.

## The touchpoints

Work through these in order. For a request that expects a reply you add **two**
packets (request + response); a server→client broadcast needs only one.

### 1. Packet contract(s) — `DivineAscension/Network/`

One file per packet (group small related ones, e.g. `MilestonePackets.cs`).
Civilization/Diplomacy/HolySite packets live in matching subfolders with
matching `namespace DivineAscension.Network.<Area>`.

```csharp
using ProtoBuf;

namespace DivineAscension.Network;

[ProtoContract]
public class FooRequestPacket
{
    public FooRequestPacket() { }                 // REQUIRED parameterless ctor for protobuf

    public FooRequestPacket(string religionUID) => ReligionUID = religionUID;

    [ProtoMember(1)] public string ReligionUID { get; set; } = string.Empty;
}
```

Rules:
- `[ProtoContract]`, sequential `[ProtoMember(n)]` starting at 1, never reuse/reorder numbers.
- Keep a public parameterless constructor.
- Default reference members (`= string.Empty`, `= new List<...>()`) so deserialization never yields null.
- Responses conventionally carry `bool Success` + `string Message`.

### 2. Register the type — `DivineAscensionModSystem.cs` (`Start`, ~line 131)

Add to the `RegisterChannel(NETWORK_CHANNEL).RegisterMessageType<...>()` chain.
**Register both** the request and response. This runs common-side so both client
and server know the type. (The terminating `;` is on the last line — keep the
chain syntactically intact.)

```csharp
.RegisterMessageType<FooRequestPacket>()
.RegisterMessageType<FooResponsePacket>()
```

### 3. Server handler — `DivineAscension/Systems/Networking/Server/<Area>NetworkHandler.cs`

Add to the existing area handler (Religion, Civilization, Diplomacy, HolySite,
Milestone, Activity, PlayerData, Blessing) — only create a new handler class for
a genuinely new area (see add-system skill for wiring a new handler into the
initializer).

- Subscribe in `RegisterHandlers()`:
  ```csharp
  _networkService.RegisterMessageHandler<FooRequestPacket>(OnFooRequest);
  ```
- Implement `private void OnFooRequest(IServerPlayer fromPlayer, FooRequestPacket packet)`:
  - Validate inputs; re-check permissions server-side (`religion.FounderUID == fromPlayer.PlayerUID`, `religion.HasPermission(...)`, `IsMember(...)`).
  - Wrap mutating logic in try/catch and log via `_logger.Error(...)`.
  - User-facing strings go through `LocalizationService.Instance.Get(LocalizationKeys.XXX, ...)` — add a key rather than hardcoding.
  - Reply with `_networkService.SendToPlayer(fromPlayer, new FooResponsePacket(...))`.
  - To notify other members, loop `religion.MemberUIDs`, resolve with `_worldService.GetPlayerByUID(uid)`, send/messenger only if non-null.
- Dependencies arrive via constructor DI (`INetworkService`, `IWorldService`, `IPlayerMessengerService`, the relevant manager). Guard each with `?? throw new ArgumentNullException`.

### 4. Client handler — `DivineAscension/Systems/Networking/Client/DivineAscensionNetworkClient.cs`

Only the **response/broadcast** (server→client) packet is handled here.
- Register in `RegisterHandlers(IClientNetworkChannel channel)`:
  ```csharp
  _clientChannel.SetMessageHandler<FooResponsePacket>(OnFooResponse);
  ```
- Implement `OnFooResponse(FooResponsePacket packet)` to raise a C# event the UI
  subscribes to. Null the event in `Dispose()` like the others.
- Send the request from the client where the UI action occurs, via the stored
  `_clientChannel.SendPacket(new FooRequestPacket(...))` (guard with `IsNetworkAvailable()`).

## Finish

- `dotnet build DivineAscension.sln -c Debug` — confirm no "message type not registered" and no nullability warnings.
- Add a test under `DivineAscension.Tests/` mirroring the handler's folder; use `SpyNetworkService` / `SpyPlayerMessenger` / `FakeWorldService` to assert the right response was sent and permissions enforced.
- `dotnet test --filter FullyQualifiedName~<Area>`.

## Checklist (all must be true)
- [ ] Contract file(s) in `/Network/` with `[ProtoContract]` + parameterless ctor.
- [ ] `RegisterMessageType<>()` added for **every** new packet in `ModSystem.Start`.
- [ ] Server `RegisterMessageHandler` + `On…` method with server-side permission re-check.
- [ ] Client `SetMessageHandler` + event (only for server→client packets), nulled in `Dispose`.
- [ ] Localized strings, not hardcoded.
- [ ] Build clean + handler test added.
