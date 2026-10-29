#!/usr/bin/env bash
# install-tool.sh — ⛔ THE ONE WAY THIS REPOSITORY INSTALLS A TOOL FROM ITS PUBLISHER (kb/Work PB3308, PB4007;
# DotnetSdkInstallDriftTests, ToolchainManifestDriftTests; DESIGN-test-build-ci.md §1.4).
#
# Usage: install-tool.sh --version <tool>             print the version toolchain.env names for <tool>
#        install-tool.sh <tool> <install-dir>         install that version there (a no-op once it is installed there)
#        install-tool.sh --bin <tool> <install-dir>   print the directory that holds the installed tool's executable
#        install-tool.sh --list                       print TOOL_INSTALLABLE, the tools this script installs
#        install-tool.sh --self-test                  the install's witnesses (offline: file:// feeds, fake archives)
# <tool> is one of TOOL_INSTALLABLE below, which `scripts/toolchain.py check` (in every CI job and every gate) holds to
# exactly the install-tool rows of toolchain.py's TOOLS, the one table of the tools. Python is not among them: CI installs
# it with actions/setup-python, the Windows dev box with the Python Install Manager, WSL and the cloud image with uv
# (which this script installs).
#
# CI (./.github/actions/setup-toolchain, in every job), the WSL toolchain refresh (scripts/wsl/setup-wsl.sh) and the
# cloud environment (scripts/cloud/setup-env.sh) install the SAME versions the SAME way: the exact version
# toolchain.env names, downloaded straight from the tool's publisher (the feeds below, in their order), checked against
# the checksum the publisher ships beside it, and unpacked into the directory the caller names. setup-env.sh runs before
# the repository is cloned, so it carries a copy of toolchain.env's block and of the block between the `tool-install`
# markers below, and the drift tests hold each copy byte-identical.
#
# Why an exact version, and never "the newest" (`dotnet-install --channel 10.0`, actions/setup-dotnet's `10.0.x`, a
# runner image's preinstalled tool): "the newest" is resolved at job time, through a redirect, so two runs of one commit
# can build with two toolchains, and a redirect that fails fails every job. From about 18:50Z on 2026-10-25 the .NET
# aka.ms redirect pointed at a storage origin answering `409 PublicAccessNotPermitted`, and every code merge group
# failed `Setup .NET 10` while builds.dotnet.microsoft.com served the same file (kb/Work PB3308). And a runner image's
# tool is whatever the image carries: CI's Windows Python was the image's 3.12 while every local host ran 3.14, and a
# self-test that was correct on every local leg went red in a merge group (kb/Work PB4007).
set -uo pipefail

# The manifest: every <TOOL>_VERSION this script installs. setup-env.sh carries its block in place of this line.
. "$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)/toolchain.env" || { echo "install-tool: cannot read toolchain.env" >&2; exit 1; }

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

# The witnesses run the real tool_install over local file:// feeds holding fake assets, so each case is exact and
# offline: the first feed serves it, a refusing first feed hands over to the second, a checksum mismatch hands over, no
# feed at all fails naming the feeds, an installed version is never fetched again, a publisher's checksum LIST (UTF-16,
# as PowerShell ships it) names the asset, a bare executable is installed as the tool, and each tool's asset is the name
# its publisher gives it on each OS.
self_test() {
  local t fails=0 root base asset
  t=$(mktemp -d "${TMPDIR:-/tmp}/install-tool-self-test.XXXXXX") || return 1
  trap 'rm -rf "$t"' RETURN
  ok() { echo "ok: $1"; }
  bad() { echo "FAIL: $1"; fails=$((fails + 1)); }

  # Each tool's asset, as its publisher names it, on Linux and on Windows (OSTYPE stands in for the host).
  local host want got_assets
  for host in linux-gnu msys; do
    if [ "$host" = linux-gnu ]; then
      want="dotnet-sdk-$DOTNET_SDK_VERSION-linux-x64.tar.gz jdk-${JDK_VERSION}_linux-x64_bin.tar.gz powershell-$PWSH_VERSION-linux-x64.tar.gz gh_${GH_VERSION}_linux_amd64.tar.gz jq-linux-amd64 uv-x86_64-unknown-linux-gnu.tar.gz"
    else
      want="dotnet-sdk-$DOTNET_SDK_VERSION-win-x64.zip jdk-${JDK_VERSION}_windows-x64_bin.zip PowerShell-$PWSH_VERSION-win-x64.zip gh_${GH_VERSION}_windows_amd64.zip jq-windows-amd64.exe uv-x86_64-pc-windows-msvc.zip"
    fi
    got_assets=$(OSTYPE=$host HOSTTYPE=x86_64
                 for tool in $TOOL_INSTALLABLE; do tool_spec "$tool" && printf '%s ' "$TOOL_ASSET"; done)
    if [ "${got_assets% }" = "$want" ]; then ok "every tool's $host asset is its publisher's name"
    else bad "every tool's $host asset is its publisher's name (got '$got_assets', want '$want')"; fi
  done
  if (tool_spec nosuch) 2>/dev/null || (FOO_VERSION='' tool_version foo) 2>/dev/null; then
    bad "an unknown tool, or one toolchain.env names no version for, is refused"
  else ok "an unknown tool, or one toolchain.env names no version for, is refused"; fi

  # curl on Windows is a native program: it reads file:///C:/… , which `pwd -W` gives; elsewhere pwd is already /….
  root=$(cd "$t" && { pwd -W 2>/dev/null || pwd; })
  case "$root" in /*) base="file://$root" ;; *) base="file:///$root" ;; esac
  # The fake assets are tar.gz on every host (Git Bash has no zip), so the witnesses name them through this stand-in:
  # the real spec, with the asset renamed and the checksum file kept in its place (the SDK's `<asset>.sha512` beside
  # it, a list for every other tool).
  eval "real_tool_spec() $(declare -f tool_spec | tail -n +2)"
  tool_spec() {
    real_tool_spec "$1" || return 1
    local dir=${TOOL_PATH%/*}
    case "$1" in
      jq) TOOL_ASSET="$1-$TOOL_V-test$TOOL_EXE" ;;
      *) TOOL_ASSET="$1-$TOOL_V-test.tar.gz" ;;
    esac
    TOOL_PATH="$dir/$TOOL_ASSET"
    case "$1" in dotnet-sdk) TOOL_SUMS="$TOOL_PATH.sha512" ;; esac
  }
  local v=$DOTNET_SDK_VERSION dotnet
  asset="dotnet-sdk-$v-test.tar.gz" dotnet=$(tool_spec dotnet-sdk && echo "$TOOL_EXE")
  mkdir -p "$t/src" "$t/good/Sdk/$v" "$t/bad/Sdk/$v" "$t/work"
  echo fake > "$t/src/$dotnet"
  tar -czf "$t/good/Sdk/$v/$asset" -C "$t/src" "$dotnet"
  (cd "$t/good/Sdk/$v" && sha512sum "$asset" > "$asset.sha512")
  cp "$t/good/Sdk/$v/$asset" "$t/bad/Sdk/$v/"
  echo "$(printf '0%.0s' {1..128})  $asset" > "$t/bad/Sdk/$v/$asset.sha512"

  check() {  # <name> <expected exit> <expected feed or -> <feeds…>: install the SDK into a fresh directory
    local name="$1" want="$2" from="$3" out rc dir="$t/install-$RANDOM$RANDOM"
    shift 3
    out=$(INSTALL_TOOL_FEEDS="$*" tool_install dotnet-sdk "$dir" "$t/work" 2>&1); rc=$?
    if [ "$rc" -ne "$want" ] \
       || { [ "$want" -eq 0 ] && { [ ! -f "$dir/$dotnet" ] || [ ! -f "$dir/.dotnet-sdk-$v.complete" ] \
                                   || [[ "$out" != *"installed in $dir from $from"* ]] \
                                   || [ "$(tool_bin dotnet-sdk "$dir")" != "$dir" ]; }; } \
       || { [ "$want" -ne 0 ] && [[ "$out" != *"no feed served"* ]]; }; then
      bad "$name (exit $rc, wanted $want; output: $out)"
    else
      ok "$name"
    fi
  }
  check "the first feed serves the asset" 0 "$base/good" "$base/good" "$base/bad"
  check "a refusing first feed hands over to the second" 0 "$base/good" "$base/missing" "$base/good"
  check "a checksum mismatch hands over to the next feed" 0 "$base/good" "$base/bad" "$base/good"
  check "no feed serves it: the install fails, naming the feeds" 1 - "$base/missing" "$base/bad"

  local dir="$t/installed" out
  INSTALL_TOOL_FEEDS="$base/good" tool_install dotnet-sdk "$dir" "$t/work" > /dev/null 2>&1
  out=$(INSTALL_TOOL_FEEDS="$base/missing" tool_install dotnet-sdk "$dir" "$t/work" 2>&1)
  if [ $? -eq 0 ] && [[ "$out" == *"already installed"* ]]; then ok "an installed version is not fetched again"
  else bad "an installed version is not fetched again (output: $out)"; fi
  rm -f "$dir/.dotnet-sdk-$v.complete"
  if ! tool_bin dotnet-sdk "$dir" > /dev/null 2>&1; then ok "a version with no marker is not installed (--bin refuses it)"
  else bad "a version with no marker is not installed (--bin refuses it)"; fi
  out=$(INSTALL_TOOL_FEEDS="$base/good" tool_install dotnet-sdk "$dir" "$t/work" 2>&1)
  if [ $? -eq 0 ] && [[ "$out" == *"installed in $dir from $base/good"* ]]; then ok "an install with no marker is redone"
  else bad "an install with no marker is redone (output: $out)"; fi

  # A transfer that breaks off (curl exit 56) is retried on the same feed; a refusal is never retried. `curl` and
  # `sleep` are shadowed by functions: the first download breaks off once, and the retry's pause costs nothing.
  : > "$t/broke"
  curl() { if [ -f "$t/broke" ]; then rm -f "$t/broke"; return 56; fi; command curl "$@"; }
  sleep() { :; }
  out=$(INSTALL_TOOL_FEEDS="$base/good" tool_install dotnet-sdk "$t/retried" "$t/work" 2>&1)
  if [ $? -eq 0 ] && [[ "$out" == *"curl exit 56 (attempt 1 of 3)"* ]] && [[ "$out" == *"from $base/good"* ]]; then
    ok "a transfer that broke off is retried on the same feed"
  else bad "a transfer that broke off is retried on the same feed (output: $out)"; fi
  out=$(INSTALL_TOOL_FEEDS="$base/missing $base/good" tool_install dotnet-sdk "$t/refused" "$t/work" 2>&1)
  if [ $? -eq 0 ] && [[ "$out" != *"attempt"* ]]; then ok "a refusal is not retried"
  else bad "a refusal is not retried (output: $out)"; fi
  unset -f curl sleep

  # A publisher's checksum LIST names the asset among others, here UTF-16 with a byte-order mark and CRLF line ends as
  # PowerShell's hashes.sha256 is; the executable sits two directories down, and --bin names that directory.
  local pwsh; pwsh=$(tool_spec pwsh && echo "$TOOL_EXE")
  v=$PWSH_VERSION asset="pwsh-$v-test.tar.gz"
  mkdir -p "$t/nested/pwsh-$v/bin" "$t/rel/v$v"
  echo fake > "$t/nested/pwsh-$v/bin/$pwsh"
  tar -czf "$t/rel/v$v/$asset" -C "$t/nested" "pwsh-$v"
  (cd "$t/rel/v$v" && printf '%s *other.zip\r\n%s *%s\r\n' "$(printf 'f%.0s' {1..64})" "$(sha256sum "$asset" | cut -d' ' -f1)" "$asset" \
     | { printf '\377\376'; sed 's/./&\x00/g'; } > hashes.sha256)
  out=$(INSTALL_TOOL_FEEDS="$base/rel" tool_install pwsh "$t/pwsh" "$t/work" 2>&1)
  if [ $? -eq 0 ] && [ "$(tool_bin pwsh "$t/pwsh")" = "$t/pwsh/pwsh-$v/bin" ] && [ -x "$t/pwsh/pwsh-$v/bin/$pwsh" ]; then
    ok "a UTF-16 checksum list names the asset, and --bin names the executable's directory"
  else bad "a UTF-16 checksum list names the asset, and --bin names the executable's directory (output: $out; bin: $(tool_bin pwsh "$t/pwsh" 2>&1))"; fi

  # A bare executable (jq's release) is installed as the tool itself, executable.
  v=$JQ_VERSION
  mkdir -p "$t/rel/jq-$v"
  asset=$(tool_spec jq && echo "$TOOL_ASSET")
  printf '#!/bin/sh\necho jq-%s\n' "$v" > "$t/rel/jq-$v/$asset"
  (cd "$t/rel/jq-$v" && sha256sum "$asset" > sha256sum.txt)
  local exe rc
  exe=$(tool_spec jq && echo "$TOOL_EXE")
  out=$(INSTALL_TOOL_FEEDS="$base/rel" tool_install jq "$t/jq" "$t/work" 2>&1); rc=$?
  if [ "$rc" -eq 0 ] && [ -x "$t/jq/$exe" ] && [ "$(tool_bin jq "$t/jq")" = "$t/jq" ]; then ok "a bare executable is installed as the tool"
  else bad "a bare executable is installed as the tool (output: $out)"; fi

  echo "=== INSTALL-TOOL SELF-TEST: $([ "$fails" -eq 0 ] && echo PASS || echo "FAIL — $fails") ==="
  [ "$fails" -eq 0 ]
}

usage() { echo "usage: $0 --version <tool> | --bin <tool> <install-dir> | --list | --self-test | <tool> <install-dir>" >&2; exit 2; }

# Git Bash: a Windows path (C:\… from a runner variable) becomes the POSIX form its tools all read.
posix_path() { case "$(uname -s)" in MINGW*|MSYS*|CYGWIN*) cygpath -u "$1" ;; *) printf '%s\n' "$1" ;; esac; }

case "${1:-}" in
  --version) [ $# -eq 2 ] || usage; tool_version "$2" && printf '%s\n' "$TOOL_V" ;;
  --bin) [ $# -eq 3 ] || usage; tool_bin "$2" "$(posix_path "$3")" ;;
  --list) [ $# -eq 1 ] || usage; printf '%s\n' "$TOOL_INSTALLABLE" ;;
  --self-test) self_test ;;
  "" | -*) usage ;;
  *)
    [ $# -eq 2 ] || usage
    dir=$(posix_path "$2")
    work=$(mktemp -d "${TMPDIR:-/tmp}/install-tool.XXXXXX") || exit 1
    trap 'rm -rf "$work"' EXIT
    tool_install "$1" "$dir" "$work"
    ;;
esac
