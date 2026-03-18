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
  NC104A (141/141), NC106A (127/127), NC116A (67/67), NC118A (30/30), NC171A (109/109),
  NC176A (125/125) — 835 total
- **Next NIST test to run**: NC105A

### What was just completed
- EXIT PARAGRAPH / EXIT SECTION: structured exit scopes, orthogonal to PERFORM
- NC104A 141/141: MOVE Format 1 with all edited-field variants
- MOVE dispatch overhaul: correct routing for Numeric/NumericEdited/Alphanumeric → edited fields
- BLANK WHEN ZERO: full pipeline from grammar → SemanticBuilder → PicLayout → runtime PicDescriptor
- CR/DB insertionChars fix in PicDescriptorFactory (storage length was wrong)
- PicRuntime: decimal.Truncate instead of (long) cast for high-precision fields
- Grammar: DATA RECORD IS (obsolete FD clause), BLANK_WHEN_ZERO token fix
- NIST preprocessor: XXXXX084 → STANDARD

### Key architectural decisions
- EXIT PARAGRAPH: IrJump to paragraph end block (fall-through semantics)
- EXIT SECTION: IrReturnConst(lastParaInSection + 1) — dispatcher skips remaining section paragraphs
- MOVE dispatch priority: AlphanumericEdited dest first (split by Numeric vs other source),
  then NumericEdited source rules, then generic IsNumericLike/IsAlphanumericLike rules
- NumericEdited → Alphanumeric: raw byte copy (COBOL treats source as alphanumeric)
- NumericEdited → AlphanumericEdited: raw bytes + edit pattern (not numeric decoding)

### Known gaps
- CALL/USING/RETURNING (parse only, not bound/lowered)
- SORT/MERGE (parse only)
- Alternate keys (not parsed)
- Some NIST preprocessor placeholders not mapped
