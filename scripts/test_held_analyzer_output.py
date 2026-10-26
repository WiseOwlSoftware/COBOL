#!/usr/bin/env python3
# SELF-TEST-PLATFORM: windows — only Windows refuses to overwrite an image a live process has loaded; elsewhere the build never met the lock
"""test_held_analyzer_output.py — A BUILD SUCCEEDS WHILE A LONG-LIVED PROCESS HOLDS THE SOURCE GENERATOR (kb/Work PB3674).

    python scripts/test_held_analyzer_output.py

THE DEFECT. A C# language server (csharp-ls, the LSP tool of MANDATORY-PRACTICES P13) loads the analyzer assembly
`Cobol.Net.Compiler.SourceGen.dll` straight from the worktree's `bin\\` and keeps it loaded for the life of its session.
Windows refuses to OVERWRITE a loaded image, so every later build that had a fresh output to copy there (the landing's
ABI-pin settle, a finisher's gate) failed with MSB3027/MSB3021 "locked by", as if the branch were broken. The project's
`MoveAsideHeldOutput` target (src/Cobol.Net.Compiler.SourceGen/Cobol.Net.Compiler.SourceGen.csproj) now deletes the stale
output and, when it is held, renames it aside (a loaded image can be renamed), so the copy succeeds.

THE TEST builds a scratch copy of that project (the repository's own bin and obj are untouched), has a real .NET
process load the output the way the language server does (`Assembly.LoadFrom` in PowerShell 7), and rebuilds with a
fresh output while it is held:
  1. the premise: the held file cannot be opened for writing (so a plain copy WOULD fail);
  2. the rebuild succeeds, the new output is in place and the old one is kept aside as `<name>.<guid>.held`, still held;
  3. a rebuild with nothing new leaves bin untouched (incremental builds stay incremental);
  4. once the holder has gone, the next fresh output deletes the held copy and leaves none.
It runs the project's real targets, so a change to them that reintroduces the lock fails here."""
import pathlib
import shutil
import subprocess
import sys

HERE = pathlib.Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
from standard_streams import ensure_utf8  # noqa: E402
from scratch_repo import remove_tree, scratch_dir  # noqa: E402  (scripts/: the one scratch fixture)
from standard_streams import ensure_utf8  # noqa: E402  (scripts/: every entry point's UTF-8 streams, kb/Work PB3637)
from test_population import scrubbed_env  # noqa: E402  (scripts/: the gate-environment scrub, kb/Work PB1718)

REPO = HERE.parent
PROJECT = REPO / "src" / "Cobol.Net.Compiler.SourceGen" / "Cobol.Net.Compiler.SourceGen.csproj"
DLL = "Cobol.Net.Compiler.SourceGen.dll"
TFM = pathlib.Path("bin") / "Debug" / "netstandard2.0"
OBJ_TFM = pathlib.Path("obj") / "Debug" / "netstandard2.0"
failures: list[str] = []


def expect(label: str, ok: bool, detail: str = "") -> None:
    print(f"  {'PASS' if ok else 'FAIL'}  {label}" + (f"  ({detail})" if detail and not ok else ""))
    if not ok:
        failures.append(label)


def stage(out: pathlib.Path) -> pathlib.Path:
    """A scratch copy of the generator project with the repository-root files that configure its build (the SDK pin,
    the central package versions, the package sources and the shared properties), so the build runs the project's own
    targets and writes nothing into the repository. Returns the copied project file."""
    for name in ("Directory.Build.props", "Directory.Packages.props", "global.json", "nuget.config"):
        shutil.copy2(REPO / name, out / name)
    (out / "nuget-feed").mkdir()                       # nuget.config names this local source
    project = out / "src" / PROJECT.parent.name
    project.mkdir(parents=True)
    for source in [PROJECT, *PROJECT.parent.glob("*.cs")]:
        shutil.copy2(source, project / source.name)
    return project / PROJECT.name


def build(project: pathlib.Path) -> tuple[int, str]:
    """Build the staged generator project as a gate or the landing's settle would."""
    env = {**scrubbed_env(), "MSBUILDDISABLENODEREUSE": "1", "DOTNET_CLI_TELEMETRY_OPTOUT": "1", "DOTNET_NOLOGO": "1"}
    p = subprocess.run(["dotnet", "build", str(project), "-c", "Debug", "--nologo", "-nodeReuse:false",
                        "-p:UseSharedCompilation=false"],
                       cwd=project.parent, env=env, capture_output=True, text=True, encoding="utf-8", errors="replace")
    return p.returncode, chr(10).join((p.stdout + p.stderr).splitlines()[-25:])


def listing(folder: pathlib.Path) -> dict[str, int]:
    return {p.name: p.stat().st_mtime_ns for p in sorted(folder.iterdir())}


def main() -> int:
    pwsh = shutil.which("pwsh")
    if pwsh is None:
        print("  FAIL  PowerShell 7 (pwsh) is needed to load the assembly as a language server does")
        return 1
    out = scratch_dir("held-analyzer-")
    holder = None
    try:
        project = stage(out)
        rc, tail = build(project)
        bin_dir, obj_dll = project.parent / TFM, project.parent / OBJ_TFM / DLL
        expect("the generator project builds from a scratch copy", rc == 0 and (bin_dir / DLL).is_file(), tail)
        if failures:
            return 1
        holder = subprocess.Popen(
            [pwsh, "-NoProfile", "-Command",
             f"[System.Reflection.Assembly]::LoadFrom('{bin_dir / DLL}') | Out-Null; 'LOADED'; Start-Sleep -Seconds 3600"],
            stdin=subprocess.DEVNULL, stdout=subprocess.PIPE, text=True)
        loaded = holder.stdout.readline().strip()   # blocks until the assembly is loaded; '' when the holder died
        try:
            with open(bin_dir / DLL, "r+b"):
                locked = False
        except PermissionError:
            locked = True
        expect("premise: a process that loaded the output holds it (it cannot be opened for writing), so a plain copy of "
               "a fresh output would fail", loaded == "LOADED" and locked, f"holder said {loaded!r}, locked={locked}")
        if failures:
            return 1

        obj_dll.unlink()                                    # a fresh output: CoreCompile runs, the copy has work to do
        rc, tail = build(project)
        held = sorted(p.name for p in bin_dir.glob(DLL + ".*.held"))
        expect("a rebuild with a fresh output succeeds while the old output is held, writes the new one beside the "
               "renamed old one, and the holder still holds its file",
               rc == 0 and len(held) == 1 and (bin_dir / DLL).stat().st_mtime_ns == obj_dll.stat().st_mtime_ns
               and holder.poll() is None, f"rc={rc} held={held}{chr(10)}{tail}")

        before = listing(bin_dir)
        rc, tail = build(project)
        expect("a rebuild with nothing new leaves bin as it was: no move, no new held copy (incremental builds stay "
               "incremental)", rc == 0 and listing(bin_dir) == before, f"rc={rc}{chr(10)}{tail}")

        holder.kill()
        holder.wait()
        holder = None
        obj_dll.unlink()
        rc, tail = build(project)
        expect("once the holder is gone the next fresh output deletes the held copy and leaves none",
               rc == 0 and not list(bin_dir.glob(DLL + ".*.held"))
               and (bin_dir / DLL).stat().st_mtime_ns == obj_dll.stat().st_mtime_ns, f"rc={rc}{chr(10)}{tail}")
    finally:
        if holder is not None:
            holder.kill()
            holder.wait()
        remove_tree(out)
    print(f"=== HELD ANALYZER OUTPUT TEST: {'RED' if failures else 'GREEN'} ({len(failures)} failure(s)) ===")
    return 1 if failures else 0


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main())
