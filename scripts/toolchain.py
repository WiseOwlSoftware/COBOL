#!/usr/bin/env python3
"""toolchain.py — THE READER OF THE ONE TOOLCHAIN MANIFEST, toolchain.env (kb/Work PB4007; DESIGN-test-build-ci.md §1.4).

    python scripts/toolchain.py versions                 # every tool and the version the manifest names
    python scripts/toolchain.py check --scope ci|image|gate|host  # does each tool on PATH report the manifest's version?
    python scripts/toolchain.py latest                   # is the manifest at each publisher's newest stable release?
    python scripts/toolchain.py bump                     # move the manifest (and its cloud copy) to those releases
    python scripts/toolchain.py ci-installs              # the tools setup-toolchain installs with install-tool.sh
    python scripts/toolchain.py --self-test

The owner, 2026-10-28: "CI tooling should match local tooling versions - always", then "CI and local should always be
using the latest version". `check` is the IDENTICAL half: CI's setup-toolchain action runs it with `--scope ci` after it
installs, the gate runs it with `--scope gate` before anything else and stops NOT RUN [toolchain] on a mismatch, and the
SessionStart readiness check prints its `--scope host` comparison. `latest` is the LATEST half: the scheduled workflow
toolchain-freshness.yml runs it every day, and a manifest behind a publisher is a red run naming the tool, never silence.

A tool's version is what the tool itself reports when it runs (`dotnet --version`, `java -version`, ...), never what an
installer or a package source claims: WSL's git stayed on 2.55 for days while its apt source existed but was disabled,
and apt said nothing (kb/Work PB4007).

TOOLS below is the ONE table of the tools: the manifest names exactly its keys and OTHER_KEYS; CI's setup-toolchain
installs exactly the rows that run in CI and come from scripts/install-tool.sh (`ci-installs`); install-tool.sh's
TOOL_INSTALLABLE is exactly the install-tool rows (`check` refuses otherwise, in every CI job and every gate); and
ToolchainManifestDriftTests reads the rows from this file's text instead of keeping a list of its own. So a new tool is
one row here, one manifest line and, for an install-tool row, one `tool_spec` arm in install-tool.sh.

git is the runner image's in CI. actions/checkout runs it before any repository file exists, and no publisher ships a
Linux git of a chosen version (the Ubuntu image lacks the zlib and libcurl headers a source build needs), so CI cannot
install it from the manifest. The manifest names each image's git (GIT_LINUX_VERSION, GIT_WINDOWS_VERSION), `latest`
reads it from the image's own readme, and each host installs that release. A CI job reports an image whose git moved as
a warning, never a failure: GitHub rolls each image on its own schedule, and a hard failure there would stop every
landing until a bump, or, with the two images on different weeks, until both converged. The daily toolchain-freshness
run holds each image strictly (`check --scope image`), so a moved image is a red scheduled run within a day.
"""
from __future__ import annotations

import argparse
import dataclasses
import json
import os
import platform
import re
import shutil
import subprocess
import sys
import tempfile
import urllib.request
from pathlib import Path
from typing import Callable

sys.path.insert(0, str(Path(__file__).resolve().parent))
from standard_streams import ensure_utf8  # noqa: E402

REPO = Path(__file__).resolve().parents[1]
MANIFEST = REPO / "toolchain.env"
#: setup-env.sh runs before the clone, so it carries the manifest's block; `bump` rewrites both copies.
COPIES = (MANIFEST, REPO / "scripts" / "cloud" / "setup-env.sh")
BEGIN, END = "# >>> toolchain", "# <<< toolchain"
#: A manifest line: NAME=value, the value one word (an exact version or a runner label).
LINE = re.compile(r"^(?P<key>[A-Z][A-Z0-9_]*)=(?P<value>\S+)$", re.M)
#: An exact version: digits and dots, never `x`, `*`, `latest` or a range.
EXACT = re.compile(r"^\d+(?:\.\d+)+$")
VERDICT = "=== TOOLCHAIN: "

#: The scopes `check` holds. `ci`: what a CI job runs once setup-toolchain installed it (a runner image's own tool is
#: reported there, never failed). `image`: the runner image's own tools, strictly (the freshness run, on each image).
#: `gate`: what the local gate runs. `host`: everything a host carries.
SCOPES = ("ci", "image", "gate", "host")

#: How a host gets a tool: scripts/install-tool.sh, from its publisher (CI and every host); actions/setup-python in CI
#: and the Python Install Manager or uv on a host; the runner image in CI and the host's own package elsewhere (git);
#: `uv tool install` (semgrep, on a host only).
INSTALL_TOOL, SETUP_PYTHON, IMAGE, UV_TOOL = "install-tool", "setup-python", "image", "uv-tool"
#: Where a tool runs: in CI and on every host, or on a host alone.
CI, HOST = "ci", "host"

#: A publisher's newest stable release the manifest may move to, and a note for the owner (None when there is none): a
#: release the manifest may not move to by itself, such as a new major .NET channel.
Release = tuple[str, str | None]
Latest = Callable[[Callable[[str], str], dict[str, str]], Release]


@dataclasses.dataclass(frozen=True)
class Tool:
    """One tool of the manifest: its name, its manifest key, how it reports its version, how a host gets it, where it
    runs, where its newest stable release is read, and the one OS it applies on (`platform.system()`, None on every
    OS). Each row of TOOLS is written on one line: ToolchainManifestDriftTests reads them from this file's text."""
    name: str
    key: str
    command: tuple[str, ...] | None     # None: the interpreter running this script (the gate's own $PY)
    pattern: str                        # the version in the command's output (stdout and stderr together)
    source: str                         # INSTALL_TOOL, SETUP_PYTHON, IMAGE or UV_TOOL
    runs: str                           # CI or HOST
    latest: Latest
    system: str | None = None

    @property
    def scopes(self) -> frozenset[str]:
        if self.runs == HOST:
            return frozenset({"host"})
        return frozenset({"ci", "gate", "host"} | ({"image"} if self.source == IMAGE else set()))

    def applies(self, system: str) -> bool:
        return self.system is None or self.system == system


def _github(repo: str, prefix: str = "v") -> Latest:
    """The newest release a GitHub repository publishes (`releases/latest` never answers a prerelease or a draft)."""
    def latest(fetch: Callable[[str], str], manifest: dict[str, str]) -> Release:
        tag = json.loads(fetch(f"https://api.github.com/repos/{repo}/releases/latest"))["tag_name"]
        return tag.removeprefix(prefix), None
    return latest


def _dotnet_sdk(fetch: Callable[[str], str], manifest: dict[str, str]) -> Release:
    """The newest SDK of the manifest's own major channel (a channel in `go-live` or `preview` is a release candidate).
    A newer GA major channel is a note, never the target: moving to it also moves global.json and every
    TargetFramework, an owner's decision, so a freshness run stays red only for a release the manifest can take."""
    index = json.loads(fetch("https://builds.dotnet.microsoft.com/dotnet/release-metadata/releases-index.json"))
    ga = [r for r in index["releases-index"] if r.get("support-phase") in ("active", "maintenance")]
    major = _key(manifest["DOTNET_SDK_VERSION"])[0]
    own = [r for r in ga if _key(r["channel-version"])[0] == major]
    if not own:
        raise ValueError(f"the .NET release index has no GA {major}.0 channel")
    newer = [r for r in ga if _key(r["channel-version"])[0] > major]
    note = None
    if newer:
        top = max(newer, key=lambda r: _key(r["channel-version"]))
        note = (f".NET {top['channel-version']} is GA (SDK {top['latest-sdk']}): moving to it also moves global.json and "
                "every TargetFramework, the owner's decision")
    return max(own, key=lambda r: _key(r["channel-version"]))["latest-sdk"], note


def _python(fetch: Callable[[str], str], manifest: dict[str, str]) -> Release:
    """The newest stable CPython that actions/setup-python can install: its versions manifest, so a release CI cannot
    install yet is never the target."""
    rows = json.loads(fetch("https://raw.githubusercontent.com/actions/python-versions/main/versions-manifest.json"))
    return max((r["version"] for r in rows if r.get("stable") and EXACT.match(r["version"])), key=_key), None


def _jdk(fetch: Callable[[str], str], manifest: dict[str, str]) -> Release:
    """The newest Oracle JDK: the newest feature release of Oracle's current releases, its `27.0.1.0.3` build number
    trimmed to the `27.0.1` its archive URL names. Oracle's other products (`8-perf`, the Java 8 Performance Pack) carry
    a feature name that is not a number, and are not the JDK."""
    items = json.loads(fetch("https://java.oraclecloud.com/currentJavaReleases"))["items"]
    jdks = [i["jdkDetails"] for i in items if i["jdkDetails"]["jdkVersion"].isdigit()]
    newest = max(jdks, key=lambda d: int(d["jdkVersion"]))
    return ".".join(newest["latestReleaseVersion"].split(".")[:3]), None


def image_readme(label: str) -> str:
    """The runner image's readme, which lists its tools: `ubuntu-26.04` is images/ubuntu/Ubuntu2604-Readme.md and
    `windows-2025-vs2026` images/windows/Windows2025-VS2026-Readme.md in actions/runner-images."""
    family, release, *variant = label.split("-")
    name = family.capitalize() + release.replace(".", "") + "".join(f"-{v.upper()}" for v in variant)
    return f"https://raw.githubusercontent.com/actions/runner-images/main/images/{family}/{name}-Readme.md"


def _image_git(label_key: str) -> Latest:
    """The git the runner image named by `label_key` carries, from its readme's `- Git 2.56.0` line (Git for Windows'
    `.windows.N` revision dropped): the only git a CI job can run, so the release the manifest and the hosts follow."""
    def latest(fetch: Callable[[str], str], manifest: dict[str, str]) -> Release:
        m = re.search(r"(?m)^- Git (\d+\.\d+\.\d+)", fetch(image_readme(manifest[label_key])))
        if m is None:
            raise ValueError(f"the {manifest[label_key]} readme names no git")
        return m.group(1), None
    return latest


def _pypi(package: str) -> Latest:
    def latest(fetch: Callable[[str], str], manifest: dict[str, str]) -> Release:
        return json.loads(fetch(f"https://pypi.org/pypi/{package}/json"))["info"]["version"], None
    return latest


#: git's own release: Git for Windows reports `2.56.0.windows.3`, its packaging revision after git's version.
GIT = r"git version (\d+\.\d+\.\d+)"
TOOLS: tuple[Tool, ...] = (
    Tool("dotnet-sdk", "DOTNET_SDK_VERSION", ("dotnet", "--version"), r"^(\d+\.\d+\.\d+)\s*$", INSTALL_TOOL, CI, _dotnet_sdk),
    Tool("python", "PYTHON_VERSION", None, r"^(\d+\.\d+\.\d+)$", SETUP_PYTHON, CI, _python),
    Tool("jdk", "JDK_VERSION", ("java", "-version"), r'version "(\d+(?:\.\d+)*)', INSTALL_TOOL, CI, _jdk),
    Tool("pwsh", "PWSH_VERSION", ("pwsh", "--version"), r"PowerShell (\d+\.\d+\.\d+)", INSTALL_TOOL, CI, _github("PowerShell/PowerShell")),
    Tool("git-linux", "GIT_LINUX_VERSION", ("git", "--version"), GIT, IMAGE, CI, _image_git("CI_LINUX_IMAGE"), "Linux"),
    Tool("git-windows", "GIT_WINDOWS_VERSION", ("git", "--version"), GIT, IMAGE, CI, _image_git("CI_WINDOWS_IMAGE"), "Windows"),
    Tool("gh", "GH_VERSION", ("gh", "--version"), r"gh version (\d+\.\d+\.\d+)", INSTALL_TOOL, CI, _github("cli/cli")),
    Tool("jq", "JQ_VERSION", ("jq", "--version"), r"jq-(\d+\.\d+(?:\.\d+)?)", INSTALL_TOOL, CI, _github("jqlang/jq", "jq-")),
    Tool("uv", "UV_VERSION", ("uv", "--version"), r"uv (\d+\.\d+\.\d+)", INSTALL_TOOL, HOST, _github("astral-sh/uv", "")),
    # semgrep publishes to PyPI first (its GitHub release can trail it by a patch).
    Tool("semgrep", "SEMGREP_VERSION", ("semgrep", "--version"), r"(\d+\.\d+\.\d+)\s*$", UV_TOOL, HOST, _pypi("semgrep")),
)
#: Manifest lines that name no tool of their own: the semgrep interpreter exception and the CI runner labels, which the
#: workflow's `runs-on:` lines must equal (ToolchainManifestDriftTests).
OTHER_KEYS = ("SEMGREP_PYTHON_VERSION", "CI_LINUX_IMAGE", "CI_WINDOWS_IMAGE")


def _key(version: str) -> tuple[int, ...]:
    return tuple(int(p) for p in re.findall(r"\d+", version))


def block(text: str) -> str | None:
    """The manifest block, its two marker lines included, or None without both markers."""
    lf = text.replace("\r\n", "\n")
    begin, end = lf.find(BEGIN), lf.find(END)
    if begin < 0 or end < begin:
        return None
    stop = lf.find("\n", end)
    return lf[begin:stop if stop >= 0 else len(lf)]


def parse(text: str) -> dict[str, str]:
    """The manifest's NAME=value lines, from its block. ValueError when there is no block, a line repeats a name, a
    tool's version is not exact, or a tool or other key is missing."""
    body = block(text)
    if body is None:
        raise ValueError(f"no `{BEGIN}` … `{END}` block")
    values: dict[str, str] = {}
    for m in LINE.finditer(body):
        if m["key"] in values:
            raise ValueError(f"{m['key']} is named twice")
        values[m["key"]] = m["value"]
    known = (*(t.key for t in TOOLS), *OTHER_KEYS)
    missing = [k for k in known if k not in values]
    if missing:
        raise ValueError(f"no {', '.join(missing)}")
    unknown = [k for k in values if k not in known]
    if unknown:
        raise ValueError(f"{', '.join(unknown)} names no tool of scripts/toolchain.py TOOLS (give it a row there)")
    loose = [f"{t.key}={values[t.key]}" for t in TOOLS if not EXACT.match(values[t.key])]
    if loose:
        raise ValueError(f"not an exact version: {', '.join(loose)}")
    return values


def load(path: Path = MANIFEST) -> dict[str, str]:
    return parse(path.read_text(encoding="utf-8"))


def installed(tool: Tool, run: Callable[[tuple[str, ...]], str | None]) -> str | None:
    """The version `tool` reports on this host, or None when it is absent or reports none."""
    if tool.command is None:
        return platform.python_version()
    out = run(tool.command)
    m = re.search(tool.pattern, out or "", re.M)
    return m.group(1) if m else None


def _run(command: tuple[str, ...]) -> str | None:
    exe = shutil.which(command[0])
    if exe is None:
        return None
    try:
        # From the repository root: `dotnet --version` answers the SDK global.json selects there, which is the one the
        # build uses (elsewhere it answers the newest installed, a preview among them).
        p = subprocess.run([exe, *command[1:]], capture_output=True, text=True, timeout=60, encoding="utf-8",
                           errors="replace", cwd=REPO)
    except (OSError, subprocess.SubprocessError):
        return None
    return p.stdout + p.stderr


def held(scope: str, system: str = platform.system()) -> list[Tool]:
    """The tools `scope` holds on `system`."""
    return [t for t in TOOLS if scope in t.scopes and t.applies(system)]


def compare(manifest: dict[str, str], scope: str, run: Callable[[tuple[str, ...]], str | None] = _run,
            system: str = platform.system()) -> tuple[list[str], list[str]]:
    """(the lines `check` prints, the mismatches): each tool of `scope` on `system`, the version it reports and the
    manifest's. In the `ci` scope a runner image's own tool (git) that differs is a GitHub warning line, never a
    mismatch: the image moved under the job, and the freshness run's `image` scope is where it is red."""
    lines, bad = [], []
    for tool in held(scope, system):
        want, got = manifest[tool.key], installed(tool, run)
        state = "OK" if got == want else ("ABSENT" if got is None else "MISMATCH")
        if state != "OK" and scope == "ci" and tool.source == IMAGE:
            lines.append(f"::warning title=toolchain::the runner image's {tool.name} is {got or 'absent'}, toolchain.env "
                         f"names {want}: the image moved; the toolchain-freshness run is red until `toolchain.py bump` "
                         "follows it (kb/Work PB4007)")
            state = "IMAGE"
        lines.append(f"  {state:8} {tool.name} {got or '(none)'}" + ("" if state == "OK" else f" — toolchain.env names {want}"))
        if state in ("ABSENT", "MISMATCH"):
            bad.append(f"{tool.name} {got or 'absent'} (toolchain.env {want})")
    return lines, bad


def summary(manifest: dict[str, str], scope: str, system: str = platform.system()) -> str:
    """The toolchain a run measured under, as its verdict names it: `dotnet-sdk 10.0.403, python 3.15.2, …`."""
    return ", ".join(f"{t.name} {manifest[t.key]}" for t in held(scope, system))


def ci_installs() -> list[str]:
    """The tools setup-toolchain installs with scripts/install-tool.sh: every install-tool row that runs in CI."""
    return [t.name for t in TOOLS if t.runs == CI and t.source == INSTALL_TOOL]


INSTALLER = REPO / "scripts" / "install-tool.sh"
INSTALLABLE = re.compile(r'(?m)^TOOL_INSTALLABLE="(?P<tools>[^"]*)"$')


def installer_problems(text: str) -> list[str]:
    """What keeps scripts/install-tool.sh's TOOL_INSTALLABLE (its `text`) from being exactly TOOLS' install-tool rows,
    in their order: a tool CI or a host would install that TOOLS does not hold, or one TOOLS says it installs that has
    no `tool_spec` arm. Empty when they agree."""
    m = INSTALLABLE.search(text.replace("\r\n", "\n"))
    if m is None:
        return ["scripts/install-tool.sh has no TOOL_INSTALLABLE line"]
    have, want = m["tools"].split(), [t.name for t in TOOLS if t.source == INSTALL_TOOL]
    return [] if have == want else [f"scripts/install-tool.sh TOOL_INSTALLABLE is `{' '.join(have)}`, but TOOLS' "
                                    f"install-tool rows are `{' '.join(want)}`: change both together"]


def check(scope: str, manifest: dict[str, str] | None = None,
          run: Callable[[tuple[str, ...]], str | None] = _run, system: str = platform.system(),
          installer: str | None = None) -> tuple[int, str]:
    """(exit code, report): 0 GREEN when every tool of `scope` on `system` reports the manifest's version and the
    installer lists exactly TOOLS' install-tool rows, else 1 RED naming each."""
    manifest = manifest if manifest is not None else load()
    lines, bad = compare(manifest, scope, run, system)
    bad += installer_problems(installer if installer is not None else INSTALLER.read_text(encoding="utf-8"))
    verdict = (f"{VERDICT}GREEN — {scope}: {summary(manifest, scope, system)} ===" if not bad else
               f"{VERDICT}RED — {scope}: {'; '.join(bad)}; install the manifest's version (winget on the Windows dev "
               "box, scripts/wsl/setup-wsl.sh in WSL), or move toolchain.env to the installed one if it is the newer "
               "release (`python scripts/toolchain.py latest`) ===")
    return (0 if not bad else 1), "\n".join([*lines, verdict])


def fetch_url(url: str) -> str:
    """GET `url`. A GitHub API request carries $GH_TOKEN when it is set: api.github.com allows an unauthenticated CI
    runner's shared address 60 requests an hour."""
    headers = {"User-Agent": "wiseowl-cobol-toolchain", "Accept": "application/json"}
    if url.startswith("https://api.github.com/") and os.environ.get("GH_TOKEN"):
        headers["Authorization"] = f"Bearer {os.environ['GH_TOKEN']}"
    request = urllib.request.Request(url, headers=headers)
    with urllib.request.urlopen(request, timeout=60) as r:  # noqa: S310 — https URLs this module names
        return r.read().decode("utf-8")


@dataclasses.dataclass(frozen=True)
class Newest:
    """One tool's freshness: the manifest's version, the newest release it may move to (None when unread), the error
    that kept the publisher from being read, and a note for the owner."""
    tool: Tool
    have: str
    new: str | None
    error: str | None = None
    note: str | None = None


def newest(manifest: dict[str, str], fetch: Callable[[str], str] = fetch_url) -> list[Newest]:
    """Every tool's freshness, each publisher asked once."""
    rows = []
    for tool in TOOLS:
        try:
            new, note = tool.latest(fetch, manifest)
            rows.append(Newest(tool, manifest[tool.key], new, note=note))
        except Exception as e:  # noqa: BLE001 — an unreadable publisher is reported, never silent
            rows.append(Newest(tool, manifest[tool.key], None, f"{type(e).__name__}: {e}"))
    return rows


def latest_report(rows: list[Newest]) -> tuple[int, str]:
    """(exit code, report): 0 when the manifest is at every publisher's newest release it may move to, 1 when it is
    behind on one, 2 when a publisher could not be read (a freshness check that cannot see is not green). A note (a new
    major .NET channel) is a GitHub warning line for the owner and changes no exit code, so the run's colour keeps
    following the releases `bump` can take."""
    lines, behind, unread = [], [], []
    for row in rows:
        tool, have, new = row.tool, row.have, row.new
        if row.error is not None or new is None:
            unread.append(tool.name)
            lines.append(f"  UNREAD   {tool.name} {have} — {row.error}")
        elif _key(new) > _key(have):
            behind.append(f"{tool.name} {have} → {new}")
            lines.append(f"  BEHIND   {tool.name} {have} — the newest release is {new}")
        else:
            lines.append(f"  LATEST   {tool.name} {have}")
        if row.note is not None:
            lines.append(f"::warning title=toolchain::{tool.name}: {row.note}")
    if behind:
        verdict = (f"{VERDICT}BEHIND — {'; '.join(behind)}: run `python scripts/toolchain.py bump` in a change of its "
                   "own, update the hosts to it, and gate and land it ===")
    elif unread:
        verdict = f"{VERDICT}UNREAD — no newest release read for {', '.join(unread)} ==="
    else:
        verdict = f"{VERDICT}LATEST — every tool is at its newest release ==="
    return (1 if behind else 2 if unread else 0), "\n".join([*lines, verdict])


def bumped(text: str, rows: list[Newest]) -> tuple[str, list[str]]:
    """`text` (the manifest, or setup-env.sh's copy) with every tool moved to its newer release, and what moved. A new
    major .NET channel is never among them: `latest` offers the manifest's own channel's newest SDK, and the newer
    channel only as a note."""
    moved = []
    for row in rows:
        if row.error is not None or row.new is None or _key(row.new) <= _key(row.have):
            continue
        text = re.sub(rf"(?m)^{row.tool.key}={re.escape(row.have)}$", f"{row.tool.key}={row.new}", text)
        moved.append(f"{row.tool.name} {row.have} → {row.new}")
    return text, moved


def bump(fetch: Callable[[str], str] = fetch_url) -> int:
    rows = newest(load(), fetch)
    moved: list[str] = []
    for path in COPIES:
        text = path.read_text(encoding="utf-8")
        new, moved = bumped(text, rows)
        if new != text:
            path.write_bytes(new.encode("utf-8"))
    for m in moved:
        print(f"  {m}")
    for row in rows:
        if row.note is not None:
            print(f"  NOT written: {row.tool.name}: {row.note}")
    print(f"{VERDICT}{'BUMPED' if moved else 'LATEST'} — {len(moved)} tool(s); paste scripts/cloud/setup-env.sh into the "
          "cloud environment and update the hosts in the same change ===")
    return 0


def self_test() -> int:
    fails: list[str] = []

    def expect(what: str, ok: bool) -> None:
        print(f"{'ok  ' if ok else 'FAIL'}  {what}")
        if not ok:
            fails.append(what)

    live = MANIFEST.read_text(encoding="utf-8")
    load()
    expect("the live manifest parses, names every tool with an exact version, and its cloud copy is the same block",
           block(live) == block(COPIES[1].read_text(encoding="utf-8")))
    expect("the live install-tool.sh lists exactly TOOLS' install-tool rows",
           installer_problems(INSTALLER.read_text(encoding="utf-8")) == [])
    images = {"CI_LINUX_IMAGE": "ubuntu-26.04", "CI_WINDOWS_IMAGE": "windows-2025-vs2026"}
    good = "\n".join([BEGIN, *(f"{t.key}=1.2.3" for t in TOOLS), *(f"{k}=x" for k in OTHER_KEYS), END]) + "\n"
    expect("a block naming every tool and key parses", parse(good)[TOOLS[0].key] == "1.2.3")
    for what, text in [("a missing block", "DOTNET_SDK_VERSION=1.2.3\n"),
                       ("a tool with no version", good.replace(f"{TOOLS[2].key}=1.2.3\n", "")),
                       ("a loose version (10.0.x)", good.replace(f"{TOOLS[0].key}=1.2.3", f"{TOOLS[0].key}=10.0.x")),
                       ("a name given twice", good.replace(END, f"{TOOLS[1].key}=1.2.4\n{END}")),
                       ("a key no TOOLS row names (XZ_VERSION)", good.replace(END, f"XZ_VERSION=5.8.1\n{END}"))]:
        try:
            parse(text)
            expect(f"{what} is refused", False)
        except ValueError:
            expect(f"{what} is refused", True)

    # The version each tool reports, from the outputs measured on the hosts on 2026-10-28.
    outputs = {"dotnet": "10.0.403\n", "java": 'java version "27.0.1" 2026-10-20\nJava(TM) SE Runtime Environment\n',
               "pwsh": "PowerShell 7.6.9\n", "git": "git version 2.56.0.windows.3\n",
               "gh": "gh version 2.103.3 (2026-10-26)\nhttps://github.com/cli/cli/releases/tag/v2.103.3\n",
               "jq": "jq-1.8.2\n", "uv": "uv 0.13.6 (329bf9448 2026-10-26 x86_64-pc-windows-msvc)\n",
               "semgrep": "A new version is available\n1.182.1\n"}

    def run(command: tuple[str, ...]) -> str | None:
        return outputs.get(command[0])

    reported = {t.name: installed(t, run) for t in TOOLS}
    expect("each tool's own report gives its version (git without Git for Windows' revision)",
           reported == {"dotnet-sdk": "10.0.403", "python": platform.python_version(), "jdk": "27.0.1", "pwsh": "7.6.9",
                        "git-linux": "2.56.0", "git-windows": "2.56.0", "gh": "2.103.3", "jq": "1.8.2", "uv": "0.13.6",
                        "semgrep": "1.182.1"})
    fixed = {**{t.key: reported[t.name] for t in TOOLS}, **{k: "x" for k in OTHER_KEYS}, **images}
    installer = 'TOOL_INSTALLABLE="dotnet-sdk jdk pwsh gh jq uv"\n'
    code, report = check("ci", fixed, run, "Windows", installer)
    expect("check GREEN when every tool reports the manifest's version, naming this OS's toolchain",
           code == 0 and f"{VERDICT}GREEN — ci: dotnet-sdk 10.0.403, python" in report and "uv" not in report
           and "git-windows 2.56.0" in report and "git-linux" not in report)
    code, report = check("gate", {**fixed, "JDK_VERSION": "27.0.2"}, lambda c: None if c[0] == "gh" else run(c), "Linux",
                         installer)
    expect("check RED naming a mismatched and an absent tool",
           code == 1 and "jdk 27.0.1 (toolchain.env 27.0.2)" in report and "gh absent (toolchain.env 2.103.3)" in report
           and "MISMATCH jdk 27.0.1" in report)

    # An image whose git moved under a CI job: a warning there, never a failure; red in the image scope and on a host.
    moved = {**fixed, "GIT_LINUX_VERSION": "2.55.0"}
    code, report = check("ci", moved, run, "Linux", installer)
    expect("a CI job on an image whose git moved is GREEN with a warning naming it",
           code == 0 and "::warning title=toolchain::the runner image's git-linux is 2.56.0, toolchain.env names 2.55.0"
           in report and "IMAGE    git-linux 2.56.0" in report)
    expect("the image scope holds the image's git strictly",
           check("image", moved, run, "Linux", installer)[0] == 1
           and check("image", moved, run, "Windows", installer)[0] == 0)
    expect("the gate holds a host's git strictly", check("gate", moved, run, "Linux", installer)[0] == 1)
    code, report = check("ci", {**moved, "JDK_VERSION": "27.0.2"}, run, "Linux", installer)
    expect("an installed tool that differs still fails a CI job", code == 1 and "jdk 27.0.1 (toolchain.env 27.0.2)" in report)

    # The one table: install-tool.sh must list exactly its install-tool rows, and CI installs those that run in CI.
    code, report = check("ci", fixed, run, "Linux", 'TOOL_INSTALLABLE="dotnet-sdk jdk pwsh gh jq"\n')
    expect("check RED when install-tool.sh lacks a TOOLS install-tool row", code == 1 and "TOOL_INSTALLABLE" in report)
    expect("install-tool.sh listing a tool TOOLS does not hold, or no list, is a problem",
           installer_problems('TOOL_INSTALLABLE="dotnet-sdk jdk pwsh gh jq uv xz"\n') != []
           and installer_problems("echo no list\n") != [] and installer_problems(installer) == [])
    expect("CI installs the install-tool rows that run in CI", ci_installs() == ["dotnet-sdk", "jdk", "pwsh", "gh", "jq"])
    expect("the scopes: CI and the gate hold seven tools on each OS, the image one, the host nine",
           all([len(held(s, system)) for s in SCOPES] == [7, 1, 7, 9] for system in ("Linux", "Windows")))
    expect("the image readme of each label",
           image_readme("ubuntu-26.04").endswith("/images/ubuntu/Ubuntu2604-Readme.md")
           and image_readme("windows-2025-vs2026").endswith("/images/windows/Windows2025-VS2026-Readme.md"))

    # The publishers' answers, in the shapes measured on 2026-10-28.
    answers = {
        "releases-index.json": json.dumps({"releases-index": [
            {"channel-version": "11.0", "latest-sdk": "11.0.100-rc.3.26522.148", "support-phase": "go-live"},
            {"channel-version": "10.0", "latest-sdk": "10.0.404", "support-phase": "active"},
            {"channel-version": "9.0", "latest-sdk": "9.0.320", "support-phase": "maintenance"}]}),
        "versions-manifest.json": json.dumps([{"version": "3.16.0-alpha.1", "stable": False},
                                              {"version": "3.15.2", "stable": True}, {"version": "3.9.25", "stable": True}]),
        "currentJavaReleases": json.dumps({"items": [
            {"jdkDetails": {"jdkVersion": "25", "latestReleaseVersion": "25.0.5.0.3"}},
            {"jdkDetails": {"jdkVersion": "8-perf", "latestReleaseVersion": "8.0.471.0.1"}},
            {"jdkDetails": {"jdkVersion": "27", "latestReleaseVersion": "27.0.1.0.3"}}]}),
        "Ubuntu2604-Readme.md": "### Tools\n- Git 2.56.0\n- Git LFS 3.8.1\n",
        "Windows2025-VS2026-Readme.md": "### Tools\n- Git 2.56.0.windows.2\n- Git LFS 3.8.0\n",
        "PowerShell/PowerShell/releases": json.dumps({"tag_name": "v7.6.9"}),
        "cli/cli/releases": json.dumps({"tag_name": "v2.103.3"}),
        "jqlang/jq/releases": json.dumps({"tag_name": "jq-1.8.2"}),
        "astral-sh/uv/releases": json.dumps({"tag_name": "0.13.6"}),
        "pypi.org/pypi/semgrep": json.dumps({"info": {"version": "1.182.1"}}),
    }

    def fetch(url: str) -> str:
        return next(v for k, v in answers.items() if k in url)

    rows = newest(fixed, fetch)
    expect("each publisher's newest stable release is read (a release candidate, a preview channel and an alpha never)",
           {r.tool.name: r.new for r in rows} == {"dotnet-sdk": "10.0.404", "python": "3.15.2", "jdk": "27.0.1",
                                                  "pwsh": "7.6.9", "git-linux": "2.56.0", "git-windows": "2.56.0",
                                                  "gh": "2.103.3", "jq": "1.8.2", "uv": "0.13.6", "semgrep": "1.182.1"}
           and all(r.note is None and r.error is None for r in rows))
    code, report = latest_report(rows)
    expect("latest names the one tool behind and exits 1", code == 1 and "dotnet-sdk 10.0.403 → 10.0.404" in report
           and report.count("  BEHIND ") == 1)

    def offline(url: str) -> str:
        if "pypi" in url:
            raise OSError("offline")
        return fetch(url)

    at_newest = {**fixed, "DOTNET_SDK_VERSION": "10.0.404"}
    code, report = latest_report(newest(at_newest, offline))
    expect("an unreadable publisher is UNREAD, exit 2, never LATEST", code == 2 and "UNREAD   semgrep" in report)
    code, report = latest_report(newest(at_newest, fetch))
    expect("latest exits 0 at every newest release", code == 0 and "LATEST — every tool" in report)
    readme = answers.pop("Windows2025-VS2026-Readme.md")
    answers["Windows2025-VS2026-Readme.md"] = "### Tools\n- Git LFS 3.8.0\n"
    code, report = latest_report(newest(at_newest, fetch))
    expect("an image readme naming no git is UNREAD", code == 2 and "UNREAD   git-windows" in report)
    answers["Windows2025-VS2026-Readme.md"] = readme

    # .NET 11 GA: the owner's decision, so a note, never BEHIND, and `bump` keeps the manifest's channel.
    answers["releases-index.json"] = json.dumps({"releases-index": [
        {"channel-version": "11.0", "latest-sdk": "11.0.100", "support-phase": "active"},
        {"channel-version": "10.0", "latest-sdk": "10.0.404", "support-phase": "active"}]})
    rows = newest(at_newest, fetch)
    dotnet = next(r for r in rows if r.tool.name == "dotnet-sdk")
    code, report = latest_report(rows)
    expect("a new GA major .NET channel is a warning note and exit 0, the manifest's own channel the target",
           dotnet.new == "10.0.404" and dotnet.note is not None and ".NET 11.0 is GA (SDK 11.0.100)" in dotnet.note
           and code == 0 and "::warning title=toolchain::dotnet-sdk: .NET 11.0 is GA" in report)
    behind = newest(fixed, fetch)
    code, report = latest_report(behind)
    expect("with .NET 11 GA a 10.0 SDK release is still BEHIND", code == 1 and "dotnet-sdk 10.0.403 → 10.0.404" in report)

    dotnet_row = TOOLS[0]
    text = good.replace(f"{dotnet_row.key}=1.2.3", "DOTNET_SDK_VERSION=10.0.403")
    new, moved_lines = bumped(text, [Newest(dotnet_row, "10.0.403", "10.0.404"), Newest(TOOLS[1], "1.2.3", "1.2.3")])
    expect("bump moves a tool behind its release, and only that line",
           new == text.replace("DOTNET_SDK_VERSION=10.0.403", "DOTNET_SDK_VERSION=10.0.404")
           and moved_lines == ["dotnet-sdk 10.0.403 → 10.0.404"])
    new, moved_lines = bumped(text, [Newest(dotnet_row, "10.0.403", None, "OSError: offline")])
    expect("bump never writes an unread release", new == text and moved_lines == [])
    with tempfile.TemporaryDirectory() as d:
        copy = Path(d) / "setup-env.sh"
        copy.write_text("#!/bin/bash\n" + text + "rest\n", encoding="utf-8", newline="\n")
        new, _ = bumped(copy.read_text(encoding="utf-8"), [Newest(dotnet_row, "10.0.403", "10.0.404")])
        expect("bump rewrites a copy carried inside another script", block(new) == block(bumped(text, [
            Newest(dotnet_row, "10.0.403", "10.0.404")])[0]) and new.endswith("rest\n"))

    print(f"=== TOOLCHAIN SELF-TEST: {'PASS' if not fails else f'FAIL — {len(fails)}'} ===")
    return 1 if fails else 0


def main(argv: list[str]) -> int:
    ap = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    ap.add_argument("--self-test", action="store_true")
    sub = ap.add_subparsers(dest="cmd")
    sub.add_parser("versions")
    c = sub.add_parser("check")
    c.add_argument("--scope", choices=SCOPES, default="host")
    sub.add_parser("latest")
    sub.add_parser("bump")
    sub.add_parser("ci-installs")
    args = ap.parse_args(argv)
    if args.self_test:
        return self_test()
    if args.cmd == "versions":
        manifest = load()
        for tool in TOOLS:
            print(f"{tool.name} {manifest[tool.key]}")
        return 0
    if args.cmd == "ci-installs":
        print(" ".join(ci_installs()))
        return 0
    if args.cmd == "check":
        code, report = check(args.scope)
        print(report)
        return code
    if args.cmd == "latest":
        code, report = latest_report(newest(load()))
        print(report)
        return code
    if args.cmd == "bump":
        return bump()
    ap.print_help()
    return 2


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main(sys.argv[1:]))
