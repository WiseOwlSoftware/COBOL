# COBOL.NET — ISO/IEC 1989:2023 Conformance Plan (SINGLE SOURCE OF TRUTH)

> **Purpose.** This is the durable, authoritative work-breakdown to take the compiler to **complete,
> production-quality ISO/IEC 1989:2023 COBOL support**. Work *from* this document — do **not** re-run the gap
> analysis each session (it has been done at least three times). When an item lands, tick it here in the same
> commit. When you discover a new gap, add it here. This supersedes ad-hoc backlogs scattered across DEVLOG /
> memory; `docs/MULTIVERSION_ROADMAP.md` is the high-level milestone view and points here for detail.
>
> **Provenance.** Built 2026-06-05 from a 15-area parallel spec-conformance audit (workflow
> `iso2023-conformance-audit`, 15 agents vs `specs/ISO_COBOL.md` + the actual compiler) plus the session's own
> surveys. Audit-confirmed findings are tagged **[A]**.

## 0. How to use this document

- **Process (unchanged project discipline):** implement **one item at a time on `main`**; build all layers
  together (grammar → semantic/binder → lowering → CIL emit → runtime → **output-verifying test**); run
  `bash scripts/guard.sh` after each (must stay **ALL GREEN**, currently **1047 unit / 455 integration / 364
  NIST**, 0 NIST FAIL); commit with a **DEVLOG** entry; then tick the item here.
- **Do NOT use worktree-isolated implementation workflows** for compiler fixes — in this repo `isolation:'worktree'`
  branches from a stale commit. Do compiler work directly on `main`. (Parallel *audit/design* agents are fine.)
- **Grammar changes are pre-authorized** in this conformance effort (log + full guard); new lexer keywords must be
  corpus-checked (grep NIST/fixtures for standalone occurrences) before adding.
- **Status legend:** ☐ todo · ◐ partial · ☑ done · 🐛 = a *correctness* defect (silent wrong result), not just a
  missing feature — these rank above pure feature gaps.

---

## 1. Current status — DONE (do not re-list as gaps)

- **M1 (COBOL-85): COMPLETE.** NIST CCVS85 = 364 baselines green (NC/IF/SM/IC/SQ/RL/IX/ST/OBSQ). Report Writer,
  collating, intrinsic-function set, file I/O all done.
- **M2 (COBOL-2002): in progress.** Landed this drive (DEVLOG 353–369):
  - **WS-2002-FORMAT — COMPLETE:** `*>` inline comments; `>>SOURCE FORMAT IS FREE|FIXED`; conditional compilation
    `>>DEFINE` / `>>IF`/`>>ELSE`/`>>END-IF` / `>>EVALUATE`/`>>WHEN`/`>>END-EVALUATE`; recognize-and-ignore of the
    other standard `>>` directives.
  - **OPTIONS paragraph** — parsed/accepted (clauses **not yet applied** — see M2-ARITH-1).
  - **REPOSITORY paragraph** — parsed/accepted (specifiers **not yet bound** — see M2-UDF-4).
  - **User-defined functions — CORE:** `FUNCTION-ID … END FUNCTION` units (compiled as callable programs);
    `FUNCTION user-name(args)` invocation as the **whole source of a MOVE/COMPUTE** (any arity, numeric **and**
    alphanumeric).
  - **CALL … RETURNING** into WORKING-STORAGE (the LINKAGE-location wiring fix).
  - **SORT Format-2** elementary self-key; **READ … PREVIOUS** (indexed + relative).
  - Intrinsic-function set already broad (incl. 2002/2014/2023 fns in `IntrinsicFunctions.cs`); EXIT
    PARAGRAPH/SECTION/PERFORM[ CYCLE] grammar present.
- **Guard baseline:** 1047 / 455 / 364.

---

## 2. Milestones

| Milestone | Standard | Validation |
|---|---|---|
| **M2** | COBOL-2002 | custom corpus `tests/conformance/2002/` (+ the existing SpecFixTests) |
| **M3** | COBOL-2014 | custom corpus `tests/conformance/2014/` |
| **M4** | COBOL-2023 | custom corpus `tests/conformance/2023/` (vs in-repo `specs/ISO_COBOL.md`) |

Order: finish M2, then M3, then M4. Within a milestone, do correctness defects (🐛) and high-value/small items
first; large subsystems (pointers, exceptions, OO) last.

---

## 3. Work-breakdown — the catalog

Each item: **ID** · feature · spec ref · severity · tractability · current state · recipe (files) · deps.

### 3.1 M2 — User-defined functions (finish the workstream)

- ☐ 🐛 **M2-UDF-1 — General inline UDF invocation.** `FUNCTION user-name(args)` inside a larger expression / `IF`
  / `DISPLAY` / as a function argument. **[A] severity HIGH (correctness: today it silently yields `0`)**,
  tractability **medium**. *Current:* only the whole-source MOVE/COMPUTE form is routed; elsewhere it falls
  through to the intrinsic path → `IntrinsicFunctions.Call("DOUBLER",…)` → silent 0. *Recipe:* a `Compilation`
  pre-pass building FUNCTION-ID `SemanticModel`s (throwaway diagnostics) → registry `name → {RETURNING
  StorageLength, PicDescriptor}` on each `SemanticModel`; new `BoundUserFunctionCall` + `IrUserFunctionCall`;
  inline emit mirroring `CilEmitter.EmitCallProgram` (~1240: build `CobolDataPointer[]` USING args + a scratch
  `byte[]` as the trailing RETURNING arg + `CobolProgramRegistry.Resolve` + `entry.Invoke(args)`) then
  `PicRuntime.DecodeNumeric(scratch,0,len,pic)` → push decimal. ~150 lines novel inline CIL. Verify
  `COMPUTE R = FUNCTION DOUBLER(X) + 1`. *(Alternatively route through a hoisted temp + the existing CALL emit.)*
- ☐ 🐛 **M2-UDF-2 — Literal / arithmetic-expression arguments.** `FUNCTION FOO(5)` / `FOO(A + 1)`. **[A] HIGH
  (correctness: returns 0 today)**, **small**. *Current:* `LoweringContext.LowerUserFunctionCall` returns false
  when an arg is not a resolvable location → falls through to intrinsic → 0. *Recipe:* materialize a non-location
  arg into a compiler temp (or BY CONTENT value) and pass it, instead of bailing. Pairs with M2-UDF-1.
- ☐ **M2-UDF-3 — Separate-compilation user functions (prototypes).** Caller + function in different translation
  units. *Medium.* §8.13 external repository + function-prototype. *Current:* caller and function must share one
  compilation group. *Recipe:* function-prototype definitions in the caller (or an external repository registry).
- ☐ **M2-UDF-4 — Bind REPOSITORY FUNCTION specifiers.** Enable `REPOSITORY. FUNCTION ALL INTRINSIC` (call
  intrinsics without the `FUNCTION` keyword) + named function specifiers. *Medium (parser ambiguity: `name(args)`
  vs subscript).* *Current:* REPOSITORY parses but specifiers are inert. §12.3.8.

### 3.2 M2 — Data types

- ☐ 🐛 **M2-DATA-1 — `USAGE BINARY-CHAR/BINARY-SHORT/BINARY-LONG/BINARY-DOUBLE [SIGNED|UNSIGNED]`.** **[A] HIGH
  (silent mis-typing today)**, **medium**. *Recipe:* lexer tokens + `usageClause` alts + a `UsageKind` mapping to
  1/2/4/8-byte two's-complement (reuse the COMP-5/native-binary emission path).
- ☐ **M2-DATA-2 — `USAGE FLOAT-SHORT / FLOAT-LONG`.** [A] medium. *Recipe:* alias onto the existing COMP-1/COMP-2
  (Single/Double) machinery. (FLOAT-EXTENDED → defer / map to Double with a note.)
- ☐ **M2-DATA-3 — National data.** `USAGE NATIONAL`, `PIC N(n)`, national literals `N"…"` / `NX"…"`,
  NATIONAL-EDITED, UTF-16 storage, MOVE/DISPLAY/compare/INSPECT semantics. *Large (new category).* The
  `DISPLAY-OF`/`NATIONAL-OF` intrinsics already exist; the data **type** does not. §13.18, §8.3.
- ☐ **M2-DATA-4 — Boolean & bit data.** `USAGE BIT`, `PIC 1(n)`, boolean literals `B"…"` / `BX"…"`, bit operators
  (B-AND/B-OR/B-XOR/B-NOT), BOOLEAN category in INITIALIZE/SET/compare. *Large (new category).* §8.3, §13.18.
- ☐ **M2-DATA-5 — Pointers & based addressing (foundational).** **[A] HIGH, large.** `USAGE POINTER /
  PROGRAM-POINTER / FUNCTION-POINTER`, `ADDRESS OF`, `SET … TO/UP/DOWN ADDRESS OF`, `BASED`, `NULL`. A `POINTER`
  token exists but there is no support. *Recipe:* `POINTER` usage + a runtime `PointerRegistry` handle model
  (mapping COBOL pointers onto .NET safely). **Unblocks ALLOCATE/FREE (M2-PROC-5) and SET ADDRESS OF.** §13.18,
  §14.9.

### 3.3 M2 — Arithmetic & configuration

- ☐ 🐛 **M2-ARITH-1 — `ROUNDED MODE` (all 8) + `DEFAULT ROUNDED`.** **[A] medium (only 2 of 8 modes implemented)**.
  Modes: AWAY-FROM-ZERO, NEAREST-AWAY-FROM-ZERO, NEAREST-EVEN, NEAREST-TOWARD-ZERO, PROHIBITED, TOWARD-GREATER,
  TOWARD-LESSER, TRUNCATION. *Recipe:* replace the `IsRounded` bool threaded into IR/`PicRuntime` with a
  `RoundingMode` enum; honour the per-statement `ROUNDED MODE IS …` and the OPTIONS `DEFAULT ROUNDED MODE`
  (parsed in DEVLOG 364 but inert). §14.9.4, §11.9.6.
- ☐ **M2-ARITH-2 — Apply remaining OPTIONS clauses.** `ARITHMETIC IS STANDARD/STANDARD-BINARY/STANDARD-DECIMAL`,
  `INTERMEDIATE ROUNDING`, `FLOAT-BINARY/DECIMAL DEFAULT`, `ENTRY-CONVENTION`. *Medium–large* (standard arithmetic
  is a real intermediate-precision change). Parsed-not-applied today. §11.9.

### 3.4 M2 — Procedure-division statements

- ☐ 🐛 **M2-PROC-1 — `INITIALIZE … TO VALUE` / `THEN TO DEFAULT` / `WITH FILLER`.** **[A] medium (phrases dropped →
  wrong result)**, **medium**. *Recipe:* honour VALUE-init, category-default, and FILLER inclusion in the
  INITIALIZE lowering. §14.9.20.
- ☐ **M2-PROC-2 — `INSPECT … BACKWARD`.** [A] medium, **small**. *Recipe:* a `BACKWARD` token + a backward flag
  → right-to-left scan in the INSPECT runtime. §14.9.
- ☐ **M2-PROC-3 — `VALIDATE` statement + validation clauses.** CLASS/DEFAULT/DESTINATION/INVALID/PRESENT WHEN/
  VARYING + error handling. *Large (new facility).* §14.9 VALIDATE, §13.
- ☐ **M2-PROC-4 — Exception handling: `RAISE`, EC framework, `>>TURN` runtime checking, `RESUME`,
  `USE … AFTER EXCEPTION`.** **[A] medium, large.** *Recipe:* an EC-condition catalog, a TURN on/off map, a
  condition register, and runtime guards on checked operations. **Backbone for RAISE/RESUME/USE and
  exception-checking PERFORM.** §14.6, §14.9.
- ☐ **M2-PROC-5 — `ALLOCATE` / `FREE` (based storage).** *Medium.* **Depends on M2-DATA-5 (pointers).** §14.9.
- ☐ **M2-PROC-6 — `GOBACK RETURNING`, `CONTINUE AFTER`, verify EXIT variants emit.** *Small–medium.* Grammar for
  EXIT PARAGRAPH/SECTION/PERFORM[ CYCLE] exists — verify semantics/emit. §14.9.

### 3.5 M2 — Preprocessor robustness & directives

- ☐ 🐛 **M2-PRE-1 — Preprocessor robustness trio.** **[A] medium (valid source crashes the parser today)**. (a)
  mid-file `>>SOURCE FORMAT` switching; (b) conditional-compilation directives **inside copied library text**
  (the pass runs before COPY); (c) a directive-skip lexer rule so an unhandled `>>`/directive can't reach the
  parser as stray tokens. §7.3.
- ☐ **M2-PRE-2 — Directive semantics depth.** Does `>>TURN` actually toggle EC checks (pairs with M2-PROC-4); is
  recognize-and-ignore of LISTING/PAGE/LEAP-SECOND/PROPAGATE/FLAG-* conformant; `>>DEFINE … AS PARAMETER` (env
  source); `>>CALL-CONVENTION` effect. *Low–medium.*

### 3.6 M2 — File & I/O (2002+ deltas only; the 85 file I/O is done)

- ☐ **M2-FILE-1 — `SHARING`, `LOCK MODE`/`LOCK ON`, `RETRY`.** *Medium.* §13 FILE-CONTROL, §14.9 OPEN.
- ☐ **M2-FILE-2 — Line-sequential organization** + 2002 FILE STATUS codes. *Medium.*

### 3.7 M2 — Object-Oriented COBOL (the single largest sub-project)

- ☐ **M2-OO-1 — OO COBOL.** CLASS-ID/END CLASS, METHOD-ID/END METHOD, INTERFACE-ID, FACTORY/OBJECT, INHERITS,
  `USAGE OBJECT REFERENCE`, `INVOKE` + inline method invocation, PROPERTY, SELF/SUPER, conformance. *Very large.*
  `CobolParserOO.g4` has CLASS-ID/METHOD-ID grammar scaffolding but it is **not bound/emitted**. Map the object
  model onto .NET types. §11 OO source units, §14.9 INVOKE. **Sequencing is an owner decision (non-OO M2 first).**

### 3.8 M3 — COBOL-2014

- ☐ **M3-1 — Dynamic-capacity tables** `OCCURS DYNAMIC [CAPACITY IN dn] [FROM…TO]`. *Medium–large.*
- ☐ **M3-2 — `TYPEDEF` / `SAME AS` / `TYPE TO`.** *Medium.*
- ☐ **M3-3 — JSON & XML** `JSON GENERATE/PARSE`, `XML GENERATE/PARSE` + special registers (XML-CODE/JSON-CODE/…).
  *Large.* `CobolParserJsonXml.g4` exists — assess completeness; likely needs binder/emit/runtime. §14.9.
- ☐ **M3-4 — File sharing/locking finalization** (if not folded into M2-FILE-1), function/method pointers,
  IEEE-754 alignment, increased limits, conditional-expression enhancements.

### 3.9 M4 — COBOL-2023

- ☐ **M4-1 — `DELETE FILE` execution.** **[A] medium, small.** *Current:* parse-only no-op stub. *Recipe:*
  binder/lowering case that deletes the file + emits its exception blocks.
- ☐ **M4-2 — `XOR` logical operator + `SMALLEST-ALGEBRAIC` + `EXCEPTION-FILE-N` intrinsics.** **[A] medium,
  small.** *Recipe:* a `logicalXor` grammar rule + two intrinsic dispatch cases.
- ☐ **M4-3 — Other 2023 intrinsic/bit/boolean additions + dynamic-table finalization + clarifications.** Audit
  `specs/ISO_COBOL.md` 2023-marked changes when M4 begins (many intrinsics already in `IntrinsicFunctions.cs` —
  verify completeness + **version gating**, i.e. a 2023 fn used under `--standard cobol85` should flag).

---

## 4. Execution order (waves) — implement top-down

**Wave 1 — UDF correctness + cheap high-value (start here):**
1. **M2-UDF-1** general inline UDF (🐛 silent 0) — and **M2-UDF-2** literal/arith args (🐛) together.
2. **M2-DATA-1** BINARY-CHAR/SHORT/LONG/DOUBLE (🐛 mis-typing).
3. **M4-1** DELETE FILE execution (cheap), **M2-PROC-2** INSPECT BACKWARD (cheap), **M4-2** XOR + 2 intrinsics
   (cheap).
4. **M2-PROC-1** INITIALIZE TO VALUE/DEFAULT/FILLER (🐛).

**Wave 2 — arithmetic + data aliases:**
5. **M2-ARITH-1** ROUNDED MODE (all 8) + DEFAULT ROUNDED (🐛 2-of-8).
6. **M2-DATA-2** FLOAT-SHORT/LONG (alias COMP-1/2).
7. **M2-PRE-1** preprocessor robustness trio (🐛 crashes).

**Wave 3 — new data categories:**
8. **M2-DATA-3** national data · **M2-DATA-4** boolean/bit · **M2-FILE-1/2** file 2002+.

**Wave 4 — big subsystems:**
9. **M2-DATA-5** pointers → **M2-PROC-5** ALLOCATE/FREE.
10. **M2-PROC-4** EC/exception framework → RAISE/RESUME/USE.
11. **M2-PROC-3** VALIDATE · **M2-ARITH-2** standard arithmetic.
12. **M2-OO-1** Object-Oriented COBOL.

**Wave 5 — M3:** dynamic tables · TYPEDEF · JSON/XML.
**Wave 6 — M4:** remaining 2023 deltas + version-gating of intrinsics.

---

## 5. Production / commercial-grade axes (run alongside conformance)

A commercial compiler needs more than spec checkboxes. Track these in parallel:

- **Diagnostics quality** — comprehensive `CBL####` coverage, accurate source locations, actionable messages,
  exit codes; never crash on invalid input (P1 hardening already started — see memory `project_p1_diagnostics`).
- **Dialect strictness** — the two-axis model (version `--standard` × strictness); every leniency dialect-gated
  (see memory `project_dialect_strictness`).
- **Conformance corpus** — `tests/conformance/<version>/` authored from `specs/ISO_COBOL.md`; a per-version
  "% to 100%" dashboard.
- **Performance** — parse/compile throughput on large programs; runtime efficiency of generated CIL.
- **Tooling & packaging** — CLI UX, `--help`, listing output, single-exe packaging, NuGet/runtime deployment.

---

## 6. POST-CONFORMANCE MILESTONES — GATED (do NOT start until an operational ISO 2023 compiler exists)

> Owner-stated; see memory `project_post_conformance_goals`. Do nothing here until M2→M4 conformance is achieved.

### 6.1 Full software architectural review
- Project **folder & file layout and naming** rationalized.
- **Class organization with proper isolation / single-responsibility — decompose any god classes.**
- Best software-design principles throughout; clear module boundaries.
- **Complete, accurate code documentation + code comments** across the source.
- Adopt the **latest C# language features**.

### 6.2 Project + executable rename
- Rename the project **`CobolSharp` → `COBOL.NET`** (rationale: it is COBOL for the .NET runtime — there is no
  "sharp" in it).
- **Produced executable MUST be named `cobol.exe`** (lowercase).
- Touch-points to plan for: `.csproj` `AssemblyName` + output exe name; the `.sln`; root namespaces
  (`CobolSharp.*`); `scripts/guard.sh` paths; `CLAUDE.md`/`README`/docs; the runtime assembly name; NuGet/package
  ids. Execute as one planned, guard-verified operation — not ad hoc.

---

## 7. Plan change-log
- **2026-06-05** — Created from the 15-area parallel conformance audit (workflow `iso2023-conformance-audit`) +
  session surveys. Initial catalog + waves. Established as SSOT.
