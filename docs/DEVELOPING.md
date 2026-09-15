# Developer Guide

## Repository Structure

The deliverable is a self-contained single-file executable, `FGOAC scooby.exe`.

| Path | Contents |
| --- | --- |
| `src\` | buildable C#/XAML project (`FGOLocalPlatform.csproj`). `DeckReaderUI\` and `DeckReaderUI.Kancolle\` are the author's card-reader namespaces, compiled into the same assembly; the names are kept as decompiled so a new upstream version can be diffed against them |
| `compat\` | fluphus's OpenGL compatibility layer for AMD and Intel graphics under `amd-shim\`, taken verbatim with its licence, plus `fgoglcompat.dll`, the older layer it replaced (see its README) |
| `linux\` | Linux compatibility module: automated 1-click setup script (`setup-fgoa-linux.sh`), network configuration (`setup-linux-network.sh`), PowerShell shim (`ps_shim.py`), and Lutris profile template (`fgolocalplatform.lutris.yml`) |
| `overlay\` | English replacements for files that live outside the assembly, laid out by their path relative to the install root |
| `patch\` | `Apply-EN-Patch.ps1` (the installer players run) and `Build-Manifest.ps1` (writes the `manifest.json` it checks against) |
| `dist\` | build output, `FGOAC scooby.exe` (not tracked) |
| `package\` | staging folder for release artifacts: zip, release notes, checksums |

---

## Building

### Prerequisites

- Windows 10 or 11, 64-bit
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- PowerShell 5.1 or newer

No game installation is required to build the launcher binary.

### Build the Launcher

```powershell
# Debug build
dotnet build src\FGOLocalPlatform.csproj

# Release publish (produces a single-file self-contained executable)
dotnet publish src\FGOLocalPlatform.csproj -c Release -r win-x64 --self-contained
```

The output executable is written to:
`src\bin\Release\net8.0-windows10.0.17763.0\win-x64\publish\FGOAC scooby.exe`

---

## Packaging a Release

To produce the release zip and manifest:

```powershell
.\package.ps1
```

This will:
1. Publish the project in Release configuration.
2. Stage all overlay files, compatibility layers, and patches into `package\`.
3. Generate `manifest.json` with SHA256 checksums.
4. Create the distributable zip under `package\`.

---

## Testing Changes

To test changes against an existing game install:

1. Copy `FGOAC scooby.exe` from the publish output into your game folder.
2. Run it directly from the game root directory.
3. Check `Platform.log` in the game root for runtime diagnostics.
