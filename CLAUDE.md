# CobolSharp — Claude Code Instructions

Read PROMPT.md before making any code change. It contains architectural doctrine and development
rules derived from 13+ sessions of building this compiler. Every rule exists because it was
violated and corrected. They are non-negotiable.

Read PROJECT_PLAN.md to understand current status and next steps.

Read DEVLOG.md for context on recent decisions, failures, and design rationale.

## Session Resume Context (updated 2026-03-18)

### Current State
- **Branch**: main
- **Integration tests**: 169 pass, 1 skip
- **Unit tests**: 119 pass
- **NIST kernel tests passing 100%**: NC101A (94/94), NC102A (39/39), NC103A (103/103),
  NC104A (141/141), NC105A (129/129), NC106A (127/127), NC116A (67/67), NC118A (30/30),
  NC171A (109/109), NC176A (125/125) — 964 total
- **Next NIST test to run**: NC107A

### What was just completed (2026-03-20)
- ANTLR: caseInsensitive character class fix, OFF token, -lib tokenVocab fix
- Implementor switches: full SPECIAL-NAMES pipeline (ImplementorSwitch → SemanticModel)
- IrMoveFieldToField: canonical field-to-field MOVE primitive with carried PICs (replaces IrPicMove)
- CorrespondingMatcher: extracted shared matching engine (FILLER, REDEFINES, qualification, OCCURS)
- BoundCorrespondingStatement: unified node for MOVE/ADD/SUBTRACT CORRESPONDING
- ADD/SUBTRACT CORRESPONDING: full pipeline through accumulator IR pattern
- STRING binding: updated for delimitedByPhrase grammar, BindFigurativeConstantExpression extracted

### Key architectural decisions
- IrMoveFieldToField carries resolved PICs — emitter uses carried PICs, no late-binding lookups
- CorrespondingMatcher is a standalone static class reusable by all CORRESPONDING operations
- BoundCorrespondingStatement uses CorrespondingKind discriminant (not 3 separate classes)
- Group-scoped OCCURS checking (stops at CORRESPONDING group operand, not root)
- No IrMoveFieldSpan batching — raw byte copy unsafe for heterogeneous PICs
- Accumulator pattern for ADD/SUBTRACT CORRESPONDING (consistent with scalar arithmetic)
- Group items are ALWAYS alphanumeric for MOVE and COMPARE (ISO §6.4.2)
- Encoding.Latin1 for byte↔string in comparisons (not ASCII)

### Known gaps
- CALL/USING/RETURNING (parse only, not bound/lowered)
- SORT/MERGE (parse only)
- Alternate keys (not parsed)
