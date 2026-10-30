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
# What the VM lacks that this repo needs, every one at the version toolchain.env names (kb/Work PB4007: CI and every
# host run the same, latest versions), installed the way CI installs it (scripts/install-tool.sh's block below):
#   * the .NET SDK — the version CI builds with (global.json's 10.0.100 and rollForward latestMinor accept it)
#   * the JDK      — the Frontend's ANTLR generation (the image's own java 21 stays behind it on PATH)
#   * pwsh         — the ANTLR generation, session-probe.ps1, fetch-gnucobol-tests.ps1
#   * gh, jq       — the landing and orchestrator scripts
#   * `python`     — toolchain.env's CPython through uv; every hook and doc command spells it `python`, and
#                    scripts/python-resolve.sh accepts no other version
# So the network allowlist also needs download.oracle.com (the JDK) and GitHub's release downloads (github.com and
# release-assets.githubusercontent.com: pwsh, gh, jq, uv and uv's CPython builds).
# git is the image's: the session-start check (scripts/hooks/tooling_check.py) says whether it is the manifest's.
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

# ── The tools, at toolchain.env's versions, installed the way CI installs them. This script runs before the repository
#    is cloned, so it cannot read toolchain.env or call scripts/install-tool.sh: the two blocks between the markers are
#    copies of theirs, and ToolchainManifestDriftTests and DotnetSdkInstallDriftTests keep each one identical. ──────────
# >>> toolchain (kept byte-identical in toolchain.env and scripts/cloud/setup-env.sh)
DOTNET_SDK_VERSION=10.0.403
PYTHON_VERSION=3.15.2
JDK_VERSION=27.0.1
PWSH_VERSION=7.6.9
GIT_LINUX_VERSION=2.56.0
GIT_WINDOWS_VERSION=2.56.0
GH_VERSION=2.103.3
JQ_VERSION=1.8.2
UV_VERSION=0.13.6
SEMGREP_VERSION=1.183.0
SEMGREP_PYTHON_VERSION=3.14.10
CI_LINUX_IMAGE=ubuntu-26.04
CI_WINDOWS_IMAGE=windows-2025-vs2026
# <<< toolchain

# >>> tool-install (kept byte-identical in scripts/install-tool.sh and scripts/cloud/setup-env.sh)
# The tools this installs; and the .NET SDK's feeds, in order: the CDN the aka.ms links front, then the feed
# dotnet-install itself falls back to. Every other tool has one publisher.
TOOL_INSTALLABLE="dotnet-sdk jdk pwsh gh jq uv"
DOTNET_SDK_FEEDS="https://builds.dotnet.microsoft.com/dotnet https://ci.dot.net/public"

# tool_version <tool>: TOOL_V, the version toolchain.env names for <tool> (its <TOOL>_VERSION), or a non-zero return
# naming the missing name. It forks nothing (nor does tool_spec): a fork costs a loaded Windows host most of a second.
tool_version() {
  local var="${1^^}"
  var="${var//-/_}_VERSION"
  TOOL_V=${!var:-}
  if [ -z "$TOOL_V" ]; then echo "install-tool: toolchain.env names no $var for '$1'" >&2; return 1; fi
}

# tool_spec <tool>: this host's release of <tool>, as TOOL_V (the version), TOOL_FEEDS (the publisher's bases, in
# order), TOOL_ASSET (the file to download), TOOL_PATH (its path under a feed), TOOL_SUMS (the path, under the same feed,
# of the checksum file that names it), TOOL_ALGO (sha256 or sha512) and TOOL_EXE (the executable the install provides).
# Each asset name is the publisher's own (checked against its release on 2026-10-28). The host is bash's own OSTYPE and
# HOSTTYPE (linux-gnu or msys, x86_64 or aarch64). A non-zero return names a host no release serves.
tool_spec() {
  local tool="$1" os arch ext exe="" triple
  tool_version "$tool" || return 1
  case "$OSTYPE" in
    linux*) os=linux ext=tar.gz ;;
    msys*|cygwin*) os=windows ext=zip exe=.exe ;;
    *) echo "install-tool: no $tool release for the OS $OSTYPE" >&2; return 1 ;;
  esac
  case "$HOSTTYPE" in
    x86_64|amd64) arch=x64 triple=x86_64 ;;
    aarch64|arm64) arch=arm64 triple=aarch64 ;;
    *) echo "install-tool: no $tool release for the machine $HOSTTYPE" >&2; return 1 ;;
  esac
  TOOL_ALGO=sha256 TOOL_EXE="${tool%-sdk}$exe"
  case "$tool" in
    dotnet-sdk)  # dotnet-sdk-<v>-<linux|win>-<x64|arm64>.<tar.gz|zip>, beside its <asset>.sha512
      TOOL_FEEDS=$DOTNET_SDK_FEEDS TOOL_ALGO=sha512
      if [ "$os" = windows ]; then TOOL_ASSET="dotnet-sdk-$TOOL_V-win-$arch.zip"; else TOOL_ASSET="dotnet-sdk-$TOOL_V-linux-$arch.tar.gz"; fi
      TOOL_PATH="Sdk/$TOOL_V/$TOOL_ASSET" TOOL_SUMS="Sdk/$TOOL_V/$TOOL_ASSET.sha512" ;;
    jdk)         # Oracle JDK: <major>/archive/jdk-<v>_<linux|windows>-<x64|aarch64>_bin.<tar.gz|zip>, beside its <asset>.sha256
      TOOL_FEEDS="https://download.oracle.com/java" TOOL_EXE="java$exe"
      TOOL_ASSET="jdk-${TOOL_V}_${os}-${arch/arm64/aarch64}_bin.$ext"
      TOOL_PATH="${TOOL_V%%.*}/archive/$TOOL_ASSET" TOOL_SUMS="${TOOL_V%%.*}/archive/$TOOL_ASSET.sha256" ;;
    pwsh)        # powershell-<v>-linux-<arch>.tar.gz or PowerShell-<v>-win-<arch>.zip, listed in the release's hashes.sha256
      TOOL_FEEDS="https://github.com/PowerShell/PowerShell/releases/download"
      if [ "$os" = windows ]; then TOOL_ASSET="PowerShell-$TOOL_V-win-$arch.zip"; else TOOL_ASSET="powershell-$TOOL_V-linux-$arch.tar.gz"; fi
      TOOL_PATH="v$TOOL_V/$TOOL_ASSET" TOOL_SUMS="v$TOOL_V/hashes.sha256" ;;
    gh)          # gh_<v>_<linux|windows>_<amd64|arm64>.<tar.gz|zip>, listed in gh_<v>_checksums.txt
      TOOL_FEEDS="https://github.com/cli/cli/releases/download"
      TOOL_ASSET="gh_${TOOL_V}_${os}_${arch/x64/amd64}.$ext"
      TOOL_PATH="v$TOOL_V/$TOOL_ASSET" TOOL_SUMS="v$TOOL_V/gh_${TOOL_V}_checksums.txt" ;;
    jq)          # the executable itself, jq-<linux|windows>-<amd64|arm64>[.exe], listed in sha256sum.txt
      TOOL_FEEDS="https://github.com/jqlang/jq/releases/download"
      TOOL_ASSET="jq-${os}-${arch/x64/amd64}$exe"
      TOOL_PATH="jq-$TOOL_V/$TOOL_ASSET" TOOL_SUMS="jq-$TOOL_V/sha256sum.txt" ;;
    uv)          # uv-<x86_64|aarch64>-unknown-linux-gnu.tar.gz or uv-<…>-pc-windows-msvc.zip, listed in sha256.sum
      TOOL_FEEDS="https://github.com/astral-sh/uv/releases/download"
      if [ "$os" = windows ]; then TOOL_ASSET="uv-$triple-pc-windows-msvc.zip"; else TOOL_ASSET="uv-$triple-unknown-linux-gnu.tar.gz"; fi
      TOOL_PATH="$TOOL_V/$TOOL_ASSET" TOOL_SUMS="$TOOL_V/sha256.sum" ;;
    *) echo "install-tool: '$tool' is not a tool this script installs (it installs: $TOOL_INSTALLABLE)" >&2; return 1 ;;
  esac
  # The witnesses' local feeds (--self-test) stand in for the publisher's; nothing else sets this.
  if [ -n "${INSTALL_TOOL_FEEDS:-}" ]; then TOOL_FEEDS=$INSTALL_TOOL_FEEDS; fi
}

# tool_fetch <url> <file>: download <url> to <file>. A transient failure is retried: curl's own retries take a timeout
# and an HTTP 408, 429 or 5xx, and this loop a transfer that broke off (curl exit 18, 56, ...). A refusal is not
# retried, since asking again gets the same answer: curl exit 22 (an HTTP error, a 404 or a 409 among them, that curl's
# retries did not clear) or 37 (a file:// feed without the file).
tool_fetch() {
  local attempt rc
  for attempt in 1 2 3; do
    curl -fsSL --retry 2 --retry-delay 5 -o "$2" "$1"; rc=$?
    case "$rc" in 0) return 0 ;; 22|37) return 1 ;; esac
    echo "install-tool: $1: curl exit $rc (attempt $attempt of 3)" >&2
    if [ "$attempt" -lt 3 ]; then sleep 5; fi
  done
  return 1
}

# tool_checksum <checksum-file> <asset>: the hash a publisher's checksum file gives <asset>. A list names the asset
# after its hash (`<hash>  <name>` or `<hash> *<name>`); a file for one asset may hold the hash alone. PowerShell's
# hashes.sha256 is UTF-16 with a byte-order mark and CRLF line ends, so the NULs, carriage returns and mark bytes go
# first (every checksum file is ASCII otherwise).
tool_checksum() {
  local text hash
  text=$(LC_ALL=C tr -d '\000\r\357\273\277\376\377' < "$1")
  hash=$(printf '%s\n' "$text" | awk -v a="$2" '{ n = $NF; sub(/^\*/, "", n) } NF >= 2 && n == a { print $1; exit }')
  if [ -z "$hash" ]; then hash=$(printf '%s\n' "$text" | awk 'NF { c++; h = $1 } END { if (c == 1) print h }'); fi
  printf '%s' "$hash"
}

# tool_install <tool> <install-dir> <work-dir>: install <tool>'s manifest version into <install-dir>, downloading into
# <work-dir>. A feed that refuses the asset or its checksum file, or serves an asset whose hash differs from the one the
# checksum file gives it, hands over to the next feed. Only a complete unpack writes the marker
# `.<tool>-<version>.complete`, which records where in <install-dir> the executable is (`.` for the top), so an
# interrupted install is redone, never trusted. Returns non-zero, naming every feed, when none served the asset.
tool_install() {
  local tool="$1" dir="$2" work="$3" feed got want marker bin
  tool_spec "$tool" || return 1
  marker="$dir/.$tool-$TOOL_V.complete"
  if [ -f "$marker" ]; then echo "$tool: $TOOL_V is already installed in $dir"; return 0; fi
  for feed in $TOOL_FEEDS; do
    echo "$tool: downloading $feed/$TOOL_PATH"
    if ! tool_fetch "$feed/$TOOL_PATH" "$work/$TOOL_ASSET" || ! tool_fetch "$feed/$TOOL_SUMS" "$work/$TOOL_ASSET.sums"; then
      echo "$tool: $feed did not serve $TOOL_ASSET and its checksum; trying the next feed" >&2
      continue
    fi
    want=$(tool_checksum "$work/$TOOL_ASSET.sums" "$TOOL_ASSET")
    got=$("${TOOL_ALGO}sum" "$work/$TOOL_ASSET") && got=${got%%[[:space:]]*} || got=""
    if [ -z "$want" ] || [ "${want,,}" != "${got,,}" ]; then
      echo "$tool: $TOOL_ASSET from $feed does not match its $TOOL_ALGO (want '${want:0:16}…', got '${got:0:16}…'); trying the next feed" >&2
      continue
    fi
    if ! mkdir -p "$dir"; then echo "$tool: cannot create $dir" >&2; return 1; fi
    case "$TOOL_ASSET" in
      *.zip) unzip -q -o "$work/$TOOL_ASSET" -d "$dir" ;;
      *.tar.gz) tar -xzf "$work/$TOOL_ASSET" -C "$dir" ;;
      *) cp "$work/$TOOL_ASSET" "$dir/$TOOL_EXE" ;;
    esac || { echo "$tool: unpacking $TOOL_ASSET into $dir failed" >&2; return 1; }
    rm -f "$work/$TOOL_ASSET" "$work/$TOOL_ASSET.sums"
    # The shallowest file named TOOL_EXE: the top for the SDK, pwsh and jq, jdk-<v>/bin for the JDK, a bin or a
    # platform directory for gh and uv.
    bin=$(cd "$dir" && find . -maxdepth 4 -type f -name "$TOOL_EXE" | awk -F/ '{ print NF "\t" $0 }' | sort -n | head -1 | cut -f2-)
    if [ -z "$bin" ]; then echo "$tool: $TOOL_ASSET holds no $TOOL_EXE" >&2; return 1; fi
    chmod +x "$dir/$bin"   # PowerShell's tarball and jq's bare executable arrive without the bit
    bin=$(dirname "$bin") && bin=${bin#./}
    printf '%s\n' "$bin" > "$marker" || { echo "$tool: cannot write $marker" >&2; return 1; }
    echo "$tool: $TOOL_V installed in $dir from $feed"
    return 0
  done
  echo "$tool: no feed served $TOOL_ASSET (tried, in order: $TOOL_FEEDS)" >&2
  return 1
}

# tool_bin <tool> <install-dir>: the directory under <install-dir> that holds the installed tool's executable, read from
# its marker (an empty marker, as an install before the marker recorded it wrote, means the top). Non-zero when the
# manifest's version is not installed there.
tool_bin() {
  local marker bin
  tool_spec "$1" || return 1
  marker="$2/.$1-$TOOL_V.complete"
  if [ ! -f "$marker" ]; then echo "install-tool: $1 $TOOL_V is not installed in $2" >&2; return 1; fi
  bin=$(head -1 "$marker")
  case "$bin" in ""|.) printf '%s\n' "$2" ;; *) printf '%s\n' "$2/$bin" ;; esac
}
# <<< tool-install

DOTNET_DIR=/usr/share/dotnet
TOOLS=/opt/wiseowl-toolchain
# install_tool <tool>: install <tool> (the SDK into DOTNET_DIR, every other tool into TOOLS/<tool>-<version>) and link its
# executable into /usr/local/bin (ahead of the image's own on PATH). A failure warns: the session still starts, and its
# readiness check names the missing tool.
install_tool() {
  local dir bin
  tool_spec "$1" || return 1
  case "$1" in dotnet-sdk) dir=$DOTNET_DIR ;; *) dir="$TOOLS/$1-$TOOL_V" ;; esac
  if tool_install "$1" "$dir" "$WORK" > "$WORK/$1.log" 2>&1 && bin=$(tool_bin "$1" "$dir"); then
    ln -sf "$bin/$TOOL_EXE" "/usr/local/bin/$TOOL_EXE" && log "$1 $TOOL_V ok"
  else
    log "WARN: $1 did not install (tail below)"; tail -5 "$WORK/$1.log" 2>/dev/null; return 1
  fi
}
# Every tool the block above installs (its TOOL_INSTALLABLE, which `scripts/toolchain.py check` holds to TOOLS'
# install-tool rows), so a tool added there is installed here with no line of its own.
for tool in $TOOL_INSTALLABLE; do install_tool "$tool" || true; done
# apphosts (any dotnet tool) locate the runtime through this file when DOTNET_ROOT is unset.
mkdir -p /etc/dotnet && echo "$DOTNET_DIR" > /etc/dotnet/install_location

cat > /etc/profile.d/dotnet.sh <<EOF
export DOTNET_ROOT=$DOTNET_DIR
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
EOF

# ── python = toolchain.env's CPython, uv-managed. The image's own /usr/local/bin/python{,3} → python3.11 sits AHEAD of
#    /usr/bin on PATH (the first smoke session, 2026-09-24, failed 3 Unit tests on it), so both names in /usr/local/bin
#    point at the manifest's interpreter, and the repo's only third-party imports (PyMuPDF for the spec renderers,
#    fontTools, PyYAML for tooling_check.py) go into it. No other interpreter stands in when the install fails: the
#    gate scripts accept only the manifest's (scripts/python-resolve.sh). ─────────────────────────────────────────────
PY=""
if UV_PYTHON_INSTALL_DIR=/opt/uv-python uv python install "$PYTHON_VERSION" >"$WORK/uv-python.log" 2>&1; then
  PY=$(UV_PYTHON_INSTALL_DIR=/opt/uv-python uv python find --managed-python "$PYTHON_VERSION" 2>/dev/null)
fi
if [ -n "$PY" ]; then
  ln -sf "$PY" /usr/local/bin/python && ln -sf "$PY" /usr/local/bin/python3 && log "python $PYTHON_VERSION -> $PY"
  python -m pip install -q --break-system-packages --root-user-action=ignore --no-warn-script-location \
      PyMuPDF fonttools pyyaml >"$WORK/pip-deps.log" 2>&1 \
    && log "PyMuPDF + fontTools + PyYAML ok" || { log "WARN: pip deps failed"; tail -5 "$WORK/pip-deps.log"; }
else
  log "WARN: uv could not install CPython $PYTHON_VERSION (tail below); the gate scripts will refuse to run"
  tail -5 "$WORK/uv-python.log" 2>/dev/null
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
log "java:   $(java -version 2>&1 | head -1)"
log "pwsh:   $(pwsh --version 2>&1 | head -1)"
log "python: $(python --version 2>&1)"
log "gh:     $(gh --version 2>&1 | head -1)"
log "jq:     $(jq --version 2>&1)"
log "uv:     $(uv --version 2>&1)"
log "git:    $(git --version 2>&1) (the image's; toolchain.env names $GIT_LINUX_VERSION)"
exit 0
