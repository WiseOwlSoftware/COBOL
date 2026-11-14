#!/usr/bin/env python3
"""rendered_views.py — THE REGISTRY OF RENDERED VIEWS: every generated index the tree does NOT hold (kb/Work PB3026).

    python scripts/rendered_views.py                    # render every light view to its path (gitignored, or outside the tree)
    python scripts/rendered_views.py drift-rules        # render the named view(s) only (the only way to render a heavy one)
    python scripts/rendered_views.py --check            # CI's `audits` job and the gate's audits: every view renders,
                                                        #   none is tracked, every in-tree one is gitignored
    python scripts/rendered_views.py --list             # name · output · generator
    python scripts/rendered_views.py --self-test

⛔ THE RULE (DESIGN-test-build-ci.md §3.17.4; owner decisions 2026-10-11 via kb/Work PB3015): THE TREE HOLDS SOURCES,
VIEWS ARE RENDERED. A committed file that every landing touching its inputs REGENERATES is a guaranteed collision once
landings are queued pull requests: two PRs that each add a drift test both rewrite `docs/DRIFT_RULES.md`'s count line,
and the second is bounced. A generated file nobody regenerates is the opposite failure: the probe printed
"kb/Conformance burn-down is STALE" for days. A view that is rendered on demand can be neither — there is nothing to
commit and nothing to go stale — so each is gitignored and its generator renders it when asked.

What is a VIEW here, and what is not: a view is DERIVED from tracked sources by a script that needs nothing outside the
tree (or the coordination directory), so it can be rendered on demand — the drift-rule index, the conformance
burn-down, the diagnostics table, the ledger page. These stay TRACKED, on purpose, and are not views: a SOURCE file
the build compiles (`*.g.cs`, `*.Table.cs`: a clone must build with `dotnet build` and no pre-step; each is pinned by
its own drift test), DATA extracted from the licensed standard (`spec-rule-catalog.json`, the reserved-word tables:
the generator needs the private PDF, which CI does not have). The register's own views are `work.py` subcommands,
not files (kb/Work PB3799).

ONE REGISTRY, ONE RULE (rule 4: no hand list per consumer). `VIEWS` below is the only place a view is named. The
gate's audits and CI's `audits` job call `--check`, which enforces, per view (no test calls it: rule (c) reads the head
of every tracked `kb/` note, and a test that did would make the vault a test input, kb/Work PB2878):
  (a) NOT TRACKED — `git ls-files` finds nothing at its output (the rule DESIGN-test-build-ci.md §3.17.4 names: tracked ∩ outputs = ∅);
  (b) GITIGNORED when its output is inside the tree — otherwise a render leaves the checkout dirty, and a dirty main
      checkout sends the orchestrator loop to `resume`;
  (c) NOTHING UNREGISTERED — no tracked `docs/**.md` or `kb/**.md` declares itself generated in its first lines (the sweep
      for the next case: a new generated index added by copying the old habit is refused until it has a row);
  (d) RENDERS — the generator's own `--check` renders it in memory and exits non-zero when its sources no longer
      produce a view (a drift test deleted its `<summary>`, an inventory row lost a field, the plan cannot be read);
  (e) NOT EDITED — an in-tree FILE view carries a first-line STAMP (what renders it, from what, the digest of its body,
      the remedy), and a file at its path with no stamp or with a body that is not the stamp's is a hand edit a
      gitignored file would otherwise swallow; a render refuses to overwrite it (kb/Work PB5430). The stamp is written
      by ONE writer, `write_view` (a generator's `--out` reaches it through `write_rendered`, the diagnostics script
      through `--write`), so a generator run directly stamps its view as `render_all` does. A view merely STALE
      (its sources moved on, its body untouched) is no finding: nothing is lost, and the next render replaces it.
A view rendered by a C# test (it reads the compiled catalogue) names that test instead, and the test is its (d).

ABSENT IS NEVER STALE: a view is not compared with a copy on disk, because nothing is promised about a copy. The
session probe renders the burn-down itself, so the copy a session reads is the one it just made.
"""
from __future__ import annotations

import argparse
import dataclasses
import hashlib
import pathlib
import re
import subprocess
import sys
import tempfile
from standard_streams import ensure_utf8

ROOT = pathlib.Path(__file__).resolve().parents[1]


@dataclasses.dataclass(frozen=True)
class View:
    """One rendered view.

    ``out`` is the repo-relative output (a file, or a directory when it ends in ``/``), or ``None`` when the generator
    renders OUTSIDE the tree on its own (the ledger lives in the coordination directory). ``render`` is the command
    that writes it (``{out}`` is replaced by the path); ``check`` is the generator's own render-without-writing check,
    or ``None`` when a test is the check (``checked_by`` names it). ``heavy`` marks a render that needs a build or an
    external tool (the diagnostics table is rendered by a C# test; the grammar diagrams need java): a bare
    ``rendered_views.py`` skips those, and naming one renders it."""
    name: str
    out: str | None
    render: tuple[str, ...]
    check: tuple[str, ...] | None
    checked_by: str = ""
    heavy: bool = False
    #: What the view is rendered FROM, as its stamp names it (an in-tree FILE view's stamp tells the reader where to edit).
    sources: str = ""

    @property
    def stamped(self) -> bool:
        """An in-tree FILE view: the one kind a person can open and edit in place, so the kind that carries a stamp."""
        return self.out is not None and not self.out.endswith("/")

    @property
    def generator(self) -> str:
        """The script that renders it (`scripts/spec/drift_rules.py`, `scripts/gen-diagnostics-doc.ps1`)."""
        return next((a for a in self.render if a.endswith((".py", ".ps1"))), pathlib.Path(self.render[0]).name)


def _py(script: str, *args: str) -> tuple[str, ...]:
    return (sys.executable, script, *args)


#: ⛔ THE ONE REGISTRY. A new generated index is ONE row here, a `.gitignore` entry for its output, and a `--check`
#: on its generator; `--check` then holds it to the rule with nothing else to remember.
VIEWS: tuple[View, ...] = (
    View("drift-rules", "docs/DRIFT_RULES.md",
         _py("scripts/spec/drift_rules.py", "--out", "{out}"),
         _py("scripts/spec/drift_rules.py", "--check"), sources="the drift tests' <summary> blocks"),
    View("conformance", "docs/CONFORMANCE.md",
         _py("scripts/spec/conformance_doc.py", "--out", "{out}"),
         _py("scripts/spec/conformance_doc.py", "--check"), sources="docs/conformance/"),
    View("conformance-burn-down", "kb/Conformance/",
         _py("scripts/spec/gen_conformance_notes.py", "--out", "{out}"),
         _py("scripts/spec/gen_conformance_notes.py", "--check")),
    View("diagnostics", "docs/DIAGNOSTICS.md",
         ("pwsh", "-NoProfile", "-File", "scripts/gen-diagnostics-doc.ps1"),
         None, checked_by="DiagnosticRegistryDriftTests.DiagnosticsDoc_RendersTheWholeCatalogue", heavy=True,
         sources="DiagnosticCatalog"),
    View("vault-reference", "kb/Reference/",
         ("pwsh", "-NoProfile", "-File", "scripts/gen-vault-reference.ps1"),
         None, checked_by="VaultReferenceGeneratorDriftTests"),
    View("grammar-diagrams", "kb/Grammar/",
         ("pwsh", "-NoProfile", "-File", "scripts/gen-grammar-diagrams.ps1"),
         None, checked_by="GrammarDiagramGeneratorDriftTests", heavy=True),
    View("ledger", None,
         _py("scripts/spec/gen_ledger.py"),
         _py("scripts/spec/gen_ledger.py", "--check")),
)


def is_view_output(path: str) -> bool:
    """Whether ``path`` is a registered view's output (or lies under a directory view): a path a note may NAME and a
    reader may OPEN although the tree does not hold it, because it is rendered on demand (``work.py check`` asks)."""
    return any(v.out is not None and (path == v.out or (v.out.endswith("/") and path.startswith(v.out)))
               for v in VIEWS)


def _git(root: pathlib.Path, *args: str) -> subprocess.CompletedProcess:
    return subprocess.run(["git", *args], cwd=root, capture_output=True, text=True, encoding="utf-8")


def _ls_files(root: pathlib.Path, *args: str, sep: str | None = None) -> list[str]:
    """``git ls-files``, or an error: a failed listing is not an empty one (a rule that finds nothing tracked because git
    could not run would pass for the wrong reason)."""
    r = _git(root, "ls-files", *args)
    if r.returncode != 0:
        raise RuntimeError(f"git ls-files {' '.join(args)} failed in {root}: {r.stderr.strip()}")
    return r.stdout.split(sep)


def tree_problems(views, root: pathlib.Path = ROOT) -> list[str]:
    """Rules (a) and (b), on the tree at ``root`` — read from git, not from the file system, so a tracked output is
    found whether or not it exists in the working tree."""
    problems = []
    for v in views:
        if v.out is None:
            continue
        tracked = _ls_files(root, "--", v.out)
        if tracked:
            shown = ", ".join(tracked[:3]) + (f" … ({len(tracked)} files)" if len(tracked) > 3 else "")
            problems.append(f"{v.name}: its output is TRACKED ({shown}) — a view is rendered, never committed: "
                            f"`git rm --cached` it")
        probe = v.out + "x.md" if v.out.endswith("/") else v.out
        if _git(root, "check-ignore", "-q", "--", probe).returncode != 0:
            problems.append(f"{v.name}: {v.out} is not gitignored — rendering it would dirty the checkout; add it "
                            f"to .gitignore")
    return problems


#: A tracked doc or note that calls itself generated and not for editing, in its first lines: "**Generated** from … — do not
#: edit by hand", "GENERATED by … — do not edit by hand", "Generated — do not hand-edit".
GENERATED_MARKER = re.compile(r"(?i)\b(generated|rendered view)\b.*\b(do not|don't|never)\b.{0,24}\bedit")
MARKER_LINES = 12
SWEPT = ("docs", "kb")
#: Not content that could be a view: the work register's notes (they quote the phrase when they are ABOUT generated files)
#: and the frozen evidence records ("Recorded by …": a measurement, not an index).
NOT_VIEWS = ("kb/Work/", "docs/rearchitecture/evidence/")


def unregistered_problems(views, root: pathlib.Path = ROOT) -> list[str]:
    """Rule (c), the sweep for the NEXT case: a tracked doc under ``docs/`` or ``kb/`` that declares itself generated is
    a view nobody registered (it would collide in the merge queue and go stale exactly as the registered ones did), so a
    new generated index cannot be added by copying the old habit. The fix is a row in ``VIEWS``, never an exemption."""
    out = []
    listing = _ls_files(root, "-z", "--", *SWEPT, sep="\0")
    for rel in sorted(f for f in listing if f.endswith(".md") and not f.startswith(NOT_VIEWS)):
        try:
            with open(root / rel, encoding="utf-8", errors="replace") as fh:
                head = [next(fh, "") for _ in range(MARKER_LINES)]
        except OSError:
            continue
        hit = next((l.strip() for l in head if GENERATED_MARKER.search(l)), None)
        if hit:
            out.append(f"{rel}: a tracked file that declares itself generated ({hit[:90]!r}) — render it as a view: add a "
                       f"row to VIEWS in scripts/rendered_views.py, gitignore its output and `git rm --cached` it")
    return out


def render_problems(views, root: pathlib.Path = ROOT) -> list[str]:
    """Rule (d): each generator's own ``--check`` renders in memory and must exit 0."""
    problems = []
    for v in views:
        if v.check is None:
            continue
        r = subprocess.run(list(v.check), cwd=root, capture_output=True, text=True, encoding="utf-8")
        if r.returncode != 0:
            body = (r.stdout + r.stderr).strip().splitlines()
            problems.append(f"{v.name}: does not render ({' '.join(v.check[1:])} exit {r.returncode})"
                            + "".join(f"\n      {line}" for line in body[-8:]))
    return problems


#: The first line of every in-tree FILE view (kb/Work PB5430): what renders it, from what, the digest of the body under it,
#: and the remedy. A gitignored file swallows an edit with no `git status` line, so the stamp is how an edit is seen.
STAMP = re.compile(r"^<!-- GENERATED by (?P<generator>\S+) from (?P<sources>.+?); body sha256 (?P<digest>[0-9a-f]{64})\. "
                   r"Do not edit: edit the sources and run python scripts/rendered_views\.py (?P<name>\S+)\. -->$")


def stamp_line(v: View, body: str) -> str:
    digest = hashlib.sha256(body.encode("utf-8")).hexdigest()
    return (f"<!-- GENERATED by {v.generator} from {v.sources or 'its generator'}; body sha256 {digest}. Do not edit: edit "
            f"the sources and run python scripts/rendered_views.py {v.name}. -->")


def edited(v: View, root: pathlib.Path = ROOT) -> str | None:
    """Rule (e), NOT EDITED: why the file at ``v``'s path is not an untouched render — no stamp, or a body whose digest is
    not the stamp's (a hand edit) — or None (absent, or untouched: a STALE untouched view is no finding, the next render
    replaces it and nothing is lost)."""
    path = root / v.out if v.stamped else None
    if path is None or not path.is_file():
        return None
    first, _, body = path.read_text(encoding="utf-8", errors="replace").partition("\n")
    found = STAMP.match(first)
    if found is None:
        return (f"{v.name}: {v.out} carries no stamp, so it is no render of {v.sources or v.generator} — carry what it "
                f"holds into the sources, delete it, and run python scripts/rendered_views.py {v.name}")
    if hashlib.sha256(body.encode("utf-8")).hexdigest() != found["digest"]:
        return (f"{v.name}: {v.out} was EDITED after it was rendered (its body is not the one its stamp records) — carry "
                f"the change into {v.sources or 'its sources'}, delete it, and run python scripts/rendered_views.py {v.name}")
    return None


def edited_problems(views, root: pathlib.Path = ROOT) -> list[str]:
    return [p for v in views if (p := edited(v, root)) is not None]


def write_view(v: View, body: str, root: pathlib.Path = ROOT) -> str | None:
    """⛔ THE ONE WRITER of an in-tree FILE view (wave 20262031 review R1-2): the stamp and the body, written together, so a
    view is stamped however its generator was run (by `render_all`, or directly as its own docstring documents). Refuses,
    returning the reason and writing nothing, when the file there is a hand edit (rule (e)): a render would lose it."""
    refused = edited(v, root)
    if refused is not None:
        return refused
    path = root / v.out
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(stamp_line(v, body) + "\n" + body, encoding="utf-8", newline="\n")
    return None


def write_rendered(path: pathlib.Path, body: str, root: pathlib.Path = ROOT, views=VIEWS) -> str | None:
    """A generator's `--out <path>`: through `write_view` when ``path`` is a stamped view's output in ``root``, else the
    body as it is (the package's copy of the conformance statement under `obj/`, a scratch render). Returns `write_view`'s
    refusal, or None."""
    target = path.resolve()
    v = next((v for v in views if v.stamped and (root / v.out).resolve() == target), None)
    if v is not None:
        return write_view(v, body, root)
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(body, encoding="utf-8", newline="\n")
    return None


def render_all(views, root: pathlib.Path = ROOT) -> int:
    rc = 0
    for v in views:
        refused = edited(v, root)
        if refused is not None:   # refused before the generator runs: a heavy one builds for minutes before it would refuse
            print(f"⛔ {refused}", flush=True)
            rc = rc or 1
            continue
        cmd = [a.replace("{out}", v.out or "") for a in v.render]
        print(f"· {v.name}: {' '.join(cmd[1:] if cmd[0] == sys.executable else cmd)}", flush=True)
        r = subprocess.run(cmd, cwd=root)
        rc = rc or r.returncode
        if r.returncode == 0 and v.stamped and (not (root / v.out).is_file() or edited(v, root) is not None):
            print(f"⛔ {v.name}: {v.generator} exited 0 but left no stamped {v.out}: a generator writes its view through "
                  f"rendered_views.write_rendered (or `rendered_views.py --write {v.name} <file>`), never on its own", flush=True)
            rc = rc or 1
    return rc


def select(names: list[str]):
    by_name = {v.name: v for v in VIEWS}
    unknown = [n for n in names if n not in by_name]
    if unknown:
        sys.exit(f"unknown view(s): {', '.join(unknown)} — known: {', '.join(by_name)}")
    return [by_name[n] for n in names] or list(VIEWS)


def to_render(names: list[str]):
    """The named views, or every view that renders without a build or an external tool."""
    return select(names) if names else [v for v in VIEWS if not v.heavy]


def self_test() -> int:
    """The tree rules on a planted repository: a tracked output, an un-ignored output and a clean one; and the
    registry itself — unique names, every in-tree output reachable by a rule."""
    results: list[tuple[str, bool, str]] = []

    def check(name: str, ok: bool, detail: str = "") -> None:
        results.append((name, ok, detail))

    from scratch_repo import ScratchRepo  # noqa: PLC0415  (this directory's: the one scratch-repository fixture)
    with tempfile.TemporaryDirectory() as td:
        root = pathlib.Path(td)
        scratch = ScratchRepo.init(root)
        (root / "docs").mkdir()
        (root / "kb" / "Conformance").mkdir(parents=True)
        (root / ".gitignore").write_text("kb/Conformance/\ndocs/OK.md\n", encoding="utf-8", newline="\n")
        (root / "docs" / "TRACKED.md").write_text("x", encoding="utf-8", newline="\n")
        (root / "kb" / "Conformance" / "a.md").write_text("x", encoding="utf-8", newline="\n")
        scratch.git("add", "-f", "docs/TRACKED.md", "kb/Conformance/a.md")
        views = (View("ok", "docs/OK.md", ("x",), None),
                 View("tracked", "docs/TRACKED.md", ("x",), None),
                 View("dir-tracked", "kb/Conformance/", ("x",), None),
                 View("unignored", "docs/UNIGNORED.md", ("x",), None),
                 View("outside", None, ("x",), None))
        found = tree_problems(views, root)
        text = "\n".join(found)
        check("a clean view raises nothing", not any(p.startswith("ok:") for p in found), text)
        check("a tracked file output is named", any(p.startswith("tracked:") and "TRACKED" in p for p in found), text)
        check("a tracked file under a directory output is named",
              any(p.startswith("dir-tracked:") and "kb/Conformance/a.md" in p for p in found), text)
        check("an un-ignored output is named", any(p.startswith("unignored:") and "gitignore" in p for p in found), text)
        check("an output outside the tree is never an in-tree finding", not any(p.startswith("outside:") for p in found), text)

        (root / "kb" / "Work").mkdir()
        (root / "docs" / "GEN.md").write_text("# T\n\n> **Generated** from X - do not edit by hand.\n", encoding="utf-8", newline="\n")
        (root / "docs" / "HAND.md").write_text("# T\n\nAn index someone wrote; see the generated ones.\n", encoding="utf-8", newline="\n")
        (root / "kb" / "Work" / "PB1.md").write_text("Generated indexes - do not hand-edit them.\n", encoding="utf-8", newline="\n")
        scratch.git("add", "docs/GEN.md", "docs/HAND.md", "kb/Work/PB1.md")
        swept = unregistered_problems(views, root)
        text = "\n".join(swept)
        check("an unregistered tracked doc that declares itself generated is named",
              len(swept) == 1 and swept[0].startswith("docs/GEN.md:"), text)

        bad = (View("fails", None, ("x",), (sys.executable, "-c", "import sys; print('boom'); sys.exit(3)")),
               View("passes", None, ("x",), (sys.executable, "-c", "pass")),
               View("by-test", None, ("x",), None, checked_by="SomeTest"))
        found = render_problems(bad, root)
        check("a generator whose --check fails is named with its output",
              len(found) == 1 and found[0].startswith("fails:") and "boom" in found[0], "\n".join(found))

        # Rule (e) and the stamp (kb/Work PB5430): a render stamps the view, an untouched (or stale) view is GREEN, an
        # edited or unstamped one is named, and a render over it is refused and leaves it as it was.
        # The planted generator writes as a real one does, through write_view (it builds the planted row itself).
        writer = (f"import sys, pathlib; sys.path.insert(0, {str(pathlib.Path(__file__).resolve().parent)!r}); "
                  "import rendered_views as r; "
                  "sys.exit(1 if r.write_view(r.View('v', 'docs/V.md', ('gen.py',), None, sources='src/'), sys.argv[1], pathlib.Path.cwd()) else 0)")
        view = View("v", "docs/V.md", (sys.executable, "-c", writer, "rendered body\n"), None, sources="src/")
        stale = View("v", "docs/V.md", (sys.executable, "-c", writer, "a later body\n"), None, sources="src/")
        path = root / "docs" / "V.md"
        check("a render writes the stamp as the view's first line",
              render_all((view,), root) == 0 and STAMP.match(path.read_text(encoding="utf-8").split("\n")[0]) is not None,
              path.read_text(encoding="utf-8")[:200])
        check("an untouched view is no finding", edited_problems((view,), root) == [])
        check("a STALE untouched view is no finding either (its sources moved on; nothing is lost)",
              edited_problems((stale,), root) == [])
        path.write_text(path.read_text(encoding="utf-8").replace("rendered body", "hand edit"), encoding="utf-8", newline="\n")
        found = edited_problems((view,), root)
        check("an EDITED view is named, with the remedy", len(found) == 1 and "EDITED" in found[0] and "src/" in found[0],
              "\n".join(found))
        check("a render over an edited view is refused and keeps the edit",
              render_all((view,), root) == 1 and "hand edit" in path.read_text(encoding="utf-8"))
        path.write_text("an unstamped file\n", encoding="utf-8", newline="\n")
        found = edited_problems((view,), root)
        check("an UNSTAMPED file at a view's path is named", len(found) == 1 and "no stamp" in found[0], "\n".join(found))
        path.unlink()
        check("an absent view is never a finding", edited_problems((view,), root) == [])

        # A generator run DIRECTLY (`drift_rules.py --out docs/DRIFT_RULES.md`, as its docstring says) stamps its view too:
        # write_rendered at a view's path is write_view (review R1-2); at any other path it writes the body as it is.
        check("a direct render at a view's path is stamped and is no finding",
              write_rendered(root / "docs" / "V.md", "direct body\n", root, (view,)) is None
              and path.read_text(encoding="utf-8").startswith("<!-- GENERATED by ") and edited_problems((view,), root) == [],
              path.read_text(encoding="utf-8")[:200])
        elsewhere = root / "obj" / "V.md"
        check("a render to any other path is the body alone (the package's copy)",
              write_rendered(elsewhere, "plain body\n", root, (view,)) is None
              and elsewhere.read_text(encoding="utf-8") == "plain body\n")
        path.write_text("hand written\n", encoding="utf-8", newline="\n")
        check("a direct render over a hand edit is refused and keeps it",
              write_rendered(path, "direct body\n", root, (view,)) is not None
              and path.read_text(encoding="utf-8") == "hand written\n")
        path.unlink()
        rogue = View("v", "docs/V.md", (sys.executable, "-c",
                                        "import sys; open('docs/V.md', 'w', encoding='utf-8').write('unstamped\\n')"),
                     None, sources="src/")
        check("a generator that writes its view on its own fails the render (it would be RED at rule (e))",
              render_all((rogue,), root) == 1)
        path.unlink()

    names = [v.name for v in VIEWS]
    check("view names are unique", len(set(names)) == len(names), str(names))
    check("every view is checked by its generator or names the test that is", all(v.check or v.checked_by for v in VIEWS))

    failed = [r for r in results if not r[1]]
    for name, ok, detail in results:
        print(f"  {'ok  ' if ok else 'FAIL'} {name}" + (f"\n         {detail}" if detail and not ok else ""))
    print(f"rendered_views self-test: {len(results) - len(failed)}/{len(results)}")
    return 1 if failed else 0


def main(argv: list[str]) -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("names", nargs="*", help="views to render or check (default: all)")
    ap.add_argument("--check", action="store_true",
                    help="exit non-zero when a view is tracked, not gitignored, or no longer renders")
    ap.add_argument("--list", action="store_true", help="print each view: name, output, generator")
    ap.add_argument("--self-test", action="store_true")
    ap.add_argument("--write", metavar="FILE", type=pathlib.Path,
                    help="place FILE, a generator's raw render, as the ONE named view, stamped (a generator that cannot "
                         "import write_rendered: gen-diagnostics-doc.ps1)")
    a = ap.parse_args(argv)

    if a.self_test:
        return self_test()
    if a.write is not None:
        if len(a.names) != 1 or not select(a.names)[0].stamped:
            sys.exit("--write names exactly one in-tree file view")
        refused = write_view(select(a.names)[0], a.write.read_text(encoding="utf-8"))
        if refused is not None:
            print(f"⛔ {refused}")
            return 1
        return 0
    views = select(a.names)
    if a.list:
        for v in views:
            print(f"{v.name:24} {v.out or '(outside the tree: the generator\'s own default)':32} "
                  f"{' '.join(v.render[1:3]) if v.render[0] == sys.executable else v.render[-1]}")
        return 0
    if a.check:
        problems = tree_problems(views) + unregistered_problems(views) + render_problems(views) + edited_problems(views)
        if problems:
            print(f"⛔ {len(problems)} RENDERED-VIEW PROBLEM(S) (scripts/rendered_views.py; DESIGN-test-build-ci.md §3.17.4):")
            for p in problems:
                print(f"   {p}")
            return 1
        print(f"rendered views OK — {len(views)} views, none tracked, every in-tree one gitignored, nothing unregistered, "
              "every one renders, none edited after its render")
        return 0
    return render_all(to_render(a.names))


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main(sys.argv[1:]))
