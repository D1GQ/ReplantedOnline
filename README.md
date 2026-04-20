# Replanted Online

A P2P multiplayer mod for **Plants vs. Zombies: Replanted** on Steam.

---

<img width="2560" height="1440" alt="PVZR-Online-Promo" src="/assets/PVZR-Online-Promo-Logo.png" />

<table>
  <tr>
    <td>
      <a href="https://gamebanana.com/wips/96467">
        <img src="https://img.shields.io/badge/GameBanana-Visit-orange?logo=gamebanana&logoColor=white&style=for-the-badge" width="250" height="50">
      </a>
    </td>
    <td>
      <a href="https://discord.gg/9PN4gxHC4B">
        <img src="https://img.shields.io/badge/Discord-Join-5865F2?logo=discord&logoColor=white&style=for-the-badge" width="200" height="50">
      </a>
    </td>
  </tr>
</table>

---

ReplantedOnline lets you play Versus mode online. Using direct peer-to-peer connections, so you don't need to use Parsec!

## Requirements

- [MelonLoader](https://github.com/LavaGang/MelonLoader)
- [BloomEngine](https://gamebanana.com/mods/640948)

## Want to help out?

Pull requests are welcome. Code structure:
### Gameplay
- `src/Modules/Versus/Configs` — handles configurations for plants/zombies
- `src/Modules/Versus/Gamemodes` — handles gamemode setup and logic
- `src/Modules/Versus/Arenas` — handles arena setup and logic
- `src/Managers/VersusLobbyManager.cs` — handles lobby logic and states
- `src/Managers/VersusGameplayManager.cs` — handles gameplay logic and states
- `src/Managers/VersusEndGameManager.cs` — handles endgame logic and states
### Networking
- `src/Network/Server/Transport` — transport for network solution
- `src/Network/NetworkDispatcher.cs` — sends and routes packets to transport
- `src/Network/Client/ReplantedLobby.cs` — handles steamworks/lan lobbies
- `src/Network/Client/ReplantedLobbyData.cs` — handles data for the lobby
- `src/Network/Packet/PacketWriter.cs` — binary serialization
- `src/Network/Packet/PacketReader.cs` — binary deserialization
- `src/Network/Packet/Messages` — packet messages for serialization and deserialization
- `src/Network/Packet/FastResolvers` — individual resolvers for non type serialization and deserialization
- `src/Network/Client/PacketHandler` — remote calls between clients
- `src/Network/Client/Object/Replanted` — network objects, used to sync individual objects

Fork the repo, make a branch, and send a PR.

## Reporting Issues!
Please report any bugs or issues you encounter in [Issues](https://github.com/D1GQ/ReplantedOnline/issues) to help improve the mod!

## Disclaimer
**ReplantedOnline** is a unofficial modification of **Plants vs. Zombies: Replanted** and is not affiliated with **PopCap Games** or **Electronic Arts**.
