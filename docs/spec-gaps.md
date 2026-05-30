# CobolSharp — Spec-Conformance Gap Audit

_2026-05-30. Method: empirical — each item was probed with a minimal program (compile + run +
inspect output), NOT inferred from diagnostic strings or comments. Several long-standing "not
supported" impressions turned out to be **stale** (the feature works; only an old parse-error hint
remains). Items are tagged **VERIFIED-WORKS**, **BUG**, **STUB/LIMITATION**, or **CLEANUP**._

Repro programs: `/e/tmp/repro/` and `/e/tmp/repro/feat/`.

## Headline outcome
Of three originally-suspected silent-correctness bugs, **two were not bugs** (verified correct) and
**one was real and is now fixed**. The compiler matches the spec considerably better than its own
diagnostic strings suggest.

---

## 1. Silent-correctness bugs (compile + run + wrong output, no diagnostic)

| # | Area | Status | Notes |
|---|------|--------|-------|
| 1 | `FUNCTION WHEN-COMPILED` | **BUG — FIXED** (DEVLOG 228, commit cc05b45) | Returned the execution-time clock; now baked as a compile-time constant at emit. |
| 2 | `ON SIZE ERROR` (all arithmetic) | **VERIFIED-WORKS** | Fires for DIVIDE-by-0, ADD/SUBTRACT/MULTIPLY/COMPUTE overflow. The "missing" impression came from dead code (`CobolProgram.DivideInto`, no IR caller) + a wrong test expectation (SUBTRACT into an *unsigned* receiver stores the absolute value per ISO → no size error). |
| 3 | `LENGTH`/`FUNCTION LENGTH` of an OCCURS DEPENDING ON group | **VERIFIED-WORKS** | Returns the *current* DEPENDING-ON length at runtime (N=3 → 12, N=7 → 28), not the max layout. (A 2026-05-29 note claiming "returns max" is stale — fixed by the DEVLOG-223 runtime-LENGTH work.) |

**No other silent-correctness bug found in this sweep.**

---

## 2. Features previously believed unsupported — VERIFIED-WORKS (diagnostics are stale)

Each compiled and produced correct output:

| Feature | Probe result |
|---------|--------------|
| `PERFORM VARYING … AFTER` (nested varying) | 2×3 loop → count 6 ✓ |
| `INSPECT … CONVERTING` | "abcde" CONVERTING "abc"→"ABC" → "ABCDE" ✓ |
| Abbreviated combined condition (`IF X = 4 OR 5`) | matches ✓ |
| Multi-target `SET … TO TRUE` (`SET A-ON B-ON TO TRUE`) | both set ✓ |
| `OCCURS … DEPENDING ON` (variable-length table) | compiles + LENGTH tracks the depending item ✓ |
| `DIVIDE … GIVING … REMAINDER` | 17 BY 5 → Q=3 R=2 ✓ |
| `FUNCTION REM` / `FUNCTION MOD` | REM(17,5)=2, MOD(−17,5)=3 ✓ |

→ **CLEANUP**: the stale "not yet supported" hints for these in `CobolErrorStrategy.cs` and
`DiagnosticDescriptors.cs` (COBOL0100/0104/0105/0106/0393/0395/0433 …) are misleading and should be
removed or re-scoped to the specific unparsed variants (if any) that still fail. They only surface on
a parse error, so they do not affect successful compiles — but they misrepresent the compiler's
capabilities to users and to audits like this one.

---

## 3. Genuine stubs / limitations (honest — surface works, full semantics do not)

| Area | Status | Detail |
|------|--------|--------|
| **NATIONAL (PIC N / national)** | **STUB** | An ASCII-backed approximation: a `PIC N(3) VALUE N"ABC"` stores/DISPLAYs "ABC", but `PicRuntime.CompareNational` delegates to alphanumeric (no UTF-16 2-byte semantics; `PicRuntime.cs:1287`), and `DISPLAY-OF` / `NATIONAL-OF` (`IntrinsicFunctions.cs:648-662`) are identity pass-throughs. True national support (2-byte chars, national collation, `CHAR-NATIONAL`) is absent. No NIST coverage. |
| **SORT / MERGE engine** | **LIMITATION** | In-memory only (`SortRuntime.cs:10`): all records held in a `List<byte[]>`. Correct + spec-allowed; would OOM on very large files. External merge-sort deferred. |
| **Screen Section / CURSOR / terminal I/O** | **STUB** | `TerminalSession`, `ScreenAttributeMapper`, `CursorCodec` are explicit placeholders (M429/M431 not built). |
| **CHAR-NATIONAL collating** | **DEFERRED** | National program-collating-sequence path intentionally left native (no NIST coverage); see the collating subsystem (DEVLOG 224–227). |

---

## 4. Dead code to remove (not spec bugs; zero-dead-code doctrine)

| Location | Why dead |
|----------|----------|
| `CobolProgram.cs` arithmetic helpers (`DivideInto`, `DivideGiving`, `MultiplyBy`, …) | Legacy class; live arithmetic goes through IR + `PicRuntime`. No IR emits calls to `CobolProgram.*`. Their silent `/0` no-op was the false lead for bug #1. |
| `CilEmitter.EmitRuntimeCall` DISPLAY = `Console.WriteLine("statement executed")` (`CilEmitter.cs:1193`) | Old stub; real DISPLAY lowers elsewhere. |
| `DiagnosticDescriptors` COBOL0467 ("user CLASS condition … evaluates to false") | No caller; the live `EmitUserClassCondition` → `IsInUserClass` path works. |

---

## Recommended priority
1. **CLEANUP (low risk, high clarity):** delete/re-scope the stale "not supported" diagnostics (§2)
   and the dead code (§4). These actively mislead.
2. **NATIONAL (§3):** the largest genuine spec gap; sizable (real 2-byte data type + collation).
   Only worth it if national-character programs are in scope — no NIST suite exercises it.
3. **SORT external merge / Screen I/O:** feature work, not correctness; schedule by need.

_All §1–§4 claims are empirically verified except where marked. Guard remained ALL GREEN
(1000 unit / 343 integration / 148 NIST) throughout; the only code change from this audit is the
WHEN-COMPILED fix (DEVLOG 228)._
