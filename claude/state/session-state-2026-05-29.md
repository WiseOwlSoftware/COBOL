# CobolSharp Session State — 2026-05-29

Paste this at the start of the next session to restore full context.

---

## 0. Update — late 2026-05-29 (SM COPY complete + SORT hang fix)

- **SM suite: 12/17 CLEAN, baselined.** Implemented the full COBOL-85 source-text-manipulation
  feature set to spec: a text-word REPLACE/COPY REPLACING engine (whitespace/line-break-insensitive
  matching; `( ) .` are text words; comma/semicolon are space-equivalent per GR6(b); literal quotes
  preserved; comment lines transparent but debug-line content participates), qualified REPLACING
  operands (`OF/IN` + subscript), `VALUE OF` FD clause, mid-line COPY, NIST archive-marker strip,
  and `COPY … OF/IN library-name` multi-library resolution. DEVLOG 209–214.
- **Guard now 149 NIST (95 NC + 42 IF + 12 SM)**, ALL GREEN (1000 unit / 336 integration).
- **SORT hang fixed** (DEVLOG 215): `FileRuntime.IsAtEnd` now treats FileNotOpen/FileNotFound/
  PermanentError as end-of-data, so SORT … USING (and any READ … AT END) over a missing file
  terminates instead of spinning. 7 of 8 ST timeouts cleared.

### THE dominant remaining obstacle: producer/consumer file orchestration
The remaining SM (SM104A) and most of ST/SQ/IX/RL depend on **companion files**: one program
writes a file (e.g. ST104A creates `SORTIN-1E`, SM103A creates TEST-FILE) and a later program
reads it. `run-suite.sh`/`guard.sh` run each test in isolation, so consumers see no input →
NO_OUTPUT or FAIL*. The NIST ASSIGN targets are `XXXXX###` placeholders; whether two programs
share a physical file depends on NistPreprocessor's substitution. **Next design decision:** how to
orchestrate these chains — run producers before consumers in a shared cwd (and map the shared
ASSIGN placeholders to a common filename), or supply `.dat` fixtures. This blocks the file-bound
tests across several suites and should be solved once, centrally.

### ST (sort/merge) snapshot after the hang fix
total=40: CLEAN=8, FAIL*=15, COMPILE_FAIL=8, NO_OUTPUT=8, RUNTIME=1 (ST132A still hangs —
different cause). Most NO_OUTPUT/FAIL* are the companion-file issue above; FAIL* also include
genuine sort-output correctness. COMPILE_FAIL (ST115A/117A/131A/135A/139A/140A/144A/147A) are
unparsed SORT/MERGE forms to investigate.

## 1. Session Summary

Depth-first NIST suite push, continuing the "run all NIST suites group by group, each to 100%"
directive (autonomous; grammar changes pre-authorized + logged; full guard must stay green).

- **IF suite (intrinsic functions): 100% — 42/45 CLEAN, all baselined.** Started at 8 CLEAN /
  ~30 FAIL* / 9 crashes / 1 timeout. The 3 non-baselined (IF401M/402M/403M) are flagging
  conformance modules that emit no CCVS report by design (compile + run rc=0; nothing to fail).
- **SM suite (source text manipulation / COPY): 7/17 CLEAN** (from 1). Built the whole copy-
  library pipeline. Remaining: 6 FAIL* (value work) + 2 COMPILE_FAIL (advanced REPLACE
  pseudo-text) + 2 flagging modules.
- Guard (`bash scripts/guard.sh`) ALL GREEN throughout: 1000 unit, 336 integration,
  **137 NIST (95 NC + 42 IF)** baselined. Branch: main.

---

## 2. Commits This Session (oldest → newest) — DEVLOG entries 196–208

| Area | Description |
|------|-------------|
| IF | Intrinsic crash-robustness (int-cast clamps, date try/catch, FACTORIAL guard) + MAX/MIN result-category propagation through IR (crashes 9→1) |
| IF | Untrimmed string args (REVERSE/UPPER/LOWER) + nested-subscript binding `MEAN(IND(1)…)` (CLEAN 8→17) |
| IF | Signed-decimal lexer token `SIGNED_DECIMALLIT` — negative decimals lost their fraction (CLEAN→18) |
| IF | `FUNCTION f(table(ALL))` occurrence expansion (CLEAN 18→28) |
| IF | Additive expressions as intrinsic args `LOG(E + .001)` (CLEAN 28→37) |
| IF | CHAR/ORD 1-based ordinal + LENGTH of nested string functions (FAIL* → 0; CLEAN→42) |
| IF | 42 baselines locked into guard (137 NIST guarded) |
| SM | Copy-library extraction (`tools/extract-nist-copylib.sh` → `tests/nist/copylib/`, 51 members) + mid-line COPY + copybook normalization |
| SM | `VALUE OF` FD clause (obsolete/inert) — CLEAN 2→5 |
| SM | Strip NIST archive markers (`*HEADER,`/`*END-OF,`) + preprocess `--copy-path` — CLEAN 5→7 |
| SM | Guard empty REPLACING operand (compiler crash → diagnostic) |
| SM | REPLACE/COPY REPLACING literal operands keep their quotes (COMPILE_FAIL 6→4) |
| SM | RETURN optional RECORD + optional AT in END phrase (COMPILE_FAIL 4→2) |

---

## 3. Key Architecture Added / Changed This Session

- **Intrinsic result category flows to emission.** `IrFunctionCall.ReturnsString` is set from the
  bound category in `DataMovementLowerer`; `CilExpressionEmitter.EmitFunctionCall` dispatches on
  it instead of a static function-name list (handles polymorphic MAX/MIN). Classify once in the
  binder, propagate, never re-classify at the leaf.
- **Subscript/arg parser** (`ExpressionBinder`): `BindSubscriptSegment` now binds nested
  subscripts `IND(1)` and ref-mod `WS(1:3)`; additive operators (`+`/`-`) route a segment to the
  arithmetic parser (was multiplicative/power/FUNCTION only); `FUNCTION f(table(ALL))` expands to
  one element ref per occurrence via `OccursInfo.MaxOccurs`.
- **Lexer** (`CobolLexer.g4`): `SIGNED_DECIMALLIT` before `SIGNED_INTEGERLIT` (longest-match).
- **Intrinsics** (`IntrinsicFunctions.cs`): `Char(n)`→code `n-1`, `Ord(c)`→code+1 (1-based ISO);
  `ToInt` clamp; FACTORIAL/date guards. `StorageHelpers.ReadFieldAsRawString` used for intrinsic
  alphanumeric args (no trim — preserves field width for REVERSE/LENGTH).
- **Copy pipeline**: `CopyProcessor.FindCopyKeyword` (mid-line COPY, skips literals + `*>`
  comments, word boundaries), IN/OF qualifier, `NormalizeCopybook` (sequence-area fixed-form
  detection for CCVS `C`/`G` indicators), empty-operand guard, literal-quote preservation.
  `ReferenceFormatProcessor.StripNistArchiveMarkers` (runs before normalization, both paths).
  CLI `--copy-path`/`-I` + `--nist` sibling-`copylib/` auto-discovery (compile + preprocess).

---

## 4. Next Steps (resume here)

Finish SM to 100%, then continue depth-first: IC → SQ → IX → RL → ST → …

**SM remaining (7/17 CLEAN):**
- **2 COMPILE_FAIL — advanced REPLACE pseudo-text** (the big one):
  - SM206A: multi-line COPY … REPLACING pseudo-text produces garbled data-division text
    (`01 +00009 REC-CT …`). Needs whitespace-insensitive, token-aware pseudo-text matching.
  - SM208A: REPLACE with multi-line, continuation-line, quote-doubled pseudo-text corrupts `TO`
    → `AO`. Same root: REPLACE pseudo-text matching is naive `string.Replace`.
  - Likely fix: rework `ApplyReplacements`/`ParseReplacements` for COBOL pseudo-text semantics
    (match on token sequences ignoring whitespace runs, span source lines, handle `==…==` with
    embedded doubled quotes and continuation `-` lines).
- **6 FAIL*** (compile+run, value correctness) — each a distinct feature, diagnosed:
  - **SM104A** COPY-TEST-3 = `DECIMAL-POINT IS COMMA` (European format; COMPUTED 0 vs
    `12.345.678,91`); COPY-TEST-4 = COPY of ENVIRONMENT DIVISION entries not taking effect.
  - **SM201A** COPY-TEST-11 / **SM206A** = multi-line pseudo-text COPY REPLACING (same root).
  - **SM202A** COPY-TEST-17 = REPLACE with multiple BY-literal operands only partially applied
    (`TRUE TWO ABCDE 12` vs `TRUE TWO + 2 = 4`).
  - **SM205A** (8) = SORT/COPY value tail (RETURN now parses; inspect report).
  - **SM207A** QUAL-TEST-02 = library-qualified COPY (`COPY text IN library`): we ignore the
    qualifier and search a single flat `copylib/`, so same-named members in different libraries
    collide ("TEXT COPIED FROM WRONG LIBRARY"). Needs library-name → directory mapping.
- **2 NO_OUTPUT**: SM301M/SM401M — verify they are flagging modules (no CCVS report), like
  IF401M/402M/403M; if so, leave unguarded.
- When a test hits 0 FAIL*, copy `tests/nist/output/<lc>.txt` → `tests/nist/valid/<TEST>.txt`
  and add it to `NIST_TESTS` in `scripts/guard.sh` (deterministic — header is fixed `Apr 1993`).

**Tooling:** `bash scripts/run-suite.sh SM` surveys; `cobolsharp preprocess <f> -o <out>` now
expands copybooks (sibling `copylib/` auto-added) for debugging COPY/REPLACE expansion.
