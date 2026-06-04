# CobolSharp — Architecture Assessment & Commercial-Hardening Roadmap

**Date:** 2026-06-03 · **Trigger:** owner goal = *production-quality, commercial-level COBOL-85 compiler,
extensible to later ISO specs; "a complete refactor or rewrite is one valid option."*

**Method:** two parallel evidence-based agent workflows over the real codebase —
(1) a 10-dimension architecture audit (each auditor read actual code, cited file:line, gave a per-layer
verdict), and (2) an 11-test root-cause diagnosis of every remaining in-scope NIST failure.

---

## Headline verdict: **DO NOT REWRITE. Targeted hardening + incremental completion.**

Across **10 independent layer audits the verdict was 8× "targeted-refactor", 2× "incremental-improvement",
0× "rewrite".** Every auditor invoked the same reasoning: the architecture is a sound, textbook layered
compiler (lex → parse → semantic → layout → bind → bound-tree → IR → lowering → CIL emit → runtime), the
debts are localized and fixable in place, and **350 NIST CCVS85 baselines + 1000 unit + 347 integration
tests encode hard-won, spec-validated correctness that a rewrite would put at risk for no structural gain**
(the Spolsky "never rewrite a working, tested system" bar is not cleared).

What this codebase already does well (evidence-backed):
- Warning-free ANTLR grammar with principled mode-based lexer (PICMODE/SUBSCRIPT), canonical two-stage
  SLL→LL parsing, ISO-cited modular rules.
- Real binder→bound-tree→IR→emit separation (verified: **zero** IR usage in `Semantics/`, zero `new Ir…` in
  the sub-binders), immutable bound nodes, a basic-block CFG, a centralized **fail-fast** emit dispatch.
- Single shared PIC/decimal pipeline (compile-time and runtime use one descriptor); declarative
  category/MOVE-legality tables; correct file-status (ISO §9.1.13) and indexed-file arrival-order semantics.
- Near-zero global state (one scoped static field), nullable-clean 0-warning build, 138-descriptor
  structured diagnostics, a real two-axis dialect model (version vs strictness).

**The central insight:** this compiler is excellent at *accepting valid input* (it was driven by NIST CCVS85,
which only feeds valid programs) but under-invests in *rejecting/diagnosing invalid input* and in
*structural self-verification* — which is exactly the gap between "passes the validation suite" and
"commercial-grade." Plus the "later-spec (2002/2014/2023) support" is currently **scaffolding/theater**
(orphaned grammars not in the build; gates with no downstream consumers) — the *seams* exist, the
*implementation above COBOL-85 does not*.

---

## Cross-cutting themes (what multiple auditors independently flagged)

**T1 — Diagnostics & robustness on INVALID input (the #1 commercial gap).**
- Undefined data-name references compile clean, exit 0 (`MOVE 5 TO NONEXISTENT-ITEM.`) — 66 silent
  `return null` binder sites; `ReferenceResolver` checks PERFORM/GO TO/READ but not data refs. *(code-health)*
- No §8.4.2 ambiguity diagnostic; PIC parsing silently swallows illegal pictures; `int.Parse` on level
  numbers crashes on bad input. *(semantic-model)*
- `CopyProcessor` silently swallows a missing copybook (a comment, no diagnostic) and malformed REPLACE; no
  source-mapping back to copybook line. *(lexer-preprocessor)*
- No top-level try/catch at the CLI; diagnostics report placeholder filename `"<source>"`. *(code-health)*
- Runtime does no arg validation → a compiler bug surfaces as an `rc=139` crash, not a diagnosable error.
  *(runtime)*

**T2 — Validation integrity: the "350 passing" headline contains FALSE GREENS, and there is no CI.**
- Guard greps `FAIL*` detail lines but **never parses the authoritative CCVS footer** ("NNN TEST(S) FAILED").
  Confirmed committed false-greens: **IX108A.txt** footer says `000 OF 001 EXECUTED / 001 TEST(S) FAILED`;
  **SQ212A.txt** and **NC303M.txt** are **0-byte** baselines that match any empty output vacuously.
  *(SQ212A was baselined in this very session — a regression to fix.)*
- The only CI workflow is `.disabled`, pins .NET 8 + Release (vs net9 Debug locally), and would not run the
  NIST suite anyway (bash-only; no xUnit wrapper). No coverage instrument. Self-reported, hand-maintained
  test counts (MEMORY says 299, guard says 350). *(test-infra — the lone two CRITICAL severities)*

**T3 — Codegen correctness asserted by EXAMPLE, never STRUCTURALLY.**
- No emit-time IL/stack verification (zero ILVerify/peverify) — the only net is `dotnet <dll>` exit-code over
  the tests; an unbalanced-stack bug on an untested path ships as a field `InvalidProgramException`.
- `IrRuntimeCall` is a stringly-typed if/else chain ending in `// Other runtime calls: NOP for now` — a
  typo'd/new runtime-call name silently NOPs (args left on stack → `InvalidProgramException`). It ignores
  `rtc.Arguments` entirely.
- The `Dispatch(startPc,exitPc)` control-flow model maps PERFORM onto the native call stack with no recursion
  guard → uncatchable `StackOverflowException` (RL111A) instead of a diagnosable COBOL error. *(codegen-cil)*

**T4 — Latent correctness bugs masked by NIST's (benign) test data.**
- Numeric width capped at 18/19 digits by a `long` bottleneck in COMP-3 decode + all `(long)` casts —
  below even COBOL-85 PIC 9(18) for large values, far below 2002 PIC 9(31). *(runtime)*
- Indexed-file keys stored/compared as `Encoding.ASCII.GetString` → any key byte ≥ 0x80 (COMP/COMP-3/binary
  keys) collides (verified empirically). Masked because NIST indexed tests use DISPLAY keys. *(runtime)*
- National PIC width hardcoded 1 byte/position (PIC N(5) reports 5, not 10). *(semantic-model/runtime)*

**T5 — Structural debt that taxes every future feature.**
- TWO layout systems; the second (`RecordLayoutBuilder`) is **dead** (zero producers of its
  `IrLoadField`/`IrStoreField`) yet emits an unused struct per record and is the last writer of the mutable
  `DataSymbol.ElementSize`. *(storage-layout)*
- Flat single-dictionary symbol table cannot represent duplicate names (papered over by a `_rejections`
  side-list); THREE divergent qualification implementations. *(semantic-model)*
- No shared bound-tree walker → nested-statement traversal duplicated in ≥3 passes (footgun for new compound
  statements). *(binder-ir)*
- Per-feature dispatch is ~7–8 hand edits across grammar/bind/lower/IR/emit/runtime — the very
  "switch-that-should-be-polymorphism" the project's own `feedback_refactor_first_always` warns against.
  *(extensibility-dialects)*

**T6 — Scale: file engines are in-memory, O(n²)-scan, crash-unsafe.**
- Indexed/Relative load the whole file at OPEN and persist only at CLOSE via `FileMode.Create`; `ReadNext`
  full-scans every call. Fine for test scale; a wall for commercial data volumes and durability. The
  `IFileHandler` seam means the engine can be replaced without touching the validated status-code logic.
  *(runtime)*

---

## Prioritized commercial-hardening roadmap

> Principle: every change is **test-gated against the existing corpus** (guard must stay green) and, where it
> adds a leniency, **dialect-gated**. Fix validation integrity FIRST so "green" is trustworthy.

### P0 — Validation integrity (foundation; the guard must not lie)
1. Make the guard parse the CCVS **footer total** (`NNN TEST(S) FAILED` + `NNN OF MMM EXECUTED`) as the
   authoritative pass signal (logic already exists in `nist-batch.sh`); reject any baselined program that did
   not execute >0 tests or reports any failure.
2. Quarantine/fix the false-greens: **SQ212A** (0-byte — this session's regression), **NC303M** (0-byte),
   **IX108A** (footer FAILED). Remove from baselines or fix the underlying cause.
3. Assert expected-baseline-count == tests-run == tests-matched; reject 0-byte baselines.
4. Re-enable CI; run guard in it; align framework/config (net9 Debug); add coverage.

### P1 — Robustness & diagnostics on invalid input (commercial "never crash, always diagnose")
5. Diagnose undefined data-name references (the 66 silent `return null` sites).
6. Top-level try/catch in the CLI → internal-compiler-error diagnostic, never a raw crash.
7. Real source path in diagnostics (kill the `"<source>"` placeholder); retire the 3 bare `"SEM"` codes.
8. PIC-validity diagnostics (`FromPicBody` must reject illegal pictures, not swallow them).
9. `CopyProcessor` diagnostics (missing copybook = error) + copybook source-mapping.
10. Runtime guard clauses → diagnosable `CobolRuntimeException`, not `rc=139`.

### P2 — Codegen hardening (correctness by construction)
11. **IL verification gate** in the guard (`dotnet ILVerify` or a Cecil stack-depth pass over every method).
12. Kill the `IrRuntimeCall` silent NOP (`else throw` + arg-count assert; ideally typed IR nodes).
13. Make `Dispatch` recursion-safe (depth guard → diagnosable error, or de-recurse) — fixes RL111A's class.

### P3 — Latent correctness limits (masked by NIST data)
14. Replace the `long` numeric bottleneck with `decimal`/`Int128` → PIC 9(18) today, 9(31) for 2002.
15. Indexed keys: ASCII string → ordinal `byte[]` (COMP/binary keys currently collide).
16. National PIC = 2 bytes/position.

### P4 — Structural dedup (pay down the per-feature tax)
17. Excise the dead `RecordLayoutBuilder`/`IrRecordType`/`IrLoadField`/`IrStoreField` (zero producers).
18. `ElementSize` → immutable `StorageLocation` (remove the mutable-symbol side-effect).
19. One canonical qualification resolver + `Scope` name→candidate-set (duplicate names, §8.4.2 ambiguity).
20. Shared `BoundTreeWalker`; finish CilEmitter decomposition; split `SemanticBuilder`.

### P5 — Extensibility foundation (the "incremental later-spec" goal)
21. Make the dialect grammar honest: delete OR genuinely build+wire the orphaned 2002/2014/2023 grammars.
22. Generic registration-based statement dispatch (one registration, not ~7 manual edits).
23. `DialectMode` scalar → feature/capability set; gate intrinsics/free-form consistently; add strict-mode
    rejection tests.
24. Prove the seam with ONE thin vertical slice (a 2002 intrinsic, fully gated, bind+emit+runtime+test).

### P6 — Scale (true commercial file handling)
25. On-disk indexed/relative engine + external merge sort behind `IFileHandler` (keep the status-code logic).

### Feature completion (the remaining in-scope NIST tests — all diagnosed high-confidence)
| Test | Status | Cluster | Root cause (one-line) |
|------|--------|---------|------------------------|
| RL106A | 2/4 FAIL* | rel-rewrite-varying | Relative handler registered with MIN (56) not MAX (102) record size for Format-2 multi-01 varying → LONG records truncated. Fix `Binder.cs` recordLength = max for varying. |
| RL119A | 0/1 FAIL* | rel-status-code | `RelativeFileHandler.Open` returns 00 (silently creates) on missing non-OPTIONAL I-O; must return 35 like Sequential/Indexed already do. |
| RL205A | 57/67 FAIL* | rel-start-not-invalid-key | START failure handling (see full diagnosis). |
| RL213A | 1/21 FAIL* | external-naming | EXTERNAL naming/registration. |
| RL111A | CRASH | dispatch-recursion | `D-CLOSE-FILES` infinite re-dispatch → StackOverflow (also a P2 codegen item). |
| RL208A | 9/11 FAIL* | implicit-close-no-persist | Implicit close / persistence in the delete-update chain. |
| IC233A/234A | COMPILE_FAIL | global-file-inherit | Contained program OPENs a containing program's `FD … GLOBAL` file; needs GLOBAL FILE inheritance (extend DEVLOG-236 GLOBAL-data to FILE SECTION) + USE GLOBAL declarative. |
| IC227A | 16/23 FAIL* | external-file | EXTERNAL **file** semantics. |
| IC235A | COMPILE_FAIL | nested-program-scope | EXTERNAL data multi-program naming conflict. |
| IC114A | 1/2 FAIL* | subprogram-file-not-registered | Subprogram's file not registered for the CALL chain. |

Deferred-optional (owner decision DEVLOG 301): **DB** (Debug) + **RW** (Report Writer). Excluded
out-of-scope obsolete: **CM, SG, OBNC, OBIC, EXEC**.

---
*Full evidence: the two workflow outputs (10-dim audit + 11-test diagnosis), each with file:line citations,
were the basis for this document. Re-run via the `arch-assessment-cobol85` / `diagnose-remaining-cobol85-nist`
workflow scripts.*
