#!/usr/bin/env bash
# install-dotnet-sdk.sh — THE ONE WAY THIS REPOSITORY INSTALLS THE .NET SDK (kb/Work PB3308; DotnetSdkInstallDriftTests).
#
# Usage: install-dotnet-sdk.sh --version         print the pinned SDK version
#        install-dotnet-sdk.sh <install-dir>     install it there (a no-op once it is installed there)
#        install-dotnet-sdk.sh --self-test       the install's witnesses: feed order, a refusing feed, a bad checksum
#
# CI (.github/actions/setup-dotnet, in every job that builds, Linux and Windows alike), the WSL toolchain refresh
# (scripts/wsl/setup-wsl.sh) and the cloud environment (scripts/cloud/setup-env.sh) install the SAME SDK the SAME way:
# the exact version pinned below, downloaded straight from the feeds below in their order, its SHA-512 checked against
# the feed's own `<archive>.sha512`, and unpacked into the directory the caller names. setup-env.sh runs before the
# repository is cloned, so it carries a copy of the block between the `dotnet-sdk-install` markers, and
# DotnetSdkInstallDriftTests holds the two copies byte-identical.
#
# Why an exact version, and not `dotnet-install --channel 10.0` or actions/setup-dotnet's `10.0.x`: both resolve "the
# newest 10.0" through the aka.ms redirect when they install. From about 18:50Z on 2026-10-25 that redirect pointed at a
# storage origin answering `409 PublicAccessNotPermitted`. dotnet-install retried that one URL three times and gave up,
# so every code merge group failed `Setup .NET 10`, while builds.dotnet.microsoft.com served the same file. An exact
# version never goes through aka.ms. The pin also makes CI build with the SDK the local hosts install (kb/Work PB1957
# row 8), and DotnetSdkInstallDriftTests requires it to be a version global.json's `sdk.version` and `rollForward` accept.
set -uo pipefail

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

# The witnesses run the real dotnet_sdk_install over local file:// feeds holding a fake archive, so each case is exact
# and offline: the first feed serves it, a refusing first feed hands over to the second, a checksum mismatch hands over,
# no feed at all fails naming the feeds, and an installed version is never fetched again.
self_test() {
  local t fails=0 root base archive
  t=$(mktemp -d "${TMPDIR:-/tmp}/install-dotnet-sdk-self-test.XXXXXX") || return 1
  trap 'rm -rf "$t"' RETURN
  # curl on Windows is a native program: it reads file:///C:/… , which `pwd -W` gives; elsewhere pwd is already /….
  root=$(cd "$t" && { pwd -W 2>/dev/null || pwd; })
  case "$root" in /*) base="file://$root" ;; *) base="file:///$root" ;; esac
  # The fake archive is a tar.gz on every host (Git Bash has no zip), so the witnesses name it themselves.
  dotnet_sdk_archive() { echo "dotnet-sdk-$DOTNET_SDK_VERSION-test.tar.gz"; }
  archive=$(dotnet_sdk_archive)
  mkdir -p "$t/src/sdk/$DOTNET_SDK_VERSION" "$t/good/Sdk/$DOTNET_SDK_VERSION" "$t/bad/Sdk/$DOTNET_SDK_VERSION" "$t/work"
  echo fake > "$t/src/sdk/$DOTNET_SDK_VERSION/dotnet.dll"
  tar -czf "$t/good/Sdk/$DOTNET_SDK_VERSION/$archive" -C "$t/src" sdk
  (cd "$t/good/Sdk/$DOTNET_SDK_VERSION" && sha512sum "$archive" > "$archive.sha512")
  cp "$t/good/Sdk/$DOTNET_SDK_VERSION/$archive" "$t/bad/Sdk/$DOTNET_SDK_VERSION/"
  echo "$(printf '0%.0s' {1..128})  $archive" > "$t/bad/Sdk/$DOTNET_SDK_VERSION/$archive.sha512"

  check() {  # <name> <expected exit> <expected feed or -> <feeds…>: install into a fresh directory
    local name="$1" want="$2" from="$3" out rc dir="$t/install-$RANDOM$RANDOM"
    shift 3
    out=$(DOTNET_SDK_FEEDS="$*" dotnet_sdk_install "$dir" "$t/work" 2>&1); rc=$?
    if [ "$rc" -ne "$want" ] \
       || { [ "$want" -eq 0 ] && { [ ! -f "$dir/sdk/$DOTNET_SDK_VERSION/dotnet.dll" ] || [ ! -f "$dir/.dotnet-sdk-$DOTNET_SDK_VERSION.complete" ] \
                                   || [[ "$out" != *"installed in $dir from $from"* ]]; }; } \
       || { [ "$want" -ne 0 ] && [[ "$out" != *"no feed served"* ]]; }; then
      echo "FAIL: $name (exit $rc, wanted $want; output: $out)"; fails=$((fails + 1))
    else
      echo "ok: $name"
    fi
  }
  check "the first feed serves the archive" 0 "$base/good" "$base/good" "$base/bad"
  check "a refusing first feed hands over to the second" 0 "$base/good" "$base/missing" "$base/good"
  check "a checksum mismatch hands over to the next feed" 0 "$base/good" "$base/bad" "$base/good"
  check "no feed serves it: the install fails, naming the feeds" 1 - "$base/missing" "$base/bad"

  local dir="$t/installed" out
  DOTNET_SDK_FEEDS="$base/good" dotnet_sdk_install "$dir" "$t/work" > /dev/null 2>&1
  out=$(DOTNET_SDK_FEEDS="$base/missing" dotnet_sdk_install "$dir" "$t/work" 2>&1)
  if [ $? -eq 0 ] && [[ "$out" == *"already installed"* ]]; then echo "ok: an installed version is not fetched again"
  else echo "FAIL: an installed version is not fetched again (output: $out)"; fails=$((fails + 1)); fi

  rm -f "$dir/.dotnet-sdk-$DOTNET_SDK_VERSION.complete"
  out=$(DOTNET_SDK_FEEDS="$base/good" dotnet_sdk_install "$dir" "$t/work" 2>&1)
  if [ $? -eq 0 ] && [[ "$out" == *"installed in $dir from $base/good"* ]]; then echo "ok: an install with no marker is redone"
  else echo "FAIL: an install with no marker is redone (output: $out)"; fails=$((fails + 1)); fi

  # A transfer that breaks off (curl exit 56) is retried on the same feed; a refusal is never retried. `curl` and
  # `sleep` are shadowed by functions: the first download breaks off once, and the retry's pause costs nothing.
  : > "$t/broke"
  curl() { if [ -f "$t/broke" ]; then rm -f "$t/broke"; return 56; fi; command curl "$@"; }
  sleep() { :; }
  out=$(DOTNET_SDK_FEEDS="$base/good" dotnet_sdk_install "$t/retried" "$t/work" 2>&1)
  if [ $? -eq 0 ] && [[ "$out" == *"curl exit 56 (attempt 1 of 3)"* ]] && [[ "$out" == *"from $base/good"* ]]; then
    echo "ok: a transfer that broke off is retried on the same feed"
  else echo "FAIL: a transfer that broke off is retried on the same feed (output: $out)"; fails=$((fails + 1)); fi
  out=$(DOTNET_SDK_FEEDS="$base/missing $base/good" dotnet_sdk_install "$t/refused" "$t/work" 2>&1)
  if [ $? -eq 0 ] && [[ "$out" != *"attempt"* ]]; then echo "ok: a refusal is not retried"
  else echo "FAIL: a refusal is not retried (output: $out)"; fails=$((fails + 1)); fi
  unset -f curl sleep

  echo "=== INSTALL-DOTNET-SDK SELF-TEST: $([ "$fails" -eq 0 ] && echo PASS || echo "FAIL — $fails") ==="
  [ "$fails" -eq 0 ]
}

case "${1:-}" in
  --version) echo "$DOTNET_SDK_VERSION" ;;
  --self-test) self_test ;;
  "" | -*) echo "usage: $0 --version | --self-test | <install-dir>" >&2; exit 2 ;;
  *)
    dir="$1"
    # Git Bash: a Windows path (C:\… from a runner variable) becomes the POSIX form its tools all read.
    case "$(uname -s)" in MINGW*|MSYS*|CYGWIN*) dir=$(cygpath -u "$dir") ;; esac
    work=$(mktemp -d "${TMPDIR:-/tmp}/install-dotnet-sdk.XXXXXX") || exit 1
    trap 'rm -rf "$work"' EXIT
    dotnet_sdk_install "$dir" "$work"
    ;;
esac
