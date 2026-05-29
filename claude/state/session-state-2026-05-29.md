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

**Concrete lead (file-I/O):** `XXXXP###`/`XXXXD###` are produce/consume file placeholders paired
by number (XXXXP002 written, XXXXD002 read = same file). Remapping both to a shared **quoted**
literal `"TF002"` regressed SM204A, while the current **unquoted** implementor-name form
(`XXXXD002`) lets SM203A→SM204A share a file correctly. So `CobolFileManager`/ASSIGN handling
differs between a quoted-literal target and an unquoted implementor-name target — investigate that
path (and whether sequential WRITE actually flushes a persistent file) before remapping. The
reverted remap lives in git history (commit "Revert premature XXXXP/XXXXD remap").

### ST (sort/merge) snapshot after the hang fix
total=40: CLEAN=8, FAIL*=15, COMPILE_FAIL=8, NO_OUTPUT=8, RUNTIME=1 (ST132A still hangs —
different cause). Most NO_OUTPUT/FAIL* are the companion-file issue above; FAIL* also include
genuine sort-output correctness. COMPILE_FAIL (ST115A/117A/131A/135A/139A/140A/144A/147A) are
unparsed SORT/MERGE forms to investigate.

## 0b. Update — file-I/O subsystem (producer/consumer orchestration fixed; COMPILE_FAIL is the wall)

**Producer/consumer orchestration — FIXED (DEVLOG 216, committed, guard-green 149 NIST).**
- NistPreprocessor maps `XXXX[PD](\d+)` → `"TF$1"`: produce/consume placeholders that share a
  number now resolve to one physical file (e.g. `tf001.txt`), independent of differing SELECT
  names (ST104A `SORTOUT-1D`/XXXXP001 ↔ ST105A `SORTIN-1E`/XXXXD001).
- `Compilation.Preprocess` reordered to normalize → **COPY** → NIST, so placeholders inside a
  COPY'd FILE-CONTROL (SM203A copies K3FCB `ASSIGN TO XXXXP002`) are mapped too.
- Verified end-to-end: SM203A→SM204A and ST104A→ST105A share a file; consumer hits 0 FAIL*.
- ASSIGN→host path: `FileRuntime.ResolveHostPath` lowercases + appends `.txt` (MYDATA→mydata.txt);
  a quoted-literal ASSIGN uses the literal, an unquoted name falls back to `fileSym.Name`
  (Binder.cs ~221). Sequential WRITE DOES persist (I had looked for the wrong filename earlier).

**File-I/O suite snapshot (after orchestration fix):**
SQ 85: CLEAN 2, **COMPILE_FAIL 81**. IX 42: CLEAN 1, **COMPILE_FAIL 40**. RL 35: CLEAN 4,
**COMPILE_FAIL 23**. ST 40: CLEAN 10, COMPILE_FAIL 8, NO_OUTPUT 7, FAIL* 13, RUNTIME 1 (ST132A).

**THE file-I/O wall = COMPILE_FAIL (144/162). Two root causes; #1 now partly handled:**
1. **CCVS column-7 conditional lines — DONE for P/J (DEVLOG 217).** `ConvertFixedToFree` already
   excluded `D`/`S`/`Y` as optional/comment lines (why NC/IF compile despite their `S`/`Y` code);
   added `P`/`J` (the file-I/O auxiliary scratch file + alternate ASSIGN target). `A`/`B`/`C`/`G`
   stay code (NC/SM use them). `P`/`J` are absent from the guarded suites → guard-safe. This
   removed the spurious-second-SELECT errors but did NOT clear many compiles on its own (cause #2).
2. **SELECT/FD grammar gaps (the remaining wall).** The standard (space-indicator) FILE-CONTROL
   content itself has forms the grammar rejects:
   - A bare organization keyword: SQ102A `SELECT SQ-FS1  ACCESS MODE IS SEQUENTIAL  SEQUENTIAL
     ASSIGN TO …` — the lone `SEQUENTIAL` (organization clause without `ORGANIZATION IS`).
   - `STATUS data-name` without the `FILE` keyword (RL101A `STATUS RL-FS2-STATUS`).
   - Clause spread across continuation/multiple lines; clause ordering.
   - INDEXED `RECORD KEY` / alternate-key and relative `RELATIVE KEY` clause forms.
   These need a careful FILE-CONTROL grammar pass against ISO §9 (SELECT) — the highest-leverage
   remaining file-I/O work, then the INDEXED/RELATIVE runtime for correct results.

Also: 8 ST COMPILE_FAIL (SORT/MERGE forms) and ST132A still hangs (a non-USING cause).

## 0c. Spec-audit follow-ups (generalizing the "too narrow" fixes)

A self-audit reviewed the session's fixes for spec-generality. Outcomes (DEVLOG 221–223):
- **table(ALL) → multi-dimensional** (DONE, E221): cartesian product over all ALL positions, each
  using its dimension's OCCURS bound.
- **AT END vs I/O-error** (DONE, E222): `IsAtEnd` strict ("10") for the AT END condition; new
  `IsReadExhausted` (EOF + terminal errors) for compiler-generated loop termination
  (`IrCheckFileAtEnd.TreatErrorsAsEnd`). Fixes the over-broad Entry-215 anti-hang.
- **Runtime FUNCTION LENGTH for ref-mod** (DONE, E223): `LENGTH(x(s:l))`→`l`, `x(s:)`→`size−s+1`;
  also fixed a real bug — `InterpretSubscriptTokens` matched a NESTED ref-mod colon (now depth-0
  only), so `LENGTH(WS(1:N))` / `f(T(I)(1:3))` parse correctly.
- **FACTORIAL/CHAR out-of-range** (NO CHANGE, spec-acceptable): COBOL-85 has no exception-condition
  framework, so a too-large result is undefined; the clamp + `ON SIZE ERROR` at the store works.

**Deferred as larger subsystems (not narrow fixes):**
- **CHAR/ORD program collating sequence**: custom collating is bypassed EVERYWHERE (even
  `CompareAlphanumeric` uses raw byte order; `ALPHABET` ordering is parsed but discarded). CHAR/ORD
  native is correct for every sequence the suite uses (NATIVE/STANDARD-1/STANDARD-2 ≡ ASCII). Doing
  CHAR/ORD alone would be inconsistent — needs a holistic collating-sequence feature (table model
  threaded to comparisons/SORT/INSPECT/class-conditions/CHAR/ORD). See [[reference_nist_xcards]].
- **General CCVS column-7 X-card model**: the P/J exclusion is the scoped version; a full model
  (per-letter include/exclude, FD GLOBAL visibility) is part of the file-I/O continuation.

**Minor follow-ups noted:** LENGTH of an ODO group still returns max layout size, not the current
DEPENDING ON size; `COMPUTE` of a value wider than the receiver (no ON SIZE ERROR) can blank the
field instead of truncating (extreme edge — 19-digit result into PIC 9(18)).

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
