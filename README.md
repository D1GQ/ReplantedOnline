# Replanted Online

A P2P multiplayer mod for **Plants vs. Zombies: Replanted** on Steam.

---

<img width="2560" height="1440" alt="PVZR-Online-Promo" src="/assets/PVZR-Online-Promo-Logo.png" />

<table>
  <tr>
    <td>
      <a href="https://gamebanana.com/wips/96467">
        <img src="https://img.shields.io/badge/GameBanana-Visit-orange?logo=gamebanana&logoColor=white&style=for-the-badge" width="300" height="50">
      </a>
    </td>
    <td>
      <a href="https://discord.gg/9PN4gxHC4B">
        <img src="https://img.shields.io/badge/Discord-Join-5865F2?logo=discord&logoColor=white&style=for-the-badge" width="250" height="50">
      </a>
    </td>
  </tr>
</table>

---

## About

ReplantedOnline lets you play Versus mode online using direct peer-to-peer connections, so you don't need to use Parsec.

> You must own a legitimate Steam copy of Plants vs. Zombies: Replanted to use this mod.

---

## Installation Requirements

- [MelonLoader v0.7.2](https://github.com/LavaGang/MelonLoader/releases/tag/v0.7.2)
- [BloomEngine](https://gamebanana.com/mods/640948)

Launch the game once after installing MelonLoader so required assemblies are generated.

---

## Compiling

### Development Requirements

- [Microsoft Visual Studio](https://visualstudio.microsoft.com/)
- .NET SDK 8.0.102+
- .NET desktop development tools

### Clone the Repository

```bash
git clone https://github.com/D1GQ/ReplantedOnline.git
cd ReplantedOnline
```

### Setup References

Run the reference setup script:

```bat
setup_references.bat
```

When prompted, enter your game's `MelonLoader` directory.

Example:

```text
C:\Program Files (x86)\Steam\steamapps\common\PVZ Replanted\MelonLoader
```

After references are installed, run:

```bat
setup_dependencies.bat
```

This downloads required external dependencies automatically.

### Build

Open:

```text
ReplantedOnline.sln
```

in Visual Studio and build the solution in `Release` configuration.

---

## Project Structure

### Gameplay
- `src/Modules/Reloaded/Versus/Configs` — contains configurations for plants/zombies
- `src/Modules/Reloaded/Versus/Gamemodes` — contains gamemode setup and logic
- `src/Modules/Reloaded/Versus/Arenas` — contains arena setup and logic
- `src/Managers/Reloaded/VersusLobbyManager.cs` — handles lobby logic and states
- `src/Managers/Reloaded/VersusGameplayManager.cs` — handles gameplay logic and states
- `src/Managers/Reloaded/VersusEndGameManager.cs` — handles endgame logic and states

### Networking
#### Client
- `src/Network/Client/ReplantedLobby.cs` — handles Steamworks/LAN lobbies
- `src/Network/Client/ReplantedLobbyData.cs` — handles lobby data
- `src/Network/Client/PacketHandler` — contains packet handlers for routed packets
- `src/Network/Client/RPC` — contains remote procedure call handlers for static RPCs
- `src/Network/Client/Object/Reloaded` — contains synced network objects

#### Server / Transport
- `src/Network/Routing/Transport` — contains networking transports
- `src/Network/Routing/NetworkDispatcher.cs` — handles packet routing and dispatching
- `src/Network/Server/LAN` — contains LAN testing server logic

#### Serialization
- `src/Network/Packet/PacketWriter.cs` — handles binary serialization
- `src/Network/Packet/PacketReader.cs` — handles binary deserialization
- `src/Network/Packet/Messages` — contains serialized packet message types
- `src/Network/Packet/FastResolvers` — contains fast serialization resolvers

---

## Reporting Issues

Please report bugs or issues here:

https://github.com/D1GQ/ReplantedOnline/issues

---

## Contact

replantedonlineofficial@gmail.com

---

## Disclaimer

**ReplantedOnline** is an unofficial modification of **Plants vs. Zombies: Replanted** and is not affiliated with **PopCap Games** or **Electronic Arts**.
