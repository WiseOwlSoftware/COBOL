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
#     builds.dotnet.microsoft.com   (the .NET SDK archive downloads from here: the first feed of the block below)
#     ftp.gnu.org                   (scripts/fetch-gnucobol-tests.ps1 — the GnuCOBOL 3.2 differential corpus)
# GitHub (the repo AND the private specs-private submodule) goes through the separate GitHub proxy regardless — but
# that proxy only authorizes repositories ATTACHED TO THE SESSION, so the specs-private submodule (the licensed PDF,
# needed only by render-spec-page.py and the figure audits — specs/ISO_COBOL.md and cite.py are in the main repo)
# clones only when BrentRector/COBOL-private is attached too; otherwise "could not read Username … terminal
# prompts disabled". `claude --cloud` attaches ONE repository; the claude.ai/code page attaches both ("+" beside the
# repo chip, or the link https://claude.ai/code?repositories=WiseOwlSoftware/COBOL,BrentRector/COBOL-private).
#
# What the VM lacks that this repo needs (everything else — git, python3, java 21 for ANTLR — is pre-installed):
#   * .NET 10 SDK — the version CI builds with (DOTNET_SDK_VERSION below, which global.json's 10.0.100 and
#                   rollForward latestMinor accept; kb/Work PB3308)
#   * pwsh        — the Frontend's ANTLR generation, session-probe.ps1, fetch-gnucobol-tests.ps1
#   * `python`    — the LATEST CPython (3.14, the owner's standard 2026-09-24, same as the dev box); every hook and
#                   doc command spells it `python`, and the scripts need >= 3.12 (PEP 701 f-strings)
# Per-CLONE work (the specs-private submodule, the git-ignored GnuCOBOL corpus under tests/external/) is NOT done
# here: this script's result is SNAPSHOTTED and reused while the repo is cloned fresh per session, so the
# SessionStart hook (scripts/hooks/session_start.py) does it on every cloud session. What this script owns is making
# that hook RUN: with two repositories attached (COBOL + COBOL-private — see above) Claude
# Code starts in their PARENT, /home/user, so the repo's .claude/settings.json never loads and no hook fires (cloud
# smoke #2, 2026-09-24: "Found 0 total hooks in registry"). The user-level SessionStart hook installed below closes
# that gap, and the corpus tarball is pre-cached here so the hook's per-clone fetch is a local copy.
#
# Constraints: must exit 0 (non-zero fails the session start) and finish in ~5 minutes.
set -uo pipefail

# Everything this script prints is ALSO kept in the snapshot: the platform shows setup stdout nowhere a session can
# read it, so without this copy no session can verify a `[setup-env]` line (cloud smoke #3, 2026-09-24).
SETUP_LOG=/var/log/wiseowl-cobol-setup-env.log
exec > >(tee "$SETUP_LOG") 2>&1

log() { echo "[setup-env] $*"; }
# The installers' downloads and logs: a directory of this run's own, never fixed names in the machine-wide /tmp (kb/Work
# PB2922). Each failure prints its log's tail above, so the directory goes when the script ends.
WORK="$(mktemp -d "${TMPDIR:-/tmp}/setup-env.XXXXXX")"
trap 'rm -rf "$WORK"' EXIT

DOTNET_DIR=/usr/share/dotnet

# ── apt index refresh, in parallel with the SDK download (the dotnet apt fallback needs it) ──────────────────────
( export DEBIAN_FRONTEND=noninteractive; apt-get update -qq || log "WARN: apt-get update failed" ) &
APT_PID=$!

# ── .NET SDK: the version CI builds with, installed the way CI installs it (kb/Work PB3308), Ubuntu's dotnet-sdk-10.0
#    as the fallback. This script runs before the repository is cloned, so it cannot call scripts/install-dotnet-sdk.sh:
#    the block between the markers is a copy of that script's, and DotnetSdkInstallDriftTests keeps the two identical. ──
# >>> dotnet-sdk-install (kept byte-identical in scripts/install-dotnet-sdk.sh and scripts/cloud/setup-env.sh)
DOTNET_SDK_VERSION=10.0.403
# The feeds, in order: the CDN the aka.ms links front, then the feed dotnet-install itself falls back to.
DOTNET_SDK_FEEDS="https://builds.dotnet.microsoft.com/dotnet https://ci.dot.net/public"

# dotnet_sdk_archive: this host's SDK archive name (dotnet-sdk-<version>-<os>-<arch>.<tar.gz|zip>), or a non-zero
# return naming the host no archive serves.
dotnet_sdk_archive() {
  local os ext arch
  case "$(uname -s)" in
    Linux) os=linux ext=tar.gz ;;
    MINGW*|MSYS*|CYGWIN*) os=win ext=zip ;;
    *) echo "dotnet-sdk: no SDK archive for the OS $(uname -s)" >&2; return 1 ;;
  esac
  case "$(uname -m)" in
    x86_64|amd64) arch=x64 ;;
    aarch64|arm64) arch=arm64 ;;
    *) echo "dotnet-sdk: no SDK archive for the machine $(uname -m)" >&2; return 1 ;;
  esac
  echo "dotnet-sdk-$DOTNET_SDK_VERSION-$os-$arch.$ext"
}

# dotnet_sdk_fetch <url> <file>: download <url> to <file>. A transient failure is retried: curl's own retries take a
# timeout and an HTTP 408, 429 or 5xx, and this loop a transfer that broke off (curl exit 18, 56, ...). A refusal is
# not retried, since asking again gets the same answer: curl exit 22 (an HTTP error, a 404 or a 409 among them, that
# curl's retries did not clear) or 37 (a file:// feed without the file).
dotnet_sdk_fetch() {
  local attempt rc
  for attempt in 1 2 3; do
    curl -fsSL --retry 2 --retry-delay 5 -o "$2" "$1"; rc=$?
    case "$rc" in 0) return 0 ;; 22|37) return 1 ;; esac
    echo "dotnet-sdk: $1: curl exit $rc (attempt $attempt of 3)" >&2
    if [ "$attempt" -lt 3 ]; then sleep 5; fi
  done
  return 1
}

# dotnet_sdk_install <install-dir> <work-dir>: install DOTNET_SDK_VERSION into <install-dir>, downloading into
# <work-dir>. A feed that refuses the archive or its `.sha512`, or serves an archive whose SHA-512 differs from its
# `.sha512`, hands over to the next feed. Only a complete unpack writes the marker `.dotnet-sdk-<version>.complete`,
# so an interrupted install is redone, never trusted. Returns non-zero, naming every feed, when none served the archive.
dotnet_sdk_install() {
  local dir="$1" work="$2" name feed url want got marker
  marker="$dir/.dotnet-sdk-$DOTNET_SDK_VERSION.complete"
  if [ -f "$marker" ]; then echo "dotnet-sdk: $DOTNET_SDK_VERSION is already installed in $dir"; return 0; fi
  name=$(dotnet_sdk_archive) || return 1
  for feed in $DOTNET_SDK_FEEDS; do
    url="$feed/Sdk/$DOTNET_SDK_VERSION/$name"
    echo "dotnet-sdk: downloading $url"
    if ! dotnet_sdk_fetch "$url" "$work/$name" || ! dotnet_sdk_fetch "$url.sha512" "$work/$name.sha512"; then
      echo "dotnet-sdk: $feed did not serve $name; trying the next feed" >&2
      continue
    fi
    want=$(cat "$work/$name.sha512") && want=${want%%[[:space:]]*} || want=""
    got=$(sha512sum "$work/$name") && got=${got%%[[:space:]]*} || got=""
    if [ -z "$want" ] || [ "${want,,}" != "${got,,}" ]; then
      echo "dotnet-sdk: $name from $feed does not match its .sha512 (want '${want:0:16}…', got '${got:0:16}…'); trying the next feed" >&2
      continue
    fi
    if ! mkdir -p "$dir"; then echo "dotnet-sdk: cannot create $dir" >&2; return 1; fi
    case "$name" in
      *.zip) unzip -q -o "$work/$name" -d "$dir" ;;
      *) tar -xzf "$work/$name" -C "$dir" ;;
    esac || { echo "dotnet-sdk: unpacking $name into $dir failed" >&2; return 1; }
    rm -f "$work/$name" "$work/$name.sha512"
    : > "$marker" || { echo "dotnet-sdk: cannot write $marker" >&2; return 1; }
    echo "dotnet-sdk: $DOTNET_SDK_VERSION installed in $dir from $feed"
    return 0
  done
  echo "dotnet-sdk: no feed served $name (tried, in order: $DOTNET_SDK_FEEDS)" >&2
  return 1
}
# <<< dotnet-sdk-install

install_dotnet() {
  if dotnet_sdk_install "$DOTNET_DIR" "$WORK" >"$WORK/dotnet-sdk.log" 2>&1; then
    log "dotnet sdk $DOTNET_SDK_VERSION ok"
  else
    log "WARN: the pinned SDK $DOTNET_SDK_VERSION did not install (tail below); falling back to Ubuntu's dotnet-sdk-10.0"
    tail -5 "$WORK/dotnet-sdk.log" 2>/dev/null
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

# ── python = the latest CPython 3.14. Ubuntu 24.04 packages stop at 3.12, and the image's own /usr/local/bin/python{,3}
#    → python3.11 sits AHEAD of /usr/bin on PATH (the first smoke session, 2026-09-24, failed 3 Unit tests on it: the
#    scripts use PEP 701 f-strings). uv comes from PyPI (default allowlist) and installs a standalone CPython 3.14;
#    fallback = the newest /usr/bin/python3.N with N >= 12. Both names in /usr/local/bin point at the result, and the
#    repo's only third-party imports (PyMuPDF for the spec renderers, fontTools, PyYAML for tooling_check.py) go into it. ─────────────────────────
PYVER=3.14
PY=""
if python3 -m pip install -q --break-system-packages --root-user-action=ignore uv >"$WORK/uv-install.log" 2>&1 \
   && UV_PYTHON_INSTALL_DIR=/opt/uv-python python3 -m uv python install "$PYVER" >>"$WORK/uv-install.log" 2>&1; then
  PY=$(UV_PYTHON_INSTALL_DIR=/opt/uv-python python3 -m uv python find --managed-python "$PYVER" 2>/dev/null)
fi
if [ -z "$PY" ]; then
  log "WARN: uv could not install CPython $PYVER (tail below); falling back to the newest system python3.12+"
  tail -5 "$WORK/uv-install.log" 2>/dev/null
  PY=$(ls /usr/bin/python3.[0-9]* 2>/dev/null | grep -E '/python3\.[0-9]+$' | sort -t. -k2 -n | tail -1)
  [ -n "$PY" ] && [ "${PY##*.}" -ge 12 ] || { log "WARN: no python3.12+ found (${PY:-none})"; PY=""; }
fi
if [ -n "$PY" ]; then
  ln -sf "$PY" /usr/local/bin/python && ln -sf "$PY" /usr/local/bin/python3 && log "python -> $PY"
  python -m pip install -q --break-system-packages --root-user-action=ignore --no-warn-script-location \
      PyMuPDF fonttools pyyaml >"$WORK/pip-deps.log" 2>&1 \
    && log "PyMuPDF + fontTools + PyYAML ok" || { log "WARN: pip deps failed"; tail -5 "$WORK/pip-deps.log"; }
fi

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
  dotnet tool install PowerShell --tool-path /usr/local/share/dotnet-tools >"$WORK/pwsh-install.log" 2>&1 \
    && ln -sf /usr/local/share/dotnet-tools/pwsh /usr/local/bin/pwsh && log "pwsh ok" \
    || { log "WARN: pwsh install failed"; tail -5 "$WORK/pwsh-install.log"; }
fi

# ── the GnuCOBOL 3.2 corpus tarball, pre-cached (pinned: the same URL + SHA-256 as scripts/fetch-gnucobol-tests.ps1,
#    which the hook runs per clone after copying this file into tests/external/ — the fetch then skips the download).
#    GPL-3.0 test text: cached OUTSIDE the repo, never committed (the fetch script's licensing posture). ──────────
CACHE_DIR=/opt/wiseowl-cobol-cache
GNUCOBOL_TGZ=gnucobol-3.2.tar.xz
GNUCOBOL_SHA=3bb48af46ced4779facf41fdc2ee60e4ccb86eaa99d010b36685315df39c2ee2
mkdir -p "$CACHE_DIR"
if curl -fsSL "https://ftp.gnu.org/gnu/gnucobol/$GNUCOBOL_TGZ" -o "$CACHE_DIR/$GNUCOBOL_TGZ.part" \
   && echo "$GNUCOBOL_SHA  $CACHE_DIR/$GNUCOBOL_TGZ.part" | sha256sum -c --quiet; then
  mv "$CACHE_DIR/$GNUCOBOL_TGZ.part" "$CACHE_DIR/$GNUCOBOL_TGZ" && log "GnuCOBOL corpus tarball cached"
else
  rm -f "$CACHE_DIR/$GNUCOBOL_TGZ.part"; log "WARN: GnuCOBOL tarball not cached (the hook's fetch will download it)"
fi

# ── the USER-LEVEL SessionStart hook (see the header): a shim that runs the repo's hook when the session's project
#    dir is NOT the repo itself (the project-level hook already covers that case — never run it twice). Merged into
#    ~/.claude/settings.json, never overwriting keys the platform may have put there. ──────────────────────────────
cat > /usr/local/bin/wiseowl-cobol-session-start <<'EOF'
#!/usr/bin/env bash
# User-level SessionStart shim installed by WiseOwl COBOL scripts/cloud/setup-env.sh — see that file's header.
for repo in "${CLAUDE_PROJECT_DIR:-/home/user}/COBOL" /home/user/COBOL "${CLAUDE_PROJECT_DIR:-/home/user}/COBOL" /home/user/COBOL; do
  [ -f "$repo/scripts/hooks/session_start.py" ] || continue
  [ "$(realpath "${CLAUDE_PROJECT_DIR:-.}")" = "$(realpath "$repo")" ] && exit 0   # the project hook runs instead
  exec python "$repo/scripts/hooks/session_start.py"
done
exit 0
EOF
chmod 755 /usr/local/bin/wiseowl-cobol-session-start
python - <<'EOF' && log "user-level SessionStart hook installed" || log "WARN: user-level SessionStart hook NOT installed"
import json, os, pathlib
p = pathlib.Path(os.path.expanduser("~/.claude/settings.json"))
p.parent.mkdir(parents=True, exist_ok=True)
s = json.loads(p.read_text(encoding="utf-8")) if p.exists() and p.read_text(encoding="utf-8").strip() else {}
entries = s.setdefault("hooks", {}).setdefault("SessionStart", [])
cmd = "/usr/local/bin/wiseowl-cobol-session-start"
if not any(h.get("command") == cmd for e in entries for h in e.get("hooks", [])):
    entries.append({"matcher": "startup|resume|clear|compact",
                    "hooks": [{"type": "command", "command": cmd, "timeout": 300}]})
p.write_text(json.dumps(s, indent=2) + "\n", encoding="utf-8")
EOF

# ── report (visible in the session's setup log) ──────────────────────────────────────────────────────────────────
log "dotnet: $(dotnet --version 2>&1 | head -1)"
log "pwsh:   $(pwsh -NoProfile -Command '$PSVersionTable.PSVersion.ToString()' 2>&1 | head -1)"
log "python: $(python --version 2>&1)"
log "java:   $(java -version 2>&1 | head -1)"
exit 0
