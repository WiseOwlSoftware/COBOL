# CobolSharp — Claude Code Instructions

Read PROMPT.md before making any code change. It contains architectural doctrine and development
rules derived from 13+ sessions of building this compiler. Every rule exists because it was
violated and corrected. They are non-negotiable.

Read PROJECT_PLAN.md to understand current status and next steps.

Read DEVLOG.md for context on recent decisions, failures, and design rationale.

## Session Resume Context (updated 2026-03-20)

### Current State
- **Branch**: nist-phase-d
- **Integration tests**: 182 pass, 1 skip
- **Unit tests**: 119 pass
- **NIST tests at 100%** (39 tests): NC101A-NC107A, NC111A, NC112A, NC115A-NC120A,
  NC122A-NC124A, NC126A, NC127A, NC131A, NC132A, NC136A, NC137A, NC140A, NC141A,
  NC170A-NC173A, NC175A-NC177A, NC202A, NC203A, NC206A, NC207A, NC210A, NC221A,
  NC222A, NC224A, NC239A, NC240A, NC241A, NC248A, NC251A, NC253A
- **Next**: Continue NIST sweep, fix NC121M subscripted DIVIDE, NC220M hang

### What was done this session (2026-03-20)
- Unified COBOLxxxx diagnostic codes across all compiler phases
- Grammar: NOT=/NOT>/NOT<, multi-target SET, SET BY arithmeticExpression
- Grammar rename: 17 production-quality rule names (dataReference, comparisonExpression, etc.)
- CONTINUE statement bound as no-op
- SET BY expression: TryEvalConstant for compile-time folding (-5, +5)
- Silent fallthrough elimination in LowerSetIndex (COBOL05xx diagnostics)
- USAGE INDEX: IsElementary/IsGroup fix, FieldSizeCalculator, CompilerPicDescriptorFactory
- DIVIDE: SafeRemainder (div/0), COBOL REMAINDER semantics (stored quotient truncation),
  IrCobolRemainder instruction, numeric edited formatting, non-GIVING accumulator pattern
- Error listener: 20-error cap, [COBOLxxxx] code extraction

### Key architectural decisions
- IrCobolRemainder carries quotient accumulator + GIVING fraction digits (no read-back from edited fields)
- BoundCompoundStatement for multi-target SET desugaring
- TryEvalConstant for compile-time decimal constant folding in expressions
- USAGE INDEX normalized to S9(9) COMP: level-77 in SemanticBuilder, sub-level in layout layer

### Known gaps
- CALL/USING/RETURNING (parse only, not bound/lowered)
- SORT/MERGE (parse only)
- Alternate keys (not parsed)
- VALUE THRU in level-88 (grammar gap — NC201A, NC250A, NC252A)
- ASCENDING/DESCENDING KEY in OCCURS (NC233A, NC237A, NC238A, NC247A)
- STATUS/PROGRAM as paragraph names (reserved word conflicts)
- Subscripted operands in DIVIDE GIVING (NC121M)
- NC220M infinite loop at runtime
