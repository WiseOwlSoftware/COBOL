#!/usr/bin/env python3
"""The scratch-repository world of the lanes' classification (DESIGN-orchestrator-loop.md section 16.2, kb/Work PB5397; kb/Work PB4395, PB5428): the changed paths of a rename, the directories the watch covers, the `check` of a classification against a tracked tree, and `code_readers`, the static twin of the tooling lane's watch, on planted trees: a csproj Exec, compiler tests naming scripts and directories through TestRepo, literal listings, import closures and a case file's closure. Each case builds the repository as it stood at that point of the old self-test, from `fixtures.world`."""
import sys

import fixtures
import lane_classification
import source_reads
import selftest_cases
from standard_streams import ensure_utf8

il = selftest_cases.subject(__file__)   # scripts/inert_lane.py
Classification = lane_classification.Classification
changed = lane_classification.changed
code_readers = source_reads.code_readers
tracked = lane_classification.tracked

# The mechanics, on a planted classification with a READ entry.
c = Classification(inert={"DEVLOG/": "", "kb/": "", "tools/pin": ""}, read={"kb/Work/": ""})
tc2 = Classification(inert={"DEVLOG/": "", "kb/": ""}, tooling_under=("scripts/",))


def case_00_code_file_renamed_into_inert_directory_shows(expect):
    root, repo, base = fixtures.world('rename')
    expect.equal("a code file renamed INTO an inert directory shows both names, so the change is full",
                 (sorted(changed(root, base)), c.classify(changed(root, base)).name),
                 (["DEVLOG/a.cs", "src/a.cs"], "full"))


def case_01_watch_covers_directory_every_inert_tracked_file(expect):
    root, repo, base = fixtures.world('rename')
    expect.equal("the watch covers the directory of every inert tracked file, none above them, the READ ones excluded",
                 c.watch_dirs(root), ["DEVLOG", "kb/Spec"])


def case_02_check_entry_naming_no_tracked_path_stale(expect):
    root, repo, base = fixtures.world('rename')
    expect.equal("check: an entry naming no tracked path is stale, and so is a READ entry under no INERT one",
                 len(il.check(Classification(inert={"DEVLOG/": "", "gone/": ""}, read={"elsewhere/": ""}), root)), 3)


def case_03_code_readers_build_s_exec_property_resolved(expect):
    root, repo, base = fixtures.world('rename', 'tooling')
    expect.equal("code_readers: the build's Exec (its property resolved), a test's literal TestRepo names (a directory is every "
                 "path below it), the build-needing self-test; never a comment or a computed name",
                 sorted(p for p, _ in code_readers(root, tracked(root), tc2.tooling_under)),
                 ["scripts/Gen.ps1", "scripts/a.py", "scripts/built.py", "scripts/spec/b.py"])


def case_04_check_each_tooling_path_build_or_compiler(expect):
    root, repo, base = fixtures.world('rename', 'tooling')
    expect.equal("check: each tooling path the build or a compiler test reads must lie under a CODE_READS entry; with them "
                 "it is clean, and a CODE_READS entry outside the tooling trees is refused",
                 (len(il.check(tc2, root)),
                  il.check(Classification(inert={"DEVLOG/": "", "kb/": ""}, tooling_under=("scripts/",), code_reads={
                      "scripts/Gen.ps1": "", "scripts/a.py": "", "scripts/built.py": "", "scripts/spec/": "",
                      "src/P/": ""}), root)),
                 (4, ["the CODE_READS entry 'src/P/' lies in no tooling tree (TOOLING_UNDER), so it changes no lane: remove it"]))


def case_05_code_readers_literal_listing_reads_files_pattern(expect):
    root, repo, base = fixtures.world('rename', 'tooling', 'listings')
    expect.equal("code_readers: a literal listing reads the files its pattern matches in that directory, or below it with "
                 "AllDirectories, and a one-directory listing its pattern too (a file added later); the directory's other "
                 "files and subdirectories are not read",
                 sorted(p for p, r in code_readers(root, tracked(root), tc2.tooling_under) if r == "tests/L.cs"),
                 ["scripts/Guard-B.sh", "scripts/deep/?.sh", "scripts/deep/x.py", "scripts/deep/y/z.py", "scripts/guard*.sh",
                  "scripts/guard-a.sh"])


def case_06_code_readers_import_closure_every_script_test(expect):
    root, repo, base = fixtures.world('rename', 'tooling', 'listings', 'imports')
    imported = sorted(p for p, r in code_readers(root, tracked(root), tc2.tooling_under) if r.startswith("tests/I.cs"))
    expect.equal("code_readers: the import closure of every script a test names reads each module it imports, through the "
                 "harness's appended scripts/, a script's inserted lib/, a lazy import and a package; a module nobody imports "
                 "and the imports of a script under a directory the test only names are not read",
                 imported, ["scripts/dir/x.py", "scripts/lib/helper.py", "scripts/lib/pkg/__init__.py",
                            "scripts/lib/pkg/mod.py", "scripts/run.py", "scripts/streams.py"])


def case_07_check_imported_module_required_be_code_reads(expect):
    root, repo, base = fixtures.world('rename', 'tooling', 'listings', 'imports')
    expect.equal("check: an imported module is required to be CODE_READS, naming the importer",
                 [b for b in il.check(tc2, root) if "reads scripts/streams.py" in b and "through suite/h.py" in b] != [], True)


def case_08_code_readers_case_file_under_selftests_tree(expect):
    root, repo, base = fixtures.world('rename', 'tooling', 'listings', 'imports', 'case_files')
    expect.equal("code_readers: a case file under a selftests/ tree reads its subject and everything the subject imports, and "
                 "the collector it imports from scripts/; a module of the subject's directory nobody imports is not read",
                 sorted(p for p, r in code_readers(root, tracked(root), tc2.tooling_under) if r.startswith("tests/J.cs")),
                 ["scripts/lib/sibling.py", "scripts/lib/tool.py", "scripts/selftest_cases.py",
                  "scripts/selftests/lib/tool/test_w.py"])


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(selftest_cases.run(sys.modules[__name__]))
