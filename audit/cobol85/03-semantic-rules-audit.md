# Semantic Rules Audit

**Date:** 2026-03-30 | **Mode:** DRY-RUN (read-only) | **Auditor:** Claude Opus 4.6
**Evidence sources:** `audit/cobol85/integration.trx`, `audit/cobol85/source-snapshot.txt`, NIST baselines (77 FAIL* across 14 tests)

| Category | Rule | Implemented? | Tested? | Evidence | Notes |
|----------|------|:---:|:---:|----------|-------|
| **Numeric/MOVE** | MOVE compatibility matrix (§14.9.26) | Yes | Partially | CategoryCompatibility.cs in source-snapshot; 43 data tests pass (integration.trx) | CBL0901/CBL0906/CBL0907/CBL0908 descriptors exist |
| **Numeric/MOVE** | Numeric truncation (left for high-order) | Yes | Yes | NC101A passes 0 FAIL*; PicRuntime.cs in source-snapshot | |
| **Numeric/MOVE** | Alphanumeric truncation (right) | Yes | Yes | StorageArea.cs in source-snapshot; data tests pass | |
| **Numeric/MOVE** | JUSTIFIED RIGHT | Yes | Unclear | StorageArea.cs in source-snapshot | No specific test name in TRX |
| **Numeric/MOVE** | NumericEdited → Alphanumeric (raw byte) | Yes | Unclear | Code path exists in CilDataEmitter | No specific test name in TRX |
| **Numeric/MOVE** | Group items as alphanumeric | Yes | Yes | Data tests pass | |
| **PERFORM** | TEST BEFORE (default) | Yes | Yes | 73 CF tests pass (integration.trx) | |
| **PERFORM** | TEST AFTER | Yes | Yes | 73 CF tests pass | |
| **PERFORM** | Range overlap detection (§14.8.35) | No | No | — | Not implemented |
| **PERFORM** | VARYING with multiple AFTER | Yes | Unclear | ControlFlowBinder in source-snapshot | NC201A 5 FAIL* may relate |
| **PERFORM** | THRU with section names | Yes | Yes | ProcedureNameResolver in source-snapshot; CF tests pass | |
| **EVALUATE** | TRUE/FALSE subjects | Yes | Partially | NC225A passes with 7 FAIL* | 7 FAIL* suggest edge cases |
| **EVALUATE** | WHEN OTHER | Yes | Yes | CF tests pass | |
| **EVALUATE** | THRU ranges in WHEN | Unclear | Unclear | — | No specific test evidence in TRX |
| **EVALUATE** | ANY keyword | Unclear | Unclear | — | No specific test evidence |
| **EVALUATE** | Condition-name as WHEN object | Unclear | Unclear | — | No specific test evidence |
| **File I/O** | FILE STATUS 2-char codes (§9.1.12) | Yes | Partially | IO/FileStatus.cs in source-snapshot; 29 FIO tests pass | Not all codes verified |
| **File I/O** | OPEN mode enforcement | Unclear | Unclear | FileStateValidator.cs in source-snapshot | No clear test evidence |
| **File I/O** | Record locking | No | No | — | Not implemented |
| **File I/O** | USE AFTER EXCEPTION routing | Yes | Unclear | FileIoBinder in source-snapshot | |
| **File I/O** | AT END / NOT AT END routing | Yes | Yes | 29 FIO tests pass | |
| **File I/O** | INVALID KEY / NOT INVALID KEY | Yes | Partially | FIO tests pass | |
| **File I/O** | OPTIONAL file OPEN behavior | Unclear | Unclear | — | |
| **File I/O** | Sequential/Indexed/Relative handlers | Yes | Partially | IO/SequentialFileHandler.cs, IndexedFileHandler.cs, RelativeFileHandler.cs in source-snapshot | No IX/SQ/RL NIST baselines |
| **Conditions** | Abbreviated combined relations (§6.3.4.2) | Yes | Partially | 24 condition tests pass; NC250A has 4 FAIL* | Not verified against all spec examples |
| **Conditions** | Class conditions (NUMERIC etc.) | Yes | Yes | NC246A exercises (14 FAIL* relate to subscripts, not class tests) | |
| **Conditions** | Sign conditions | Yes | Unclear | ConditionBinder in source-snapshot | |
| **Conditions** | Condition-name (level 88) with VALUE THRU | Yes | Yes | Condition tests pass | |
| **Conditions** | Collating sequence impact | Unclear | No | — | |
| **Runtime** | Decimal arithmetic (not float) | Yes | Yes | PicRuntime.cs uses System.Decimal; 24 arith tests pass | |
| **Runtime** | SIZE ERROR detection | Yes | Yes | NC117A passes 0 FAIL* | ArithmeticStatus type exists |
| **Runtime** | ROUNDED at correct digit | Yes | Partially | PicRuntime.cs in source-snapshot | 77 FAIL* may include rounding edge cases |
| **Runtime** | Intermediate precision (§6.4.1) | Unclear | Unclear | — | Spec allows implementation-defined |
| **Runtime** | STRING pointer overflow | Yes | Yes | NC218A exercises (9 FAIL* in other areas) | |
| **Runtime** | UNSTRING pointer overflow | Yes | Yes | NC218A exercises | |
| **Runtime** | INSPECT left-to-right non-overlapping | Yes | Partially | InspectRuntime.cs in source-snapshot; 11 string tests pass | |
| **Runtime** | COMP-3 packed decimal | Yes | Yes | PicRuntime.cs; data tests pass | |
| **Runtime** | COMP/COMP-4 binary | Yes | Yes | PicRuntime.cs; data tests pass | |
| **Runtime** | BLANK WHEN ZERO | Yes | Partially | PicDescriptor.cs in source-snapshot | |
