# CobolSharp — Claude Code Instructions

Read PROMPT.md before making any code change. It contains architectural doctrine and development
rules derived from 13+ sessions of building this compiler. Every rule exists because it was
violated and corrected. They are non-negotiable.

Read PROJECT_PLAN.md to understand current status and next steps.

Read DEVLOG.md for context on recent decisions, failures, and design rationale.

## Session Resume Context (updated 2026-03-18)

### Current State
- **Branch**: phase-c
- **Integration tests**: 153 pass, 1 skip
- **NIST kernel tests passing 100%**: NC101A (94/94), NC102A (39/39), NC103A (103/103),
  NC104A (141/141), NC105A (129/129), NC106A (127/127), NC116A (67/67), NC118A (30/30),
  NC171A (109/109), NC176A (125/125) — 964 total
- **Next NIST test to run**: NC107A

### What was just completed
- NC105A 129/129: MOVE Format 2, group semantics, edited fields, JUSTIFIED RIGHT
- Group MOVE/COMPARE: IsGroup flag on PicDescriptor, guards in CilEmitter dispatch
- JUSTIFIED RIGHT: full pipeline grammar → DataSymbol → PicDescriptor → runtime
- COMP truncation by PIC digit count (mod 10^digits, not binary capacity)
- Figurative MOVE to edited fields: pattern-aware dispatch
- Floating symbol comma suppression: FindFloatingPlacement for full floating zone
- Numeric literal text preservation: BoundLiteralExpression.OriginalText
- Latin1 encoding in comparisons (ASCII corrupted 0x80-0xFF bytes)
- All 459 NIST test programs extracted, all NC X-card placeholders mapped

### Key architectural decisions
- Group items are ALWAYS alphanumeric for MOVE and COMPARE (ISO §6.4.2)
- PicDescriptor.IsGroup set by CompilerPicDescriptorFactory for group items
- CilEmitter else-fallback uses MoveAlphanumericToAlphanumeric (honors JUSTIFIED)
- String literal to NumericEdited: route through IrPicMoveLiteralNumeric
- String literal to Numeric: MoveStringLiteralToNumeric (temp buffer + runtime MOVE)
- Encoding.Latin1 for byte↔string in comparisons (not ASCII)

### Known gaps
- CALL/USING/RETURNING (parse only, not bound/lowered)
- SORT/MERGE (parse only)
- Alternate keys (not parsed)
