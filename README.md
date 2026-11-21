

# 🌐 RAGECOOP

[![Downloads][downloads-shield]][downloads-url]
[![Contributors][contributors-shield]][contributors-url]
[![Forks][forks-shield]][forks-url]
[![Stargazers][stars-shield]][stars-url]
[![Issues][issues-shield]][issues-url]


# 🧠 That's it

RAGECOOP brings multiplayer experience to the story mode, you can complete missions together with your friends, use mods without any restriction/getting banned, or just mess around with your fella!

# 👁 Requirements (Legacy — `GTA5.exe`)

This fork targets **GTA V Legacy only**. Enhanced is out of scope for v1.

- **ScriptHookV — must match your game build.** Latest official release (15 Jul 2026) is **v3889.0** for Legacy (game 1.0.3889.0) / **v1158.13** for Enhanced, from https://www.dev-c.com/gtav/scripthookv/ — download only from there. If you are still on Legacy 1.0.3411.0, use ScriptHookV 1.0.3411.0 instead. A mismatched hook gives `FATAL: Unknown game version`.
- **ScriptHookVDotNet — v3.7.0-nightly.** There is **no stable 3.7.0** — stable is still **v3.6.0** (29 Dec 2022), and v3.6.0 / v3.5.1 break on game 1.0.3258.0 and later. Use the latest nightly (v3.7.0-nightly.206, 15 Nov 2026) from https://github.com/scripthookvdotnet/scripthookvdotnet-nightly/releases — the repo already bundles 3.7.0.188 in `libs/`.
- **LemonUI SHVDN3 2.2 or later** — latest release v2.2 (22 May 2025), bundled DLL is 2.2.0: https://github.com/LemonUIbyLemon/LemonUI/releases
- **.NET Framework 4.8 Runtime** (client) — 4.8 is the minimum; 4.8.1 is the latest servicing release.
- **Visual C++ 2019 x64 Redistributable** — required by ScriptHookVDotNet.
- **.NET 6.0 Runtime** (server only, to run `RageCoop.Server`) — note: .NET 6 reached end of support on 12 Nov 2024 and gets no security fixes; see Building below.

# 📋 Building the project

You'll need:
- **.NET 10 SDK** (current LTS, supported to 14 Nov 2028) — do **not** install the .NET 6 SDK: .NET 6 reached end of support on 12 Nov 2024, and VS Code's C# Dev Kit dropped .NET 6/7 SDK support in Jan 2026. The .NET 10 SDK can still build the `net6.0` server project (expect warning `NETSDK1138: The target framework 'net6.0' is out of support`).
- **.NET Framework 4.8 Developer Pack / SDK** (client, `net48`)
- **StorySync** (in this fork, `StorySync/`): see `StorySync/BUILD.md` — it needs the RAGECOOP build first, then `dotnet build StorySync.sln -c Release`.

> The server and test projects still target `net6.0`, which is end-of-support. Retargeting them to `net10.0` is a follow-up, not done in this README pass — the build still works, but the server runs on an unpatched runtime.

Recommended IDE:
- Visual Studio Code
- Visual Studio 2022

Then run `dotnet build` in the solution directory, built binaries are in the `bin` folder

# 📚 Third-party libraries
- [ScriptHookVDotNet3](https://github.com/scripthookvdotnet/scripthookvdotnet) — stable v3.6.0; this fork uses the v3.7.0-nightly line (bundled 3.7.0.188, see Requirements)
- [LemonUI.SHVDN3](https://github.com/LemonUIbyLemon/LemonUI) — v2.2
- Lidgren Network Custom
- - No new features (only improvements)
- [Newtonsoft.Json](https://github.com/JamesNK/Newtonsoft.Json)
- [ClearScript](https://github.com/microsoft/ClearScript)
- [SharpZipLib](https://github.com/icsharpcode/SharpZipLib)
- [DotNetCorePlugins](https://github.com/natemcmaster/DotNetCorePlugins)

# 👋 Features

1. Synchronized bullets
2. Synchronized vehicle/player/NPC
3. Synchronized projectiles
4. Simple ragdoll sync
5. Decent compatibility with other mods, set up a private modded server to have some fun!
6. Weaponized vehicle sync(WIP).
7. Optimization for high-Ping condition, play with friends around the world!
8. Powerful scripting API and resource system, easily [add multiplayer functionality to your mod](HTTPS://docs.ragecoop.com).

# ⚠ Known issues

See [Bugs (upstream)](https://github.com/RAGECOOP/RAGECOOP-V/issues/33) — file new issues for this fork at https://github.com/XxZorro13xX/RAGECOOP-V/issues

# 🐛 Important: SHVDN Timeout Configuration

If RageCoop terminates on startup with the following error in the SHVDN console:

```
[ERROR] Blocking script! Script RageCoop.Client.Main was terminated because it caused the game to freeze too long.
```

You need to increase the SHVDN script timeout threshold.

## Fix

1. Navigate to your **GTA V root directory** (where `GTA5.exe` is located)
2. Open **`ScriptHookVDotNet.ini`** in a text editor
3. Find the line:
   ```
   ScriptTimeoutThreshold=5000
   ```
4. Change it to:
   ```
   ScriptTimeoutThreshold=15000
   ```
5. Save the file and restart GTA V

## Why does this happen?

RageCoop initializes several systems on first load — including voice chat and network components — which can take longer than SHVDN's default 5-second timeout allows. Increasing the threshold to 15 seconds gives RageCoop enough time to finish initializing without being terminated.

# 🔫 Installation
Refer to the [upstream wiki](https://github.com/RAGECOOP/RAGECOOP-V/wiki) — it covers the base RAGECOOP install; StorySync install is in `StorySync/README.md`.

# 🧨 Downloads

No release has been published from **this fork** yet — build from source (see Building above). Do not use upstream releases for this fork: they do not include StorySync.
- This fork's releases (once published): https://github.com/XxZorro13xX/RAGECOOP-V/releases
- Upstream releases (base RAGECOOP only, no StorySync): https://github.com/RAGECOOP/RAGECOOP-V/releases/latest

Please note that this is incompatible with all previous versions of ragecoop, remove old files before installing.



# 🦆 Special thanks to

- [Makinolo](https://github.com/Makinolo), [oldnapalm](https://github.com/oldnapalm)
- - For testing, ideas, contributions and the first modification with the API
- [crosire](https://github.com/crosire)
- - For the extensive work in ScriptHookVDotNet
- [justalemon](https://github.com/justalemon)
- - For the extensive work in LemonUI

# 📝 License

This project is licensed under [MIT license](https://github.com/XxZorro13xX/RAGECOOP-V/blob/main/LICENSE)

[downloads-shield]: https://img.shields.io/github/downloads/XxZorro13xX/RAGECOOP-V/total?style=for-the-badge
[downloads-url]: https://github.com/XxZorro13xX/RAGECOOP-V/releases
[contributors-shield]: https://img.shields.io/github/contributors/XxZorro13xX/RAGECOOP-V.svg?style=for-the-badge
[contributors-url]: https://github.com/XxZorro13xX/RAGECOOP-V/graphs/contributors
[forks-shield]: https://img.shields.io/github/forks/XxZorro13xX/RAGECOOP-V.svg?style=for-the-badge
[forks-url]: https://github.com/XxZorro13xX/RAGECOOP-V/network/members
[stars-shield]: https://img.shields.io/github/stars/XxZorro13xX/RAGECOOP-V.svg?style=for-the-badge
[stars-url]: https://github.com/XxZorro13xX/RAGECOOP-V/stargazers
[issues-shield]: https://img.shields.io/github/issues/XxZorro13xX/RAGECOOP-V.svg?style=for-the-badge
[issues-url]: https://github.com/XxZorro13xX/RAGECOOP-V/issues


