#!/bin/bash
# ======================================================================
# FGO Arcade Linux Community Edition - Apply English Translation Patch
# ======================================================================

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"

PAYLOAD_ZH="${PROJECT_ROOT}/payload/App/zh"
APP_ZH="${PROJECT_ROOT}/App/zh"
LAUNCHER_JSON="${PROJECT_ROOT}/App/fgo-launcher.json"

echo "======================================================================"
echo "   FGO Arcade Linux - Apply English Translation Patch"
echo "======================================================================"
echo "[*] Project Directory: ${PROJECT_ROOT}"

# 1. Verify payload exists
if [[ ! -d "${PAYLOAD_ZH}" ]]; then
    echo "[ERROR] Payload directory not found at: ${PAYLOAD_ZH}"
    echo "        Please ensure the release package payload folder is present."
    exit 1
fi

# 2. Synchronize translation files
echo "[*] Copying English translation assets to App/zh/..."
mkdir -p "${APP_ZH}"
cp -r "${PAYLOAD_ZH}/"* "${APP_ZH}/"

# Clean up any misplaced fgozh.dll in App/ root to prevent path mismatch
if [[ -f "${PROJECT_ROOT}/App/fgozh.dll" ]]; then
    rm -f "${PROJECT_ROOT}/App/fgozh.dll"
fi

# 3. Create or update en-patch.json marker
MARKER_FILE="${APP_ZH}/en-patch.json"
cat <<EOF > "${MARKER_FILE}"
{
  "version": "1.1.1",
  "date": "$(date +%Y-%m-%d)",
  "status": "applied",
  "platform": "linux"
}
EOF
echo "[+] Wrote patch marker to: ${MARKER_FILE}"

# 4. Ensure chineseEnabled is true in App/fgo-launcher.json
if [[ -f "${LAUNCHER_JSON}" ]]; then
    python3 -c "
import json
p = '${LAUNCHER_JSON}'
try:
    with open(p, 'r', encoding='utf-8') as f:
        data = json.load(f)
    data['chineseEnabled'] = True
    with open(p, 'w', encoding='utf-8') as f:
        json.dump(data, f, indent=2, ensure_ascii=False)
    print('[+] Enabled translation hook (chineseEnabled=true) in App/fgo-launcher.json')
except Exception as e:
    print(f'[!] Warning: Could not update {p}: {e}')
"
fi

echo "======================================================================"
echo "[✓] English translation patch applied successfully!"
echo "    The game will now display in English when launched."
echo "======================================================================"
