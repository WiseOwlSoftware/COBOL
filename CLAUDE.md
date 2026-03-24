# CobolSharp — Claude Code Instructions

Read PROMPT.md before making any code change. It contains architectural doctrine and development
rules derived from 13+ sessions of building this compiler. Every rule exists because it was
violated and corrected. They are non-negotiable.

Read PROJECT_PLAN.md to understand current status and next steps.

Read DEVLOG.md for context on recent decisions, failures, and design rationale.

## Session Resume Context (updated 2026-03-22)

### Current State
- **Branch**: nist-phase-d
- **Integration tests**: 189 pass, 1 skip
- **Unit tests**: 217 pass
- **Diagnostic descriptors**: 175 (COBOL0001-COBOL0600 + CBL0601-CBL3606)
- **NIST tests at 100%** (39 tests): NC101A-NC107A, NC111A, NC112A, NC115A-NC120A,
  NC122A-NC124A, NC126A, NC127A, NC131A, NC132A, NC136A, NC137A, NC140A, NC141A,
  NC170A-NC173A, NC175A-NC177A, NC202A, NC203A, NC206A, NC207A, NC210A, NC221A,
  NC222A, NC224A, NC239A, NC240A, NC241A, NC248A, NC251A, NC253A
- **Next**: Continue NIST sweep, fix NC121M subscripted DIVIDE, NC220M hang

### What was done this session (2026-03-22)
- COMP-5 (COMPUTATIONAL-5): full end-to-end — lexer/parser/enum/sizing/IR/CIL/runtime
  (little-endian, full binary capacity, binary-capacity overflow). 22 unit tests.
- COMPUTATIONAL-1/2/3/5 full-word lexer tokens (pre-existing gap for all COMPUTATIONAL-N)
- PicDescriptorFactory refactored: USAGE-aware storage length (was DISPLAY-only)
- RENAMES (level 66): full end-to-end — parse, resolve FROM/THRU, validate (CBL0810-0812),
  compute storage alias, bind, emit. THROUGH synonym. 2 integration tests.
- Diagnostic consolidation: all 55 ad-hoc COBOL string codes migrated to DiagnosticDescriptors.
  SemanticBuilder refactored from raw List<Diagnostic> to DiagnosticBag.
- ALTER statement: version-aware (error in COBOL-2002+, warning+support in 85/Default).
  Runtime alter indirection table, IrAlter/IrReturnAlterable, bare GO TO. 2 integration tests.
- `--standard` CLI option wired through to Compilation pipeline (was TODO).
- DialectMode expanded: Cobol2014, Cobol2023 added.
- Sign conditions (IS [NOT] POSITIVE/NEGATIVE/ZERO): BoundSignConditionExpression, lowered
  as comparison against zero. 1 integration test.
- Negated conditions (NOT): BindUnaryLogical rewritten to dispatch all primaryCondition
  alternatives (was broken — only handled comparisonExpression). 1 integration test.
- Bug fix: VALUE +N (unary plus) was silently dropping the value. Fixed FindNumericLiteralInArith.
- GenerateIfNewer.ps1: checks all .g4 files recursively (not just top-level).
- All audit docs updated throughout.

### Key architectural decisions
- COMP-5 uses BinaryPrimitives for little-endian encode/decode (idiomatic .NET 9)
- RENAMES items are storage aliases — no IrField needed, resolved via GetStorageLocation
- ALTER uses slot-based indirection table (int[]) — zero overhead for non-ALTER programs
- Sign conditions rewritten as comparisons (POSITIVE → > 0), not a new IR instruction

### Known gaps
- SORT/MERGE (parse only, IR is stub)
- Abbreviated conditions (IF A > B OR < C — implicit operand reuse; NC211A, NC250A)
- ALPHABET clause THRU/ALSO in SPECIAL-NAMES (NC215A, NC219A)
- NC220M infinite loop at runtime (IrElementRef destination issue)
- Compile-time CALL parameter validation (needs inter-program metadata)
