# CobolSharp Conformance Documentation

> **STATUS NOTE (doc-classifier):** This document is a legacy compliance snapshot. For current implementation status, consult `docs/MASTER_PLAN.md` §2 (current state as of 2026-06-07) and `docs/ISO2023_CONFORMANCE_PLAN.md` (the authoritative conformance SSOT). Implementations reflected here may be stale relative to recent Stage-4 migrations (data model, pointers, OO grammar).

## ISO/IEC 1989:2023 Conformance Status

### Implemented Features

| Feature | Status | Notes |
|---------|--------|-------|
| Free-form reference format | Full | Default mode |
| Fixed-form reference format | Full | Auto-detected, converted to free-form |
| IDENTIFICATION DIVISION | Full | PROGRAM-ID, CLASS-ID, METHOD-ID, INTERFACE-ID |
| ENVIRONMENT DIVISION | Partial | FILE-CONTROL parsed; CONFIGURATION skipped |
| DATA DIVISION | Full | FILE, WORKING-STORAGE, LINKAGE, REPORT, SCREEN sections |
| PROCEDURE DIVISION | Full | All major statements parsed |
| Paragraphs and sections | Full | Including PERFORM THRU |
| PICTURE clause | Full | All symbols including editing |
| USAGE clause | Full | DISPLAY, BINARY, PACKED-DECIMAL, INDEX, POINTER, NATIONAL |
| Data hierarchy | Full | Levels 01-49, 66, 77, 88; OCCURS, REDEFINES |
| Arithmetic | Full | ADD, SUBTRACT, MULTIPLY, DIVIDE, COMPUTE |
| Conditionals | Full | IF/ELSE, EVALUATE/WHEN, relational/class/sign conditions |
| PERFORM | Full | Inline, out-of-line, THRU, TIMES, UNTIL |
| GO TO | Partial | Basic transfer; cross-PERFORM not supported |
| File I/O | Full | Sequential, indexed, relative; OPEN/READ/WRITE/CLOSE |
| COPY/REPLACE | Full | Preprocessor with REPLACING, nested COPY |
| Intrinsic functions | ~70 | Math, string, date/time, financial, aggregates |
| CALL/CANCEL | Parsing | CIL emission pending |
| STRING/UNSTRING/INSPECT | Parsing | Partial — INSPECT BACKWARD done (DEVLOG 378); core architecture designed |
| Report Writer | Parsing | Runtime pending |
| OO COBOL | Grammar DONE; Semantic/CIL pending | Grammar version-factored (2026-06-04); semantic/emit per OO_IMPLEMENTATION_DESIGN.md §6.6 (DEVLOG 440) |
| Exception handling | Parsing | Runtime pending |

### Implementor-Defined Behavior

- **Character set**: ASCII (UTF-8). EBCDIC supported via codepage conversion.
- **Maximum record length**: Limited by .NET array size (2GB theoretical).
- **Maximum numeric precision**: 28-29 digits (.NET decimal type).
- **File naming**: External file names map directly to OS file paths.
- **Indexed file implementation**: In-memory SortedDictionary, persisted to flat file.

### Processor-Dependent Behavior

- **USAGE BINARY size**: 2 bytes (1-4 digits), 4 bytes (5-9), 8 bytes (10-18). With the typed-native data model (Stage-4 slice 2, DEVLOG 416), the binder targets a .NET 10 `long` for these items.
- **USAGE PACKED-DECIMAL**: Standard BCD encoding.
- **POINTER**: A pointer is represented by the single managed carrier `ManagedPointer` (GC-tracked managed reference; no native `unsafe`, no 8-byte byte handle, no PointerRegistry). See `docs/RECORD_STRUCT_STORAGE_DESIGN.md` §10.
- **Collating sequence**: ASCII/Unicode ordinal.
