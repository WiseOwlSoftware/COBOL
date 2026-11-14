#!/usr/bin/env python3
"""The planted worlds of the lanes' tests (DESIGN-orchestrator-loop.md section 16.2, kb/Work PB5397): `world(*steps)` is a scratch
repository holding the base commit and then each named step, in the order the old self-test made them, so a case asks for the
repository as it stood at the point it checks; `refused` is whether a constructor refuses its arguments."""
from collections.abc import Callable

from scratch_repo import ScratchRepo, scratch_dir


def refused(make: Callable[[], object]) -> bool:
    try:
        make()
    except ValueError:
        return True
    return False

def _base(repo):
    repo.commit("base", {rel: rel + "\n" for rel in ("src/a.cs", "DEVLOG/0001-x.md", "kb/Work/PB1.md", "kb/Spec/s.md")})


def _rename(repo):
    repo.git("mv", "src/a.cs", "DEVLOG/a.cs")
    repo.commit("rename")


def _tooling(repo):
    # The static twin of the tooling lane's watch (`code_readers`), on a planted tree: a csproj Exec of a script through
    # a property, a compiler test naming a script and a directory through TestRepo (and one through a computed name,
    # which only the watch can see), and a script whose self-test needs a build.
    repo.commit("tooling", {
        "src/P/P.csproj": "<Project><PropertyGroup><Gen>Gen.ps1</Gen></PropertyGroup><Target Name=\"G\">"
                          "<Exec Command=\"pwsh -File &quot;$(MSBuildProjectDirectory)/../../scripts/$(Gen)&quot;\" />"
                          "</Target></Project>\n",
        "scripts/Gen.ps1": "x\n", "scripts/a.py": "x\n", "scripts/spec/b.py": "x\n", "scripts/free.py": "x\n",
        "scripts/built.py": "#!/usr/bin/env python3\n# SELF-TEST-NEEDS: build — reads the built assemblies\n"
                            "import argparse\nargparse.ArgumentParser().add_argument('--self-"
                            "test')\n",
        "tests/T.cs": "class T { string a = File.ReadAllText(TestRepo.Scripts(\"a.py\")); string d = TestRepo.Scripts(\"spec\");\n"
                      "  // TestRepo.Scripts(\"free.py\") in a comment reads nothing\n"
                      "  string e = TestRepo.Scripts(name); }\n"})


def _listings(repo):
    # A listing by a literal pattern reads what it matches (PB4395 review R1-1: CorpusManifestTests lists the top-level
    # scripts/guard*.sh): never a sibling the pattern misses, never a subdirectory without AllDirectories.
    repo.commit("listings", {
        "scripts/guard-a.sh": "x\n", "scripts/Guard-B.sh": "x\n", "scripts/other.sh": "x\n", "scripts/sub/guard-c.sh": "x\n",
        "scripts/deep/x.py": "x\n", "scripts/deep/y/z.py": "x\n", "scripts/deep/y/z.sh": "x\n",
        "tests/L.cs": "class L { void M() {\n"
                      "  foreach (var f in Directory.EnumerateFiles(TestRepo.Scripts(), \"guard*.sh\").Order()) { }\n"
                      "  var d = Directory.GetFiles(TestRepo.Scripts(\"deep\"), \"*.py\", SearchOption.AllDirectories);\n"
                      "  var t = Directory.EnumerateFiles(TestRepo.Scripts(\"deep\"), \"?.sh\", SearchOption.TopDirectoryOnly); } }\n"})


def _imports(repo):
    # THE IMPORT CLOSURE (kb/Work PB4395 review R2-1): a script a compiler test runs is read with every module it
    # imports, through the sys.path edits of the chain. A harness outside the tooling trees appends scripts/ and imports
    # a module there; a script inserts its lib/ and imports a module there, which imports a package's module lazily.
    repo.commit("imports", {
        "suite/h.py": "import os, sys\nsys.path.append(os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', "
                      "'scripts'))\nfrom streams import ensure\n",
        "scripts/streams.py": "X = 1\n",
        "scripts/run.py": "import sys\nfrom pathlib import Path\nHERE = Path(__file__).resolve().parent\n"
                          "sys.path.insert(0, str(HERE / 'lib'))\nimport helper\n",
        "scripts/lib/helper.py": "def f():\n    from pkg import mod\n",
        "scripts/lib/pkg/__init__.py": "", "scripts/lib/pkg/mod.py": "Y = 2\n", "scripts/lib/unused.py": "Z = 3\n",
        "scripts/dir/x.py": "import sys\nfrom pathlib import Path\n"
                            "sys.path.insert(0, str(Path(__file__).resolve().parents[1] / 'lib'))\nimport unused\n",
        "tests/I.cs": "class I { string h = TestRepo.At(\"suite\", \"h.py\"); string r = TestRepo.Scripts(\"run.py\");\n"
                      "  string d = TestRepo.Scripts(\"dir\"); }\n"})


def _case_files(repo):
    # A CASE FILE'S CLOSURE (kb/Work PB5428, DESIGN-orchestrator-loop.md section 16.2): `selftest_cases.subject(__file__)`
    # is an import of the case file's subject, and `import selftest_cases` resolves against scripts/, as the runner's
    # PYTHONPATH makes it; the subject's own sibling import is read through it.
    repo.commit("case files", {
        "scripts/selftest_cases.py": "X = 1\n",
        "scripts/lib/tool.py": "import sibling\n", "scripts/lib/sibling.py": "Y = 1\n", "scripts/lib/other.py": "Z = 1\n",
        "scripts/selftests/lib/tool/test_w.py": "import selftest_cases\nsubject = selftest_cases.subject(__file__)\n",
        "tests/J.cs": "class J { string c = TestRepo.Scripts(\"selftests/lib/tool/test_w.py\"); }\n"})


_STEPS = {'rename': _rename, 'tooling': _tooling, 'listings': _listings, 'imports': _imports, 'case_files': _case_files}


def world(*steps: str):
    """(the repository root, the repository, the base commit) after the base commit and `steps`."""
    root = scratch_dir("inert-lane-")
    repo = ScratchRepo.init(root)
    _base(repo)
    base = repo.git("rev-parse", "HEAD")
    for step in steps:
        _STEPS[step](repo)
    return root, repo, base
