# CobolSharp — Claude Code Instructions

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

### Current State
- **Branch**: main; guard ALL GREEN — **1196 unit / 507 integration / 364 NIST** (`bash scripts/guard.sh`);
  baselines 0 FAIL*.
- **DEVLOG at entry 430.** M1 (COBOL-85) complete; M2 (COBOL-2002) in progress. **The #1-priority data-model
  migration — CORE (Stage 3) COMPLETE:** the substrates (`CobolNum`/`CobolDecimal`/`CobolString` + oracles, 394–399)
  + the `RecordClassificationPass` classifier (397–398, wired into the Binder 401) underpin typed flips that landed
  ONE rule at a time, each guard-green + a flag-on≡flag-off `TypedFieldFlipTests` differential test, all gated behind
  `EnableTypedFields` (default OFF → corpus byte-identical):
  - **character → `.NET string`** (S3a–c, 403–409): standalone, record-struct member, all MOVE pairs, DISPLAY,
    COMPARE, class conditions, figurative SPACE/ZERO.
  - **numeric → `long`** (unsigned-int) / **`decimal`** (signed-scaled) over DISPLAY/COMP/BINARY (410–420): VALUE,
    MOVE (literal + field, all combos), DISPLAY (sign-overpunch), COMPARE, `IS NUMERIC`, **arithmetic** (ADD/SUB/MUL/
    DIV/COMPUTE/REMAINDER via a materialize prologue/epilogue), `MOVE ZEROS`. COMP-5/float/packed excluded.
  - **groups → (nested) `record struct`s** (S3b 405, **nested S5 427**): mixed `string`/`long`/`decimal` members,
    member-path access; **fixed OCCURS tables (char + numeric) → `T[]`** (422–426) with subscripted + PERFORM-VARYING
    access; SEARCH safely stays byte; every byte-trigger (REDEFINES/RENAMES/edited/file/EXTERNAL/LINKAGE/ref-mod/ODO/
    whole-table/whole-group operand) correctly stays byte.
  - **Byte-engine ISO-2023 conformance fix (424):** a VALUE on an OCCURS item now initializes EVERY occurrence
    (§13.18.63.4 GR 9; conformance `table_value_occurs`; zero baseline shifts).
  - **Definition of done (428):** a representative business program flips its WHOLE data division byte-identically.
- **RESUME AT → Stage-4 pointer slice 1** (BASED + ADDRESS OF + SET ADDRESS OF + deref), then the rest (all
  autonomous-eligible): **pointers → managed .NET references** via the single **`ManagedPointer`** carrier (renamed
  from `CobolDataPointer`, DEVLOG 429; GC-tracked, no native heap / no handle table / no `unsafe`; **PointerRegistry
  REJECTED — settled, NOT gated**; grammar approved; design in `RECORD_STRUCT_STORAGE_DESIGN.md §10`) and **OO → .NET
  classes**; Stage-5 **Roslyn C# backend**;
  Stage-6 finalize + flip-on-by-default decision + rename `CobolSharp`→`COBOL.NET` (exe `cobol.exe`). See plan
  **§0.5** + `docs/RECORD_STRUCT_STORAGE_DESIGN.md` §6/§9.
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
