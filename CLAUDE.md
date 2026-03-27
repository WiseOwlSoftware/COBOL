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

## Session Resume Context (updated 2026-03-27)

### Current State
- **Branch**: main
- **Integration tests**: 183 pass, 1 skip (split into 10 focused test files)
- **Unit tests**: 216 pass
- **Diagnostic descriptors**: 175+ (COBOL0001-COBOL0600 + CBL0601-CBL3606)
- **NIST tests at 100%** (60 in guard): NC101A-NC107A, NC111A-NC112A,
  NC115A-NC121M, NC122A-NC124A, NC126A-NC127A, NC131A-NC134A, NC136A-NC137A,
  NC140A-NC141A, NC170A-NC173A, NC175A-NC177A, NC202A-NC203A, NC206A-NC207A,
  NC210A-NC211A, NC221A-NC222A, NC224A, NC231A-NC234A, NC236A, NC238A-NC244A,
  NC248A, NC251A, NC253A-NC254A
- **Next**: ODO runtime, runtime hangs, collating sequence

### What was done this session (2026-03-26/27)
- Split monolithic EndToEndTests.cs (5,346 lines) into 10 focused test files
- Fixed `dotnet clean && dotnet build` (MSBuild target ordering)
- Un-skipped CALL and ref-mod integration tests (both features now implemented)
- Deleted 6 obsolete .md files, updated README.md and PROJECT_PLAN.md
- Moved CobolLexer.g4 to Core/, added tokenVocab to all imported grammars
  (fixes VSCode ANTLR4 extension false warnings for token references)

### Key architectural decisions
- SUBSCRIPT lexer mode for spec-true COBOL-85 subscript parsing (Entry 150)
- CobolLexer.g4 co-located with parser fragments in Core/ for IDE support
- RENAMES single-field inherits source PIC (not always alphanumeric)
- OCCURS group keys are valid per COBOL-85 spec

### Known gaps
- SORT/MERGE (parse only, IR is stub)
- ALL literal repetition fixed; NC211A at 100%
- OCCURS DEPENDING ON runtime truncation — SEARCH/comparison don't respect active ODO count
- NC220M/NC237A infinite loop at runtime (undiagnosed, likely ODO-related)
- NC250A: ZERO as arithmetic operand causes grammar backtracking
- NC215A/NC219A: collating sequence (ALPHABET) not applied to comparisons
- NC252A: qualified RENAMES failures (3 tests)
- Subscripted PERFORM VARYING (NC201A)
- Compile-time CALL parameter validation (needs inter-program metadata)
