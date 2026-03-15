CobolSharp COBOL Semantic Rules & Edge‑Case Behavior Specification (CIL‑Only)
============================================================================

Purpose
-------
Define the authoritative semantic rules and edge‑case behaviors that CobolSharp must implement to ensure:
- Full COBOL‑85 → COBOL‑2023 correctness
- Predictable, deterministic execution
- Compatibility with industry‑standard COBOL compilers
- Clean lowering to .NET CIL
- Correct integration with the CobolSharp runtime

This document is the semantic “constitution” of the compiler.

------------------------------------------------------------
SECTION 1 — DATA SEMANTICS
------------------------------------------------------------

1.1 PIC semantics
-----------------
PIC X(n):
- Always space‑padded on assignment
- Truncation on overflow
- No implicit numeric conversion

PIC 9(n):
- Right‑justified
- Zero‑padded
- Truncation on overflow
- Numeric class test = true

PIC S9(n):
- Same as PIC 9(n) but with sign
- Sign stored per USAGE rules

PIC with V:
- Implied decimal point
- Stored without decimal point
- Decimal alignment required on arithmetic

1.2 USAGE semantics
-------------------
DISPLAY:
- Stored as ASCII/Unicode characters
- No binary interpretation

COMP:
- Binary integer
- Size depends on compiler option (CobolSharp uses fixed mapping)

COMP‑3 (packed decimal):
- Two digits per byte
- Last nibble is sign
- Odd number of digits padded with leading zero

COMP‑5:
- Native binary integer
- No truncation on assignment

1.3 REDEFINES
-------------
Rules:
- No storage allocation for redefined items
- All children share the same byte region
- Moves to one view must update all views
- Condition names (88‑levels) must reflect current storage

Edge cases:
- Redefining a larger item over a smaller one is allowed
- Redefining OCCURS is allowed but must not change storage size
- Redefining COMP‑3 over DISPLAY is allowed but requires careful marshaling

1.4 OCCURS DEPENDING ON
------------------------
Rules:
- Upper bound fixed at compile time
- Actual bound determined at runtime
- DEPENDING ON variable must be numeric
- Out‑of‑range values clamp to legal range
- Storage allocated for max size; logical size varies

1.5 Condition names (88‑levels)
-------------------------------
Rules:
- Boolean tests on storage
- Multiple 88‑levels may be true simultaneously
- VALUE ranges allowed
- VALUE THRU allowed

------------------------------------------------------------
SECTION 2 — MOVE & STRING SEMANTICS
------------------------------------------------------------

2.1 MOVE rules
--------------
MOVE literal TO alphanumeric:
- Space‑pad or truncate

MOVE literal TO numeric:
- Validate numeric content
- On invalid numeric → runtime exception (ON EXCEPTION)

MOVE alphanumeric TO numeric:
- Same as above

MOVE numeric TO alphanumeric:
- Convert to DISPLAY format
- Right‑justify unless JUSTIFIED

MOVE CORRESPONDING:
- Match by name
- Skip non‑matching fields
- Skip group items without elementary children

2.2 STRING
----------
Rules:
- Concatenate operands left‑to‑right
- Respect POINTER
- Respect DELIMITED BY
- OVERFLOW if target too small

2.3 UNSTRING
------------
Rules:
- Split input by delimiters
- Respect TALLYING
- Respect POINTER
- Respect COUNT IN
- OVERFLOW if insufficient receiving fields

------------------------------------------------------------
SECTION 3 — ARITHMETIC SEMANTICS
------------------------------------------------------------

3.1 Decimal alignment
---------------------
Before arithmetic:
- Align decimal points
- Pad with zeros as needed

3.2 ROUNDED
-----------
Rules:
- Round half‑up
- Apply rounding only to target field
- Intermediate results use full precision

3.3 SIZE ERROR
--------------
Occurs when:
- Result cannot fit in target field
- Packed decimal overflow
- Binary overflow
- Division by zero

ON SIZE ERROR:
- Suppresses assignment
- Executes handler block

NOT ON SIZE ERROR:
- Executes only if no size error occurred

3.4 COMPUTE
-----------
Rules:
- Evaluate expression left‑to‑right
- Use decimal arithmetic for mixed types
- Use binary arithmetic only when all operands are binary

------------------------------------------------------------
SECTION 4 — CONTROL FLOW SEMANTICS
------------------------------------------------------------

4.1 PERFORM
-----------
PERFORM paragraph:
- Calls paragraph as subroutine
- Returns to next statement

PERFORM THRU:
- Calls range of paragraphs
- Returns after last paragraph

PERFORM UNTIL:
- Test at top of loop
- Loop may execute zero times

PERFORM VARYING:
- Initialize
- Test
- Execute
- Increment
- Repeat

4.2 GO TO
---------
Rules:
- Unconditional branch
- May cross paragraph boundaries
- Illegal to branch into/out of DECLARATIVES

4.3 EVALUATE
------------
Rules:
- First matching WHEN executes
- WHEN OTHER always matches
- Conditions evaluated in order

------------------------------------------------------------
SECTION 5 — FILE I/O SEMANTICS
------------------------------------------------------------

5.1 OPEN
--------
INPUT:
- File must exist
- Position at first record

OUTPUT:
- Create or truncate file

I‑O:
- File must exist
- Allow read/write

EXTEND:
- Create if missing
- Position at end

5.2 READ
--------
READ NEXT:
- Sequential read
- AT END on EOF

READ PREVIOUS:
- Only for indexed/relative
- AT END on BOF

READ KEY:
- Key must exist
- INVALID KEY on failure

5.3 WRITE / REWRITE / DELETE
----------------------------
WRITE:
- Append or insert depending on organization

REWRITE:
- Requires successful READ first

DELETE:
- Only for indexed/relative

------------------------------------------------------------
SECTION 6 — EXCEPTION SEMANTICS
------------------------------------------------------------

6.1 INVALID KEY
---------------
Triggered by:
- READ KEY failure
- REWRITE without prior READ
- DELETE without prior READ
- Duplicate key on WRITE

6.2 AT END
----------
Triggered by:
- READ NEXT on EOF
- READ PREVIOUS on BOF

6.3 ON EXCEPTION
----------------
Triggered by:
- Numeric overflow
- Packed decimal errors
- JSON/XML errors
- File I/O errors not covered by INVALID KEY or AT END

------------------------------------------------------------
SECTION 7 — OO & GENERICS SEMANTICS
------------------------------------------------------------

7.1 CLASS-ID
------------
Rules:
- Maps to .NET class
- Supports inheritance
- Supports interfaces

7.2 METHOD-ID
-------------
Rules:
- Maps to .NET method
- Supports parameters and return values
- Supports overloading (via signature)

7.3 GENERIC classes/methods
---------------------------
Rules:
- Type parameters map to .NET generics
- Constraints enforced at compile time
- Specialization allowed in optimizer

------------------------------------------------------------
SECTION 8 — JSON/XML SEMANTICS
------------------------------------------------------------

8.1 JSON PARSE
--------------
Rules:
- Map JSON object to COBOL group
- Map arrays to OCCURS
- ON EXCEPTION for malformed JSON

8.2 JSON GENERATE
-----------------
Rules:
- Serialize COBOL group to JSON
- WITH DETAIL preserves nested structure

8.3 XML PARSE / GENERATE
------------------------
Rules:
- Similar to JSON
- COUNT IN tracks node count
- PROCESSING PROCEDURE allowed

------------------------------------------------------------
SECTION 9 — EDGE‑CASE BEHAVIOR
------------------------------------------------------------

9.1 Zero‑length fields
----------------------
- Allowed
- Always treated as empty string
- Cannot be numeric

9.2 Negative OCCURS DEPENDING ON
--------------------------------
- Clamp to 0

9.3 MOVE to overlapping REDEFINES
---------------------------------
- Must update all views consistently

9.4 PERFORM with GO TO inside
-----------------------------
- Allowed
- Must not break call/return semantics

9.5 Numeric conversion of invalid DISPLAY data
----------------------------------------------
- ON EXCEPTION triggered
- No assignment performed

9.6 Packed decimal with invalid sign nibble
-------------------------------------------
- ON EXCEPTION triggered

9.7 EVALUATE with mixed types
-----------------------------
- All operands converted to common type
- Numeric > alphanumeric > boolean

------------------------------------------------------------
Summary
------------------------------------------------------------
The CobolSharp Semantic Rules & Edge‑Case Behavior Specification:
- Defines the authoritative semantics for all COBOL features
- Ensures correctness, determinism, and compatibility
- Covers data, arithmetic, control flow, file I/O, OO, JSON/XML, and exceptions
- Specifies all edge‑case behaviors required for real‑world COBOL workloads
- Is fully aligned with the CIL‑only architecture
