CobolSharp COBOL Language Feature Support Matrix (CIL‑Only)
==========================================================

Purpose
-------
Provide a comprehensive, authoritative matrix of COBOL language features supported by CobolSharp, aligned with ISO/IEC 1989:2023.  
This document defines:
- What is fully supported
- What is partially supported
- What is planned
- What is intentionally not supported

All features listed here compile to **.NET CIL only** — no VM, no alternate backend.

Legend
------
Status values:
- FULL: Fully implemented and tested
- PARTIAL: Implemented with limitations
- PLANNED: Scheduled for implementation
- NO: Not supported by design

Sections:
1. Data Division  
2. Procedure Division  
3. Control Flow  
4. Arithmetic & Numeric  
5. File I/O  
6. OO & Generics  
7. Intrinsic Functions  
8. JSON/XML  
9. Compiler Directives  
10. Dialect Support  
11. Platform Integration  

1. Data Division
----------------
01‑level items: FULL  
Group items: FULL  
Elementary items: FULL  
PIC X: FULL  
PIC 9: FULL  
PIC S9: FULL  
PIC with V (implied decimal): FULL  
USAGE DISPLAY: FULL  
USAGE COMP: FULL  
USAGE COMP‑3 (packed decimal): FULL  
USAGE COMP‑5: FULL  
USAGE BINARY: FULL  
USAGE NATIONAL: FULL  
USAGE POINTER: PARTIAL (managed pointer only)  
REDEFINES: FULL  
RENAMES: FULL  
OCCURS: FULL  
OCCURS DEPENDING ON: FULL  
SYNC: NO (ignored)  
JUSTIFIED: FULL  
BLANK WHEN ZERO: FULL  
SIGN LEADING/TRAILING: FULL  
EXTERNAL: FULL  
GLOBAL: FULL  
CONSTANT: FULL  
VALUE clause: FULL  
VALUE OF: FULL  
ANY LENGTH: PARTIAL (string only)  

2. Procedure Division
---------------------
Paragraphs: FULL  
Sections: FULL  
ENTRY points: FULL  
GIVING RETURNING: FULL  
CHAINING: FULL  
DECLARATIVES: FULL  
USE BEFORE REPORTING: PLANNED  
USE AFTER STANDARD ERROR: FULL  
USE AFTER STANDARD EXCEPTION: FULL  
CALL: FULL (.NET interop included)  
INVOKE: FULL  
PERFORM: FULL  
PERFORM THRU: FULL  
PERFORM UNTIL: FULL  
PERFORM VARYING: FULL  
PERFORM TIMES: FULL  
GO TO: FULL  
GO TO DEPENDING ON: FULL  
STOP RUN: FULL  
EXIT PROGRAM: FULL  
EXIT PARAGRAPH: FULL  
EXIT SECTION: FULL  
CONTINUE: FULL  

3. Control Flow
----------------
IF/ELSE: FULL  
EVALUATE: FULL  
SEARCH: FULL  
SEARCH ALL: FULL  
NEXT SENTENCE: FULL  
ALTER: NO (intentionally unsupported)  
ON EXCEPTION: FULL  
NOT ON EXCEPTION: FULL  
INVALID KEY: FULL  
AT END: FULL  

4. Arithmetic & Numeric
-----------------------
ADD: FULL  
SUBTRACT: FULL  
MULTIPLY: FULL  
DIVIDE: FULL  
COMPUTE: FULL  
ROUNDED: FULL  
SIZE ERROR: FULL  
ON SIZE ERROR: FULL  
CORRESPONDING (CORR): FULL  
NUMERIC class tests: FULL  
POSITIVE/NEGATIVE/ZERO: FULL  
OVERFLOW: FULL  
Packed decimal arithmetic: FULL  
Binary arithmetic: FULL  
Decimal arithmetic: FULL  

5. File I/O
-----------
Sequential files: FULL  
Relative files: FULL  
Indexed files: FULL  
ORGANIZATION: FULL  
ACCESS MODE: FULL  
READ NEXT/PREVIOUS: FULL  
READ KEY: FULL  
WRITE: FULL  
REWRITE: FULL  
DELETE: FULL  
START: FULL  
OPEN INPUT/OUTPUT/I‑O/EXTEND: FULL  
CLOSE: FULL  
LOCK MODE: PARTIAL  
FILE STATUS: FULL  
LINAGE: PLANNED  
REPORT WRITER: NO (intentionally excluded)  

6. OO & Generics
----------------
CLASS-ID: FULL  
OBJECT: FULL  
INHERITS: FULL  
IMPLEMENTS: FULL  
METHOD-ID: FULL  
PROPERTY: FULL  
FACTORY methods: FULL  
STATIC methods: FULL  
INSTANCE methods: FULL  
NEW: FULL  
SELF/SUPER: FULL  
INTERFACE-ID: FULL  
GENERIC classes: FULL  
GENERIC methods: FULL  
GENERIC constraints: FULL  
EVENTS: PLANNED  
DELEGATES: PLANNED  

7. Intrinsic Functions
----------------------
NUMVAL: FULL  
NUMVAL‑C: FULL  
INTEGER: FULL  
INTEGER‑PART: FULL  
FRACTION‑PART: FULL  
LENGTH: FULL  
TRIM: FULL  
LOWER‑CASE: FULL  
UPPER‑CASE: FULL  
RANDOM: FULL  
CURRENT‑DATE: FULL  
WHEN‑COMPILED: FULL  
ORD/ORD‑MAX/ORD‑MIN: FULL  
REVERSE: FULL  
SUBSTITUTE: FULL  
SUBSTITUTE‑CASE: FULL  
JSON‑related functions: FULL  
XML‑related functions: FULL  
LOCALE‑dependent functions: PARTIAL  

8. JSON/XML
-----------
JSON PARSE: FULL  
JSON GENERATE: FULL  
XML PARSE: FULL  
XML GENERATE: FULL  
COUNT IN: FULL  
PROCESSING PROCEDURE: FULL  
VALIDATION: PARTIAL  
SCHEMA‑driven mapping: PLANNED  

9. Compiler Directives
----------------------
COPY: FULL  
COPY REPLACING: FULL  
REPLACE: FULL  
>>IF/>>ELSE/>>END‑IF: FULL  
>>DEFINE: FULL  
>>UNDEFINE: FULL  
>>EVALUATE: FULL  
>>SET: FULL  
>>DISPLAY: FULL  
>>SOURCE: FULL  
>>PAGE: NO (ignored)  
>>TITLE: NO (ignored)  

10. Dialect Support
-------------------
COBOL‑85: FULL  
COBOL‑2002: FULL  
COBOL‑2014: FULL  
COBOL‑2023: FULL  
IBM extensions: PARTIAL  
Micro Focus extensions: PARTIAL  
GnuCOBOL extensions: PARTIAL  
ANSI‑74: NO  

11. Platform Integration
------------------------
CALL to .NET methods: FULL  
INVOKE .NET objects: FULL  
Interop marshaling: FULL  
Async .NET calls: PARTIAL (via helpers)  
Reflection: PARTIAL  
Unsafe pointers: NO  
Native P/Invoke: PLANNED  
AOT compatibility: FULL  
WASM compatibility: FULL (via dotnet publish)  

Summary
-------
CobolSharp provides:
- Full support for the vast majority of COBOL‑85 → COBOL‑2023 features
- Full support for OO, generics, JSON/XML, and modern constructs
- Full support for .NET interop
- Full support for CIL‑only compilation
- Partial support for legacy or obscure features
- Intentional exclusion of ALTER and REPORT WRITER

This matrix defines the authoritative feature baseline for CobolSharp.
