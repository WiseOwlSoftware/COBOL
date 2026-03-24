# CobolSharp — Claude Code Instructions

Read PROMPT.md before making any code change. It contains architectural doctrine and development
rules derived from 13+ sessions of building this compiler. Every rule exists because it was
violated and corrected. They are non-negotiable.

Read PROJECT_PLAN.md to understand current status and next steps.

Read DEVLOG.md for context on recent decisions, failures, and design rationale.

## Session Resume Context (updated 2026-03-24)

### Current State
- **Branch**: main
- **Integration tests**: 184 pass, 1 skip
- **Unit tests**: 217 pass
- **Diagnostic descriptors**: 175+ (COBOL0001-COBOL0600 + CBL0601-CBL3606)
- **NIST tests at 100%** (31 in guard): NC101A-NC107A, NC111A, NC112A, NC115A, NC117A,
  NC122A-NC124A, NC126A, NC127A, NC131A, NC132A, NC136A, NC137A, NC140A, NC141A,
  NC176A, NC202A, NC206A, NC207A, NC210A, NC221A, NC233A,
  NC239A, NC240A, NC241A, NC248A, NC253A
- **Next**: Condition-name conditions, ODO runtime, runtime hangs, collating sequence

### What was done this session (2026-03-24)
- NIST blocker sweep: OCCURS validation relaxed (7 levels, group keys allowed),
  ALL ZEROS figurative parsing fixed, CIL op_Explicit ambiguity removed,
  RENAMES single-field category inheritance fixed, abbreviated conditions grammar added
- NC233A reaches 100% and added to guard suite
- CALL/USING/RETURNING full implementation (prior session work committed)
- Code quality sweep 3.1-3.5 (prior session work committed)
- Flow-sensitive FileStateValidator (CBL0702, CBL3206)
- 7 dormant validators wired
- NC252A compiles (was internal compiler error)

### Key architectural decisions
- RENAMES single-field inherits source PIC (not always alphanumeric)
- OCCURS group keys are valid per COBOL-85 spec
- ZERO-in-arithmetic grammar blocked by ALL(*) backtracking with signCondition

### Known gaps
- SORT/MERGE (parse only, IR is stub)
- Abbreviated conditions partially implemented (grammar + rewrite pass, but bare operands
  in some contexts still fail — NC201A, NC211A)
- Condition-name conditions (`IF switch-condition`) — NC211A, NC254A
- OCCURS DEPENDING ON runtime truncation — SEARCH/comparison don't respect active ODO count
- NC220M/NC237A infinite loop at runtime (undiagnosed)
- NC250A: ZERO as arithmetic operand causes grammar backtracking
- NC215A/NC219A: collating sequence (ALPHABET) not applied to comparisons
- NC252A: qualified RENAMES failures (3 tests)
- Subscripted PERFORM VARYING (NC201A)
- Compile-time CALL parameter validation (needs inter-program metadata)
