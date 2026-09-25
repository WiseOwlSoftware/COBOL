# Golden lane 1 (cloud) — WRITER brief

You are a golden WRITER for the COBOL.NET conformance burn-down. Repo: `/home/user/CobolSharp` (read `CLAUDE.md`
first — rules 1, 3, 4 bind you). Your input: `/tmp/gl/in/in-<SLUG>.json` — read it WHOLE. Each row is an
inventory rule already verdicted CONFORMS (or DOCUMENTED-NON-SUPPORT owing a witness) with NO spec-derived test.
Your job: give each row a spec-derived golden, or report why it cannot have one.

## Hard boundaries
- The repo is READ-ONLY to you. Never write inside `/home/user/CobolSharp`, never run `git`, `dotnet build`,
  `dotnet test`. You MAY run `python3 scripts/spec/*.py` read-only tools (cite.py, where.py) and the BUILT compiler:
  `/home/user/CobolSharp/src/Cobol.Net.Cli/bin/Debug/net10.0/cobol <file.cob> --run --std <85|2002|2014|2023>`
  (without `--run` it only compiles). Run it from your run dir `/tmp/gl/run/<SLUG>/` (mkdir -p).
- Drafts go under `/tmp/gl/out/<SLUG>/` at their repo-relative destination path, e.g.
  `/tmp/gl/out/<SLUG>/tests/conformance/85/l1c07_evaluate_directive_when_other.cob` (+ `.out`), negatives
  `/tmp/gl/out/<SLUG>/tests/conformance/negative/l1c07-<desc>.cob` (+ `.err`).
- Drafts that FAIL (suspected compiler defect) move to `/tmp/gl/held/<SLUG>/` (same subpaths) — never left in out/.
- HARD CAP ~150 turns. Near the cap: write your report with the undecided rule-ids in `deferred`, and return.
  An honest `deferred` list is a correct outcome; a silently short report is the failure.

## Naming (collision-proof; the director checks)
- Your file number NN = the number after `misc-p` in your slug, zero-padded to 2 (misc-p7 → 07).
- Positive file names: `l1c<NN>_<short_desc>` (lowercase, underscores). Negative: `l1c<NN>-<short-desc>`.
- PROGRAM-IDs (every program in a file, subprograms too): `L1C<NN><SUFFIX>` e.g. `L1C07A`, `L1C07B`, ... ≤ 30 chars,
  unique across your drafts. Check: `grep -rn "PROGRAM-ID\. *<ID>\b" /home/user/CobolSharp/tests/conformance` finds none.

## Per row, IN THIS ORDER
1. **Derive before you run.** Read the rule text, then the surrounding section in `specs/ISO_COBOL.md` (general
   format, syntax rules, general rules it depends on). Run `python3 scripts/spec/cite.py --check <clause> "<text>"`
   for EVERY clause you cite, and paste the OK line into the program's header comment. A REAL clause that answers a
   DIFFERENT question is the failure mode to fear most. Adjudicator notes are context only — derive from the RULE.
2. **Existing golden?** If one of `existing-goldens-by-subject` already exercises EXACTLY this rule's branch with a
   spec-derived expectation (read it, and its .out), disposition `existing-golden` with its test-ref and the lines
   that pin the rule. Coverage = the rule's branch, not merely the verb being used. When in doubt, write a new one.
3. **Write the golden.** A minimal deterministic program (fixed-form reference format like the existing corpus:
   comment lines `      *> ...` with `*>` in column 7, code in area A/B, nothing past column 72) that exercises
   THIS rule's branch and DISPLAYs results a wrong implementation of the rule would change. Header comment block:
   first line `      *> ISO §<clause> <rule> — <what is exercised>`, then the quoted rule, the cite.py OK line(s),
   and **the derivation of every expected output line** from the rule text. WRITE THE `.out` NOW, before any run
   — it is the spec's answer. Deterministic only (no CURRENT-DATE values, no addresses). Files that the program
   creates: use relative names unique to your program (e.g. `L1C07A.DAT`) and clean up is not needed.
   - Directory = the LOWEST edition in the row's `editions` in which every construct your program uses exists
     (the directory selects `--std`; dirs are 85, 2002, 2014, 2023). A 1985 row gets an 85 golden unless the
     program needs later syntax (then say why). If behaviour differs by edition, write one golden per edition.
   - Several rows may share one program when the same output pins them all — say which lines pin which rule.
   - A rule that REQUIRES REJECTION (a syntax rule: "shall", "shall not", "only if") gets a NEGATIVE: `.cob` whose
     FIRST line is `      *> reject-at: <years>` (every edition where the source must be rejected, e.g. `85 2002 2014 2023`)
     and a `.err` holding ONE line: the diagnostic substring (a COBOLNETnnnn code; look it up in
     `docs/DIAGNOSTICS.md` / `src` — the code's MEANING must be this rule's violation). The program must be
     otherwise valid, so the only reason to reject is this rule.
4. **Then run it.** Positive: `cobol <f>.cob --run --std <ed> > <f>.observed 2>&1; echo EXIT=$?`, compare with the
   .out (trailing spaces per line and a final newline are ignored by the runner; nothing else is). Negative: compile
   at EVERY reject-at edition; each must fail with the .err substring present in the output.
5. **Mismatch?** First re-derive: if YOUR program is wrong (a syntax slip unrelated to the rule, a derivation error
   you can show from the spec text with a cite), fix the program/.out and say exactly why in `notes`. ⛔ NEVER edit
   the .out to match the compiler's output because the compiler printed it. If the compiler still disagrees with
   the spec-derived expectation (wrong output, crash, rejects the legal program, accepts the illegal one), it is a
   SUSPECTED COMPILER DEFECT: move the draft to `/tmp/gl/held/<SLUG>/`, disposition `suspected-defect`, and add a
   `defects` entry. Say that the row's CONFORMS verdict is therefore suspect.
6. **not-closable**: a rule with no observable (pure documentation; implementor latitude with nothing to DISPLAY;
   needs an environment the runner lacks — e.g. interactive input, a printer, OO runtime not claimed). Give the
   reason; never manufacture a test. `docs/CONFORMANCE.md` records the project's implementor choices — a golden
   pinning a documented choice is legitimate; cite the row.

## Report — write `/tmp/gl/reports/<SLUG>.json` (valid JSON), and return a ≤15-line summary
```json
{"slug": "...",
 "rows": [{"rule_id": "...", "disposition": "new-golden|existing-golden|suspected-defect|not-closable",
           "test_ref": "conformance:85/l1c07_x | conformance:negative/l1c07-x | (empty)",
           "files": ["tests/conformance/85/l1c07_x.cob", "..."], "edition_dir": "85",
           "derivation": "how each expected line follows from the rule text",
           "citations": ["cite.py --check ... -> OK §...", "..."], "ran": "pass|mismatch|n/a", "notes": "..."}],
 "manifest_entries": {"85": ["l1c07_x"], "2023": []},
 "negative_manifest_entries": ["l1c07-y"],
 "records": [{"rule-id": "...", "test-ref": "conformance:85/l1c07_x"}],
 "defects": [{"rule_ids": ["..."], "mechanism": "one line", "repro": "/tmp/gl/held/<SLUG>/tests/...cob",
              "std": "2023", "rule_quote": "...", "cite_line": "OK §...", "expected": "...", "observed": "...",
              "wrong_answer": true, "crashes": false, "rejects_legal_source": false, "under_rejects": false,
              "code_site": "src/...cs:line if found (python3 scripts/spec/where.py <clause> [rule])"}],
 "deferred": []}
```
`records` are WITNESS-ONLY (`rule-id` + `test-ref` only — the verdict and notes stay as adjudicated), one per row
whose golden(s) passed (`new-golden`) or `existing-golden`. Multiple refs: `"a; b"`. Every row in the input appears
exactly once in `rows` (or in `deferred`).

## Lessons from wave 1 (binding)
- Fixed form: a `>>` compiler directive starts in column 8 or later (§7.3.3 SR3), never column 7.
- Compile/run from a COPY in `/tmp/gl/run/<SLUG>/` so the compiler's .dll/.g.cs/runtimeconfig outputs never land in
  out/. out/ holds only .cob/.out/.err and support files (.cpy) — list support files in the row's `files`.
- A negative's `.err` must be SPECIFIC to this rule: when the code is shared by several checks, use the code plus
  the head of the message (`COBOLNET0820: class 'X': the INHERITS chain is cyclic`), stopping before any text that
  is itself wrong (e.g. a mis-cited clause in the message). A generic parse code (COBOL0001/03xx) is acceptable
  ONLY when the rule is a general-format / pure-syntax rule, and then name that in `notes`.
- A golden for an implementor-defined behaviour must observe the DOCUMENTED choice itself (e.g. the EC that the
  documented determination names, via `>>TURN ... CHECKING ON` + FUNCTION EXCEPTION-STATUS), not an outcome every
  implementation would share.
- Write .cob/.out/.err with LF line endings.
