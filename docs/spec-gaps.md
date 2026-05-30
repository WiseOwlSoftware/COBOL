# CobolSharp — Spec-Conformance Gap Audit

_2026-05-30. Method: empirical — each item probed with a minimal program (compile + run + inspect
output), not inferred from diagnostic strings or comments. Several long-standing "not supported"
impressions turned out to be **stale** (the feature works; only an old parse-error hint remains).
Corrected after end-to-end verification (see DEVLOG 228 + the 229 correction). Tags: **VERIFIED-WORKS**,
**OPEN BUG**, **IN PROGRESS**, **STUB/LIMITATION**, **CLEANUP**._

Repro programs: `/e/tmp/repro/`, `/e/tmp/wct/`.

## Honest headline
Of three originally-suspected silent-correctness bugs: **one was never a bug** (ON SIZE ERROR),
**one is a real open bug** (LENGTH of an OCCURS DEPENDING ON group), and **one is real and only
partially addressed** (WHEN-COMPILED — emitter special-case added but not yet effective on the MOVE
path). Net: **no silent bug from this sweep is fully fixed yet.** Guard stays ALL GREEN
(1000 / 343 / 148) because these are latent gaps with no baselined coverage.

---

## 1. Silent-correctness bugs (compile + run + wrong output, no diagnostic)

| # | Area | Status | Notes |
|---|------|--------|-------|
| 1 | `FUNCTION WHEN-COMPILED` | **IN PROGRESS — not effective** | Must return compile time; still returns the execution-time clock for `MOVE FUNCTION WHEN-COMPILED`. A baked-constant special-case was added to `CilExpressionEmitter.EmitIrIntrinsicCall` (correct, in HEAD) but the value reaches the program via a path that bypasses it (the `IrFunctionCall` MOVE lowering). Proven: same DLL, two runs, different timestamps. **The `IntrinsicWhenCompiledTests` test does NOT guard this — it checks only format+date.** Next: trace `DataMovementLowerer`→`EmitFunctionCall`→`EmitIrIntrinsicCall` and ensure WHEN-COMPILED is baked on every path (or fold to a constant in the binder). |
| 2 | `ON SIZE ERROR` (all arithmetic) | **VERIFIED-WORKS** | Fires for DIVIDE-by-0 and ADD/SUBTRACT/MULTIPLY/COMPUTE overflow (incl. `… GIVING`). The "missing" impression came from dead code (`CobolProgram.DivideInto`, no IR caller) + a wrong test expectation (SUBTRACT into an *unsigned* receiver stores the absolute value per ISO → no size error). |
| 3 | `LENGTH` / `FUNCTION LENGTH` of an OCCURS DEPENDING ON group | **OPEN BUG** | Returns the **max** layout, not the current DEPENDING-ON length: `FUNCTION LENGTH(TBL)` over `ELT … OCCURS 1 TO 10 DEPENDING ON N` returns 40 for both N=3 and N=7 (spec: 12, 28). Root cause: `ExpressionBinder.BindLength`/`StaticLength` fold to compile-time `Symbol.ElementSize` with no ODO branch. Fix: compute base + dependingOnValue×elementSize at runtime for an ODO group (and `LENGTH OF`). |

---

## 2. Features previously believed unsupported — VERIFIED-WORKS (diagnostics are stale)

Each compiled and produced correct output:

| Feature | Probe result |
|---------|--------------|
| `PERFORM VARYING … AFTER` (nested varying) | 2×3 loop → count 6 ✓ |
| `INSPECT … CONVERTING` | "abcde" CONVERTING "abc"→"ABC" → "ABCDE" ✓ |
| Abbreviated combined condition (`IF X = 4 OR 5`) | matches ✓ |
| Multi-target `SET … TO TRUE` | both 88s set ✓ |
| `OCCURS … DEPENDING ON` (variable-length table) | compiles + runs ✓ (but `LENGTH` of the group is wrong — see #3) |
| `DIVIDE … GIVING … REMAINDER` | 17 BY 5 → Q=3 R=2 ✓ |
| `FUNCTION REM` / `FUNCTION MOD` | REM(17,5)=2, MOD(−17,5)=3 ✓ |

→ **CLEANUP**: the stale "not yet supported" hints in `CobolErrorStrategy.cs` /
`DiagnosticDescriptors.cs` (COBOL0100/0104/0105/0106/0393/0395/0433 …) only surface on a parse error,
so they don't affect successful compiles — but they misrepresent the compiler's capabilities. Remove
or re-scope to the specific unparsed variants (if any) that still fail.

---

## 3. Genuine stubs / limitations (honest — surface works, full semantics do not)

| Area | Status | Detail |
|------|--------|--------|
| **NATIONAL (PIC N / national)** | **STUB** | ASCII-backed approximation: `PIC N(3) VALUE N"ABC"` stores/DISPLAYs "ABC", but `PicRuntime.CompareNational` delegates to alphanumeric (no UTF-16; `PicRuntime.cs:1287`), and `DISPLAY-OF`/`NATIONAL-OF`/`CHAR-NATIONAL` are identity pass-throughs (`IntrinsicFunctions.cs:648-662`). No NIST coverage. |
| **SORT / MERGE engine** | **LIMITATION** | In-memory only (`SortRuntime.cs:10`). Correct + spec-allowed; would OOM on very large files. External merge-sort deferred. |
| **Screen Section / CURSOR / terminal I/O** | **STUB** | `TerminalSession`, `ScreenAttributeMapper`, `CursorCodec` are explicit placeholders (M429/M431). |
| **CHAR-NATIONAL collating** | **DEFERRED** | National program-collating path intentionally native (no NIST coverage); see collating subsystem (DEVLOG 224–227). |

---

## 4. Dead code to remove (not spec bugs; zero-dead-code doctrine)

| Location | Why dead |
|----------|----------|
| `CobolProgram.cs` arithmetic helpers (`DivideInto`, `DivideGiving`, `MultiplyBy`, …) | Legacy; live arithmetic goes through IR + `PicRuntime`. No IR caller. (False lead for bug #2.) |
| `CilEmitter.EmitRuntimeCall` DISPLAY = `Console.WriteLine("statement executed")` (`CilEmitter.cs:1193`) | Old stub; real DISPLAY lowers elsewhere. |
| `DiagnosticDescriptors` COBOL0467 ("user CLASS condition … evaluates to false") | No caller; live `EmitUserClassCondition`→`IsInUserClass` works. |

---

## Recommended priority
1. **Finish WHEN-COMPILED (#1)** — small once the MOVE path is traced; add a cross-run-stability test.
2. **ODO-group LENGTH (#3)** — runtime length computation; moderate.
3. **CLEANUP** (§2 stale diagnostics, §4 dead code) — low risk, high clarity.
4. **NATIONAL / SORT-external / Screen I/O** — feature work, schedule by need.

_§2, §3, §4 and item #2/#3 in §1 are empirically verified. Item #1 (WHEN-COMPILED) is confirmed
still-broken. Guard ALL GREEN throughout (1000 / 343 / 148)._
