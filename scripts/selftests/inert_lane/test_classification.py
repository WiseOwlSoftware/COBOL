#!/usr/bin/env python3
"""The lanes' classification (DESIGN-orchestrator-loop.md section 16.2, kb/Work PB5397; kb/Work PB2878, PB4326, PB4395, PB4409, PB4453): inert, read, tooling and code paths, the lane of a change, the two flags, the proof of the flag, the watch's coverage of a path and of a listing, and the real classification's entries. Pure: planted classifications and paths, no file."""
import sys

import lane_classification
import selftest_cases
from standard_streams import ensure_utf8
from fixtures import refused

CLASSIFICATION = lane_classification.CLASSIFICATION
Classification = lane_classification.Classification
Lane = lane_classification.Lane
MARKDOWN_ENTRY = lane_classification.MARKDOWN_ENTRY
TEST_FREE_LANES = lane_classification.TEST_FREE_LANES
under = lane_classification.under

real = CLASSIFICATION
# The mechanics, on a planted classification with a READ entry.
c = Classification(inert={"DEVLOG/": "", "kb/": "", "tools/pin": ""}, read={"kb/Work/": ""})
tc = Classification(inert={"kb/": ""}, tooling_under=("scripts/",), tooling_suffixes=(".py",),
                    tooling_reads={"data/a.json": "x", "lib/": "y"})
# THE TOOLING LANE (kb/Work PB4395), on a planted classification: scripts/ and .github/ are tooling trees, one script
# and one workflow are CODE_READS, and a .py file elsewhere is tooling by its suffix but in a compiler tree.
tl = Classification(inert={"DEVLOG/": "", "kb/": ""}, tooling_under=("scripts/", ".github/", "toolchain.env"),
                    tooling_suffixes=(".py",), tooling_reads={"docs/CONFORMANCE.md": "x"},
                    code_reads={"scripts/gen.ps1": "the build", "toolchain.env": "every test", "scripts/spec/": "y"})


def case_00_classification_kb_work_pb4453_devlog_entry_note(expect):
    expect.equal("the classification (kb/Work PB4453): a DEVLOG entry, a note of the knowledge base (the register included), the "
                 "documentation tree, a brief, a skill, a README and every other Markdown file are inert, wherever they lie; code, "
                 "tests, the skills pin, a name that only starts like an inert directory, the ISO text (a READ entry) and the "
                 "data files a compiler test reads under docs/ are not",
                 [real.is_inert(p) for p in ("DEVLOG/2428-x.md", "kb/Work/PB1.md", "kb/Compiler/Pipeline.md", "docs/CONFORMANCE.md",
                                              ".claude/skills/x/SKILL.md", "CLAUDE.md", "src/WiseOwl.Cobol.Runtime/README.md",
                                              "docs/rearchitecture/evidence/x.json", "docs/manual/user-guide/files.md",
                                              "src/a.cs", "tests/b.cs", "tools/claude-skills", "DEVLOG", "DEVLOGX/a.cs", "kb",
                                              "kbx/a.cs", "specs/ISO_COBOL_2023.md", "docs/rearchitecture/spec-rule-catalog.json",  # spec_pdf: names-the-file
                                              "docs/rearchitecture/architecture-edges.json",
                                              "docs/rearchitecture/evidence/arch-census/a.json")],
                 [True] * 9 + [False] * 11)


def case_01_entry_pattern_over_file_name_at_any(expect):
    expect.equal("a `**/` entry is a pattern over a file NAME at any depth, case-insensitively, and never a longer name",
                 [under(p, MARKDOWN_ENTRY) for p in ("a.md", "d/e/f.md", "d/E.MD", "d/e.mdx", "d/e.md/x", "d/md", MARKDOWN_ENTRY)],
                 [True, True, True, False, False, False, True])


def case_02_path_under_inert_entry_inert_under_read(expect):
    expect.equal("a path under an INERT entry is inert; under a READ entry it is not, nor its directory",
                 [c.is_inert("DEVLOG/a.md"), c.is_inert("kb/Spec/x.md"), c.is_inert("kb/Workshop.md"),
                  c.is_inert("kb/Work/PB1.md"), c.is_inert("kb/Work/")], [True, True, True, False, False])


def case_03_exact_path_entry_covers_path_below_never(expect):
    expect.equal("an exact-path entry covers that path and below it, never a sibling with a longer name",
                 [c.is_inert("tools/pin"), c.is_inert("tools/pin/x"), c.is_inert("tools/pin-x")], [True, True, False])


def case_04_empty_change_full_lane(expect):
    expect.equal("an empty change is the full lane", c.classify([]).inert, False)


def case_05_all_inert_change_inert_lane(expect):
    expect.equal("an all-inert change is the inert lane", c.classify(["DEVLOG/pending-x.md", "kb/Spec/y.md"]).name, "inert")


def case_06_one_path_test_may_read_makes_change(expect):
    expect.equal("one path a test may read makes the change full", c.classify(["DEVLOG/a.md", "kb/Work/PB1.md"]).name, "full")


def case_07_tooling_scripts_workflows_tools_agent_definitions_two(expect):
    # The tooling flag (PB4326), on the real classification.
    expect.equal("tooling: scripts, workflows, tools, the agent definitions, the two root files the self-tests read, any "
                 "Python, PowerShell or shell file anywhere, and the data the self-tests read (TOOLING_READS: the test helper "
                 "TestRepo.cs, the grammar, the agent-fleet settings, the inventory) are tooling; code, tests, the documents (inert "
                 "since kb/Work PB4453, so no script self-test is bound to one) and a name that only starts like a tooling "
                 "directory or a read file are not",
                 [real.is_tooling(p) for p in ("scripts/gate_slot.py", ".github/workflows/build-and-test.yml", "tools/claude-skills",
                                               ".claude/agents/x.md", ".gitattributes", "toolchain.env", "conformance-suite/harness/x.py",
                                               "tests/x/run.sh", "tests/_shared/TestRepo.cs",
                                               "src/WiseOwl.Cobol.Frontend/Grammar/Core/Data.g4", ".agent-fleet.json",
                                               "tests/version-matrix/inventory/GR-4.4-1.json",
                                               "src/WiseOwl.Cobol.Runtime/RuntimeAbi.pin",
                                               "docs/CONFORMANCE.md", "docs/DESIGN.md", "docs/REARCHITECTURE_PLAN.md",
                                               "src/a.cs", "tests/b.cs", "docs/DOC_INDEX.md", "scriptsx/a.md", "toolchain.env.bak",
                                               "tests/_shared/TestRepoX.cs", ".agent-fleet.json.bak", "tests/_shared/Other.cs")],
                 [True] * 13 + [False] * 11)


def case_08_full_lane_change_touches_script_says_so(expect):
    expect.equal("a full-lane change that touches a SCRIPT says so, one that does not says so, and an inert one never does; "
                 "a compiler change to only a file a script self-test reads (TestRepo.cs) runs no script "
                 "self-test (owner 2026-11-01)",
                 [real.classify(["src/a.cs", "scripts/x.py"]).tooling, real.classify(["src/a.cs", "docs/x.md"]).tooling,
                  real.classify(["kb/x.py", "DEVLOG/a.md"]).tooling, real.classify(["kb/x.py", "DEVLOG/a.md"]).inert,
                  real.classify(["tests/_shared/TestRepo.cs"]).tooling, real.classify(["src/a.cs", "tests/b.cs"]).tooling,
                  real.classify(["src/a.cs", "docs/CONFORMANCE.md"]).tooling],
                 [True, False, False, True, False, False, False])


def case_09_proof_flag_what_self_test_read_paths(expect):
    expect.equal("the proof of the flag: of what a self-test read, the paths neither tooling, nor a TOOLING_READS entry, nor "
                 "inert are named, a directory read by git too; a directory listing and the whole tree are not judged",
                 tc.unclassified_reads(["scripts/a.py", "data/a.json", "lib/x/y", "kb/Work/PB1.md", "docs/z.md", "src/q.cs"],
                                       ["paths:lib", "paths:other", "content:kb/Work", "entries:docs", "content:", "paths:"]),
                 ["docs/z.md", "other/", "src/q.cs"])


def case_10_proof_flag_finds_nothing_what_self_test(expect):
    expect.equal("the proof of the flag finds nothing in what a self-test read when every path is classified",
                 tc.unclassified_reads(["scripts/a.py", "data/a.json"], ["paths:scripts", "content:lib"]), [])


def case_11_two_flags_owner_2026_11_01_kb(expect):
    expect.equal("THE TWO FLAGS (owner 2026-11-01, kb/Work PB4409; the documents inert since kb/Work PB4453): a compiler change to a "
                 "file a script self-test reads (TestRepo.cs) sets `reads_tooling` (CI's Windows self-tests) and never `tooling` "
                 "(the local gate's); a script sets both; plain code, a document beside code and an inert change set neither; a "
                 "document alone, however many a self-test reads, is the inert lane (the audits run, no test does)",
                 [(real.classify(p).name, real.classify(p).tooling, real.classify(p).reads_tooling) for p in
                  (["tests/_shared/TestRepo.cs"], ["src/a.cs", "scripts/x.py"], ["src/a.cs", "tests/b.cs"],
                   ["src/a.cs", "docs/CONFORMANCE.md"], ["docs/CONFORMANCE.md"], ["docs/REARCHITECTURE_PLAN.md", "README.md"],
                   ["DEVLOG/a.md"], ["specs/ISO_COBOL_2023.md"], ["docs/rearchitecture/spec-rule-catalog.json"])],  # spec_pdf: names-the-file
                 [("full", False, True), ("full", True, True), ("full", False, False), ("full", False, False), ("inert", False, False),
                  ("inert", False, False), ("inert", False, False), ("full", False, True), ("full", False, True)])


def case_12_empty_change_touches_tooling_what_self_test(expect):
    expect.equal("an empty change touches tooling and what a self-test reads: nothing proves it does not",
                 (real.classify([]).tooling, real.classify([]).reads_tooling), (True, True))


def case_13_lane_whose_tooling_set_without_reads_tooling(expect):
    expect.equal("a lane whose `tooling` is set without `reads_tooling`, an inert lane that reads tooling, and a tooling lane "
                 "without the flag are refused",
                 [refused(lambda: Lane("full", "x", tooling=True, reads_tooling=False)),
                  refused(lambda: Lane("inert", "x", tooling=False, reads_tooling=True)),
                  refused(lambda: Lane("tooling", "x", tooling=False, reads_tooling=True)),
                  refused(lambda: Lane("full", "x", tooling=False, reads_tooling=True))],
                 [True, True, True, False])


def case_14_change_tooling_no_compiler_test_reads_tooling(expect):
    expect.equal("a change to tooling no compiler test reads is the tooling lane, inert paths beside it included; the lane "
                 "runs the self-tests and no build",
                 [(lane.name, lane.tooling, lane.builds) for lane in
                  (tl.classify(["scripts/steer.ps1"]), tl.classify(["scripts/a.py", ".github/w.yml", "DEVLOG/x.md", "kb/W/P.md"]))],
                 [("tooling", True, False)] * 2)


def case_15_code_reads_path_directory_entry_s_path(expect):
    expect.equal("a CODE_READS path, a directory entry's path, a script file outside the tooling trees, a TOOLING_READS data "
                 "file and any code path each make the change the full lane; the tooling flag is set by a changed script only, "
                 "never by a TOOLING_READS data file (owner 2026-11-01: a compiler change runs no script self-tests)",
                 [(tl.classify(p).name, tl.classify(p).tooling) for p in
                  (["scripts/gen.ps1"], ["scripts/spec/cite.py", "scripts/a.py"], ["toolchain.env"],
                   ["conformance-suite/harness/run.py"], ["docs/CONFORMANCE.md"], ["scripts/a.py", "src/a.cs"])],
                 [("full", True)] * 4 + [("full", False), ("full", True)])


def case_16_exact_code_reads_entry_covers_path_never(expect):
    expect.equal("an exact CODE_READS entry covers that path, never a sibling with a longer name",
                 [tl.in_tooling_lane(p) for p in ("scripts/gen.ps1", "scripts/gen.ps1x", "scripts/specx/a.py")],
                 [False, True, True])


def case_17_watch_reports_inert_path_tooling_lane_path(expect):
    expect.equal("the watch reports an inert path and a tooling-lane path, never a CODE_READS one or code",
                 [tl.is_watched(p) for p in ("DEVLOG/a.md", "scripts/a.py", "scripts/gen.ps1", "scripts/spec/x.py",
                                             "src/a.cs", "docs/CONFORMANCE.md")],
                 [True, True, False, False, False, False])


def case_18_listing_directory_reported_when_every_name_can(expect):
    expect.equal("a LISTING (a directory) is reported when every name it can list is watched: an inert directory and a "
                 "tooling-lane one that holds no CODE_READS entry; never one that holds one (scripts/, which the import system "
                 "lists to resolve a CODE_READS module: kb/Work PB4395 review R2-1), a CODE_READS directory, or code",
                 [tl.is_watched(p) for p in ("DEVLOG/", "kb/W/", "scripts/orch/", "scripts/", "scripts/spec/", "scripts/spec/x/",
                                             "src/")],
                 [True, True, True, False, False, False, False])


def case_19_listing_pattern_entry_covers_every_path_matches(expect):
    expect.equal("a LISTING PATTERN entry covers every path it matches, a file added later and the pattern itself included, "
                 "case-insensitively within its one directory",
                 [under(p, "scripts/guard*.sh") for p in ("scripts/guard-a.sh", "scripts/guard-new.sh", "scripts/Guard-B.sh",
                                                           "scripts/guard*.sh", "scripts/sub/guard-a.sh", "scripts/guard-a.py",
                                                           "scripts/xguard.sh")],
                 [True, True, True, True, False, False, False])


def case_20_lane_one_three_tooling_lane_always_runs(expect):
    expect.equal("a lane is one of the three, and the tooling lane always runs the self-tests while the inert one never does",
                 [refused(lambda n=n, t=t: Lane(n, "", tooling=t, reads_tooling=t)) for n, t in
                  (("inert", True), ("tooling", False), ("code", True), ("full", False))], [True, True, True, False])


def case_21_lanes_whose_gate_runs_no_compiler_test(expect):
    expect.equal("the lanes whose gate runs no compiler test", sorted(TEST_FREE_LANES), ["inert", "tooling"])


def case_22_data_self_tests_known_read_tooling_reads(expect):
    expect.equal("the data the self-tests are known to read are TOOLING_READS entries (PB4326 R1-1, R2-1)",
                 [e for e in ("tests/_shared/TestRepo.cs", "src/WiseOwl.Cobol.Frontend/Grammar/", ".agent-fleet.json",
                              "tests/version-matrix/",
                              "src/WiseOwl.Cobol.Runtime/RuntimeAbi.pin") if e not in real.tooling_reads], [])


def case_23_real_classification_build_s_compiler_tests_tooling(expect):
    expect.equal("the real classification: the build's and the compiler tests' tooling is CODE_READS (.gitattributes, "
                 "toolchain.env, the generators under scripts/), while a tool no compiler test names is the tooling lane "
                 "(PB4395's acceptance: scripts/orchestrator/steer.ps1, which no CODE_READS entry may swallow again)",
                 [real.classify([p]).name for p in (".gitattributes", "toolchain.env", "scripts/gen-reserved-words.ps1",
                                                    "scripts/gen-grammar-diagrams.ps1", "scripts/standard_streams.py",
                                                    "scripts/guard-a-new-guard.sh", "tools/a-new-tool.py",
                                                    "scripts/orchestrator/steer.ps1")],
                 ["full", "full", "full", "full", "full", "full", "tooling", "tooling"])


def case_24_every_category_is_kept_in_key_order(expect):
    expect.equal("the real classification keeps every category sorted, so two branches that each declare a path merge by plain git "
                 "(kb/Work PB5398)", real.order_findings(), [])
    found = Classification(inert={"b/": "", "a/": ""}, read={"c/": "", "d/": ""}, tooling_reads={"z": "", "y": ""},
                           code_reads={"m": "", "n": ""}).order_findings()
    expect("an INERT and a TOOLING_READS category out of order are each a finding naming the key that sorts early; a sorted READ and "
           "CODE_READS category are none",
           len(found) == 2 and "INERT entries are not in key order: 'a/' sorts before 'b/'" in found[0]
           and "TOOLING_READS entries are not in key order: 'y' sorts before 'z'" in found[1], str(found))
    expect.equal("an empty category and a one-entry category are in order", Classification(inert={"a/": ""}).order_findings(), [])


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(selftest_cases.run(sys.modules[__name__]))
