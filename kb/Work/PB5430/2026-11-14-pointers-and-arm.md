## 2026-11-14: commit 3, the pointers and the conformance arm, built (kb/Work PB5430)

**Sites:** `scripts/session-probe.ps1` (the view rendered at session start), `scripts/spec/conformance_source.py`
(`Conformance.keys`), `scripts/spec/audit_code_citations.py` (`CONFORMANCE_CITE`, `CONFORMANCE_HISTORY`,
`conformance_keys`, `conformance_findings`, `phantom_scan`, `CONFORMANCE_SELF_TEST`, `conformance_self_test`),
`kb/Work/PB4053.md` (retired), `docs/rearchitecture/DESIGN-orchestrator-loop.md` (section 16.8, commit 3 as built)

**The pointers.** Every instruction that names a place to write a determination now names `docs/conformance/`: CLAUDE.md
rule 1, the `spec-lookup` skill, `dispatch-spec-implementer.md`, `golden-lane-brief.md`, `cloud-adjudicate-brief.md`
(with the render command, since a fresh clone holds no view), the issue template, the DOC_INDEX row, the plan's
publication criterion and DESIGN-test-build-ci.md section 3.17.4 (which also states the stamp and rule (e)). README.md's
three links and its tree listing name the directory. The imperative sweep's remaining hits are citations of the
statement by section, which the design keeps.

**The arm.** `audit_code_citations.py --check`: `⛔ 0 finding(s)` with no baseline; its self-test fires on the three
misattribution shapes the burn-down rewrote and stays silent on a real section, a real key, a sentence-final dot, a
template and a marked example. Measured before the arm turned on: of the 23 unresolved citations, 16 were templates and 7
in kb/Work note bodies (PB288, PB1278, PB1534, PB1535, PB762 and this note), the two exclusions it decides.

**PB4053** retired with its acceptance run (`kb/Work/PB4053/2026-11-14-retired.md`).
