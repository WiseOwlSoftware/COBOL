# CobolSharp — Claude Code Instructions

## ⛔ TOP-LEVEL PLAN: read `docs/MASTER_PLAN.md` FIRST
It is the single SSOT + autonomous-execution playbook to reach the North Star — a **commercial-quality,
decades-sustainable, full ISO/IEC 1989:2023** COBOL compiler, **no back-compat (rewrite/re-architect anything),
implemented with maximum autonomy + maximum practical parallelism.** MASTER_PLAN sequences every phase and
orchestrates the prior planning/architecture docs (it does not reinvent them). `resume-prompt.md` and
`docs/ISO2023_CONFORMANCE_PLAN.md` are sub-plans it points to. (memory `feedback_commercial_quality_north_star`.)

## 🗺 DOC MAP: read `docs/DOC_INDEX.md` to navigate the docs
The index of all docs (~126) — each doc's subject, type (LIVE/DESIGN/LEDGER/SPEC), and a maintenance guide. **Consult
it to find the right doc; keep it in sync** when you add, retire, or materially change a doc. There is exactly one
canonical doc per subsystem — extend it, never fork a second. A new subsystem ⇒ a new doc (with a status banner) + a
new index row.

Read PROMPT.md before making any code change. It contains architectural doctrine and development
rules derived from 13+ sessions of building this compiler. Every rule exists because it was
violated and corrected. They are non-negotiable.

Read PROJECT_PLAN.md to understand current status and next steps.

Read DEVLOG.md for context on recent decisions, failures, and design rationale.

specs/ISO_COBOL.md contains the definitive ISO/IEC 1989:2023 COBOL specification (in the
CobolSharp-private submodule). Refer to it for all specification, behavior, syntax, and semantic
questions. It is the authoritative source — do not guess or assume COBOL semantics without
consulting it. Initialize the submodule with: `git submodule update --init --recursive`

## Session Resume Context (updated 2026-06-06)

**→ Start a new session from `resume-prompt.md` (repo root).** The single live plan is
`docs/ISO2023_CONFORMANCE_PLAN.md` (its **§0.5** holds the current top priority).

### #1 PRIORITY: the .NET-native data-model migration
The owner approved a foundational re-architecture to **"the best native .NET implementation of COBOL."** **Do this
FIRST, ahead of all remaining conformance features**, then keep every currently-passing test green at 100% (fixing
bugs as they surface), running autonomously with all appropriate parallelism. The design is settled and reviewed:
`docs/DATA_MODEL_ARCHITECTURE.md` (the ADR — typed-native `record struct`s; `long`/`decimal`/`bool`; character →
`string` (UTF-16); byte image only as a classifier-scoped REDEFINES/file/hot-loop fallback; pointers → managed
refs; OO → .NET classes) + `docs/DATA_MODEL_REVIEW.md` (the adversarial review). **Do not re-litigate it** (the
owner co-authored it, DEVLOG 393); implement its 7-stage migration (ADR §10), guard-green at every step, character
data first. Resolve the owner-gated decisions per ADR §12 (numeric substrate = `BigInteger` before Stage 1;
classifier-trigger completeness before any Stage-3 typed flip).

### Current State (updated 2026-06-08, DEVLOG 456; latest commit `a10bda9`, branch `main`, tree clean)
- **→ The live kickoff prompt is `resume-prompt.md` (repo root). Read it first.** This block is a snapshot.
- **Branch**: main; guard ALL GREEN — **1204 unit / 535 integration / 364 NIST** (`bash scripts/guard.sh`, or the
  ~3.3-min parallel `scripts/guard-fast.sh`); baselines 0 FAIL*. Stack: **.NET 10 / C# 14**.
- **DEVLOG at entry 456.** M1 (COBOL-85) complete; M2 (COBOL-2002) in progress.
- ⛔ **OWNER DIRECTIVE (2026-06-08): OO is TYPED-NATIVE per ADR §7 — object data → per-instance typed .NET fields,
  methods → real .NET methods, INVOKE → callvirt; NEVER the byte `State`. The `EnableTypedFields` default-OFF gate is
  migration-safety for the LEGACY corpus, NOT design intent — OO is an always-on consumer of the typed pipeline.**
  Tests may break temporarily mid-migration; bar = ALL green at completion. (Memory `feedback_oo_typed_native_not_byte`.)
- **Data-model migration (for PROGRAMS): CORE done through Stage-4** (gated `EnableTypedFields`, default OFF → corpus
  byte-identical): character→`string`, numeric→`long`/`decimal`, flat+nested groups→`record struct`, fixed
  OCCURS→`T[]`, and **pointers→`ManagedPointer`**. Global flip-on + island-the-byte-engine is Stage-6/B3 (future).
- **Phase A done** (DEVLOG 445–446): CLI `--standard`→DialectLevel verified + regression net; **CI enabled**.
- **OO TYPED-NATIVE — object data model COMPLETE + multi-method + INVOKE SELF (DEVLOG 453–456):**
  - Object data → per-instance typed .NET fields: `PIC X`→instance `string` (453), `PIC 9`→instance `long`/`decimal`
    (454), `01` group→instance `record struct` + `OCCURS`→instance typed array (456). Byte `State` empty on a
    fully-typed class. Instance dimension via the `CilDataEmitter.EmitTypedFieldOwner` chokepoint; also filled the
    general typed-numeric→byte MOVE cell (454).
  - Multi-method classes + INVOKE SELF (455): N `METHOD-ID`/class (each its own .NET method, exit-bounded dispatch;
    per-method paragraph scope fixes CBL3104), `INVOKE SELF`→`callvirt` incl. polymorphic across INHERITS
    (COBOL0112 lifted). 3-agent adversarial panel → edge gaps fail LOUD: **COBOL0116** (SECTION-in-method),
    **COBOL0117** (multi-method class WITH method params — per-method LINKAGE is a later slice).
  - Earlier OO (447–451, now on the typed path): NEW, INVOKE USING/RETURNING, OBJECT REFERENCE, INHERITS+virtual,
    INVOKE SUPER. Deferred loud: COBOL0111/0113/0114/0115/0116/0117.
  - Conformance (9): `oo_hello`/`oo_method_perform`/`oo_method_args`/`oo_inherit`/`oo_super`/`oo_instance_data`/
    `oo_self`/`oo_self_polymorphic`/`oo_object_group` + `OoTests`.
- **RESUME AT → (a) subclass own typed OBJECT data** (lift COBOL0113 — emit the subclass's own typed instance fields
  + init in the subclass ctor; factor the typed field-defs + typed-init out of `CilEmitter.EmitProgramState`; reject
  byte-island subclass data which has no own State); **(b) per-method LINKAGE** (multi-method-with-args, lifts
  COBOL0117) + typed-field-by-reference INVOKE args; **(c) FACTORY**; then 5 PROPERTY / 6 universal-ref+EC; then the
  remaining M2/M3/M4 catalog (`docs/ISO2023_CONFORMANCE_PLAN.md` §3). Then Stage-5 Roslyn backend, Stage-6 finalize +
  flip-on + rename `CobolSharp`→`COBOL.NET` (exe `cobol.exe`). See `resume-prompt.md` + `docs/OO_IMPLEMENTATION_DESIGN.md` §5.
- The blocks below are HISTORICAL (2026-05 / 2026-03 sessions); see `resume-prompt.md` + DEVLOG for everything since.

### (historical) Current State as of 2026-03-28
- **Unit tests**: 421 pass · **Integration**: 274 · **NIST**: 95 in guard
- **Intrinsic functions**: 94/94 dispatched · **Source of truth**: GRAMMAR_AUDIT.md

### What was done this session (2026-03-27/28/29)
- **NIST expansion (65→95)**: 30 new tests via grammar fixes (DISPLAY UPON, SET ON/OFF,
  WRITE ADVANCING, STRING/UNSTRING, INSPECT, IS >= operator, ACCEPT FROM, PIC lexer,
  preprocessor continuation, VALUE THRU negative numbers), semantic fixes (comparisons,
  REDEFINES, RENAMES, EVALUATE, ALPHABET, qualified names), ZERO_ARITH token rewriting,
  SLL two-stage parsing (6× speedup), PERFORM VARYING subscripted FROM/BY fix.
- **Spec compliance audit**: 8 parallel agents audited entire compiler vs ISO spec
- **P0 bug sweep**: 8 critical bugs fixed (OPEN multi-clause, READ INVALID KEY,
  NumericEdited MOVE, LOCAL-STORAGE routing, file status codes, etc.)
- **P1 bug sweep**: 12 wrong-computation bugs fixed (PERFORM TEST AFTER, MOVE
  subscript eval, INTEGER/MOD intrinsics, signed DISPLAY default, etc.)
- **P2 feature sweep**: 14 COBOL-85 required features implemented (SORT/MERGE,
  Alphabetic category, CLASS/SYMBOLIC/ALPHABET, EXIT CYCLE, ODO runtime, etc.)
- **Remaining gaps**: 12 partial implementations completed (SYNCHRONIZED, COMP-1/2
  IEEE 754, LOCAL-STORAGE re-init, EXTERNAL shared storage, file status codes, etc.)
- **Intrinsic functions**: Full binder pipeline wired, 94/94 functions dispatched,
  all stubs replaced, reserved word conflicts fixed, 212 tests added
- **Grammar audit**: 10 agents did token-by-token grammar-vs-spec comparison,
  version-categorized all ~300 mismatches (138 COBOL-85, 122 COBOL-2002, 42 COBOL-2023)
- **Grammar fixes**: 7 agents fixed ~70 COBOL-85 grammar gaps (45 lexer tokens,
  FD clauses, INITIALIZE, CORR, exponentiation, ALPHABET, empty paragraphs, etc.)
- **Nested programs**: Grammar + multi-program compilation pipeline
- **Doc cleanup**: Deleted 6 obsolete .md files, consolidated audit docs into
  single GRAMMAR_AUDIT.md, updated README/PROJECT_PLAN

### Key architectural decisions
- SUBSCRIPT lexer mode for spec-true COBOL-85 subscript parsing (Entry 150)
- CobolLexer.g4 co-located with parser fragments in Core/ for IDE support
- SortRuntime.cs: in-memory sort (external merge sort deferred)
- ExternalStorage.cs: ConcurrentDictionary for EXTERNAL shared storage
- IrCachedLocation: ensures MOVE source evaluated once for multi-target
- GRAMMAR_AUDIT.md is the single source of truth for compliance

### Next session: Iterative audit-fix-verify loop
The user requested a looping process:
1. **Audit team**: agents compare grammar + compiler against spec + GRAMMAR_AUDIT.md
2. **Fix team**: agents correct any gaps found
3. **Verify**: build + test + guard
4. **Repeat** until audit finds zero gaps

~56 COBOL-85 grammar gaps remain (of ~138 identified). Key items FIXED this session:
- DISPLAY UPON/NO ADVANCING, SET TO ON/OFF, WRITE ADVANCING optional, STRING/UNSTRING optionality,
  INSPECT FOR+, IS >= operator, ACCEPT FROM mnemonic-name (all done)
Key remaining items:
- WRITE/REWRITE FILE form, retry-phrase, locking
- SORT table format (Format 2)
- USE GLOBAL/EXCEPTION/INPUT-OUTPUT modes
- START WITH LENGTH
- CURRENCY WITH PICTURE SYMBOL (blocked by PICMODE lexer architecture)
- NC220M/NC237A runtime hangs (undiagnosed)
- All 95 NC-series NIST tests pass (was 65 at session start)
- Remaining non-NC suites (IC, IF, IX, SQ, ST, etc.) not yet attempted
- Key infrastructure: ZERO_ARITH token rewriter, SLL+BailErrorStrategy parsing,
  PIC_STRING lexer action for trailing period, preprocessor continuation trailing space fix
