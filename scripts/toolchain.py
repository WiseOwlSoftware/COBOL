#!/usr/bin/env python3
"""toolchain.py — THE READER OF THE ONE TOOLCHAIN MANIFEST, toolchain.env (kb/Work PB4007; DESIGN-test-build-ci.md §1.4).

    python scripts/toolchain.py versions                 # every tool and the version the manifest names
    python scripts/toolchain.py check --scope ci|gate|host  # does each tool on PATH report the manifest's version?
    python scripts/toolchain.py latest                   # is the manifest at each publisher's newest stable release?
    python scripts/toolchain.py bump                     # move the manifest (and its cloud copy) to those releases
    python scripts/toolchain.py --self-test

The owner, 2026-10-28: "CI tooling should match local tooling versions - always", then "CI and local should always be
using the latest version". `check` is the IDENTICAL half: CI's setup-toolchain action runs it with `--scope ci` after it
installs, the gate runs it with `--scope gate` before anything else and stops NOT RUN [toolchain] on a mismatch, and the
SessionStart readiness check prints its `--scope host` comparison. `latest` is the LATEST half: the scheduled workflow
toolchain-freshness.yml runs it every day, and a manifest behind a publisher is a red run naming the tool, never silence.

A tool's version is what the tool itself reports when it runs (`dotnet --version`, `java -version`, ...), never what an
installer or a package source claims: WSL's git stayed on 2.55 for days while its apt source existed but was disabled,
and apt said nothing (kb/Work PB4007).
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

#: The scopes `check` holds: what CI installs and runs, what the local gate runs, and everything a host carries.
SCOPES = ("ci", "gate", "host")


@dataclasses.dataclass(frozen=True)
class Tool:
    """One tool of the manifest: its name, its manifest key, how it reports its version, where it must match, and where
    its newest stable release is read."""
    name: str
    key: str
    command: tuple[str, ...] | None     # None: the interpreter running this script (the gate's own $PY)
    pattern: str                        # the version in the command's output (stdout and stderr together)
    scopes: frozenset[str]
    latest: Callable[[Callable[[str], str]], str]


def _github(repo: str, prefix: str = "v") -> Callable[[Callable[[str], str]], str]:
    """The newest release a GitHub repository publishes (`releases/latest` never answers a prerelease or a draft)."""
    def latest(fetch: Callable[[str], str]) -> str:
        return json.loads(fetch(f"https://api.github.com/repos/{repo}/releases/latest"))["tag_name"].removeprefix(prefix)
    return latest


def _dotnet_sdk(fetch: Callable[[str], str]) -> str:
    """The newest SDK of the newest GA channel (a channel in `go-live` or `preview` is a release candidate). A new major
    channel also needs global.json and every TargetFramework moved, so `bump` reports it rather than writes it."""
    index = json.loads(fetch("https://builds.dotnet.microsoft.com/dotnet/release-metadata/releases-index.json"))
    ga = [r for r in index["releases-index"] if r.get("support-phase") in ("active", "maintenance")]
    return max(ga, key=lambda r: _key(r["channel-version"]))["latest-sdk"]


def _python(fetch: Callable[[str], str]) -> str:
    """The newest stable CPython that actions/setup-python can install: its versions manifest, so a release CI cannot
    install yet is never the target."""
    rows = json.loads(fetch("https://raw.githubusercontent.com/actions/python-versions/main/versions-manifest.json"))
    return max((r["version"] for r in rows if r.get("stable") and EXACT.match(r["version"])), key=_key)


def _jdk(fetch: Callable[[str], str]) -> str:
    """The newest Oracle JDK: the newest feature release of Oracle's current releases, its `27.0.1.0.3` build number
    trimmed to the `27.0.1` its archive URL names. Oracle's other products (`8-perf`, the Java 8 Performance Pack) carry
    a feature name that is not a number, and are not the JDK."""
    items = json.loads(fetch("https://java.oraclecloud.com/currentJavaReleases"))["items"]
    jdks = [i["jdkDetails"] for i in items if i["jdkDetails"]["jdkVersion"].isdigit()]
    newest = {"jdkDetails": max(jdks, key=lambda d: int(d["jdkVersion"]))}
    return ".".join(newest["jdkDetails"]["latestReleaseVersion"].split(".")[:3])


def _git(fetch: Callable[[str], str]) -> str:
    """git's newest release: the highest `vX.Y.Z` tag of git/git (its release candidates carry `-rc`)."""
    tags = json.loads(fetch("https://api.github.com/repos/git/git/tags?per_page=100"))
    return max((t["name"][1:] for t in tags if re.fullmatch(r"v\d+\.\d+\.\d+", t["name"])), key=_key)


def _pypi(package: str) -> Callable[[Callable[[str], str]], str]:
    def latest(fetch: Callable[[str], str]) -> str:
        return json.loads(fetch(f"https://pypi.org/pypi/{package}/json"))["info"]["version"]
    return latest


CI, GATE, HOST = (frozenset(s) for s in (("ci", "gate", "host"), ("gate", "host"), ("host",)))
TOOLS: tuple[Tool, ...] = (
    Tool("dotnet-sdk", "DOTNET_SDK_VERSION", ("dotnet", "--version"), r"^(\d+\.\d+\.\d+)\s*$", CI, _dotnet_sdk),
    Tool("python", "PYTHON_VERSION", None, r"^(\d+\.\d+\.\d+)$", CI, _python),
    Tool("jdk", "JDK_VERSION", ("java", "-version"), r'version "(\d+(?:\.\d+)*)', CI, _jdk),
    Tool("pwsh", "PWSH_VERSION", ("pwsh", "--version"), r"PowerShell (\d+\.\d+\.\d+)", CI, _github("PowerShell/PowerShell")),
    # git's own release: Git for Windows reports `2.56.0.windows.3`, its packaging revision after git's version.
    Tool("git", "GIT_VERSION", ("git", "--version"), r"git version (\d+\.\d+\.\d+)", CI, _git),
    Tool("gh", "GH_VERSION", ("gh", "--version"), r"gh version (\d+\.\d+\.\d+)", CI, _github("cli/cli")),
    Tool("jq", "JQ_VERSION", ("jq", "--version"), r"jq-(\d+\.\d+(?:\.\d+)?)", CI, _github("jqlang/jq", "jq-")),
    Tool("uv", "UV_VERSION", ("uv", "--version"), r"uv (\d+\.\d+\.\d+)", HOST, _github("astral-sh/uv", "")),
    # semgrep publishes to PyPI first (its GitHub release can trail it by a patch).
    Tool("semgrep", "SEMGREP_VERSION", ("semgrep", "--version"), r"(\d+\.\d+\.\d+)\s*$", HOST, _pypi("semgrep")),
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
    missing = [k for k in (*(t.key for t in TOOLS), *OTHER_KEYS) if k not in values]
    if missing:
        raise ValueError(f"no {', '.join(missing)}")
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


def compare(manifest: dict[str, str], scope: str, run: Callable[[tuple[str, ...]], str | None] = _run
            ) -> tuple[list[str], list[str]]:
    """(the lines `check` prints, the mismatches): each tool of `scope`, the version it reports and the manifest's."""
    lines, bad = [], []
    for tool in (t for t in TOOLS if scope in t.scopes):
        want, got = manifest[tool.key], installed(tool, run)
        state = "OK" if got == want else ("ABSENT" if got is None else "MISMATCH")
        lines.append(f"  {state:8} {tool.name} {got or '(none)'}" + ("" if state == "OK" else f" — toolchain.env names {want}"))
        if state != "OK":
            bad.append(f"{tool.name} {got or 'absent'} (toolchain.env {want})")
    return lines, bad


def summary(manifest: dict[str, str], scope: str) -> str:
    """The toolchain a run measured under, as its verdict names it: `dotnet-sdk 10.0.403, python 3.15.2, …`."""
    return ", ".join(f"{t.name} {manifest[t.key]}" for t in TOOLS if scope in t.scopes)


def check(scope: str, manifest: dict[str, str] | None = None,
          run: Callable[[tuple[str, ...]], str | None] = _run) -> tuple[int, str]:
    """(exit code, report): 0 GREEN when every tool of `scope` reports the manifest's version, else 1 RED naming each."""
    manifest = manifest if manifest is not None else load()
    lines, bad = compare(manifest, scope, run)
    verdict = (f"{VERDICT}GREEN — {scope}: {summary(manifest, scope)} ===" if not bad else
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


def newest(manifest: dict[str, str], fetch: Callable[[str], str] = fetch_url
           ) -> list[tuple[Tool, str, str | None, str | None]]:
    """(tool, the manifest's version, the publisher's newest, the error that kept it from being read) for every tool."""
    rows = []
    for tool in TOOLS:
        try:
            rows.append((tool, manifest[tool.key], tool.latest(fetch), None))
        except Exception as e:  # noqa: BLE001 — an unreadable publisher is reported, never silent
            rows.append((tool, manifest[tool.key], None, f"{type(e).__name__}: {e}"))
    return rows


def latest_report(rows: list[tuple[Tool, str, str | None, str | None]]) -> tuple[int, str]:
    """(exit code, report): 0 when the manifest is at every publisher's newest release, 1 when it is behind on one, 2
    when a publisher could not be read (a freshness check that cannot see is not green)."""
    lines, behind, unread = [], [], []
    for tool, have, new, error in rows:
        if error is not None:
            unread.append(tool.name)
            lines.append(f"  UNREAD   {tool.name} {have} — {error}")
        elif _key(new) > _key(have):
            behind.append(f"{tool.name} {have} → {new}")
            lines.append(f"  BEHIND   {tool.name} {have} — the newest release is {new}")
        else:
            lines.append(f"  LATEST   {tool.name} {have}")
    if behind:
        verdict = (f"{VERDICT}BEHIND — {'; '.join(behind)}: run `python scripts/toolchain.py bump` in a change of its "
                   "own, update the hosts to it, and gate and land it ===")
    elif unread:
        verdict = f"{VERDICT}UNREAD — no newest release read for {', '.join(unread)} ==="
    else:
        verdict = f"{VERDICT}LATEST — every tool is at its newest release ==="
    return (1 if behind else 2 if unread else 0), "\n".join([*lines, verdict])


def bumped(text: str, rows: list[tuple[Tool, str, str | None, str | None]]) -> tuple[str, list[str]]:
    """`text` (the manifest, or setup-env.sh's copy) with every tool moved to its newer release, and what moved. A new
    major .NET channel is not written: it also moves global.json and every TargetFramework, an owner's decision."""
    moved = []
    for tool, have, new, error in rows:
        if error is not None or new is None or _key(new) <= _key(have):
            continue
        if tool.name == "dotnet-sdk" and _key(new)[0] != _key(have)[0]:
            moved.append(f"dotnet-sdk {new} NOT written: a new major channel moves global.json and every TargetFramework "
                         "(the owner's decision)")
            continue
        text = re.sub(rf"(?m)^{tool.key}={re.escape(have)}$", f"{tool.key}={new}", text)
        moved.append(f"{tool.name} {have} → {new}")
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
    manifest = load()
    expect("the live manifest parses, names every tool with an exact version, and its cloud copy is the same block",
           block(live) == block(COPIES[1].read_text(encoding="utf-8")))
    good = "\n".join([BEGIN, *(f"{t.key}=1.2.3" for t in TOOLS), *(f"{k}=x" for k in OTHER_KEYS), END]) + "\n"
    expect("a block naming every tool and key parses", parse(good)[TOOLS[0].key] == "1.2.3")
    for what, text in [("a missing block", "DOTNET_SDK_VERSION=1.2.3\n"),
                       ("a tool with no version", good.replace(f"{TOOLS[2].key}=1.2.3\n", "")),
                       ("a loose version (10.0.x)", good.replace(f"{TOOLS[0].key}=1.2.3", f"{TOOLS[0].key}=10.0.x")),
                       ("a name given twice", good.replace(END, f"{TOOLS[1].key}=1.2.4\n{END}"))]:
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
    reported = {t.name: installed(t, lambda c: outputs.get(c[0])) for t in TOOLS}
    expect("each tool's own report gives its version (git without Git for Windows' revision)",
           reported == {"dotnet-sdk": "10.0.403", "python": platform.python_version(), "jdk": "27.0.1", "pwsh": "7.6.9",
                        "git": "2.56.0", "gh": "2.103.3", "jq": "1.8.2", "uv": "0.13.6", "semgrep": "1.182.1"})
    fixed = {**{t.key: reported[t.name] for t in TOOLS}, **{k: "x" for k in OTHER_KEYS}}
    code, report = check("ci", fixed, lambda c: outputs.get(c[0]))
    expect("check GREEN when every tool reports the manifest's version, naming the toolchain",
           code == 0 and f"{VERDICT}GREEN — ci: dotnet-sdk 10.0.403, python" in report and "uv" not in report)
    code, report = check("gate", {**fixed, "JDK_VERSION": "27.0.2"}, lambda c: None if c[0] == "gh" else outputs.get(c[0]))
    expect("check RED naming a mismatched and an absent tool",
           code == 1 and "jdk 27.0.1 (toolchain.env 27.0.2)" in report and "gh absent (toolchain.env 2.103.3)" in report
           and "MISMATCH jdk 27.0.1" in report)
    expect("the scopes: CI and the gate hold seven tools, the host all nine",
           [len([t for t in TOOLS if s in t.scopes]) for s in SCOPES] == [7, 7, 9])

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
        "git/git/tags": json.dumps([{"name": "v2.57.0-rc0"}, {"name": "v2.56.0"}, {"name": "v2.9.5"}]),
        "PowerShell/PowerShell/releases": json.dumps({"tag_name": "v7.6.9"}),
        "cli/cli/releases": json.dumps({"tag_name": "v2.103.3"}),
        "jqlang/jq/releases": json.dumps({"tag_name": "jq-1.8.2"}),
        "astral-sh/uv/releases": json.dumps({"tag_name": "0.13.6"}),
        "pypi.org/pypi/semgrep": json.dumps({"info": {"version": "1.182.1"}}),
    }

    def fetch(url: str) -> str:
        return next(v for k, v in answers.items() if k in url)

    have = {**fixed, "JDK_VERSION": "27.0.1"}
    rows = newest(have, fetch)
    expect("each publisher's newest stable release is read (a release candidate, a preview channel and an alpha never)",
           {t.name: new for t, _, new, _ in rows} == {"dotnet-sdk": "10.0.404", "python": "3.15.2", "jdk": "27.0.1",
                                                      "pwsh": "7.6.9", "git": "2.56.0", "gh": "2.103.3", "jq": "1.8.2",
                                                      "uv": "0.13.6", "semgrep": "1.182.1"})
    code, report = latest_report(rows)
    expect("latest names the one tool behind and exits 1", code == 1 and "dotnet-sdk 10.0.403 → 10.0.404" in report
           and report.count("  BEHIND ") == 1)
    def offline(url: str) -> str:
        if "pypi" in url:
            raise OSError("offline")
        return fetch(url)

    at_newest = {**have, "DOTNET_SDK_VERSION": "10.0.404"}
    code, report = latest_report(newest(at_newest, offline))
    expect("an unreadable publisher is UNREAD, exit 2, never LATEST", code == 2 and "UNREAD   semgrep" in report)
    code, report = latest_report(newest(at_newest, fetch))
    expect("latest exits 0 at every newest release", code == 0 and "LATEST — every tool" in report)

    text = good.replace(f"{TOOLS[0].key}=1.2.3", "DOTNET_SDK_VERSION=10.0.403")
    new, moved = bumped(text, [(TOOLS[0], "10.0.403", "10.0.404", None), (TOOLS[1], "1.2.3", "1.2.3", None)])
    expect("bump moves a tool behind its release, and only that line",
           new == text.replace("DOTNET_SDK_VERSION=10.0.403", "DOTNET_SDK_VERSION=10.0.404")
           and moved == ["dotnet-sdk 10.0.403 → 10.0.404"])
    new, moved = bumped(text, [(TOOLS[0], "10.0.403", "11.0.100", None)])
    expect("bump never writes a new major .NET channel", new == text and "NOT written" in moved[0])
    with tempfile.TemporaryDirectory() as d:
        copy = Path(d) / "setup-env.sh"
        copy.write_text("#!/bin/bash\n" + text + "rest\n", encoding="utf-8", newline="\n")
        new, _ = bumped(copy.read_text(encoding="utf-8"), [(TOOLS[0], "10.0.403", "10.0.404", None)])
        expect("bump rewrites a copy carried inside another script", block(new) == block(bumped(text, [
            (TOOLS[0], "10.0.403", "10.0.404", None)])[0]) and new.endswith("rest\n"))

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
    args = ap.parse_args(argv)
    if args.self_test:
        return self_test()
    if args.cmd == "versions":
        for tool in TOOLS:
            print(f"{tool.name} {load()[tool.key]}")
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
