# CobolSharp — Claude Code Instructions

Read PROMPT.md before making any code change. It contains architectural doctrine and development
rules derived from 13+ sessions of building this compiler. Every rule exists because it was
violated and corrected. They are non-negotiable.

Read PROJECT_PLAN.md to understand current status and next steps.

Read DEVLOG.md for context on recent decisions, failures, and design rationale.

## Session Resume Context (updated 2026-03-21)

### Current State
- **Branch**: nist-phase-d
- **Integration tests**: 176 pass, 1 skip
- **Unit tests**: 195 pass
- **NIST tests at 100%** (39 tests): NC101A-NC107A, NC111A, NC112A, NC115A-NC120A,
  NC122A-NC124A, NC126A, NC127A, NC131A, NC132A, NC136A, NC137A, NC140A, NC141A,
  NC170A-NC173A, NC175A-NC177A, NC202A, NC203A, NC206A, NC207A, NC210A, NC221A,
  NC222A, NC224A, NC239A, NC240A, NC241A, NC248A, NC251A, NC253A
- **Next**: Continue NIST sweep, fix NC121M subscripted DIVIDE, NC220M hang

### What was done this session (2026-03-21)
- Closed all remaining validation gaps across BoundTreeValidator
- OPEN EXTEND validation (CBL0701)
- READ extensions: IsNext, KeyDataName on BoundReadStatement (CBL1701-1703)
- WRITE FROM validation (CBL1801), REWRITE FROM bound + validated (CBL1902)
- START KEY operand check (CBL1603)
- New BoundReturnStatement + binder + IR stub (CBL2101)
- New BoundCallStatement + BoundCallArgument + binder + IR stub (CBL3310)
- SELECT/FD consistency: FD without SELECT emits CBL0601 warning
- 12 new unit tests for all wired diagnostics

### Key architectural decisions
- BoundCallArgument carries ParameterMode + BoundExpression (mirrors ProcedureParameter)
- RETURN/CALL IR stubs emit display messages and take exception/at-end paths
- WRITE/REWRITE FROM use permissive validation (COBOL moves are extremely permissive)

### Known gaps
- CALL/USING/RETURNING bound + validated, but IR is stub (no inter-program linkage)
- SORT/MERGE (parse only)
- Alternate keys (not parsed)
- VALUE THRU in level-88 (grammar gap — NC201A, NC250A, NC252A)
- ASCENDING/DESCENDING KEY in OCCURS (NC233A, NC237A, NC238A, NC247A)
- STATUS/PROGRAM as paragraph names (reserved word conflicts)
- Subscripted operands in DIVIDE GIVING (NC121M)
- NC220M infinite loop at runtime
