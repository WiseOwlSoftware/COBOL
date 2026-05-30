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

## Session Resume Context (updated 2026-05-29)

**→ Start a new session from `resume-prompt.md` (repo root)** — it is the authoritative,
up-to-date resume orientation. `claude/state/session-state-2026-05-29.md` has full detail.

### Current State
- **Branch**: main; working tree clean; guard ALL GREEN.
- **Guard** (`bash scripts/guard.sh`): 1000 unit / 336 integration / **149 NIST baselines
  (95 NC + 42 IF + 12 SM)** — all must stay green, baselines 0 FAIL*.
- **NIST suites**: NC 100%, **IF 100%**, **SM COPY-feature 100%**, **IC in progress 16/47**.
  SQ/IX/RL/ST surveyed, paused (file-I/O FILE-CONTROL wall + collating subsystem remain).
- DEVLOG is at entry 223. The block below is HISTORICAL (the 2026-03-28 session); see
  resume-prompt.md / DEVLOG 196–223 for everything since.

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
