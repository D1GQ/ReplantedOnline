# Replanted Online

<img width="2560" height="1440" alt="PVZR-Online-Promo" src="/assets/PVZR-Online-Promo-Logo.png" />

<p align="left">
  <a href="https://gamebanana.com/wips/96467"><img src="https://img.shields.io/badge/GameBanana-Visit-orange?logo=gamebanana&logoColor=white&style=for-the-badge" width="250" height="50"></a>
  <a href="https://discord.gg/9PN4gxHC4B"><img src="https://img.shields.io/badge/Discord-Join-5865F2?logo=discord&logoColor=white&style=for-the-badge" width="200" height="50"></a>
</p>

A peer-to-peer (P2P) online multiplayer mod for **Plants vs. Zombies: Replanted** on Steam.

## 🌐 What is ReplantedOnline?

ReplantedOnline adds P2P online multiplayer capabilities to Plants vs. Zombies: Replanted, allowing you to:
- **Play Versus mode online** with friends
- **Direct P2P connections** - no dedicated servers required

## 📋 Features

- **Online Versus Mode** - Play against friends over the internet
- **Peer-to-Peer Networking** - Connect directly without dedicated servers
- **Lobby System** - Create and join game sessions
- **Real-time Game Sync** - Synchronized gameplay experience

## 🔧 Requirements & Dependencies

This mod requires:
- **[MelonLoader](https://github.com/LavaGang/MelonLoader)** - Mod framework for Unity
- **[BloomEngine](https://gamebanana.com/mods/640948)** - Mod Config framework for PVZR

## 🤝 Contributing

**I'm open to contributions!** If you'd like to help develop ReplantedOnline:

1. Fork the repository
2. Create a feature branch
3. Submit a pull request

Architecture Overview:
- NetworkDispatcher: Handles packet routing
- NetLobby: Steamworks lobby management  
- Packet Reader/Writer: Binary serialization
- RPC: Remote procedure calls

## 🐛 Reporting Issues

Please report any bugs or issues you encounter to help improve the mod!

---

**Note**: This is a fan-made modification and is not affiliated with PopCap Games or Electronic Arts. Plants vs. Zombies is a registered trademark of PopCap Games.
