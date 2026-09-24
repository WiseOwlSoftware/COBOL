#!/usr/bin/env bash
# Cloud-environment setup script for claude.ai/code sessions (Anthropic-hosted VM: Ubuntu 24.04 x86_64, run as
# root BEFORE Claude Code launches; the result is snapshotted and reused by later sessions, and re-runs only when
# the script or the allowed hosts change, or after ~7 days).
#
# ⛔ This file is the CANONICAL copy. The environment runs the text pasted into its "Setup script" field at
# claude.ai/code → environment settings — after editing this file, paste it there again, in the same change set.
#
# The environment's network access must be CUSTOM = the default list ("Also include default list of common package
# managers") PLUS:
#     builds.dotnet.microsoft.com   (dotnet-install.sh downloads the SDK from here)
#     ftp.gnu.org                   (scripts/fetch-gnucobol-tests.ps1 — the GnuCOBOL 3.2 differential corpus)
# GitHub (the repo AND the private specs-private submodule) goes through the separate GitHub proxy regardless.
#
# What the VM lacks that this repo needs (everything else — git, python3, java 21 for ANTLR — is pre-installed):
#   * .NET 10 SDK — global.json pins 10.0.100 (rollForward latestMinor); CI uses setup-dotnet 10.0.x
#   * pwsh        — the Frontend's ANTLR generation, session-probe.ps1, fetch-gnucobol-tests.ps1
#   * `python`    — every hook and doc command spells it `python`, not `python3`
# The specs-private submodule is NOT fetched here: the repo is cloned per session, so the SessionStart hook
# (scripts/hooks/session_start.py) initializes it on every cloud session.
#
# Constraints: must exit 0 (non-zero fails the session start) and finish in ~5 minutes.
set -uo pipefail

log() { echo "[setup-env] $*"; }

DOTNET_DIR=/usr/share/dotnet

# ── python → python3, in parallel with the SDK download ──────────────────────────────────────────────────────────
( export DEBIAN_FRONTEND=noninteractive
  apt-get update -qq && apt-get install -y -qq python-is-python3 >/dev/null && log "python-is-python3 ok" \
    || log "WARN: python-is-python3 install failed" ) &
APT_PID=$!

# ── .NET 10 SDK: dotnet-install.sh (latest 10.0 = what CI's setup-dotnet 10.0.x resolves), apt as the fallback ──
install_dotnet() {
  if curl -fsSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh \
     && bash /tmp/dotnet-install.sh --channel 10.0 --install-dir "$DOTNET_DIR" >/tmp/dotnet-install.log 2>&1; then
    log "dotnet-install.sh ok"
  else
    log "WARN: dotnet-install.sh failed (tail below); falling back to Ubuntu's dotnet-sdk-10.0"
    tail -5 /tmp/dotnet-install.log 2>/dev/null
    wait "$APT_PID" 2>/dev/null
    DEBIAN_FRONTEND=noninteractive apt-get install -y -qq dotnet-sdk-10.0 >/dev/null \
      && DOTNET_DIR=/usr/lib/dotnet && log "apt dotnet-sdk-10.0 ok" \
      || { log "ERROR: no .NET 10 SDK installed"; return 1; }
  fi
  ln -sf "$DOTNET_DIR/dotnet" /usr/local/bin/dotnet
  # apphosts (pwsh below, any dotnet tool) locate the runtime through this file when DOTNET_ROOT is unset.
  mkdir -p /etc/dotnet && echo "$DOTNET_DIR" > /etc/dotnet/install_location
}
install_dotnet || true
wait "$APT_PID" 2>/dev/null || true

cat > /etc/profile.d/dotnet.sh <<EOF
export DOTNET_ROOT=$DOTNET_DIR
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
EOF

# ── pwsh as a .NET global tool (NuGet is on the default allowlist; avoids adding the Microsoft apt feed, whose
#    dotnet packages conflict with Ubuntu's) ──────────────────────────────────────────────────────────────────────
if command -v dotnet >/dev/null; then
  export DOTNET_ROOT=$DOTNET_DIR DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1
  dotnet tool install PowerShell --tool-path /usr/local/share/dotnet-tools >/tmp/pwsh-install.log 2>&1 \
    && ln -sf /usr/local/share/dotnet-tools/pwsh /usr/local/bin/pwsh && log "pwsh ok" \
    || { log "WARN: pwsh install failed"; tail -5 /tmp/pwsh-install.log; }
fi

# ── report (visible in the session's setup log) ──────────────────────────────────────────────────────────────────
log "dotnet: $(dotnet --version 2>&1 | head -1)"
log "pwsh:   $(pwsh -NoProfile -Command '$PSVersionTable.PSVersion.ToString()' 2>&1 | head -1)"
log "python: $(python --version 2>&1)"
log "java:   $(java -version 2>&1 | head -1)"
exit 0
