<div align="center">

# FGOAC scooby

**An English launcher and installer for the Fate/Grand Order Arcade local platform**

[![Release](https://img.shields.io/github/v/release/githubuser420x/FGOAC-scooby?style=flat-square&color=c792ea)](https://github.com/githubuser420x/FGOAC-scooby/releases/latest)
[![Downloads](https://img.shields.io/github/downloads/githubuser420x/FGOAC-scooby/total?style=flat-square&color=82aaff)](https://github.com/githubuser420x/FGOAC-scooby/releases)
[![Discord](https://img.shields.io/discord/1482811440656093335?style=flat-square&label=discord&color=7289da)](https://discord.gg/C7aQpwbC)

</div>

`FGOAC scooby` translates the [Fate/Grand Order Arcade](https://arcade.fate-go.jp/) offline launcher
to English, fixes its edge-case crashes and ships the AMD graphics compatibility layer. The installer
carries no game files: it is applied over an existing install.

---

## Requirements

| | |
| --- | --- |
| The game | An existing **FGO Arcade local platform V1.01 or V1.02** install (Cloud23333's package) - the folder that holds `App` and `Server`. V1.02 is the one to be on: it fixes ERROR 4102, the blank Servant records and the sync error after enhancing a Servant |
| OS | Windows 10 or 11, 64-bit; or **Linux** (via Proton / Lutris, see [`docs/LINUX_GUIDE.md`](docs/LINUX_GUIDE.md)) |
| GPU | NVIDIA on a current driver. AMD: the launcher installs the older compatibility layer on a fresh install without an NVIDIA card; it runs on RX 500, RX 6000, RX 7600 and desktop Ryzen graphics. The newer layer by fluphus (Settings > Display) runs on the RX 7900 XTX; on other cards it crashes at the first battle. Intel integrated graphics and Ryzen laptop graphics are not covered by either layer yet. Both layers are community work and not fully optimized yet: expect lower frame rates and some rendering errors than on NVIDIA. A newer build of a layer can be dropped into the compat folder and installed from Settings > Display |
| CPU | Intel Core 3rd generation (2012) or newer, or any Ryzen. Pentium and Celeron chips before the 12th generation lack F16C, an instruction set the game uses, and stop with 0xC000001D at start |
| Drive | Any drive **except E: or Y:** - see the table further down |
| Rights | Administrator: one Windows prompt when the launcher starts (or sudo once on Linux for network setup) |

.NET and Python are not needed. The launcher carries its own runtime, and the platform brings its own
Python.

## Installation

Download the latest release zip from [Releases](https://github.com/githubuser420x/FGOAC-scooby/releases/latest)
and extract it directly into the game folder, then run `FGOAC scooby.exe`.

On first start the launcher creates the Windows Firewall rules for the server and the game, creates
its own folders, allows the game and the card reader to access the temporary directory, creates **Master** with a full Servant and Craft Essence roster, and sets the display to windowed 1280x720 on
your main monitor. Later starts go straight to Play.

Already installed? The Play page shows a bar when a newer version is out; **Install now** downloads
it, applies it and restarts the launcher, and a short window lists what changed. Your accounts, decks,
settings and graphics layer stay as they are. Unzipping a newer release over the folder does the same.

[`docs/GUIDE_EN.pdf`](docs/GUIDE_EN.pdf) is the full player guide: getting Servants, controls, a
sortie step by step, the exchange shops and troubleshooting. For Linux players, see [`docs/LINUX_GUIDE.md`](docs/LINUX_GUIDE.md).

## What works

- **Sorties**: Grand Order (Singularity F, Orleans, Septem, Okeanos, London, E Pluribus Unum,
  Camelot, Babylonia, Final Singularity), Grail War (CPU and player vs player across two PCs on a
  local network), Chaldea Gate, and Event Quests (all past events, their materials and shops).
- **Summoning**: in-game summon works; the gacha rates table is in the launcher under *Draw Rates*.
- **Card Reader**: the launcher's built-in reader (virtual and physical cards), and an external
  FGO physical card reader.
- **Save data**: all progress is saved to the local database under `Server\data`.

## AMD graphics layer

AMD Radeon users: the bundled compatibility layer translates the game's OpenGL calls to Direct3D 11
so it renders on non-NVIDIA GPUs. The launcher detects AMD graphics on first run and turns the layer
on. If you change your graphics card later, toggle it from **Settings > Display**.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
