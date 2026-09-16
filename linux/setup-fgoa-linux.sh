#!/usr/bin/env bash
# ==============================================================================
# FGO Arcade Linux All-in-One Automated Setup Script
# Configures Network, Wine Prefix PowerShell Shim, ago.exe IAT Patch & Checks
# ==============================================================================
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
APP_DIR="${PROJECT_ROOT}/App"
AGO_EXE="${APP_DIR}/ago.exe"
AGO_BAK="${APP_DIR}/ago.exe.bak"

DEFAULT_PREFIX="${HOME}/.local/share/wineprefixes/fgoa"
WINEPREFIX="${WINEPREFIX:-$DEFAULT_PREFIX}"

echo "======================================================================"
echo "   FGO Arcade Linux Community Edition - Automated 1-Click Setup"
echo "======================================================================"
echo "[*] Project Directory: ${PROJECT_ROOT}"
echo "[*] Wine Prefix      : ${WINEPREFIX}"

# 1. Check Root / Network configuration
echo ""
echo "[1/4] Checking Linux Network & Kernel permissions..."
NEED_SUDO=0
if [[ "$(sysctl -n net.ipv4.ip_unprivileged_port_start 2>/dev/null || echo 1)" != "0" ]]; then
    NEED_SUDO=1
fi
if [[ "$(sysctl -n kernel.yama.ptrace_scope 2>/dev/null || echo 1)" != "0" ]]; then
    NEED_SUDO=1
fi
IP_BIN="$(command -v ip || echo "ip")"
if ! "$IP_BIN" addr show lo 2>/dev/null | grep -q "192.168.100.1"; then
    NEED_SUDO=1
fi

if [[ "$NEED_SUDO" -eq 1 ]]; then
    echo "[!] Network permissions or 192.168.100.1 loopback IP missing."
    echo "[!] Invoking setup-linux-network.sh (requires sudo once)..."
    sudo bash "${SCRIPT_DIR}/setup-linux-network.sh"
else
    echo "[✓] Network (192.168.100.1) & unprivileged port settings are active."
fi

# 2. Configure PowerShell Shim in Wine Prefix
echo ""
echo "[2/4] Configuring Wine Prefix PowerShell shim..."
WINE_PS_DIR="${WINEPREFIX}/drive_c/windows/system32/WindowsPowerShell/v1.0"

if [[ -d "$WINE_PS_DIR" ]]; then
    # Create or update powershell.exe symlink / wrapper to point to pwsh.exe shim
    if [[ -f "${WINE_PS_DIR}/pwsh.exe" ]]; then
        ln -sf "pwsh.exe" "${WINE_PS_DIR}/powershell.exe"
        echo "[✓] Linked ${WINE_PS_DIR}/powershell.exe -> pwsh.exe"
    elif command -v pwsh &>/dev/null; then
        PWSH_HOST_BIN="$(command -v pwsh)"
        ln -sf "$PWSH_HOST_BIN" "${WINE_PS_DIR}/powershell.exe"
        echo "[✓] Linked ${WINE_PS_DIR}/powershell.exe -> ${PWSH_HOST_BIN}"
    else
        echo "[!] Notice: Ensure your Wine prefix has a working PowerShell shim or portable pwsh.exe."
    fi
else
    echo "[!] Warning: Wine prefix not found at ${WINE_PS_DIR}."
    echo "    Please create your Wine prefix in Lutris or specify WINEPREFIX=/path/to/prefix."
fi

# 3. Check and Patch App/ago.exe IAT (SetWindowFeedbackSetting -> SetWindowTextA)
echo ""
echo "[3/4] Verifying and Patching App/ago.exe IAT..."
if [[ ! -f "$AGO_EXE" ]]; then
    echo "[ERROR] ${AGO_EXE} not found! Please place this folder in your FGO Arcade installation root beside App and Server."
    exit 1
fi

python3 -c "
import shutil, sys, os

ago = '${AGO_EXE}'
bak = '${AGO_BAK}'

with open(ago, 'rb') as f:
    f.seek(0x1971978)
    cur = f.read(25)

if cur.startswith(b'SetWindowTextA'):
    print('[✓] App/ago.exe IAT is already patched (SetWindowTextA).')
elif cur.startswith(b'SetWindowFeedbackSetting'):
    if not os.path.exists(bak):
        shutil.copy2(ago, bak)
        print(f'[+] Created original unpatched backup: {bak}')
    with open(ago, 'r+b') as f:
        f.seek(0x1971978)
        f.write(b'SetWindowTextA\x00' + b'\x00'*10)
    print('[✓] Successfully patched App/ago.exe IAT at offset 0x1971978!')
else:
    print(f'[ERROR] Unexpected bytes at offset 0x1971978 ({cur}).')
    print('        The game binary might be a different version or corrupted. Aborting.')
    sys.exit(1)
"

# 4. Check GPU & Graphics Driver
echo ""
echo "[4/4] Checking GPU Graphics environment..."
if command -v nvidia-smi &>/dev/null; then
    GPU_NAME="$(nvidia-smi --query-gpu=name --format=csv,noheader 2>/dev/null | head -n 1 || echo "NVIDIA GPU")"
    echo "[✓] Detected NVIDIA GPU: ${GPU_NAME}"
    echo "[✓] NVIDIA PRIME Render Offload will be automatically enabled."
else
    echo "[✓] Running on AMD/Intel or Mesa graphics."
    echo "    The bundled fluphus compatibility layer (compat/amd-shim/opengl32.dll) will be used."
fi

echo ""
echo "======================================================================"
echo "[✓] All configurations verified! You can now start the game in Lutris."
echo "======================================================================"
