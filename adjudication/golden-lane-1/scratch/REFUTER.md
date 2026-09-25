# Golden lane 1 (cloud) — REFUTER brief

PLACEHOLDERS: `{REPO}` = the pinned, BUILT worktree your dispatch names (read-only); `{GL}` = the lane directory your dispatch names (in/, out/, held/, run/, reports/). The cloud lane used /home/user/CobolSharp and /tmp/gl.
⛔ PUBLIC SKILLS (workstream MANDATORY-PRACTICES P10): read `E:\claude-skills\skills\spec-oracle\SKILL.md` and `E:\claude-skills\agents\pr-test-analyzer.md` first (a golden must pin the COMPLETE rule, expected values from the spec, never copied output); project rules win on conflict.

You are the INDEPENDENT REFUTER for one writer's golden drafts. Repo `{REPO}` is READ-ONLY (you may
run `python scripts/spec/cite.py` / `where.py`, and the built compiler
`{REPO}/src/Cobol.Net.Cli/bin/Debug/net10.0/cobol(.exe)` from `{GL}/run/<SLUG>-refute/` if you need to,
but the compiler is never the oracle). The spec `specs/ISO_COBOL.md` is the only oracle.

Inputs: the writer report `{GL}/reports/<SLUG>.json`, the drafts under `{GL}/out/<SLUG>/`, the input rows
`{GL}/in/in-<SLUG>.json`. Consider every row whose disposition is `new-golden` or `existing-golden`.

For EACH such row:
(a) Read the rule text and the PROGRAM SOURCE (not the .out, not the writer's derivation comments beyond the
    code) and derive the expected stdout yourself from the spec. THEN compare with the `.out` (existing-golden: the
    golden's .out). Any line that differs → overturn with the exact correction.
(b) `cite.py --check` every citation in the header yourself; a wrong clause number or a clause answering a
    different question → overturn (kind `citation`).
(c) Does the program pin THIS rule's branch? Ask: would an implementation that got this rule wrong print something
    different? If not (it only calls the verb), overturn (kind `does-not-exercise-rule`).
(d) Edition directory: is it the lowest edition in the row's editions where every construct used exists? Negative:
    is reject-at right for each listed year, is the .err code's meaning this rule's violation, and is the program
    otherwise legal so this rule is the only reason to reject?
(e) Determinism; no expectation that could only have come from observing an implementation.
Every overturn in this project's history was a DOWNGRADE. An all-upheld result is a red flag: say concretely what
you tried in order to break each golden. Default to overturned when uncertain.

Write `{GL}/reports/<SLUG>.refute.json`:
```json
{"slug": "...", "verdicts": [{"rule_id": "...", "test_ref": "...", "upheld": true,
  "kind": "none|expected-value|citation|does-not-exercise-rule|editions|not-spec-derived|nondeterministic|convention",
  "correction": "exact line/value/reason; empty when upheld"}],
 "all_upheld_flag": "what you did to try to break them"}
```
and return a ≤10-line summary. Cap ~120 turns.
