CobolSharp Error Model — Declaratives, ExceptionState, USE AFTER & Structured Recovery Architecture (CIL‑Only)
=============================================================================================================

> **STATUS** — The authoritative reference for CobolSharp's declaratives / USE / structured-error
> subsystem: `DECLARATIVES`, `USE` statements, `ExceptionState` propagation, statement-level handlers
> (`ON EXCEPTION` / `ON SIZE ERROR` / `ON OVERFLOW`), error routing, and structured recovery.
>
> **ACTUAL implementation status (verify against `src/` before relying on any claim below):**
> - **IMPLEMENTED (~80–90%) — the file-I/O declarative subsystem.** `DECLARATIVES`…`END DECLARATIVES`,
>   `USE [GLOBAL] BEFORE REPORTING`, and `USE [GLOBAL] AFTER STANDARD (EXCEPTION|ERROR) PROCEDURE ON <target>`
>   parse (grammar `Core/CobolControlFlow.g4` `useStatement`) and lower for file operations. Routing covers
>   per-file-name and per-open-mode (INPUT/OUTPUT/I-O/EXTEND) scopes, the local-handler-takes-precedence gate
>   (`FileRuntime.ShouldRunUseDeclarative`, excluding AT END / INVALID KEY when the statement serviced them),
>   declarative-as-PERFORM execution with re-entrancy guarding, and **cross-program GLOBAL** declarative dispatch
>   via `CobolSharp.Runtime.GlobalUseDeclarativeRegistry` (ISO §14.9.49.4 GR4 / §8.4.6.2.2; NIST IC233A/234A,
>   RL111A re-entrancy §14.9.49.4 GR2). Lowering lives in `CodeGen/Lowering/FileIoLowerer.cs`
>   (`EmitUseDeclarative` / `EmitPerformDeclarativeSection`) + `IrCheckUseDeclarative` /
>   `FileRuntime.EnterUseDeclarative`/`ExitUseDeclarative`.
> - **IMPLEMENTED — statement-level handlers** `ON SIZE ERROR` / `NOT ON SIZE ERROR`, `ON OVERFLOW`,
>   `ON EXCEPTION` / `INVALID KEY` / `AT END` on their applicable statements (these are the COBOL-85 surface,
>   part of M1-complete).
> - **DESIGN-ONLY (Phase C) — the full ISO-2023 structured-exception (EC) model:** there is no implemented
>   `EC-…` exception-condition framework, no `>>TURN`/`>>CHECKING`, no `EXCEPTION-OBJECT`, and no
>   `RAISE`/`RESUME` statement. `USE AFTER STANDARD EXCEPTION` over arbitrary .NET exceptions, and the
>   JSON/XML/SORT/Report-Writer declarative routing described in §6–§8, are aspirational target designs
>   (JSON/XML PARSE/GENERATE itself is design-only — Phase C). Treat those sections as the intended end state,
>   not current behavior.
>
> **Stack:** .NET 10 / C# 14. **Backend:** CIL-only via Mono.Cecil (no custom VM / no bytecode interpreter;
> a Roslyn C# backend is a future *additive* Stage-5 option with Cecil as the oracle). Debugger/PDB visibility
> described here is itself design-only (Phase E).
>
> **Authoritative plan/SSOT:** `docs/MASTER_PLAN.md` (full ISO-2023 EC/exception model = its Phase C, M2-PROC-4).
> **Doctrine:** `PROMPT.md`. **Spec:** `specs/ISO_COBOL.md`.

Purpose
-------
Define the authoritative architecture for:
- Declaratives (USE BEFORE REPORTING / USE AFTER ERROR / USE AFTER EXCEPTION / USE AFTER STANDARD EXCEPTION)
- ExceptionState propagation
- ON EXCEPTION / ON SIZE ERROR / ON OVERFLOW
- File I/O error routing
- JSON/XML error routing
- SORT error routing
- Report Writer error routing
- Standard exception handling
- Structured recovery and resumption
- Declarative activation, scoping, re-entry and re-execution rules
- CIL‑friendly lowering
- Debugger visibility and stepping
- AOT/WASM‑safe error semantics

This document governs how CobolSharp implements COBOL’s structured error‑handling model.

------------------------------------------------------------
SECTION 1 — ERROR MODEL OVERVIEW
------------------------------------------------------------

CobolSharp error handling consists of:
1. **Statement‑level handlers**
   - ON EXCEPTION
   - ON SIZE ERROR
   - ON OVERFLOW
   (Local handlers always take precedence over declaratives — see §5 routing.)

2. **Declaratives**
   - USE BEFORE REPORTING  (Report Writer; planned)
   - USE AFTER ERROR ON file
   - USE AFTER EXCEPTION ON file
   - USE AFTER EXCEPTION ON json/xml  (design-only, Phase C)
   - USE AFTER EXCEPTION ON REPORT    (design-only, Phase C)
   - USE AFTER STANDARD EXCEPTION

3. **ExceptionState**
   - Centralized error record
   - Populated by subsystems

4. **Structured recovery**
   - Declarative runs
   - Control returns to the statement after the failing statement (unless GO TO used)
   - PERFORM stack preserved

5. **No unwinding unless fatal**
   - Declaratives do not unwind the CALL stack
   - Declaratives do not unwind the PERFORM stack
   - Only STOP RUN terminates execution

------------------------------------------------------------
SECTION 1A — WHAT DECLARATIVES ARE
------------------------------------------------------------

Declaratives are special PROCEDURE DIVISION sections that:
- Are invoked automatically when certain events occur, outside normal control flow
- Resume execution after the failing statement (unless GO TO used)
- Have access to full program state

Declaratives:
- Do NOT execute during normal program flow
- Cannot be PERFORMed from normal code, and are not GO TO targets from normal code
- Cannot contain ENTRY statements
- Are parsed before the main PROCEDURE DIVISION and registered, but not executed unless triggered

------------------------------------------------------------
SECTION 2 — EXCEPTIONSTATE ARCHITECTURE
------------------------------------------------------------

2.1 Fields
----------
ExceptionState contains:
- Category
  (SIZE ERROR, OVERFLOW, I/O ERROR, JSON ERROR, XML ERROR, SORT ERROR, REPORT WRITER ERROR, .NET EXCEPTION)
- Message
- Source subsystem
- File name (I/O)
- Key value (indexed)
- JSON/XML token / error metadata
- Report Writer error metadata
- Raw .NET exception (optional)
- Stack trace (optional)
- Severity

2.2 Lifecycle
-------------
ExceptionState is:
- Populated on error by the originating runtime engine
- Passed to ON EXCEPTION or the selected declarative
- Cleared after the handler (ON EXCEPTION block or declarative) completes

2.3 Severity levels
-------------------
- Recoverable
- Non‑recoverable
- Fatal

Only fatal errors terminate execution.

------------------------------------------------------------
SECTION 3 — STATEMENT‑LEVEL HANDLERS
------------------------------------------------------------

3.1 ON EXCEPTION
----------------
Applies to:
- CALL
- INVOKE
- JSON/XML GENERATE
- SORT
- STRING/UNSTRING
- File I/O

Behavior:
- If an error occurs → execute ON EXCEPTION block; skip NOT ON EXCEPTION block.
- If no error → execute NOT ON EXCEPTION block (where present).

3.2 ON SIZE ERROR
-----------------
Applies to:
- Arithmetic (ADD/SUBTRACT/MULTIPLY/DIVIDE/COMPUTE)
- MOVE to numeric
- COMP/COMP‑3/COMP‑5 overflow

Behavior:
- Target not modified
- ON SIZE ERROR block executed

3.3 ON OVERFLOW
---------------
Applies to:
- STRING
- UNSTRING
- (and MOVE overflow paths)

Behavior:
- Target not modified
- ON OVERFLOW block executed

------------------------------------------------------------
SECTION 4 — DECLARATIVES ARCHITECTURE
------------------------------------------------------------

4.1 Declarative structure
-------------------------
DECLARATIVES.
    Section‑Name SECTION.
        USE AFTER ERROR ON fileName.
        Procedure‑1.
    Another‑Section SECTION.
        USE AFTER EXCEPTION ON JSON.
        Procedure‑2.
END DECLARATIVES.

Each declarative section has a SECTION header, a single USE statement, and one or more paragraphs.

4.2 Declarative types
---------------------
- USE BEFORE REPORTING (Report Writer; planned)
- USE AFTER ERROR ON file
- USE AFTER EXCEPTION ON file
- USE AFTER EXCEPTION ON JSON  (design-only)
- USE AFTER EXCEPTION ON XML   (design-only)
- USE AFTER EXCEPTION ON REPORT (design-only)
- USE AFTER STANDARD EXCEPTION

4.3 Triggering rules
--------------------
A declarative is triggered when:
- No applicable statement‑level handler is present, OR
- The statement‑level handler does not handle the error category.

(In file I/O, AT END and INVALID KEY serviced locally suppress the declarative — encoded by the
`excludeAtEnd` / `excludeInvalidKey` flags passed to `FileRuntime.ShouldRunUseDeclarative`.)

4.4 Execution model
-------------------
- Declarative runs like a PERFORM, on its own PERFORM frame.
- After completion → return to the statement after the failing statement (unless GO TO used).
- PERFORM stack preserved; CALL stack preserved.
- The failing statement is NOT re-executed (no retry semantics — see §9.3).

4.5 Declarative priority (when multiple match)
----------------------------------------------
1. File‑specific declarative (USE … ON file-name)
2. JSON/XML declarative (design-only)
3. Report Writer declarative (design-only)
4. Standard exception declarative (USE AFTER STANDARD EXCEPTION)

------------------------------------------------------------
SECTION 4B — DECLARATIVE ACTIVATION & SCOPING RULES
------------------------------------------------------------

4B.1 Declaratives are global within their program
- Apply to the entire program: all paragraphs/sections, all file operations,
  and (per target design) all JSON/XML/Report-Writer operations.

4B.2 One DECLARATIVES block per program
- Declaratives cannot be nested as a block; only one DECLARATIVES region is allowed per program.

4B.3 Declaratives in nested programs
- A nested program's declaratives apply only to that program and do not affect the outer program,
  EXCEPT for `USE GLOBAL` declaratives, which a containing program registers for dispatch on behalf of
  contained programs that lack their own applicable declarative (see §5B, GlobalUseDeclarativeRegistry).

4B.4 Inter-declarative calls
- A declarative may PERFORM paragraphs inside its own declarative section.
- Declaratives do not PERFORM each other directly; if multiple USE statements match, the most specific
  one runs (§4.5) and the others are ignored.

------------------------------------------------------------
SECTION 5 — FILE I/O ERROR ROUTING
------------------------------------------------------------

5.1 File status codes (examples)
--------------------------------
- "10" end of file
- "21" key invalid (sequence error)
- "22" duplicate key
- "23" key not found / record not found
- "30" permanent error
- "34" boundary violation
- "35" file not found
- "90"/"92" logic / implementor-defined error

(Full file-status semantics are owned by the File-I/O subsystem; see the file-I/O architecture docs
and `src/CobolSharp.Runtime/FileRuntime.cs`.)

5.2 Routing order (most-specific first)
---------------------------------------
On a failing READ/WRITE/REWRITE/DELETE/START/OPEN/CLOSE:
1. INVALID KEY / AT END (local, on the statement)
2. SIZE ERROR (local — not applicable to most file ops)
3. ON EXCEPTION (local)
4. USE AFTER EXCEPTION ON file-name (declarative)
5. USE AFTER ERROR ON file-name (declarative)
6. USE AFTER STANDARD EXCEPTION (declarative)
7. Continue with ExceptionState set / runtime exception propagation

If a local handler services the condition, the declarative is NOT invoked.

------------------------------------------------------------
SECTION 5B — CROSS-PROGRAM (GLOBAL) USE DECLARATIVE DISPATCH
------------------------------------------------------------

`USE GLOBAL AFTER STANDARD (ERROR|EXCEPTION)` declaratives are dispatched across the nested-program
boundary by `CobolSharp.Runtime.GlobalUseDeclarativeRegistry` (ISO §14.9.49.4 GR4 / §8.4.6.2.2):

- A containing program that declares a `USE GLOBAL` declarative registers a handler delegate
  (`RegisterForMode(scope, handler)` for open-mode scopes 0/1/2/3 = INPUT/OUTPUT/I-O/EXTEND, or
  `RegisterForFile(fileName, handler)` for file-name scope, encoded as scope = -1).
- When an I/O exception arises during a statement in a CONTAINED program that has no applicable USE
  declarative of its own, the contained program calls `GlobalUseDeclarativeRegistry.Dispatch(...)`.
  Dispatch re-applies the same `FileRuntime.ShouldRunUseDeclarative` gate (excluding AT END / INVALID KEY
  the contained statement already serviced), then runs the containing program's registered handler.
- All programs of one compilation share a single .NET assembly, so the delegate is a static method on the
  containing program's type and runs against the containing program's live, on-stack ProgramState. GLOBAL
  files are shared by name, so `FileRuntime`'s name-keyed status is already visible to both programs.
- `Clear()` is called at run-unit start. File-name-scoped handlers take precedence over open-mode-scoped
  handlers for the same operation.

A re-entrancy guard (`FileRuntime.EnterUseDeclarative` / `ExitUseDeclarative`) prevents a USE declarative
from recursively re-entering itself on an I/O error it itself raises (ISO §14.9.49.4 GR2; NIST RL111A).

------------------------------------------------------------
SECTION 6 — JSON/XML ERROR ROUTING  (design-only, Phase C)
------------------------------------------------------------

6.1 JSON errors
---------------
- Invalid token / unexpected type / overflow / missing field / encoding error

6.2 XML errors
--------------
- Invalid element / namespace mismatch / attribute error / encoding error

6.3 Routing
-----------
1. ON EXCEPTION (local, on the JSON/XML PARSE/GENERATE statement)
2. USE AFTER EXCEPTION ON JSON / ON XML (declarative)
3. USE AFTER STANDARD EXCEPTION (declarative)

> Implementation note: JSON/XML PARSE/GENERATE lowering+runtime is itself Phase C (design-only); this
> routing is the intended end-state, not current behavior.

------------------------------------------------------------
SECTION 7 — SORT ERROR ROUTING
------------------------------------------------------------

7.1 SORT errors
---------------
- File I/O error / key extraction error / collation error / merge failure

7.2 Routing
-----------
1. ON EXCEPTION
2. USE AFTER STANDARD EXCEPTION

------------------------------------------------------------
SECTION 7B — REPORT WRITER ERROR ROUTING  (design-only, Phase C)
------------------------------------------------------------

7B.1 USE AFTER EXCEPTION ON REPORT
- Triggered by page-overflow errors, invalid LINE/COLUMN, invalid control-break state, output file errors.

7B.2 USE BEFORE REPORTING
- A pre-write declarative hook for Report Writer detail groups (planned).

7B.3 Routing priority
- Same shape as JSON/XML: ON EXCEPTION → REPORT declarative → USE AFTER STANDARD EXCEPTION.

> Report Writer itself is implemented (see `docs/REPORT_WRITER_CONTROL_DESIGN.md`); its *declarative*
> exception routing is the design-only part.

------------------------------------------------------------
SECTION 8 — STANDARD EXCEPTION HANDLING  (partly design-only)
------------------------------------------------------------

8.1 Triggered by:
-----------------
- .NET exceptions not mapped to a specific COBOL exception
- Runtime errors / invalid PIC / invalid COMP‑3 nibble
- DISPLAY → NATIONAL non‑ASCII; NATIONAL truncation of a surrogate pair
- Object reference null; INVOKE failure
- Out‑of‑range index; arithmetic overflow not caught by SIZE ERROR
- JSON/XML parse errors with no specific declarative

8.2 Routing
-----------
1. ON EXCEPTION (local)
2. USE AFTER STANDARD EXCEPTION (declarative)

8.3 Lowering shape
------------------
try { operation } catch (Exception ex) { ctx.ExceptionState = …; dispatch declarative; }
The handler executes the declarative and returns control to the statement after the failing operation
(it does NOT re‑execute the failing statement).

> The file-I/O slice of this is implemented. Catching *arbitrary* .NET exceptions and routing them to
> `USE AFTER STANDARD EXCEPTION` (the general structured-exception net) is design-only (Phase C); it pairs
> with the ISO-2023 exception-condition (EC) model, which is not yet implemented.

------------------------------------------------------------
SECTION 9 — STRUCTURED RECOVERY MODEL
------------------------------------------------------------

9.1 Declarative execution
-------------------------
Declarative runs as a PERFORM, with its own PERFORM frame, without unwinding the caller.
ExecutionContext saves the current execution point (call stack + PERFORM stack), switches to the
declarative handler, executes it, then restores and continues. Declaratives do NOT unwind the
PERFORM stack (unlike EXIT PROGRAM / STOP RUN) and do NOT change normal paragraph flow.

9.2 Resumption
--------------
After the declarative: execution resumes at the statement after the failing statement, and
ExceptionState is cleared.

9.3 No retry semantics
----------------------
Declaratives do not retry the failing statement.

9.4 No unwinding
----------------
Declaratives do not unwind the CALL stack or the PERFORM stack.

9.5 GO TO / PERFORM inside a declarative
----------------------------------------
- GO TO inside a declarative transfers control permanently and skips the return to the failing statement.
- PERFORM inside a declarative uses a separate PERFORM stack frame (PERFORM VARYING included).

------------------------------------------------------------
SECTION 10 — CIL LOWERING RULES
------------------------------------------------------------

10.1 Statement‑level handler lowering
-------------------------------------
The compiler generates:
    try { /* operation */ }
    catch { ExceptionState = …; goto onExceptionBlock; }

10.2 Declarative registration & dispatch
----------------------------------------
- At program startup, declarative sections are registered (per-program for local USE, and via
  `GlobalUseDeclarativeRegistry` for `USE GLOBAL`).
- File operations emit an `IrCheckUseDeclarative(fileName, scope, cond, excludeAtEnd, excludeInvalidKey)`
  guard (see `FileIoLowerer.EmitUseDeclarative`); when the gate fires, the lowering emits
  `FileRuntime.EnterUseDeclarative` → PERFORM of the declarative section paragraphs →
  `FileRuntime.ExitUseDeclarative`.
- File-operation try/catch wraps file ops; (per target design) JSON/XML/arithmetic ops likewise.

10.3 Resumption lowering
------------------------
After the declarative runs (no GO TO), the compiler branches to the continuation label after the
failing statement; ExceptionState is cleared.

10.4 No re‑execution of the failing statement
---------------------------------------------
The handler returns to the next instruction, never to the failing instruction.

------------------------------------------------------------
SECTION 11 — DEBUGGER INTEGRATION  (design-only, Phase E)
------------------------------------------------------------

The debugger surface (design-only) shows:
- Declarative section name and the USE condition that triggered it
- ExceptionState contents (category, message, file status, key)
- File status codes; JSON/XML token / error details; Report Writer error details
- Call stack and PERFORM stack before/after the handler
- The failing statement, the resume location, and any GO TO transitions

Sequence points are emitted for: declarative entry, declarative exit, and the USE statement.

------------------------------------------------------------
SECTION 12 — AOT/WASM‑SAFE ERROR MODEL
------------------------------------------------------------

12.1 No reflection — exception types mapped statically.
12.2 No dynamic codegen — all handlers compiled statically (CIL via Mono.Cecil).
12.3 Deterministic behavior — error routing identical across CoreCLR, AOT, and WASM.

------------------------------------------------------------
SECTION 13 — EDGE‑CASE BEHAVIOR
------------------------------------------------------------

13.1 Declarative triggers inside a declarative
- Allowed; nested declaratives run. File I/O inside a declarative may trigger declaratives recursively;
  CobolSharp bounds the recursion (re-entrancy guard + runtime-limited depth) to prevent infinite loops.

13.2 ON EXCEPTION inside a declarative — allowed.

13.3 Declarative modifies the failing record — allowed.

13.4 Declarative triggers STOP RUN — terminates the entire run unit.

13.5 Declarative triggers EXIT PROGRAM — terminates the program (returns to caller) normally.

13.6 Multiple declaratives match — the highest‑priority declarative runs (§4.5); the others are ignored.

13.7 Declarative handler itself raises an exception
- Triggers the STANDARD EXCEPTION declarative if one exists; otherwise propagates to the runtime.

13.8 Declarative triggered during cleanup — ignored.

------------------------------------------------------------
Summary
------------------------------------------------------------
The CobolSharp Error Model:
- Implements full COBOL file-I/O declaratives (USE AFTER ERROR/EXCEPTION, per-file/per-mode scope, GLOBAL
  cross-program dispatch, re-entrancy guarding) and statement-level handlers (ON EXCEPTION / SIZE ERROR /
  OVERFLOW) today; the broader ISO-2023 exception-condition (EC) model and JSON/XML/SORT/Report-Writer
  declarative routing are the Phase-C target design.
- Provides deterministic ExceptionState routing and structured, resumable recovery without stack unwinding.
- Returns control to the statement after the failing statement (no retry), preserving the PERFORM and CALL stacks.
- Generates clean, verifiable, debugger‑friendly CIL (Mono.Cecil; no custom VM).
- Ensures correctness across CoreCLR, AOT, and WASM.
