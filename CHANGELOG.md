# Changelog

All notable changes to this project will be documented in this file.
The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

This project derives from Cloud23333's English-patched FGOLocalPlatform and
shares its version numbers up to 1.02. From 1.1.0 on, releases are the English
installer that puts them there.

## [Unreleased]

### Added

- **First-class Linux support** (via Proton / Lutris / Wine):
  - Automated 1-Click setup script (`linux/setup-fgoa-linux.sh`).
  - Python PowerShell Shim (`linux/ps_shim.py`) to manage local MariaDB and Artemis server instances seamlessly under Wine.
  - Linux host network configuration script (`linux/setup-linux-network.sh`) to configure loopback virtual bridge `192.168.100.1/32` and privileged port binding.
  - Pre-configured Lutris game profile template (`linux/fgolocalplatform.lutris.yml`).
  - Comprehensive Linux player and troubleshooting guide (`docs/LINUX_GUIDE.md`).
- Automated 25-byte IAT patch for `App/ago.exe` (offset `0x1971978`) fixing `SetWindowFeedbackSetting` crash (`0x80000100` / `STATUS_WINE_STUB`) under Wine.
- Dual-GPU hybrid graphics support via automatic NVIDIA PRIME render offload.

## [1.1.2] - 2026-09-16

Works on Cloud23333's V1.01 and V1.02. Unzip this release over your game folder and run the
launcher, or take it through Check for updates. Your accounts, decks, settings and whichever
graphics layer you have stay as they are.

### Fixed

- The older AMD graphics layer is back in the package. A fresh install without an NVIDIA card
  gets it by itself, since the newer layer crashes at the first battle on RX 500, RX 6000 and
  RX 7600 cards. An install that already has a layer keeps it; if you turn the layer off and on
  again, the fresh choice is the older one, and the newer one is a click away.
- "Go back to the older layer" now also shows when App\opengl32.dll is a copy the launcher did
  not put there, and the Display page says when a fgoglcompat.dll sits where the game cannot use it.
- A game folder whose fgohook.dll is still the 11.00 build is refused with a message that says
  to apply V1.02 again, instead of crashing at start.
- Play works from a folder whose name has square brackets.
- The help page explains ERROR 6401, the 0xC000001D stop on CPUs without F16C, and the AMD
  first-battle crash.

### Added

- Deck loadouts: save the deck under a name, keep as many as you like, load any of them, and
  Export or Import to share with other players (Cards and Deck).
- Draw-rate presets: the same for the Draw Rates table.
- Sort the card list by name or card number.
- Max Master Level on the Account page.
- A short "what's new" window the first time the launcher starts after an update.
- A newer build of either graphics layer can be dropped into the compat folder and installed from
  Settings > Display, so a layer update no longer waits for a launcher release. The Display page
  now says up front that both layers are community work and not fully optimized yet.

## [1.1.1] - 2026-09-15

Works on Cloud23333's V1.01 and V1.02. Unzip this release over your game folder and run the
launcher, or take it through Check for updates. Your accounts, decks and settings stay as they are.

### Fixed

- First battle crash on AMD Radeon RX 500, RX 6000, RX 7600 and desktop Ryzen integrated
  graphics (Settings > Display > Graphics compatibility layer).
- Windows Defender quarantine warning on `App\opengl32.dll` and `App\dxgi.dll` by switching to
  the [amd-shim](https://github.com/fluphus/amd-shim) build.
- Virus scan race condition when updating from a previous version.
- Crash when running the installer from a folder outside the game directory.
- Gift batch generation for new accounts (carried over from V1.02).
- Zero Servants shown in deck editor when card images were missing.

### Added

- In-app update check with one-click download and restart.
- English platform patch apply script and manifest builder.
- Automatic firewall rule creation on first launch.
- Clearer troubleshooting guidance in `docs/GUIDE_EN.pdf`.

## [1.1.0] - 2026-09-14

First public release of the English launcher and installer.
