---
title: Language Features
area: spec
status: draft
last_updated: 2026-07-23
related_files:
  - README.md
  - specs/ISO_COBOL.md
  - docs/COBOLNET_DESIGN.md
  - docs/CONFORMANCE.md
tags:
  - cobolsharp
  - spec
---

# Language Features

COBOL.NET implements standard COBOL by **division**, translating source into idiomatic typed-native C# (a COBOL
record becomes a `record struct`; an elementary item becomes a native field — no byte substrate; see
[[kb/Architecture/High-Level Design]]). The implemented surface (README + spec §11–§16):

## Identification Division
PROGRAM-ID and program structure; the obsolete comment paragraphs (AUTHOR, INSTALLATION, DATE-WRITTEN,
DATE-COMPILED, SECURITY) accepted at 85 and edition-gated thereafter.

## Environment Division
- **SPECIAL-NAMES**: implementor switches (ON/OFF STATUS), **ALPHABET** (STANDARD-1, STANDARD-2, NATIVE, literal
  `THRU`/`ALSO`, hexadecimal), **CLASS** definitions, `DECIMAL-POINT IS COMMA`, `CURRENCY SIGN … WITH PICTURE SYMBOL` (2002+).
- **FILE-CONTROL**: SELECT, ASSIGN, ORGANIZATION, ACCESS MODE, FILE STATUS, RECORD KEY, ALTERNATE RECORD KEY,
  COLLATING SEQUENCE, SHARING / LOCK MODE / RETRY.

## Data Division
- **PICTURE** symbols: `9 X A S V P Z * + - CR DB B 0 /`.
- **USAGE**: DISPLAY, BINARY/COMP/COMP-4, PACKED-DECIMAL/COMP-3, COMP-5, INDEX, BINARY-CHAR/-SHORT/-LONG/-DOUBLE,
  FLOAT-SHORT/-LONG (COMP-1/COMP-2), FLOAT-BINARY-32/-64; NATIONAL (UTF-16); `PACKED-DECIMAL … WITH NO SIGN` (2023).
- **Hierarchy**: groups, elementary items, OCCURS (fixed + `DEPENDING ON` + `OCCURS DYNAMIC`), `ASCENDING/DESCENDING
  KEY`, `INDEXED BY`, REDEFINES, RENAMES (level 66), levels 77/88.
- **Clauses**: VALUE (literals, figurative constants, VALUE THRU on 88s), BLANK WHEN ZERO, JUSTIFIED RIGHT,
  SIGN IS LEADING/TRAILING SEPARATE, DYNAMIC LENGTH.

## Procedure Division
- **Arithmetic**: ADD, SUBTRACT, MULTIPLY, DIVIDE (REMAINDER), COMPUTE (full precedence), ON SIZE ERROR, ROUNDED
  (all §14.7.4 modes: NEAREST-AWAY-FROM-ZERO, NEAREST-EVEN, PROHIBITED, TRUNCATION).
- **Control flow**: IF/END-IF, EVALUATE/WHEN/OTHER, PERFORM (inline, out-of-line, THRU, TIMES, UNTIL, VARYING),
  GO TO, ALTER. See [[kb/IR/Control Flow]].
- **Data movement**: MOVE (category-compatibility matrix), MOVE CORRESPONDING, INITIALIZE.
- **String**: STRING, UNSTRING (DELIMITED BY, POINTER, TALLYING, OVERFLOW), INSPECT (TALLYING, REPLACING, CONVERTING).
- **Conditions**: relational, sign, class (NUMERIC/ALPHABETIC/…), condition-name, switch-status, abbreviated combined.
- **File I/O**: OPEN, CLOSE, READ (sequential + keyed, PREVIOUS), WRITE (BEFORE/AFTER ADVANCING), REWRITE, DELETE,
  START; **organizations**: sequential, line-sequential, indexed (+ alternate keys), relative; DELETE FILE (2023).
- **Inter-program**: CALL (static/dynamic, BY REFERENCE/CONTENT/VALUE, RETURNING), ENTRY, CANCEL, ON EXCEPTION,
  INITIAL programs; GOBACK, STOP RUN (WITH STATUS), EXIT PROGRAM/PARAGRAPH/SECTION.

## Intrinsic Functions (§15)
~70 functions across categories: **math** (SQRT, LOG, MOD, FACTORIAL, REM), **string** (LENGTH, REVERSE,
UPPER-CASE, LOWER-CASE, TRIM), **date/time** (CURRENT-DATE, INTEGER-OF-DATE), **financial** (ANNUITY,
PRESENT-VALUE), **aggregates** (MAX, MIN, SUM, MEAN, MEDIAN). 2023 additions (BASECONVERT, CONCAT, CONVERT,
FIND-STRING, SUBSTITUTE, …) are edition-gated. See [[kb/Runtime/Execution Model]].

## OO & post-85
OO core (CLASS-ID, INHERITS single-dispatch method resolution, INVOKE); the exception-condition (EC) engine with
declaratives and RESUME (§14.9.33); Report Writer nucleus (partial); JSON/XML GENERATE surface. Multiple inheritance
and parametric polymorphism are the *optional* A.4.10 items — not claimed.

## Key concepts
- Typed-native model: record→`record struct`, elementary→native field.
- Full PICTURE symbol set; broad USAGE incl. IEEE float + NATIONAL/UTF-16.
- Complete verb set per §14; EC engine + declaratives; single-dispatch OO.
- ~70 intrinsics in 5 categories; 2023 intrinsics edition-gated.

## See also
- [[kb/Spec/Version Targeting]] — which features exist in which edition.
- [[kb/Runtime/Execution Model]] — how verbs/intrinsics execute at runtime.
- [[kb/IR/Data Flow]] — how PICTURE/USAGE map to native .NET types.

## Backlinks
- [[kb/Spec/MOC]] — indexes this note.
- [[kb/Index]] — lists this as a major note.
- Lookup: [[kb/Spec/Lookup/Keywords]] · [[kb/Spec/Lookup/Grammar]] · [[kb/Spec/Lookup/Construct Catalogue]].
- Pipeline: [[kb/Compiler/Pipeline-to-ISO-Mapping]] — the Lex / Parse / Bind / Codegen / Runtime phases map here.
