#!/usr/bin/env python3
"""THE BUILD'S ANALYZER IS BUILT ONCE PER BUILD, AND NOT AGAIN ON AN UNCHANGED TREE (kb/Work PB4950 round 10).

THE DEFECT. src/WiseOwl.Cobol.Analyzers is referenced as an analyzer by every src product project (src/Directory.Build.props).
The runtime builds its ReadyToRun images with a nested publish per platform (WiseOwl.Cobol.Runtime.csproj,
CobolRuntimeReadyToRunImages), and that publish adds global properties of its own (Configuration=Release, RuntimeIdentifier,
PublishDir, EnableSourceControlManagerQueries, IncludeSourceRevisionInInformationalVersion). MSBuild hands a referenced project
every global property of the build that references it, and offers no way to drop them all, only a named list
(GlobalPropertiesToRemove, RemoveProperties). So each nested publish built the analyzer AGAIN, as another configuration, into the
same obj/Release/netstandard2.0:
  * two builds wrote one assembly at once (CS2012 "used by another process", round 9);
  * the two configurations wrote different assembly attributes, so every Release build recompiled the analyzer, and no Release
    build on an unchanged tree was incremental (round 10).

THE SHAPE. One configuration builds the analyzer, whatever the referencing build passes:
  * every reference pins it to Release (SetConfiguration): a build tool has one configuration, and the caller's is not its;
  * a nested build of a src project builds no references (BuildProjectReferences=false in its properties): the outer build of
    the same project has already built them, and the nested build only re-compiles the project itself. The references are then
    resolved by GetTargetPath, which writes nothing. AnalyzerWiringDriftTests refuses a nested MSBuild call in src that omits it.

THE TEST stages a scratch copy of the runtime and the analyzer, with the repository files that configure their build, committed
in a scratch git repository as the checkout is. It builds the runtime in Release twice at detailed verbosity, and that build runs
the nested publishes and the analyzer reference together.
  1. The first build runs the analyzer's CoreCompile under exactly one configuration.
  2. The second build, on an unchanged tree, does not recompile the analyzer, and its assembly in obj is untouched.
With the round-9 shape (GlobalPropertiesToRemove, nested publishes building references) both arms were RED: CoreCompile ran in
two requests, and the second build compiled the analyzer in both. Cost: about 15 s on a warm host (two Release builds of the
runtime, the first with restore and two crossgen images)."""
import re
import shutil
import subprocess
import sys
from pathlib import Path

import selftest_cases
from scratch_repo import ScratchRepo, scratch_dir
from standard_streams import ensure_utf8
from test_population import scrubbed_env

REPO = Path(__file__).resolve().parents[4]
ANALYZERS = "WiseOwl.Cobol.Analyzers"
RUNTIME = "WiseOwl.Cobol.Runtime"
OBJ_DLL = Path("obj") / "Release" / "netstandard2.0" / f"{ANALYZERS}.dll"
#: The start of the analyzer project's CoreCompile, and the request (node:request) that runs it.
CORE_COMPILE = re.compile(r'^\s*(\d+:\d+)>Target "CoreCompile" in file "[^"]*" from project "[^"]*' + re.escape(ANALYZERS)
                          + r'\.csproj"', re.MULTILINE)


def _stage():
    """The runtime and the analyzer, with the repository-root and src files that configure their build, committed in a scratch
    git repository as the checkout is. A Release build in a repository stamps the commit and the nested publishes do not
    (EnableSourceControlManagerQueries=false), so without one the analyzer's two configurations would write the same attributes
    and the rebuild this test exists to catch would not happen. Returns the runtime's project file; nothing is written into the
    repository."""
    out = scratch_dir("analyzer-once-")
    for name in ("Directory.Build.props", "Directory.Packages.props", "global.json", "nuget.config"):
        shutil.copy2(REPO / name, out / name)
    (out / "nuget-feed").mkdir()                       # nuget.config names this local source
    (out / "src").mkdir()
    for name in ("Directory.Build.props", "Directory.Build.targets"):
        shutil.copy2(REPO / "src" / name, out / "src" / name)
    for project in (ANALYZERS, RUNTIME):
        shutil.copytree(REPO / "src" / project, out / "src" / project, ignore=shutil.ignore_patterns("bin", "obj"))
    ScratchRepo.init(out).commit("the staged runtime and analyzer")
    return out / "src" / RUNTIME / f"{RUNTIME}.csproj"


def _build(project):
    """A Release build of the runtime, as the merge group's Release leg builds it, at detailed verbosity."""
    env = {**scrubbed_env(), "MSBUILDDISABLENODEREUSE": "1", "DOTNET_CLI_TELEMETRY_OPTOUT": "1", "DOTNET_NOLOGO": "1"}
    p = subprocess.run(["dotnet", "build", str(project), "-c", "Release", "--nologo", "-nodeReuse:false", "-v:detailed",
                        "-p:UseSharedCompilation=false"],
                       cwd=project.parent, env=env, capture_output=True, text=True, encoding="utf-8", errors="replace")
    return p.returncode, p.stdout + p.stderr


def _analyzer_compiles(log):
    """The requests that ran the analyzer's CoreCompile target (one per configuration that built it), and how many of them
    compiled (the target ran rather than being skipped as up to date)."""
    requests, compiled = [], 0
    for m in CORE_COMPILE.finditer(log):
        requests.append(m.group(1))
        target = log[m.end():m.end() + 2000].split("Done building target", 1)[0]
        if re.search(r'Building target "CoreCompile" (completely|partially)', target):
            compiled += 1
    return requests, compiled


def case_00_one_release_build_builds_the_analyzer_once_and_a_second_not_at_all(expect):
    project = _stage()
    rc, log = _build(project)
    expect("the runtime builds in Release from a scratch copy (with its nested ReadyToRun publishes)", rc == 0,
           "\n".join(log.splitlines()[-25:]))
    if rc != 0:
        return
    requests, _ = _analyzer_compiles(log)
    expect("one Release build runs the analyzer's CoreCompile under exactly ONE configuration, whatever its nested publishes "
           "pass", len(requests) == 1, f"CoreCompile ran in requests {requests}")
    dll = project.parents[1] / ANALYZERS / OBJ_DLL
    before = dll.stat().st_mtime_ns if dll.is_file() else None
    rc, log = _build(project)
    requests, compiled = _analyzer_compiles(log)
    after = dll.stat().st_mtime_ns if dll.is_file() else None
    expect("a second Release build on an unchanged tree does not recompile the analyzer (its assembly in obj is untouched)",
           rc == 0 and compiled == 0 and before is not None and before == after,
           f"rc={rc} compiled in {compiled} of requests {requests}; obj assembly {'unchanged' if before == after else 'rewritten'}")


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(selftest_cases.run(sys.modules[__name__]))
