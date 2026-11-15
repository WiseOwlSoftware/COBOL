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
the nightly workflow (nightly.yml, job `latest`) runs it every day, and a manifest behind a publisher is a red run naming the tool, never silence.

A tool's version is what the tool itself reports when it runs (`dotnet --version`, `java -version`, ...), never what an
installer or a package source claims: WSL's git stayed on 2.55 for days while its apt source existed but was disabled,
and apt said nothing (kb/Work PB4007).

TOOLS below is the ONE table of the tools: the manifest names exactly its keys and OTHER_KEYS; CI's setup-toolchain
installs exactly the rows that run in CI and come from scripts/install-tool.sh (`ci-installs`); install-tool.sh's
TOOL_INSTALLABLE is exactly the install-tool rows (`check` refuses otherwise, in every CI job and every gate); and
scripts/toolchain_audit.py imports the rows instead of keeping a list of its own. So a new tool is
one row here, one manifest line and, for an install-tool row, one `tool_spec` arm in install-tool.sh.

git is the runner image's in CI. actions/checkout runs it before any repository file exists, and no publisher ships a
Linux git of a chosen version (the Ubuntu image lacks the zlib and libcurl headers a source build needs), so CI cannot
install it from the manifest. The manifest names each image's git (GIT_LINUX_VERSION, GIT_WINDOWS_VERSION), `latest`
reads it from the image's newest release (image_release, the one source of an image's git: kb/Work PB5458), the
manifest follows that release either way (`latest` says AHEAD for a git newer than it and `bump` sets it back,
Newest.state), and each host installs that release. A CI job reports an image whose git moved as a warning, never a failure: GitHub rolls each
image on its own schedule, and a hard failure there would stop every landing until a bump, or, with the two images on
different weeks, until both converged. The daily nightly workflow run holds each image to its newest release
(`check --scope image`), so a moved image is a red scheduled run within a day; a job that landed on an image other than
the newest release mid-rollout is a warning only while the manifest names that release's git, since then no bump can
follow it.
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
#: reported there, never failed). `image`: the runner image's own tools against the manifest and the image's newest
#: release (the nightly `images` job, on each image; compare).
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
    OS)."""
    name: str
    key: str
    command: tuple[str, ...] | None     # None: the interpreter running this script (the gate's own $PY)
    pattern: str                        # the version in the command's output (stdout and stderr together)
    source: str                         # INSTALL_TOOL, SETUP_PYTHON, IMAGE or UV_TOOL
    runs: str                           # CI or HOST
    latest: Latest
    system: str | None = None
    image: str | None = None            # an IMAGE row: the manifest key of the runner label whose image carries it

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
    TargetFramework, an owner's decision, so the nightly `latest` job stays red only for a release the manifest can take."""
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


#: The runner images' repository. It publishes each image as a release tagged `<image>/<version>` (`ubuntu26/20261111.196`,
#: the `Image Release:` line a job's log prints) when GitHub starts rolling the image out, a prerelease until the rollout
#: completes, and moves the readme under images/ on `main` only once it has: on 2026-11-12 the nightly ran on
#: ubuntu26/20261111.196 (git 2.56.1) while `main`'s readme still described 20261101.186 (git 2.56.0), so the readme on
#: `main` is not the image a job runs on (kb/Work PB5458).
RUNNER_IMAGES = "actions/runner-images"
#: The `- Git 2.56.1` line of an image's readme (Git for Windows' `- Git 2.56.0.windows.4` read as 2.56.0).
README_GIT = re.compile(r"(?m)^- Git (\d+\.\d+\.\d+)")


def image_tag_prefix(label: str) -> str:
    """The release-tag prefix of the runner image `label` names: `ubuntu-26.04` is `ubuntu26`, `windows-2025-vs2026`
    `win25-vs2026`. ValueError for a family whose tags this function does not know (never a guessed prefix)."""
    family, release, *variant = label.split("-")
    if family == "ubuntu":
        short = "ubuntu" + release.split(".")[0]
    elif family == "windows":
        short = "win" + release[-2:]
    else:
        raise ValueError(f"no actions/runner-images release tag is known for the runner label {label}")
    return "-".join([short, *variant])


def image_readme(label: str, ref: str) -> str:
    """The readme of the runner image `label` names at `ref` (a release tag), which lists its tools: `ubuntu-26.04` is
    images/ubuntu/Ubuntu2604-Readme.md and `windows-2025-vs2026` images/windows/Windows2025-VS2026-Readme.md."""
    family, release, *variant = label.split("-")
    name = family.capitalize() + release.replace(".", "") + "".join(f"-{v.upper()}" for v in variant)
    return f"https://raw.githubusercontent.com/{RUNNER_IMAGES}/{ref}/images/{family}/{name}-Readme.md"


@dataclasses.dataclass(frozen=True)
class ImageRelease:
    """The newest release of a runner image: its tag, the git its readme names, and whether GitHub is still rolling it
    out (a prerelease), when a job may land on it or on the release before it."""
    tag: str
    git: str
    rolling_out: bool


def image_release(label: str, fetch: Callable[[str], str]) -> ImageRelease:
    """THE ONE SOURCE OF AN IMAGE'S GIT (kb/Work PB5458): the newest release of the image `label` names, a prerelease
    being rolled out included (a job lands on it from the day the rollout starts), drafts never, and the git its readme
    at that tag names. `latest` and `bump` read it, and the `images` job's `check --scope image` reads it to tell a
    manifest behind the image from an image other than its newest release. ValueError when no release of the image is
    among the newest 100 or its readme names no git."""
    prefix = image_tag_prefix(label) + "/"
    releases = json.loads(fetch(f"https://api.github.com/repos/{RUNNER_IMAGES}/releases?per_page=100"))
    own = [r for r in releases if r["tag_name"].startswith(prefix) and not r.get("draft")]
    if not own:
        raise ValueError(f"no {prefix}* release among {RUNNER_IMAGES}' newest 100 releases")
    newest_release = max(own, key=lambda r: _key(r["tag_name"].removeprefix(prefix)))
    tag = newest_release["tag_name"]
    m = README_GIT.search(fetch(image_readme(label, tag)))
    if m is None:
        raise ValueError(f"the {tag} readme names no git")
    return ImageRelease(tag, m.group(1), bool(newest_release.get("prerelease")))


def _image_git(label_key: str) -> Latest:
    """The git of the newest release of the runner image named by the manifest's `label_key` (image_release): the only
    git a CI job can run, so the release the manifest and the hosts follow."""
    def latest(fetch: Callable[[str], str], manifest: dict[str, str]) -> Release:
        return image_release(manifest[label_key], fetch).git, None
    return latest


def _pypi(package: str) -> Latest:
    def latest(fetch: Callable[[str], str], manifest: dict[str, str]) -> Release:
        return json.loads(fetch(f"https://pypi.org/pypi/{package}/json"))["info"]["version"], None
    return latest


#: git's own release: Git for Windows reports `2.56.0.windows.3`, its packaging revision after git's version.
GIT = r"git version (\d+\.\d+\.\d+)"


def _image_tool(name: str, key: str, label_key: str, system: str) -> Tool:
    """The git row of the runner image the manifest's `label_key` names: its newest release is where `latest` reads it
    and where the image scope tells a manifest behind from a rollout, both through image_release."""
    return Tool(name, key, ("git", "--version"), GIT, IMAGE, CI, _image_git(label_key), system, label_key)


TOOLS: tuple[Tool, ...] = (
    Tool("dotnet-sdk", "DOTNET_SDK_VERSION", ("dotnet", "--version"), r"^(\d+\.\d+\.\d+)\s*$", INSTALL_TOOL, CI, _dotnet_sdk),
    Tool("python", "PYTHON_VERSION", None, r"^(\d+\.\d+\.\d+)$", SETUP_PYTHON, CI, _python),
    Tool("jdk", "JDK_VERSION", ("java", "-version"), r'version "(\d+(?:\.\d+)*)', INSTALL_TOOL, CI, _jdk),
    Tool("pwsh", "PWSH_VERSION", ("pwsh", "--version"), r"PowerShell (\d+\.\d+\.\d+)", INSTALL_TOOL, CI, _github("PowerShell/PowerShell")),
    _image_tool("git-linux", "GIT_LINUX_VERSION", "CI_LINUX_IMAGE", "Linux"),
    _image_tool("git-windows", "GIT_WINDOWS_VERSION", "CI_WINDOWS_IMAGE", "Windows"),
    Tool("gh", "GH_VERSION", ("gh", "--version"), r"gh version (\d+\.\d+\.\d+)", INSTALL_TOOL, CI, _github("cli/cli")),
    Tool("jq", "JQ_VERSION", ("jq", "--version"), r"jq-(\d+\.\d+(?:\.\d+)?)", INSTALL_TOOL, CI, _github("jqlang/jq", "jq-")),
    Tool("uv", "UV_VERSION", ("uv", "--version"), r"uv (\d+\.\d+\.\d+)", INSTALL_TOOL, HOST, _github("astral-sh/uv", "")),
    # semgrep publishes to PyPI first (its GitHub release can trail it by a patch).
    Tool("semgrep", "SEMGREP_VERSION", ("semgrep", "--version"), r"(\d+\.\d+\.\d+)\s*$", UV_TOOL, HOST, _pypi("semgrep")),
)
#: Manifest lines that name no tool of their own: the semgrep interpreter exception and the CI runner labels, which the
#: workflow's `runs-on:` lines must equal (scripts/toolchain_audit.py).
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


#: How long a tool's version command may take to answer before the probe records that it did not (kb/Work PB5616: the
#: limit is recorded with the outcome, never raised or retried on a guess).
PROBE_LIMIT_S = 60


@dataclasses.dataclass(frozen=True)
class Answered:
    """The version command ran and ended: its output (stdout and stderr together) and its exit code."""
    text: str
    exit_code: int


@dataclasses.dataclass(frozen=True)
class NotOnPath:
    """The tool's command is not on PATH (`shutil.which` found nothing): the tool is absent from this host."""


@dataclasses.dataclass(frozen=True)
class TimedOut:
    """The version command did not end within `limit_s` seconds."""
    limit_s: int


@dataclasses.dataclass(frozen=True)
class NoStart:
    """The version command was found on PATH but could not be run: the error the OS or subprocess raised."""
    error: str


#: WHAT RUNNING A TOOL'S VERSION COMMAND DID (kb/Work PB5616). Before it, `_run` answered None for four different facts,
#: and the check called every one of them "absent": a `dotnet --version` that did not answer on a loaded host told the
#: operator to install an SDK that was installed, and the record kept nothing that said which fact it was.
Probe = Answered | NotOnPath | TimedOut | NoStart


@dataclasses.dataclass(frozen=True)
class Reported:
    """What a tool reported on this host: its `version`, or None with the `fact` that says why none was read. `unread`
    is True when the host could not run the tool just now (it did not answer, could not start, or answered something
    that is not a version): the version is unknown, and the tool is NOT shown to be missing or of another version.
    A tool that is not on PATH is absent (`unread` False)."""
    version: str | None
    fact: str | None = None
    unread: bool = False


def installed(tool: Tool, run: Callable[[tuple[str, ...]], Probe]) -> Reported:
    """The version `tool` reports on this host, or the fact of why none was read (Reported)."""
    if tool.command is None:
        return Reported(platform.python_version())
    probe = run(tool.command)
    match probe:
        case NotOnPath():
            return Reported(None, "absent (not on PATH)")
        case TimedOut(limit_s=limit):
            return Reported(None, f"did not answer within {limit} s", True)
        case NoStart(error=error):
            return Reported(None, f"could not start: {error}", True)
        case Answered(text=text, exit_code=code):
            m = re.search(tool.pattern, text, re.M)
            if m:
                return Reported(m.group(1))
            shown = text[:200].replace("\r", "\\r").replace("\n", "\\n")
            return Reported(None, f'answered no version: "{shown}", exit {code}', True)
    raise TypeError(f"{tool.name}: the probe answered {probe!r}, which is no Probe")


def _run(command: tuple[str, ...]) -> Probe:
    exe = shutil.which(command[0])
    if exe is None:
        return NotOnPath()
    try:
        # From the repository root: `dotnet --version` answers the SDK global.json selects there, which is the one the
        # build uses (elsewhere it answers the newest installed, a preview among them).
        p = subprocess.run([exe, *command[1:]], capture_output=True, text=True, timeout=PROBE_LIMIT_S,
                           encoding="utf-8", errors="replace", cwd=REPO)
    except subprocess.TimeoutExpired:
        return TimedOut(PROBE_LIMIT_S)
    except (OSError, subprocess.SubprocessError) as e:
        return NoStart(f"{type(e).__name__}: {e}")
    return Answered(p.stdout + p.stderr, p.returncode)


def held(scope: str, system: str = platform.system()) -> list[Tool]:
    """The tools `scope` holds on `system`."""
    return [t for t in TOOLS if scope in t.scopes and t.applies(system)]


def _image_moved(tool: Tool, got: str, want: str, manifest: dict[str, str],
                 fetch: Callable[[str], str]) -> tuple[str, str | None]:
    """(state, the clause the check line adds) for an image's own tool reporting `got` where the manifest names `want`,
    in the image scope. The manifest's duty is to name the git of the image's newest release (review R1-1, Mythos
    session 2026-11-13), so: MISMATCH when that release carries `got` too (the manifest is behind it), red, and `bump`
    clears it; ROLLOUT, a warning, only when the manifest names that release's git and the job ran on another image
    (GitHub rolling an image out, either side of it), which no change of the manifest can follow; MISMATCH, naming all
    three gits, when the job ran on another image and the manifest is off the release too, behind or ahead of it;
    MISMATCH when the release cannot be read (a check that cannot see is not green)."""
    label = manifest[tool.image]
    try:
        release = image_release(label, fetch)
    except Exception as e:  # noqa: BLE001 — an unreadable release is reported and red, never a pass
        return "MISMATCH", f"the newest {label} release was not read ({type(e).__name__}: {e})"
    if got == release.git:
        return "MISMATCH", f"the newest {label} release {release.tag} carries {got}: `python scripts/toolchain.py bump`"
    if want != release.git:
        return "MISMATCH", (f"toolchain.env names {want}, this image carries {got} and the newest {label} release "
                            f"{release.tag} carries {release.git}: `python scripts/toolchain.py bump`")
    side = "an older" if _key(got) < _key(release.git) else "a newer"
    rollout = "is rolling out" if release.rolling_out else "is deployed"
    return "ROLLOUT", (f"the job ran on {side} image than the newest {label} release {release.tag} ({rollout}, git "
                       f"{release.git}), which `bump` follows; no change of the manifest follows this image")


@dataclasses.dataclass(frozen=True)
class Comparison:
    """What `compare` found: the `lines` `check` prints; `bad`, each tool that is absent (not on PATH) or reports
    another version than the manifest's, which installing the manifest's version clears; and `unread`, each tool whose
    version the host could not read just now (Reported.unread: it did not answer, could not start, or answered no
    version), which is not shown to be missing and which no install clears (kb/Work PB5616). Either fails a check."""
    lines: list[str]
    bad: list[str]
    unread: list[str]


def compare(manifest: dict[str, str], scope: str, run: Callable[[tuple[str, ...]], Probe] = _run,
            system: str = platform.system(),
            fetch: Callable[[str], str] | None = None) -> Comparison:
    """Each tool of `scope` on `system`, the version it reports (or why none was read) and the manifest's (Comparison).
    In the `ci` scope a runner image's own tool (git) that differs or is absent is a GitHub warning line, never a
    mismatch: the image moved under the job, and the nightly `images` job's `image` scope is where it is red; one whose
    version was unread still fails, since an unresolved version never passes. The image scope reads the image's newest
    release (`fetch`, the network by default) for a git that differs, and only warns when the manifest names that
    release's git and the job ran on another image (_image_moved)."""
    lines, bad, unread = [], [], []
    for tool in held(scope, system):
        want, reported = manifest[tool.key], installed(tool, run)
        got = reported.version
        state = ("OK" if got == want else "UNREAD" if reported.unread else "ABSENT" if got is None else "MISMATCH")
        shown = got or reported.fact
        why = None
        if state in ("ABSENT", "MISMATCH") and scope == "ci" and tool.source == IMAGE:
            lines.append(f"::warning title=toolchain::the runner image's {tool.name} is {shown}, toolchain.env "
                         f"names {want}: the image moved; the nightly `images` job is red while the image's newest "
                         "release carries it, until `toolchain.py bump` follows it (kb/Work PB4007, PB5458)")
            state = "IMAGE"
        if state == "MISMATCH" and scope == "image" and tool.source == IMAGE:
            state, why = _image_moved(tool, got, want, manifest, fetch or fetch_url)
            if state == "ROLLOUT":
                lines.append(f"::warning title=toolchain::the runner image's {tool.name} is {got}, toolchain.env names "
                             f"{want}: {why} (kb/Work PB5458)")
        lines.append(f"  {state:8} {tool.name} {shown}"
                     + ("" if state == "OK" else f" — toolchain.env names {want}") + (f"; {why}" if why else ""))
        if state in ("ABSENT", "MISMATCH"):
            bad.append(f"{tool.name} {shown} (toolchain.env {want})")
        elif state == "UNREAD":
            unread.append(f"{tool.name} {shown} (toolchain.env {want})")
    return Comparison(lines, bad, unread)


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


#: `check`'s exit codes: 0 every tool reports the manifest's version; 1 a tool is absent or reports another version
#: (install the manifest's), or an installer problem; EXIT_UNREAD, nothing is shown absent or of another version but a
#: tool's version could not be read just now (kb/Work PB5616): linux-gate.sh reads it as NOT RUN [toolchain-unread].
EXIT_UNREAD = 3

#: What a tool whose version was unread means, said wherever it is reported (the check, the gate, the readiness check).
UNREAD_ADVICE = ("the host could not run it just now, so its version is unread; it is NOT shown to be missing or of "
                 "another version, so install nothing: the line names what happened, and a recurrence is root-caused "
                 "from it (kb/Work PB5616)")


def check(scope: str, manifest: dict[str, str] | None = None,
          run: Callable[[tuple[str, ...]], Probe] = _run, system: str = platform.system(),
          installer: str | None = None, fetch: Callable[[str], str] | None = None) -> tuple[int, str]:
    """(exit code, report): 0 GREEN when every tool of `scope` on `system` reports the manifest's version (in the image
    scope, or differs only because the job ran on an image other than its newest release) and the installer lists
    exactly TOOLS' install-tool rows; else RED naming each, exit 1 when anything is absent, of another version or an
    installer problem, EXIT_UNREAD when the only failures are versions the host could not read just now."""
    manifest = manifest if manifest is not None else load()
    found = compare(manifest, scope, run, system, fetch)
    bad = found.bad + installer_problems(installer if installer is not None else INSTALLER.read_text(encoding="utf-8"))
    if not bad and not found.unread:
        return 0, "\n".join([*found.lines, f"{VERDICT}GREEN — {scope}: {summary(manifest, scope, system)} ==="])
    said = []
    if bad:
        said.append(f"{'; '.join(bad)}; install the manifest's version (winget on the Windows dev box, "
                    "scripts/wsl/setup-wsl.sh in WSL), or move toolchain.env to the installed one if it is the newer "
                    "release (`python scripts/toolchain.py latest`)")
    if found.unread:
        said.append(f"{'; '.join(found.unread)}: {UNREAD_ADVICE}")
    return (1 if bad else EXIT_UNREAD), "\n".join([*found.lines, f"{VERDICT}RED — {scope}: {'; '.join(said)} ==="])


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

    @property
    def state(self) -> str:
        """THE ONE DECISION `latest` and `bump` share: UNREAD (no newest release read), BEHIND (the manifest is older
        than it), AHEAD (an image's own tool newer than the image's newest release: no release carries it, kb/Work
        PB5458 review R2-1) or LATEST. A tool the image carries FOLLOWS that release either way, so the manifest must
        equal it; an installable tool never moves backward (a manifest ahead of its publisher fails loudly at install)."""
        if self.error is not None or self.new is None:
            return "UNREAD"
        if _key(self.new) > _key(self.have):
            return "BEHIND"
        if self.tool.source == IMAGE and self.new != self.have:
            return "AHEAD"
        return "LATEST"


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
    behind on one, or names an image's git newer than the image's newest release (AHEAD, Newest.state), 2 when a
    publisher could not be read (a freshness check that cannot see is not green). A note (a new major .NET channel) is a
    GitHub warning line for the owner and changes no exit code, so the run's colour keeps following the releases `bump`
    can take."""
    lines, behind, ahead, unread = [], [], [], []
    for row in rows:
        tool, have, new, state = row.tool, row.have, row.new, row.state
        if state == "UNREAD":
            unread.append(tool.name)
            lines.append(f"  UNREAD   {tool.name} {have} — {row.error}")
        elif state == "BEHIND":
            behind.append(f"{tool.name} {have} → {new}")
            lines.append(f"  BEHIND   {tool.name} {have} — the newest release is {new}")
        elif state == "AHEAD":
            ahead.append(f"{tool.name} {have} → {new}")
            lines.append(f"  AHEAD    {tool.name} {have} — no release carries it; the image's newest release carries "
                         f"{new}, and `bump` sets it to that")
        else:
            lines.append(f"  LATEST   {tool.name} {have}")
        if row.note is not None:
            lines.append(f"::warning title=toolchain::{tool.name}: {row.note}")
    if behind or ahead:
        off = [*([f"BEHIND — {'; '.join(behind)}"] if behind else []), *([f"AHEAD — {'; '.join(ahead)}"] if ahead else [])]
        verdict = (f"{VERDICT}{'; '.join(off)}: run `python scripts/toolchain.py bump` in a change of its own, update "
                   "the hosts to it, and gate and land it ===")
    elif unread:
        verdict = f"{VERDICT}UNREAD — no newest release read for {', '.join(unread)} ==="
    else:
        verdict = f"{VERDICT}LATEST — every tool is at its newest release ==="
    return (1 if behind or ahead else 2 if unread else 0), "\n".join([*lines, verdict])


def bumped(text: str, rows: list[Newest]) -> tuple[str, list[str]]:
    """`text` (the manifest, or setup-env.sh's copy) with every tool moved to its newer release, and an image's git
    ahead of the image's newest release set back to it (Newest.state), and what moved. A new major .NET channel is
    never among them: `latest` offers the manifest's own channel's newest SDK, and the newer channel only as a note."""
    moved = []
    for row in rows:
        if row.state not in ("BEHIND", "AHEAD"):
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

    def run(command: tuple[str, ...]) -> Probe:
        return Answered(outputs[command[0]], 0) if command[0] in outputs else NotOnPath()

    reported = {t.name: installed(t, run).version for t in TOOLS}
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
    code, report = check("gate", {**fixed, "JDK_VERSION": "27.0.2"}, lambda c: NotOnPath() if c[0] == "gh" else run(c), "Linux",
                         installer)
    expect("check RED naming a mismatched and an absent tool",
           code == 1 and "jdk 27.0.1 (toolchain.env 27.0.2)" in report
           and "gh absent (not on PATH) (toolchain.env 2.103.3)" in report and "MISMATCH jdk 27.0.1" in report)

    # kb/Work PB5616: each thing a probe can do is its own fact, and only "not on PATH" is "absent". A probe that did not
    # answer, could not start, or answered no version still fails the check (an unresolved version never passes), under
    # its own name and with its own exit code, and never with the advice to install the tool.
    outcomes = {"timeout": (TimedOut(PROBE_LIMIT_S), f"dotnet-sdk did not answer within {PROBE_LIMIT_S} s"),
                "no-start": (NoStart("PermissionError: [WinError 5] Access is denied"),
                             "dotnet-sdk could not start: PermissionError: [WinError 5] Access is denied"),
                "no-version": (Answered("The command could not be loaded\r\n" + "x" * 300, 145),
                               'dotnet-sdk answered no version: "The command could not be loaded\\r\\n'
                               + "x" * 167 + '", exit 145'),
                "absent": (NotOnPath(), "dotnet-sdk absent (not on PATH)")}
    for name, (probe, line) in outcomes.items():
        code, report = check("gate", fixed, lambda c, probe=probe: probe if c[0] == "dotnet" else run(c), "Linux",
                             installer)
        state = "ABSENT  " if name == "absent" else "UNREAD  "
        expect(f"a dotnet probe outcome '{name}' is the check line `{line}`, RED with exit "
               f"{1 if name == 'absent' else EXIT_UNREAD}, and only 'absent' advises an install",
               f"  {state} {line} — toolchain.env names 10.0.403" in report.splitlines()
               and f"{line} (toolchain.env 10.0.403)" in report
               and code == (1 if name == "absent" else EXIT_UNREAD)
               and ("install the manifest's version" in report) == (name == "absent")
               and ("install nothing" in report) == (name != "absent")
               and (name == "absent" or "absent" not in report))
    expect("a good answer from a failing exit still gives its version, and is OK",
           installed(TOOLS[0], lambda c: Answered("10.0.403\n", 1)) == Reported("10.0.403"))
    code, report = check("gate", fixed, lambda c: (TimedOut(PROBE_LIMIT_S) if c[0] == "dotnet" else NotOnPath()
                                                   if c[0] == "gh" else run(c)), "Linux", installer)
    expect("an absent tool beside an unread one is exit 1 with both named, the install advice only for the absent one",
           code == 1 and "gh absent (not on PATH) (toolchain.env 2.103.3); install the manifest's version" in report
           and f"dotnet-sdk did not answer within {PROBE_LIMIT_S} s (toolchain.env 10.0.403): {UNREAD_ADVICE}" in report)

    # The real probe (_run), its process start planted: each outcome of subprocess.run becomes its own Probe, and a
    # `dotnet --version` that times out is reported as that, never as an SDK that is absent (the 2026-11-15 08:38Z
    # NOT RUN [toolchain] that called an installed SDK absent; run 000004-20261115T083827Z).
    def planted(which, outcome):
        real_which, real_run = shutil.which, subprocess.run

        def fake_run(argv, **kw):
            if isinstance(outcome, BaseException):
                raise outcome
            return subprocess.CompletedProcess(argv, outcome[1], outcome[0], "")
        shutil.which, subprocess.run = (lambda name: which), fake_run
        try:
            return _run(("dotnet", "--version")), check("gate", fixed, _run, "Linux", installer)
        finally:
            shutil.which, subprocess.run = real_which, real_run
    exe = "C:/dotnet/dotnet.exe"
    probes = {"not on PATH": planted(None, ("", 0)),
              "timed out": planted(exe, subprocess.TimeoutExpired(["dotnet"], PROBE_LIMIT_S)),
              "failed to start": planted(exe, OSError(8, "Exec format error")),
              "answered no version": planted(exe, ("no SDKs were found\n", 1)),
              "answered": planted(exe, ("10.0.403\n", 0))}
    got = [p for p, _ in probes.values()]
    expect(f"_run turns each outcome of the process into its own Probe: {got}",
           got == [NotOnPath(), TimedOut(PROBE_LIMIT_S), NoStart("OSError: [Errno 8] Exec format error"),
                   Answered("no SDKs were found\n", 1), Answered("10.0.403\n", 0)])
    timed_out = probes["timed out"][1][1]
    expect("a dotnet --version that timed out is reported as not answering within the limit, never as absent",
           f"dotnet-sdk did not answer within {PROBE_LIMIT_S} s" in timed_out and "dotnet-sdk absent" not in timed_out
           and "install nothing" in timed_out)

    # An image whose git moved under a CI job: a warning there, never a failure; red in the image scope and on a host.
    moved = {**fixed, "GIT_LINUX_VERSION": "2.55.0"}
    code, report = check("ci", moved, run, "Linux", installer)
    expect("a CI job on an image whose git moved is GREEN with a warning naming it",
           code == 0 and "::warning title=toolchain::the runner image's git-linux is 2.56.0, toolchain.env names 2.55.0"
           in report and "IMAGE    git-linux 2.56.0" in report)
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
    expect("the image readme of each label at a release tag",
           image_readme("ubuntu-26.04", "ubuntu26/20261111.196").endswith(
               "/runner-images/ubuntu26/20261111.196/images/ubuntu/Ubuntu2604-Readme.md")
           and image_readme("windows-2025-vs2026", "win25-vs2026/20261103.298").endswith(
               "/runner-images/win25-vs2026/20261103.298/images/windows/Windows2025-VS2026-Readme.md"))
    try:
        image_tag_prefix("macos-15")
        unknown_refused = False
    except ValueError:
        unknown_refused = True
    expect("each label's release-tag prefix, and a family with no known tags refused",
           image_tag_prefix("ubuntu-26.04") == "ubuntu26" and image_tag_prefix("windows-2025-vs2026") == "win25-vs2026"
           and image_tag_prefix("ubuntu-24.04") == "ubuntu24" and unknown_refused)

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
        # actions/runner-images on 2026-11-12 (kb/Work PB5458): the Ubuntu 26.04 image 20261111.196 rolls out as a
        # prerelease carrying git 2.56.1 while the readme on `main` still describes 20261101.186 and its git 2.56.0; the
        # newest Windows 2025 VS2026 image is a full release; a draft and the other images' tags are never the image's.
        "repos/actions/runner-images/releases": json.dumps([
            {"tag_name": "ubuntu26/20261120.1", "prerelease": True, "draft": True},
            {"tag_name": "win25/20261111.331", "prerelease": True, "draft": False},
            {"tag_name": "ubuntu26/20261111.196", "prerelease": True, "draft": False},
            {"tag_name": "ubuntu26-arm64/20261111.184", "prerelease": True, "draft": False},
            {"tag_name": "win25-vs2026/20261103.298", "prerelease": False, "draft": False},
            {"tag_name": "ubuntu26/20261101.186", "prerelease": False, "draft": False},
            {"tag_name": "win25-vs2026/20261030.294", "prerelease": False, "draft": False}]),
        "runner-images/main/images/ubuntu/Ubuntu2604-Readme.md": "### Tools\n- Git 2.56.0\n- Git LFS 3.8.1\n",
        "runner-images/ubuntu26/20261111.196/images/ubuntu/Ubuntu2604-Readme.md": "### Tools\n- Git 2.56.1\n- Git LFS 3.8.1\n",
        "runner-images/main/images/windows/Windows2025-VS2026-Readme.md": "### Tools\n- Git 2.56.0.windows.2\n",
        "runner-images/win25-vs2026/20261103.298/images/windows/Windows2025-VS2026-Readme.md":
            "### Tools\n- Git 2.56.0.windows.4\n- Git LFS 3.8.1\n",
        "PowerShell/PowerShell/releases": json.dumps({"tag_name": "v7.6.9"}),
        "cli/cli/releases": json.dumps({"tag_name": "v2.103.3"}),
        "jqlang/jq/releases": json.dumps({"tag_name": "jq-1.8.2"}),
        "astral-sh/uv/releases": json.dumps({"tag_name": "0.13.6"}),
        "pypi.org/pypi/semgrep": json.dumps({"info": {"version": "1.182.1"}}),
    }

    def fetch(url: str) -> str:
        return next(v for k, v in answers.items() if k in url)

    # The manifest at the newest git of each image's newest release, so dotnet-sdk is the one tool behind below.
    fixed = {**fixed, "GIT_LINUX_VERSION": "2.56.1"}
    rows = newest(fixed, fetch)
    expect("each publisher's newest stable release is read (a release candidate, a preview channel and an alpha never; "
           "an image's git from its newest release, never the lagging readme on main)",
           {r.tool.name: r.new for r in rows} == {"dotnet-sdk": "10.0.404", "python": "3.15.2", "jdk": "27.0.1",
                                                  "pwsh": "7.6.9", "git-linux": "2.56.1", "git-windows": "2.56.0",
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

    # An image's git FOLLOWS the image's newest release, either way; an installable tool never moves backward (R2-1).
    ahead = newest({**at_newest, "GIT_LINUX_VERSION": "2.57.0", "GH_VERSION": "2.200.0"}, fetch)
    code, report = latest_report(ahead)
    expect("latest names an image's git ahead of the image's newest release AHEAD and exits 1; an installable tool "
           "ahead of its publisher stays LATEST",
           code == 1 and "  AHEAD    git-linux 2.57.0 — no release carries it" in report
           and "  LATEST   gh 2.200.0" in report
           and "AHEAD — git-linux 2.57.0 → 2.56.1" in report)
    text = good.replace("GIT_LINUX_VERSION=1.2.3", "GIT_LINUX_VERSION=2.57.0").replace("GH_VERSION=1.2.3",
                                                                                      "GH_VERSION=2.200.0")
    new, moved_lines = bumped(text, ahead)
    expect("bump sets an image's git ahead of its newest release to the release's, and leaves an installable tool ahead",
           "GIT_LINUX_VERSION=2.56.1" in new and "GH_VERSION=2.200.0" in new
           and moved_lines == ["git-linux 2.57.0 → 2.56.1"])
    windows = "runner-images/win25-vs2026/20261103.298/images/windows/Windows2025-VS2026-Readme.md"
    readme, answers[windows] = answers[windows], "### Tools\n- Git LFS 3.8.0\n"
    code, report = latest_report(newest(at_newest, fetch))
    expect("an image readme naming no git is UNREAD", code == 2 and "UNREAD   git-windows" in report)
    answers[windows] = readme

    # The nightly `images` job (`check --scope image`) reads the same release `latest` does (kb/Work PB5458).
    def on_image(git: str) -> Callable[[tuple[str, ...]], Probe]:
        return lambda c: Answered(f"git version {git}\n", 0) if c[0] == "git" else run(c)

    def unreachable(url: str) -> str:
        raise OSError(f"offline: {url}")

    code, report = check("image", {**fixed, "GIT_LINUX_VERSION": "2.56.0"}, on_image("2.56.1"), "Linux", installer, fetch)
    expect("the image scope is RED when the image's newest release carries the git the manifest lacks (2026-11-12)",
           code == 1 and "MISMATCH git-linux 2.56.1" in report
           and "the newest ubuntu-26.04 release ubuntu26/20261111.196 carries 2.56.1" in report)
    code, report = check("image", {**fixed, "GIT_LINUX_VERSION": "2.56.1"}, on_image("2.56.0"), "Linux", installer, fetch)
    expect("the image scope warns, GREEN, on an older image while its newest release rolls out",
           code == 0 and "ROLLOUT  git-linux 2.56.0" in report and "an older image than" in report
           and "(is rolling out, git 2.56.1)" in report and "::warning title=toolchain::" in report)
    code, report = check("image", {**fixed, "GIT_LINUX_VERSION": "2.56.1"}, on_image("2.56.2"), "Linux", installer, fetch)
    expect("the image scope warns, GREEN, on an image newer than every published release",
           code == 0 and "ROLLOUT  git-linux 2.56.2" in report and "a newer image than" in report)
    # The manifest's duty is the newest release's git: off it, a job on another image is red too (review R1-1).
    code, report = check("image", {**fixed, "GIT_LINUX_VERSION": "2.57.0"}, on_image("2.56.0"), "Linux", installer, fetch)
    expect("the image scope is RED when the manifest is ahead of every release, mid-rollout",
           code == 1 and "MISMATCH git-linux 2.56.0" in report and "toolchain.env names 2.57.0" in report
           and "the newest ubuntu-26.04 release ubuntu26/20261111.196 carries 2.56.1" in report and "bump" in report)
    newer = "repos/actions/runner-images/releases"
    releases, answers[newer] = answers[newer], json.dumps(
        [{"tag_name": "ubuntu26/20261118.200", "prerelease": True, "draft": False}, *json.loads(answers[newer])])
    answers["runner-images/ubuntu26/20261118.200/images/ubuntu/Ubuntu2604-Readme.md"] = "### Tools\n- Git 2.56.2\n"
    code, report = check("image", {**fixed, "GIT_LINUX_VERSION": "2.56.0"}, on_image("2.56.1"), "Linux", installer, fetch)
    expect("the image scope is RED when the manifest is behind both the image and its newest release",
           code == 1 and "MISMATCH git-linux 2.56.1" in report and "toolchain.env names 2.56.0" in report
           and "ubuntu26/20261118.200 carries 2.56.2" in report and "bump" in report)
    answers[newer] = releases
    code, report = check("image", {**fixed, "GIT_LINUX_VERSION": "2.56.0"}, on_image("2.56.1"), "Linux", installer,
                         unreachable)
    expect("the image scope is RED when the image's newest release cannot be read",
           code == 1 and "was not read (OSError: offline" in report)
    code, report = check("image", fixed, on_image("2.56.0.windows.4"), "Windows", installer, unreachable)
    expect("the image scope reads no release when the image's git is the manifest's",
           code == 0 and "OK       git-windows 2.56.0" in report)

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
