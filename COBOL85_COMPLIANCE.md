# COBOL-85 Compliance Status

Tracks ISO/IEC 1989:1985 (COBOL-85) compliance for the CobolSharp compiler.
Non-COBOL-85 features are dialect-gated in the grammar (default dialect level: 85).

## Divisions & Sections

| Feature | Grammar | Binding | Lowering | Emitter | Tests | Status |
|---------|---------|---------|----------|---------|-------|--------|
| IDENTIFICATION DIVISION | Yes | Yes | N/A | N/A | Yes | Done |
| ENVIRONMENT DIVISION | Yes | Yes | N/A | N/A | Yes | Done |
| CONFIGURATION SECTION | Yes | Partial | N/A | N/A | - | Tolerant (accepts any IDENTIFIER) |
| INPUT-OUTPUT SECTION | Yes | Yes | N/A | N/A | Yes | Done |
| FILE-CONTROL (SELECT/ASSIGN/ORG/ACCESS/KEY/STATUS) | Yes | Yes | Yes | Yes | Yes | Done |
| DATA DIVISION | Yes | Yes | N/A | N/A | Yes | Done |
| FILE SECTION (FD) | Yes | Yes | Yes | Yes | Yes | Done |
| WORKING-STORAGE SECTION | Yes | Yes | Yes | Yes | Yes | Done |
| LINKAGE SECTION | Yes | Partial | - | - | - | Parse only |
| PROCEDURE DIVISION | Yes | Yes | Yes | Yes | Yes | Done |
| PROCEDURE DIVISION USING | Yes | Partial | - | - | - | Parse only |

## Data Description

| Feature | Grammar | Binding | Lowering | Emitter | Tests | Status |
|---------|---------|---------|----------|---------|-------|--------|
| Levels 01-49, 77 | Yes | Yes | Yes | Yes | Yes | Done |
| Level 66 (RENAMES) | Yes | Partial | - | - | - | Parse only |
| Level 88 (conditions) | Yes | Yes | Yes | Yes | Yes | Done |
| PIC clause | Yes | Yes | Yes | Yes | Yes | Done |
| USAGE (DISPLAY/COMP/COMP-3/BINARY) | Yes | Yes | Yes | Yes | Yes | Done |
| OCCURS (1D/2D/3D) | Yes | Yes | Yes | Yes | Yes | Done |
| OCCURS DEPENDING ON | Yes | Partial | - | - | - | Parse only |
| INDEXED BY | Yes | Partial | - | - | - | Parse only |
| REDEFINES | Yes | Yes | Yes | Yes | Yes | Done |
| VALUE clause | Yes | Yes | Yes | Yes | Yes | Done |
| SIGN clause | Yes | Yes | Yes | Yes | Yes | Done |
| JUSTIFIED | Yes | Partial | - | - | - | Parse only |
| SYNCHRONIZED | Yes | Partial | - | - | - | Parse only |
| BLANK WHEN ZERO | Yes | Yes | Yes | Yes | Yes | Done |

## Procedure Division Verbs

| Verb | Grammar | Binding | Lowering | Emitter | Tests | Status |
|------|---------|---------|----------|---------|-------|--------|
| ACCEPT (DATE/TIME/DAY/DAY-OF-WEEK) | Yes | Yes | Yes | Yes | Yes | Done |
| ADD (TO/GIVING/ON SIZE ERROR) | Yes | Yes | Yes | Yes | Yes | Done |
| CALL (BY REFERENCE/CONTENT) | Yes | Partial | - | - | - | Parse only |
| CANCEL | Yes | - | - | - | - | Parse only |
| CLOSE | Yes | Yes | Yes | Yes | Yes | Done |
| COMPUTE | Yes | Yes | Yes | Yes | Yes | Done |
| CONTINUE | Yes | Yes | Yes | Yes | - | Done |
| DELETE (record) | Yes | Yes | Yes | Yes | Yes | Done |
| DISPLAY | Yes | Yes | Yes | Yes | Yes | Done |
| DIVIDE (INTO/BY/GIVING/REMAINDER) | Yes | Yes | Yes | Yes | Yes | Done |
| EVALUATE (TRUE/subjects/WHEN/WHEN OTHER) | Yes | Yes | Yes | Yes | Yes | Done |
| EXIT | Yes | Yes | Yes | Yes | - | Done |
| EXIT PERFORM | Yes | Yes | Yes | Yes | Yes | Done |
| GO TO (simple) | Yes | Yes | Yes | Yes | Yes | Done |
| GO TO DEPENDING ON | Yes | Yes | Yes | Yes | Yes | Done |
| GOBACK | Yes | Yes | Yes | Yes | - | Done |
| IF / ELSE / END-IF | Yes | Yes | Yes | Yes | Yes | Done |
| INITIALIZE | Yes | Yes | Yes | Yes | Yes | Done |
| INSPECT (TALLYING/REPLACING/CONVERTING) | Yes | Yes | Yes | Yes | Yes | Done |
| MERGE | Yes | - | - | - | - | Parse only |
| MOVE (identifier/literal) | Yes | Yes | Yes | Yes | Yes | Done |
| MULTIPLY (BY/GIVING) | Yes | Yes | Yes | Yes | Yes | Done |
| NEXT SENTENCE | Yes | Yes | Yes | Yes | Yes | Done |
| OPEN (INPUT/OUTPUT/I-O/EXTEND) | Yes | Yes | Yes | Yes | Yes | Done |
| PERFORM (simple/THRU/TIMES/UNTIL/VARYING/AFTER/inline) | Yes | Yes | Yes | Yes | Yes | Done |
| READ (sequential/AT END/NOT AT END/INTO) | Yes | Yes | Yes | Yes | Yes | Done |
| RELEASE | Yes | - | - | - | - | Parse only |
| RETURN | Yes | - | - | - | - | Parse only |
| REWRITE | Yes | Yes | Yes | Yes | Yes | Done |
| SEARCH | Yes | Yes | Yes | Yes | Yes | Done |
| SEARCH ALL | Yes | Yes | Yes | Yes | Yes | Done |
| SET (TO TRUE/FALSE) | Yes | Yes | Yes | Yes | Yes | Done |
| SET (index UP/DOWN BY) | Yes | Yes | Yes | Yes | - | Done |
| SORT | Yes | - | - | - | - | Parse only |
| START | Yes | Yes | Yes | Yes | Yes | Done |
| STOP RUN | Yes | Yes | Yes | Yes | Yes | Done |
| STRING | Yes | Yes | Yes | Yes | Yes | Done |
| SUBTRACT (FROM/GIVING/ON SIZE ERROR) | Yes | Yes | Yes | Yes | Yes | Done |
| UNSTRING | Yes | Yes | Yes | Yes | Yes | Done |
| WRITE (record/FROM/AFTER ADVANCING) | Yes | Yes | Yes | Yes | Yes | Done |

## File I/O

| Feature | Status |
|---------|--------|
| Sequential file handler (binary + line-sequential) | Done |
| Relative file handler | Done |
| Indexed file handler (SortedDictionary-based) | Done |
| OPEN INPUT/OUTPUT/I-O/EXTEND | Done |
| CLOSE | Done |
| READ sequential (AT END/NOT AT END) | Done |
| READ INTO | Done |
| WRITE / WRITE FROM | Done |
| WRITE AFTER/BEFORE ADVANCING | Done |
| REWRITE | Done |
| DELETE (indexed/relative) | Done |
| START (indexed, all conditions) | Done |
| INVALID KEY / NOT INVALID KEY | Done (DELETE, START) |
| FILE STATUS codes (00, 10, 21-24, 30, 35, 37, 41-44, 47) | Done |
| Organization-aware file registration | Done |
| Record length from FD layout | Done |

## Dialect Gates (non-COBOL-85 features disabled by default)

| Feature | Dialect | Gate |
|---------|---------|------|
| TYPE clause | 2023 | `{is2023()}?` |
| LINKAGE procedure parameters | 2002 | `{is2002()}?` |
| RETURNING in PROCEDURE DIVISION | 2002 | `{is2002()}?` |
| CALL BY VALUE | 2002 | `{is2002()}?` |
| SET object reference | 2002 | `{is2002()}?` |
| DELETE FILE | 2023 | `{is2023()}?` |
| JSON statements | 2014 | `{is2014()}?` |
| XML statements | 2014 | `{is2014()}?` |
| INVOKE statement | 2002 | `{is2002()}?` |
| Inline method invocation | 2023 | `{is2023()}?` |
| END-ADD/SUBTRACT/MULTIPLY/DIVIDE/COMPUTE | 2002 | `{is2002()}?` |
| END-WRITE/REWRITE/DELETE/RETURN/START | 2002 | `{is2002()}?` |
| END-SORT/MERGE/CALL | 2002 | `{is2002()}?` |

## Semantic Constraints (enforced in binder, not grammar)

| Constraint | Status |
|------------|--------|
| DIVIDE REMAINDER requires GIVING | Not enforced |
| MULTIPLY BY targets in non-GIVING must be identifiers | Not enforced |
| READ: AT END vs INVALID KEY per file organization | Not enforced |
| PERFORM: no illegal option combinations | Not enforced |

## NIST Kernel Test Status

| Test | Status | Notes |
|------|--------|-------|
| NC101A (MULTIPLY) | 94/94 PASS | Byte-for-byte match |
| NC102A | In progress | Grammar fixes applied |
| NC103A-NC107A | Not tested | |
| NC106A (SUBTRACT) | 127/127 PASS | |
| NC108M-NC141A | Not tested | |
| NC170A-NC177A | Not tested | |
| NC171A (DIVIDE) | 109/109 PASS | |
| NC176A (ADD) | 125/125 PASS | |
| NC116A (SUBTRACT/SIGN) | 67/67 PASS | |
| NC118A (ADD GIVING) | 30/30 PASS | |
| NC201A-NC254A | Not tested | |
| NC302M-NC401M | Not tested | |
