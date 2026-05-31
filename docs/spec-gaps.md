# CobolSharp — Spec-Conformance Gap Audit

_2026-05-30. Method: empirical — each item probed with a minimal program (compile + run + inspect
output), not inferred from diagnostic strings or comments. Several long-standing "not supported"
impressions turned out to be **stale** (the feature works; only an old parse-error hint remains).
Corrected after end-to-end verification (see DEVLOG 228 + the 229 correction). Tags: **VERIFIED-WORKS**,
**OPEN BUG**, **IN PROGRESS**, **STUB/LIMITATION**, **CLEANUP**._

Repro programs: `/e/tmp/repro/`, `/e/tmp/wct/`.

## Honest headline
All three originally-suspected silent-correctness bugs are now resolved: **one was never a bug**
(ON SIZE ERROR), and **two are fixed and verified** — WHEN-COMPILED (baked at compile time;
cross-run stable) and LENGTH of an OCCURS DEPENDING ON group (now computed at runtime from the
current depending-on value, DEVLOG 231). Guard stays ALL GREEN (1000 / 345 / 148).

> Note: the `IntrinsicWhenCompiledTests` integration test checks format + compile-date only. The
> compile-time-baked (cross-run-stable) property is verified manually (DEVLOG 230) — running the same
> compiled DLL twice yields an identical timestamp — because a test can't easily assert "compiled
> earlier than now" deterministically.

---

## 1. Silent-correctness bugs (compile + run + wrong output, no diagnostic)

| # | Area | Status | Notes |
|---|------|--------|-------|
| 1 | `FUNCTION WHEN-COMPILED` | **FIXED & VERIFIED** (commit 00713a9, DEVLOG 228/230) | Now baked at compile time: `CilExpressionEmitter.EmitIrIntrinsicCall` special-cases WHEN-COMPILED and emits `Ldstr` of a constant captured once at compiler-process start. Effective on the MOVE path (`EmitFunctionCall`→`EmitIrIntrinsicCall`). Verified cross-run stable: the same DLL run twice prints an identical timestamp. (An intermediate DEVLOG-229 claim that it was "still broken" was a test-harness error — stale DLL + path race — corrected in DEVLOG 230.) |
| 2 | `ON SIZE ERROR` (all arithmetic) | **VERIFIED-WORKS** | Fires for DIVIDE-by-0 and ADD/SUBTRACT/MULTIPLY/COMPUTE overflow (incl. `… GIVING`). The "missing" impression came from dead code (`CobolProgram.DivideInto`, no IR caller) + a wrong test expectation (SUBTRACT into an *unsigned* receiver stores the absolute value per ISO → no size error). |
| 3 | `FUNCTION LENGTH` of an OCCURS DEPENDING ON group | **FIXED & VERIFIED** (DEVLOG 231) | Was returning the **max** layout (40 for both N=3 and N=7); now returns the current DEPENDING-ON length (12, 28). `ExpressionBinder.BuildVariableLengthExpression` emits a runtime expression `maxLength − Σ (maxOccurs − dependingValue) × repetition × elementSize` when the argument has a subordinate ODO table (ISO §15.50.4 rule 4(b)/rule 7); the repetition factor handles a variable table nested inside fixed OCCURS levels. Guarded by the `Function_Length_OdoGroup_*` tests. (No `LENGTH OF` special register exists in the grammar — only `FUNCTION LENGTH` — so that form is out of scope.) |

---

## 2. Features previously believed unsupported — VERIFIED-WORKS (diagnostics are stale)

Each compiled and produced correct output:

| Feature | Probe result |
|---------|--------------|
| `PERFORM VARYING … AFTER` (nested varying) | 2×3 loop → count 6 ✓ |
| `INSPECT … CONVERTING` | "abcde" CONVERTING "abc"→"ABC" → "ABCDE" ✓ |
| Abbreviated combined condition (`IF X = 4 OR 5`) | matches ✓ |
| Multi-target `SET … TO TRUE` | both 88s set ✓ |
| `OCCURS … DEPENDING ON` (variable-length table) | compiles + runs ✓ (`LENGTH` of the group now correct — see #3) |
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
1. **CLEANUP** (§2 stale diagnostics, §4 dead code) — low risk, high clarity.
2. **NATIONAL / SORT-external / Screen I/O** — feature work, schedule by need.

_All §1 silent-correctness items are resolved (WHEN-COMPILED and ODO-group LENGTH fixed & verified;
ON SIZE ERROR was never broken). §2, §3, §4 are empirically verified. Guard ALL GREEN throughout
(1000 / 345 / 148)._
