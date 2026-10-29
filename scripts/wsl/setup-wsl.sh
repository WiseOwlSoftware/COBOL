#!/usr/bin/env bash
# User-space WSL toolchain refresh for WiseOwl COBOL: every tool at the version toolchain.env names, the versions CI's
# ./.github/actions/setup-toolchain installs (kb/Work PB4007; parity target: CI and scripts/cloud/setup-env.sh).
# Run it again after toolchain.env moves (`python scripts/toolchain.py bump`): it installs the new versions, points
# ~/.local/bin at them, removes the versions it replaced, and ends with `toolchain.py check --scope host`.
set -u
log() { echo "[wsl-tooling] $*"; }
SCRIPTS="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
. "$SCRIPTS/../toolchain.env"
# The installers' downloads and logs: a directory of this run's own, never fixed names in the machine-wide /tmp (kb/Work
# PB2922). Each failure prints its log's tail, so the directory goes when the script ends.
WORK="$(mktemp -d "${TMPDIR:-/tmp}/setup-wsl.XXXXXX")"
trap 'rm -rf "$WORK"' EXIT
TOOLS="$HOME/tools"
mkdir -p "$TOOLS" "$HOME/.local/bin"

# .NET SDK -> ~/.dotnet: the version CI builds with, installed the way CI installs it (scripts/install-tool.sh: the
# manifest's version, from its feeds in their order, never through the aka.ms redirect; kb/Work PB3308).
bash "$SCRIPTS/install-tool.sh" dotnet-sdk "$HOME/.dotnet" > "$WORK/dotnet-sdk.log" 2>&1 \
  && log "dotnet sdk $DOTNET_SDK_VERSION ok" || { log "dotnet sdk FAILED"; tail -5 "$WORK/dotnet-sdk.log"; }

# PATH wiring in ~/.profile (login shells) and ~/.bashrc (interactive)
for rc in "$HOME/.profile" "$HOME/.bashrc"; do
  grep -q 'DOTNET_ROOT="$HOME/.dotnet"' "$rc" 2>/dev/null || cat >> "$rc" <<'EOF'
export DOTNET_ROOT="$HOME/.dotnet"
export PATH="$HOME/.dotnet:$HOME/.dotnet/tools:$HOME/.local/bin:$PATH"
EOF
done
export DOTNET_ROOT="$HOME/.dotnet"
export PATH="$HOME/.dotnet:$HOME/.dotnet/tools:$HOME/.local/bin:$PATH"

# The JDK (ANTLR), pwsh, gh, jq and uv -> ~/tools/<tool>-<version>, each executable linked into ~/.local/bin, the way
# CI installs them (scripts/install-tool.sh: the publisher's release, checked against its checksum). A version the
# manifest no longer names is removed once the new one is linked.
for tool in jdk pwsh gh jq uv; do
  version=$(bash "$SCRIPTS/install-tool.sh" --version "$tool")
  dir="$TOOLS/$tool-$version"
  if bash "$SCRIPTS/install-tool.sh" "$tool" "$dir" > "$WORK/$tool.log" 2>&1 && bin=$(bash "$SCRIPTS/install-tool.sh" --bin "$tool" "$dir"); then
    case "$tool" in jdk) exe=java ;; *) exe=$tool ;; esac
    ln -sf "$bin/$exe" "$HOME/.local/bin/$exe"
    for old in "$TOOLS/$tool"-*; do [ "$old" = "$dir" ] || [ ! -d "$old" ] || rm -rf "$old"; done
    log "$tool $version ok"
  else
    log "$tool $version FAILED"; tail -5 "$WORK/$tool.log"
  fi
done
# pwsh used to be a dotnet global tool here, ahead of ~/.local/bin on PATH: the manifest's pwsh replaces it.
if [ -e "$HOME/.dotnet/tools/pwsh" ]; then dotnet tool uninstall --global PowerShell > /dev/null 2>&1 && log "removed the dotnet-tool pwsh"; fi

# Python: the manifest's CPython, uv-managed, as `python3` and `python` in ~/.local/bin.
uv python install "$PYTHON_VERSION" --default > "$WORK/uv-python.log" 2>&1 \
  && log "cpython $PYTHON_VERSION ok" || { log "cpython FAILED"; tail -5 "$WORK/uv-python.log"; }
PYTHON_EXE="$(uv python find --managed-python "$PYTHON_VERSION" 2>/dev/null)"

# Python packages the scripts import, kept at parity with the Windows dev box (latest versions):
# pymupdf (fitz: scripts/render-spec-page.py and the spec tools), scipy + numpy (statistics for process evaluations),
# pyyaml (scripts/hooks/tooling_check.py proves every agent role and skill frontmatter parses, kb/Work PB3097).
# The uv-managed interpreter is marked externally managed (PEP 668); it is user-space and ours, so install into it.
PY_PACKAGES="pymupdf scipy numpy pyyaml"
uv pip install --upgrade --system --break-system-packages --python "$PYTHON_EXE" $PY_PACKAGES \
  > "$WORK/uv-pip.log" 2>&1 && log "python packages ok ($PY_PACKAGES)" || { log "python packages FAILED"; tail -5 "$WORK/uv-pip.log"; }

# semgrep (scripts/semgrep/verify.py) on its own uv-managed CPython: it cannot run on the project's yet (toolchain.env
# SEMGREP_PYTHON_VERSION says why).
uv tool install --force "semgrep==$SEMGREP_VERSION" --python "$SEMGREP_PYTHON_VERSION" --managed-python \
  > "$WORK/semgrep.log" 2>&1 && log "semgrep $SEMGREP_VERSION ok" || { log "semgrep FAILED"; tail -5 "$WORK/semgrep.log"; }

log "pypkgs: $("$PYTHON_EXE" -c 'import fitz, scipy, numpy, yaml; print("pymupdf", fitz.VersionBind, "scipy", scipy.__version__, "numpy", numpy.__version__, "pyyaml", yaml.__version__)' 2>&1 | tail -1)"
# git comes from the git-core PPA, which installs only its newest release: the check below says whether it is the
# manifest's (a host check reads what each tool reports, never whether its source exists; kb/Work PB4007).
"$PYTHON_EXE" "$SCRIPTS/toolchain.py" check --scope host
log "DONE"
