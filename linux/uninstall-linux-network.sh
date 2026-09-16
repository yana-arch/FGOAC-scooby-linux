#!/usr/bin/env bash
# ==============================================================================
# FGO Arcade Linux Host Network Uninstallation / Rollback Script
# Reverts sysctl settings and disables fgoa-vnet.service
# ==============================================================================
set -euo pipefail

if [[ $EUID -ne 0 ]]; then
   echo "[ERROR] This script must be run as root (use sudo)." 
   exit 1
fi

echo "[*] Rolling back Linux host network configuration for FGO Arcade..."

# 1. Stop and disable systemd service
SERVICE_FILE="/etc/systemd/system/fgoa-vnet.service"
if systemctl is-active --quiet fgoa-vnet.service 2>/dev/null || systemctl is-enabled --quiet fgoa-vnet.service 2>/dev/null; then
    echo "[*] Disabling and stopping fgoa-vnet.service..."
    systemctl disable --now fgoa-vnet.service || true
fi
if [[ -f "$SERVICE_FILE" ]]; then
    rm -f "$SERVICE_FILE"
    systemctl daemon-reload
    echo "[+] Removed $SERVICE_FILE"
fi

# 2. Remove loopback IP
IP_BIN="$(command -v ip || echo "ip")"
if "$IP_BIN" addr show lo | grep -q "192.168.100.1"; then
    "$IP_BIN" addr del 192.168.100.1/32 dev lo || true
    echo "[+] Removed 192.168.100.1/32 from lo."
fi

# 3. Remove sysctl configuration
SYSCTL_CONF="/etc/sysctl.d/50-fgoa-unprivileged-ports.conf"
if [[ -f "$SYSCTL_CONF" ]]; then
    rm -f "$SYSCTL_CONF"
    echo "[+] Removed $SYSCTL_CONF"
    sysctl --system >/dev/null
    echo "[+] Restored system default sysctl parameters."
fi

echo "[✓] Linux network configuration rollback complete."
