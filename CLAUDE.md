# CobolSharp — Claude Code Instructions

Read PROMPT.md before making any code change. It contains architectural doctrine and development
rules derived from 13+ sessions of building this compiler. Every rule exists because it was
violated and corrected. They are non-negotiable.

Read PROJECT_PLAN.md to understand current status and next steps.

Read DEVLOG.md for context on recent decisions, failures, and design rationale.

## Session Resume Context (updated 2026-03-17)

### Current State
- **Branch**: phase-c
- **Integration tests**: 148 pass, 1 skip
- **NIST kernel tests passing 100%**: NC101A (94/94), NC102A (39/39), NC103A (103/103),
  NC106A (127/127), NC116A (67/67), NC118A (30/30), NC171A (109/109), NC176A (125/125)
- **Next NIST test to run**: NC104A

### What was just completed
- COBOL-85 grammar overhaul with dialect gates (default level 85)
- Section support (GO TO/PERFORM section-name, implicit THRU)
- PERFORM TIMES with runtime loop (IrPerformTimes, IrPerformInlineTimes)
- LowerCondition rewrite: normalize → classify → matrix dispatch
- PicDescriptorFactory: fixed $, +, -, 0 digit counting for edited fields
- FormatByEditPattern: fixed vs floating symbol handling
- MOVE-to-edited fields (alphanumeric-edited and numeric-edited)
- Comparison classifier: both operands must be strictly Numeric for numeric comparison
- Pseudo-MOVE sign stripping for mixed numeric/alphanumeric comparisons
- Complete file I/O (DELETE, START, WRITE FROM, OPEN I-O, org-aware registration)
- STRING, UNSTRING, SEARCH/SEARCH ALL, EXIT PERFORM

### Key architectural decisions
- Sections map to paragraph index ranges via SemanticModel section-paragraph membership
- ResolveProcedureName: GO TO → first paragraph; THRU end → last paragraph
- IrPerformInlineTimes uses CIL-local int counter (IrTemp concept) — no unrolling
- ComparisonOperand normalizes BoundExpressions before matrix dispatch
- All END-xxx scope terminators are COBOL-85 (ungated)
- Dialect gates on: TYPE, RETURNING, BY VALUE, DELETE FILE, JSON/XML, INVOKE, FUNCTION

### Known gaps
- CALL/USING/RETURNING (parse only, not bound/lowered)
- SORT/MERGE (parse only)
- EXIT SECTION/EXIT PARAGRAPH (not yet implemented)
- Alternate keys (not parsed)
- Some NIST preprocessor placeholders not mapped
