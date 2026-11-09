---
title: Lookup — Construct Catalogue (183 constructs × edition)
area: spec-lookup
status: draft
last_updated: 2026-07-23
related_files:
  - tests/version-matrix/constructs.json
  - docs/VERSION_CHANGE_REFERENCE.md
  - src/WiseOwl.Cobol.Editions/ConstructRegistry.cs
tags:
  - wiseowl-cobol
  - spec
  - lookup
---

# Lookup — Construct Catalogue (183 constructs × edition)

The complete per-construct edition inventory, distilled from the canonical `tests/version-matrix/constructs.json`
(the SSOT that `VERSION_CHANGE_REFERENCE.md` and the in-code `ConstructRegistry` are renderings of). Each row: the
construct id, a paraphrased summary, ISO § citation, `Introduced`/`Removed` edition years, the `COBOL####` diagnostic
(suffix), and `Status` (`pending` = catalogued but not yet implemented; else `active`). See
[[kb/Spec/Version Targeting]] and [[kb/Semantics/Passes]] for how these are gated.

## 1. Identification & Environment Division
| Construct id | Summary | ISO § | Intro | Removed | Diag | Status |
|---|---|---|---|---|---|---|
| currency-picture-symbol-2002 | CURRENCY SIGN WITH PICTURE SYMBOL (separate-symbol form) | §2023:12.3.7 | 2002 | | 0893 | active |
| options-arithmetic-native-2002 | OPTIONS ARITHMETIC IS NATIVE | §2023:11.9.5 | 2002 | | 0900 | active |
| arithmetic-standard-decimal-2014 | ARITHMETIC IS STANDARD-DECIMAL (decimal128 intermediates) | §2023:11.9.5/§2023:8.8.1.5 | 2014 | | 0900 | active |
| arithmetic-standard-binary-2014 | ARITHMETIC IS STANDARD-BINARY (binary128; obsolete 2023) | §2023:11.9.5/§2023:8.8.1.4 | 2014 | | 0900 | pending |
| options-default-rounded-2014 | OPTIONS DEFAULT ROUNDED clause | §2023:11.9.6 | 2014 | | 0900 | active |
| options-intermediate-rounding-2014 | OPTIONS INTERMEDIATE ROUNDING clause | §2023:11.9.11 | 2014 | | 0900 | active |
| options-entry-convention-2014 | OPTIONS ENTRY-CONVENTION IS COBOL (the COBOL alternative; the clause itself is 2002) | §2023:11.9.7.2, §2023:11.9.7.4 GR2 | 2014 | | 0900 | active |
| options-float-binary-2014 | OPTIONS FLOAT-BINARY clause | §2023:11.9.8 | 2014 | | 0900 | active |
| options-float-decimal-2014 | OPTIONS FLOAT-DECIMAL clause | §2023:11.9.9 | 2014 | | 0900 | active |
| options-initialize-2023 | OPTIONS INITIALIZE clause | §2023:11.9.10 | 2023 | | 0900 | active |
| options-paragraph-2002 | OPTIONS paragraph (container) | §2023:11.9 | 2002 | | 0804 | active |
| arithmetic-standard-2002 | ARITHMETIC IS STANDARD (obsolete 2014, removed 2023) | §2023:8.8.1 | 2002 | 2023 | 0807 | active |
| program-id-recursive-2002 | PROGRAM-ID … RECURSIVE attribute | §2023:11.10 | 2002 | | 0885 | active |
| special-names-for-national-2002 | SPECIAL-NAMES FOR ALPHANUMERIC/NATIONAL phrase | §2023:12.3.7 | 2002 | | 0900 | active |
| alphabet-national-2002 | ALPHABET FOR NATIONAL UCS-4/UTF-8/UTF-16 | §2023:12.3.7.2 | 2002 | | 0900 | active |
| program-collating-national-2002 | PROGRAM COLLATING SEQUENCE national forms | §2023:12.3.6.2 | 2002 | | 0900 | active |
| memory-size-removed-2002 | OBJECT-COMPUTER MEMORY SIZE (deleted 2002) | VCR Table 7 | 85 | 2002 | 0902 | active |
| segment-limit-removed-2002 | OBJECT-COMPUTER SEGMENT-LIMIT (deleted 2002) | VCR Table 7 | 85 | 2002 | 0902 | active |
| debugging-mode-removed-2014 | SOURCE-COMPUTER WITH DEBUGGING MODE (obsolete 2002, removed 2014) | VCR Table 7 row 7.9; kb/Work R61 | 85 | 2014 | 0902 | active |
| identification-comments-removed-2002 | AUTHOR/INSTALLATION/DATE-*/SECURITY paragraphs (deleted 2002) | VCR Table 7 | 85 | 2002 | 0902 | active |
| remarks-removed-85 | REMARKS paragraph (in no held edition; §1985:VI.3.2.1.1 prints none) | VCR Table 7 | 85 | 85 | 0902 | active |

## 2. Data Division — items, PICTURE, USAGE, clauses
| Construct id | Summary | ISO § | Intro | Removed | Diag | Status |
|---|---|---|---|---|---|---|
| pic-wide-19-digits-2002 | Fixed-point item wider than 18 digits | §2023:8.3.3.3.2 | 2002 | | 0802 | active |
| usage-packed-no-sign-2023 | USAGE PACKED-DECIMAL WITH NO SIGN | §2023:13.18.60.4 GR11 | 2023 | | 0900 | active |
| based-clause-2002 | BASED clause (pointer/ALLOCATE-addressed item) | §2023:13.18.5 | 2002 | | 0900 | active |
| any-length-clause-2002 | ANY LENGTH clause (linkage length tracks argument) | §2023:13.18.2 | 2002 | | 0900 | active |
| dynamic-length-item-2014 | DYNAMIC LENGTH elementary item | §2023:13.18.19 | 2014 | | 0900 | active |
| type-clause-2002 | TYPE IS clause (references a TYPEDEF) | §2023:13.18.58 | 2002 | | 0900 | active |
| typedef-def-2002 | TYPEDEF clause (named template, no storage) | §2023:13.18.58 | 2002 | | 0900 | active |
| same-as-clause-2002 | SAME AS clause (copies another entry's description) | §2023:13.18.49 | 2002 | | 0900 | active |
| external-type-declaration-2023 | Strongly-typed EXTERNAL type (STRONG+EXTERNAL TYPEDEF) | §2023:13.18.22.3 | 2023 | | 0900 | active |
| value-numeric-literal-numeric-edited-2023 | Non-zero numeric VALUE for numeric-edited item | §2023:13.18.63 SR6 | 2023 | | 0900 | active |
| picture-editing-2023 | PICTURE EDITING phrase (user-defined editing) | §2023:13.18.40.2 | 2023 | | 0900 | active |
| sync-on-group-2023 | SYNCHRONIZED on a group item | §2023:13.18.55 | 2023 | | 0900 | active |
| local-storage-section-2002 | LOCAL-STORAGE SECTION (per-activation data) | §2023:13.6 | 2002 | | 0900 | active |
| constant-entry-2002 | Constant entry (01 name CONSTANT AS …) | §2023:13.10 | 2002 | | 0900 | active |
| constant-record-2014 | CONSTANT RECORD clause (level-01 constant) | §2023:13.18.15 | 2014 | | 0900 | active |
| national-data-2002 | National data (PIC N / USAGE NATIONAL, UTF-16) | §2023:8.5.2 | 2002 | | 0900 | active |
| national-edited-2002 | National-edited data (PIC N with B 0 /) | §2023:8.5.2.11 | 2002 | | 0900 | pending |
| boolean-data-2002 | Boolean data (PIC 1 / USAGE BIT) | §2023:8.5.2 | 2002 | | 0900 | active |
| pic-external-float-2002 | External floating-point PICTURE (symbol E) | §2023:13.18.40 | 2002 | | 0900 | pending |
| usage-pointer-2002 | USAGE POINTER data item | §2023:13.18.60 | 2002 | | 0900 | active |
| usage-object-reference-2002 | USAGE OBJECT REFERENCE (OO reference field) | §2023:13.18.60 | 2002 | | 0900 | active |
| usage-program-pointer-2002 | USAGE PROGRAM-POINTER | §2023:13.18.60 GR24 | 2002 | | 0900 | active |
| usage-function-pointer-2014 | USAGE FUNCTION-POINTER | §2023:13.18.60 | 2014 | | 0900 | pending |
| usage-binary-char-family-2002 | BINARY-CHAR/-SHORT/-LONG/-DOUBLE integers | §2023:13.18.60.4 GR12 | 2002 | | 0900 | active |
| usage-float-short-2002 | USAGE FLOAT-SHORT (native float) | §2023:13.18.60.4 GR13 | 2002 | | 0900 | active |
| usage-float-long-2002 | USAGE FLOAT-LONG (native double) | §2023:13.18.60.4 GR13 | 2002 | | 0900 | active |
| usage-float-extended-2002 | USAGE FLOAT-EXTENDED (mapped to double) | §2023:13.18.60.4 GR13 | 2002 | | 0900 | active |
| usage-float-binary32-2014 | USAGE FLOAT-BINARY-32 (IEEE binary32) | §2023:13.18.60.4 GR14 | 2014 | | 0900 | active |
| usage-float-binary64-2014 | USAGE FLOAT-BINARY-64 (IEEE binary64) | §2023:13.18.60.4 GR15 | 2014 | | 0900 | active |
| usage-float-binary128-2014 | USAGE FLOAT-BINARY-128 (unsupported) | §2023:13.18.60.4 GR16 | 2014 | | 1564 | pending |
| usage-float-decimal16-2014 | USAGE FLOAT-DECIMAL-16 (unsupported) | §2023:13.18.60.4 GR17 | 2014 | | 1564 | pending |
| usage-float-decimal34-2014 | USAGE FLOAT-DECIMAL-34 (unsupported) | §2023:13.18.60.4 GR18 | 2014 | | 1564 | pending |

## 3. Data Division — tables / OCCURS / REDEFINES / RENAMES
| Construct id | Summary | ISO § | Intro | Removed | Diag | Status |
|---|---|---|---|---|---|---|
| occurs-dynamic-2014 | OCCURS DYNAMIC (dynamic-capacity table) | §2023:13.18.38 Fmt 4 | 2014 | | 0900 | active |
| value-table-format-2002 | Format-2 table VALUE (subscript-keyed initializers) | §2023:13.18.63.2 | 2002 | | 0900 | active |

## 4. Procedure — Arithmetic & Data Movement
| Construct id | Summary | ISO § | Intro | Removed | Diag | Status |
|---|---|---|---|---|---|---|
| nucleus-move-display | Baseline MOVE/DISPLAY (edition-invariant) | baseline | 85 | | — | active |
| allocate-2002 | ALLOCATE (dynamic storage) | §2023:14.9.3 | 2002 | | 0900 | active |
| free-2002 | FREE (release allocated storage) | §2023:14.9.15 | 2002 | | 0900 | active |
| set-address-2002 | SET ADDRESS OF (data-pointer assignment) | §2023:14.9.39 Fmt 7 | 2002 | | 0900 | active |
| pointer-arithmetic-2002 | SET pointer UP/DOWN BY (pointer arithmetic) | §2023:14.9.39 Fmt 10 | 2002 | | 0900 | active |
| set-dyn-length-size-2023 | SET [SIZE OF] dynamic-length item TO n | §2023:14.9.39 Fmt 16 | 2023 | | 0900 | active |
| concat-operator-2002 | Concatenation expression (`&` operator) | §2023:8.8.3 | 2002 | | 0900 | active |
| boolean-operators-2002 | B-AND/B-OR/B-XOR/B-NOT operators | §2023:8.8.2 | 2002 | | 0900 | active |
| boolean-shift-operators-2023 | B-SHIFT-L/R/LC/RC shift operators | §2023:8.8.2 rule 8 | 2023 | | 0900 | active |
| rounded-mode-is-2014 | ROUNDED MODE IS phrase (8-mode set) | §2023:14.7.4 | 2014 | | 0803 | active |
| arithmetic-intermediate-precision-2023 | Implementor-defined arithmetic mode (behavior variant) | §2023:8.8/Annex E.2 | 85 | | — | pending |
| move-alphanumeric-figurative-removed-2023 | MOVE alnum figurative to numeric (removed 2023) | §2023:14.9.25.3 SR5 | 85 | 2023 | 0902 | active |
| move-all-digit-integer-obsolete-2023 | MOVE digit-only ALL literal to integer (obsolete 2023) | §2023:14.9.25.3 SR5 | 85 | | 0903 | active |
| move-quote-numeric-obsolete-2014 | MOVE QUOTE to numeric (obsolete 2014, removed 2023) | §2023:14.9.25.3 SR5 | 85 | 2023 | 0902 | active |
| initialize-filler-2002 | INITIALIZE … WITH FILLER | §2023:14.9.20 | 2002 | | 0830 | active |
| initialize-to-value-2002 | INITIALIZE … TO VALUE | §2023:14.9.20 | 2002 | | 0831 | active |
| initialize-to-default-2002 | INITIALIZE … TO DEFAULT | §2023:14.9.20 | 2002 | | 0832 | active |
| initialize-then-replacing-2002 | INITIALIZE … THEN REPLACING | §2023:14.9.20 | 2002 | | 0833 | active |
| accept-four-digit-year-2002 | ACCEPT FROM DATE YYYYMMDD / DAY YYYYDDD | §2023:14.9.1 | 2002 | | 0815 | active |
| end-accept-2002 | END-ACCEPT scope terminator | §2023:14.9.1 | 2002 | | 0816 | pending |

## 5. Procedure — Control Flow
| Construct id | Summary | ISO § | Intro | Removed | Diag | Status |
|---|---|---|---|---|---|---|
| exit-section-2002 | EXIT SECTION (structured section exit) | §2023:14.9.14.2 Fmt 4 | 2002 | | 0900 | active |
| exit-paragraph-2002 | EXIT PARAGRAPH (structured paragraph exit) | §2023:14.9.14.2 Fmt 4 | 2002 | | 0900 | active |
| exit-perform-2002 | EXIT PERFORM [CYCLE] | §2023:14.9.14.2 Fmt 3 | 2002 | | 0900 | active |
| perform-until-exit-2023 | PERFORM UNTIL EXIT (infinite loop) | §2023:14.9.28.4 GR11 | 2023 | | 0900 | active |
| continue-after-2023 | CONTINUE AFTER n SECONDS (timed pause) | §2023:14.9.9 | 2023 | | 0900 | active |
| next-sentence-archaic-2023 | NEXT SENTENCE (archaic 2023, warning) | Annex F.1 | 85 | | 0903 | active |
| stop-literal-removed-2002 | STOP literal (operator message; deleted 2002) | §2023:14.9.42 | 85 | 2002 | 0902 | active |
| alter-removed-2002 | ALTER statement (deleted 2002) | §2023:14.9.17 | 85 | 2002 | 0810 | active |
| bare-goto-removed-2002 | GO TO without procedure-name (deleted 2002) | §2023:14.9.17 | 85 | 2002 | 0811 | active |
| segment-numbers-removed-2002 | Section-header segment-number (deleted 2002) | VCR Table 7 | 85 | 2002 | 0902 | active |

## 6. Procedure — String Ops
| Construct id | Summary | ISO § | Intro | Removed | Diag | Status |
|---|---|---|---|---|---|---|
| inspect-backward-2023 | INSPECT BACKWARD (right-to-left scan) | §2023:14.9.22.2 | 2023 | | 0845 | active |

## 7. Procedure — Conditions & Exception Handling
| Construct id | Summary | ISO § | Intro | Removed | Diag | Status |
|---|---|---|---|---|---|---|
| logical-xor-operator-2023 | Logical XOR / EXCLUSIVE-OR operator | §2023:8.8.4.9 | 2023 | | 0900 | active |
| perform-exception-checking-2023 | Format-3 PERFORM … WHEN (exception interceptor) | §2023:14.9.28.2 Fmt 3 | 2023 | | 0900 | pending |
| procedure-raising-2002 | PROCEDURE DIVISION RAISING (declares propagated ECs) | §2023:14.2.2 | 2002 | | 0900 | active |
| raise-statement-2002 | RAISE statement | §2023:14.9.29 | 2002 | | 0876 | active |
| resume-statement-2002 | RESUME statement (exception recovery) | §2023:14.9.33 | 2002 | | 0876 | active |
| set-last-exception-2002 | SET LAST EXCEPTION TO OFF | §2023:14.9.39 Fmt 13 | 2002 | | 0879 | active |
| statement-raising-2002 | GOBACK/EXIT … RAISING phrase | §2023:14.9.18.2 | 2002 | | 0879 | active |
| use-after-exception-condition-2002 | USE AFTER EXCEPTION CONDITION declarative | §2023:14.9.49.2 Fmt 3 | 2002 | | 0877 | active |
| use-after-exception-object-2002 | USE AFTER EXCEPTION OBJECT (EC-OO) | §2023:14.9.49.2 Fmt 4 | 2002 | | 0876 | pending |
| use-for-debugging-removed-2002 | USE FOR DEBUGGING declarative (deleted 2002) | VCR Table 7 | 85 | 2002 | 0902 | active |

## 8. File I/O
| Construct id | Summary | ISO § | Intro | Removed | Diag | Status |
|---|---|---|---|---|---|---|
| read-previous-2002 | READ … PREVIOUS (reverse keyed read) | §2023:14.9.30 Fmt 1 | 2002 | | 0900 | active |
| start-first-last-2002 | START … FIRST/LAST positioning | §2023:14.9.41 | 2002 | | 0900 | active |
| start-with-length-2002 | START KEY … WITH LENGTH (partial-key) | §2023:14.9.41 | 2002 | | 0900 | active |
| delete-file-2023 | DELETE FILE (physical file deletion) | §2023:14.9.10 Fmt 2 | 2023 | | 0900 | active |
| write-before-and-after-advancing-2023 | WRITE BEFORE AFTER ADVANCING | §2023:14.9.51.2 Format 1 / E.3.3 item 2 | 2023 | | 0900 | active |
| file-sharing-clause-2002 | SHARING clause / OPEN SHARING | §2023:12.4.5.15 | 2002 | | 0900 | active |
| lock-mode-clause-2002 | LOCK MODE clause (MANUAL/AUTOMATIC) | §2023:12.4.5.9 | 2002 | | 0900 | active |
| file-collating-clause-2002 | File-control COLLATING SEQUENCE (indexed keys) | §2023:12.4.5.7 | 2002 | | 0900 | active |
| alternate-key-suppress-when-2023 | ALTERNATE RECORD KEY SUPPRESS WHEN | §2023:12.4.5.6.2 | 2023 | | 0900 | active |
| retry-phrase-2002 | RETRY phrase (TIMES/SECONDS/FOREVER) | §2023:14.7.9 | 2002 | | 0900 | active |
| unlock-statement-2002 | UNLOCK statement (release record locks) | §2023:14.9.47 | 2002 | | 0900 | active |
| record-lock-phrase-2002 | Record-lock phrase on READ/WRITE/REWRITE | §2023:14.9.30/.51/.35 | 2002 | | 0900 | active |
| release-from-literal-2002 | RELEASE … FROM a literal | §2023:14.9.32.2 | 2002 | | 0871 | active |
| sort-collating-national-2002 | SORT/MERGE COLLATING SEQUENCE national | §2023:14.9.40.3 | 2002 | | 0872 | active |
| table-sort-2002 | SORT of a table (Format 2, in-place) | §2023:14.9.40 | 2002 | | 0870 | active |
| merge-in-sort-merge-proc-removed-2023 | MERGE inside a SORT/MERGE procedure (prohibited 2023) | §2023:14.9.24 | 85 | 2023 | 1572 | active |
| label-records-removed-2002 | FD LABEL RECORDS clause (deleted 2002) | §2023:13.18 | 85 | 2002 | 0902 | active |
| value-of-removed-2002 | FD VALUE OF clause (deleted 2002) | VCR Table 7 | 85 | 2002 | 0902 | active |
| data-records-removed-2002 | FD/SD DATA RECORDS clause (deleted 2002) | §2023:13.4.6 | 85 | 2002 | 0873 | active |
| multiple-file-tape-removed-2002 | I-O-CONTROL MULTIPLE FILE TAPE (deleted 2002) | VCR Table 7 | 85 | 2002 | 0902 | active |
| open-reversed-removed-2002 | OPEN … REVERSED tape phrase (deleted 2002) | §2023:14.9.27 | 85 | 2002 | 0902 | active |
| close-with-lock-removed-2023 | CLOSE … WITH LOCK (removed 2023) | VCR row 7 | 85 | 2023 | 0902 | active |
| rerun-removed-2002 | I-O-CONTROL RERUN checkpoint hint (deleted 2002) | VCR Table 7 | 85 | 2002 | 0902 | active |

## 9. Interprogram (CALL / return / linkage / UDF)
| Construct id | Summary | ISO § | Intro | Removed | Diag | Status |
|---|---|---|---|---|---|---|
| call-by-value-2002 | CALL … USING BY VALUE | §2023:14.9.4 | 2002 | | 0900 | active |
| pd-header-by-value-2002 | PROCEDURE DIVISION USING BY VALUE formals | §2023:14.2.2 | 2002 | | 0900 | active |
| procedure-returning-2002 | PROCEDURE DIVISION … RETURNING | §2023:14.2 | 2002 | | 0900 | active |
| call-returning-2002 | CALL … RETURNING status-item | §2023:14.9.4 | 2002 | | 0884 | active |
| goback-bare-2002 | GOBACK statement (bare) | §2023:14.9.18 | 2002 | | 0880 | active |
| goback-returning-2002 | GOBACK RETURNING status | §2023:14.9.18 | 2002 | | 0900 | active |
| stop-run-status-2002 | STOP RUN WITH status phrase | §2023:14.9.42 | 2002 | | 0900 | active |
| goback-status-2023 | GOBACK WITH NORMAL/ERROR STATUS | §2023:14.9.18.2 | 2023 | | 0900 | active |
| user-function-invocation-2002 | User-defined FUNCTION name(args) reference | §2023:9.4 | 2002 | | 0900 | active |
| function-prototype-2002 | FUNCTION-ID … IS PROTOTYPE | §2023:11.5 Fmt 2 | 2002 | | 0900 | active |
| exit-function-window | EXIT FUNCTION (removed 2023) | Annex E row 6 | 2002 | 2023 | 0902 | active |
| exit-program-archaic-2023 | EXIT PROGRAM (archaic 2023, warning) | Annex F.1 | 85 | | 0903 | active |
| call-on-overflow-removed-2023 | CALL … ON OVERFLOW (removed 2023; use ON EXCEPTION) | Annex E.2 1c | 85 | 2023 | 0882 | active |
| enter-removed-2002 | ENTER statement (deleted 2002) | VCR Table 7 | 85 | 2002 | 0902 | active |

## 10. Object Orientation
| Construct id | Summary | ISO § | Intro | Removed | Diag | Status |
|---|---|---|---|---|---|---|
| invoke-2002 | INVOKE (OO method call) | §2023:14.9.23 | 2002 | | 0900 | active |
| class-definition-2002 | Class definition (CLASS-ID unit) | §2023:11.2 | 2002 | | 0900 | active |
| interface-definition-2002 | Interface definition (INTERFACE-ID unit) | §2023:11.5 | 2002 | | 0900 | active |
| repository-class-2002 | REPOSITORY CLASS entry | §2023:12.3.8 | 2002 | | 0900 | active |
| repository-interface-2002 | REPOSITORY INTERFACE entry | §2023:12.3.8 | 2002 | | 0900 | active |
| repository-property-2002 | REPOSITORY PROPERTY entry | §2023:12.3.8 | 2002 | | 0900 | active |
| property-clause-2002 | PROPERTY clause (synthesizes accessors) | §2023:13.18.42 | 2002 | | 0900 | active |
| set-object-reference-2002 | SET … TO object-reference (Fmt 5) | §2023:14.9.39 Fmt 5 | 2002 | | 0900 | active |
| implements-clause-2002 | IMPLEMENTS clause (FACTORY/OBJECT) | §2023:11.8 | 2002 | | 0900 | pending |
| method-property-selector-2002 | METHOD-ID GET/SET PROPERTY selector | §2023:11.7 | 2002 | | 0900 | pending |
| inline-method-invocation-2023 | In-line method invocation identifier(args) | §2023:8.4.3 | 2023 | | 0900 | pending |
| exit-method-window | EXIT METHOD (removed 2023) | Annex E row 5 | 2002 | 2023 | 0902 | active |
| method-working-storage-window | WORKING-STORAGE in a method (static; banned 2023) | §2023:13.5.3 SR1 | 2002 | 2023 | 0902 | active |

## 11. Intrinsic Functions
| Construct id | Summary | ISO § | Intro | Removed | Diag | Status |
|---|---|---|---|---|---|---|
| exception-file-argument-2023 | FUNCTION EXCEPTION-FILE(file-connector) | §2023:15.28.3 | 2023 | | 0900 | active |
| exception-file-n-argument-2023 | FUNCTION EXCEPTION-FILE-N(file-connector) | §2023:15.29.4 | 2023 | | 0900 | active |
| exception-file-n-2002 | EXCEPTION-FILE-N / EXCEPTION-LOCATION-N (national twins) | §2023:15.29 | 2002 | | 1502 | active |
| boolean-of-integer-2002 | BOOLEAN-OF-INTEGER / INTEGER-OF-BOOLEAN | §2023:15.13 | 2002 | | 1502 | active |
| date-to-yyyymmdd-2002 | YEAR-TO-YYYY / DATE-TO-YYYYMMDD / DAY-TO-YYYYDDD / SECONDS-PAST-MIDNIGHT | §2023:15.100 | 2002 | | 1502 | active |
| test-numval-2002 | TEST-DATE/DAY/NUMVAL/NUMVAL-C validators | §2023:15.90–.94 | 2002 | | 1502 | active |
| byte-length-2002 | FUNCTION BYTE-LENGTH (byte count, ≠ LENGTH) | §2023:15.14 | 2002 | | 1502 | active |
| smallest-algebraic-2023 | FUNCTION SMALLEST-ALGEBRAIC | §2023:15.83 | 2023 | | 1502 | active |

## 12. Report Writer
| Construct id | Summary | ISO § | Intro | Removed | Diag | Status |
|---|---|---|---|---|---|---|
| report-present-when-2002 | Report-group PRESENT WHEN (conditional presentation) | §2023:13.18.41 Fmt 1 | 2002 | | 0900 | active |
| report-varying-2002 | Report-group VARYING (per-repetition counter) | §2023:13.18.64 | 2002 | | 0900 | active |
| report-multi-column-2002 | Multiple/relative COLUMN (repeating item) | §2023:13.18.14 Fmt 1 | 2002 | | 0900 | active |
| report-multi-line-2002 | Multiple LINE clause (repeating lines) | §2023:13.18.35 Fmt 1 | 2002 | | 0900 | pending |

## 13. Compiler-directing / Conditional Compilation / Directives
| Construct id | Summary | ISO § | Intro | Removed | Diag | Status |
|---|---|---|---|---|---|---|
| ref-mod-zero-length-2023 | `>>REF-MOD-ZERO-LENGTH` directive | §2023:7.3.23 | 2023 | | 0900 | active |
| flag-14-directive-2023 | `>>FLAG-14` migration-flagging directive | §2023:7.3.15 | 2023 | | 0900 | active |
| flag-02-directive-2014 | `>>FLAG-02` migration-flagging (obsolete 2023) | §2023:7.3.14 | 2014 | | 0900 | active |
| cobol-words-directive-2023 | `>>COBOL-WORDS` (EQUATE/UNDEFINE/SUBSTITUTE/RESERVE) | §2023:7.3.10 | 2023 | | 0900 | active |
| copy-replacing-non-pseudo-text-removed-2023 | Non-pseudo-text COPY REPLACING operand (removed 2023) | Annex E.2 | 85 | 2023 | 0902 | active |
| fixed-form-word-continuation-removed-2023 | Fixed-form word continuation (removed 2023) | Annex E.2 | 85 | 2023 | 0902 | active |
| col7-continuation-obsolete-2023 | Column-7 continuation indicator (obsolete 2023) | Annex F.2 item 4 | 85 | | 0903 | active |

## 14. Reserved words / edition-gating meta
| Construct id | Summary | ISO § | Intro | Removed | Diag | Status |
|---|---|---|---|---|---|---|
| word-length-63-2023 | COBOL word up to 63 characters | §2023:8.3.2.1 | 2023 | | 1567 | active |
| user-word-as-2002 | AS reserved in 2002 | §2023:8.9 | 85 | 2002 | 0901 | active |
| user-word-constant-2002 | CONSTANT reserved in 2002 | §2023:8.9 | 85 | 2002 | 0901 | active |
| user-word-raising-2002 | RAISING reserved in 2002 | §2023:8.9 | 85 | 2002 | 0901 | active |
| user-word-prototype-2002 | PROTOTYPE reserved in 2002 | §2023:8.9 | 85 | 2002 | 0901 | active |
| user-word-b-and-2002 | B-AND reserved in 2002 | §2023:8.9 | 85 | 2002 | 0901 | active |
| user-word-sharing-2002 | SHARING reserved in 2002 | §2023:8.9 | 85 | 2002 | 0901 | active |
| user-word-program-pointer-2002 | PROGRAM-POINTER reserved in 2002 | §2023:8.9 | 85 | 2002 | 0901 | active |
| user-word-function-pointer-2014 | FUNCTION-POINTER reserved in 2014 | §2023:8.9 | 85 | 2014 | 0901 | active |
| user-word-commit-2023 | COMMIT reserved in 2023 | §2023:8.9 | 85 | 2023 | 0901 | active |
| user-word-xor-2023 | XOR reserved in 2023 | §2023:8.9 | 85 | 2023 | 0901 | active |
| user-word-exclusive-or-2023 | EXCLUSIVE-OR reserved in 2023 | §2023:8.9 | 85 | 2023 | 0901 | active |
| user-word-editing-2023 | EDITING reserved in 2023 | §2023:8.9 | 85 | 2023 | 0901 | active |
| user-word-location-2023 | LOCATION reserved in 2023 | §2023:8.9 | 85 | 2023 | 0901 | active |
| receive-as-user-word | RECEIVE (85-reserved → free 2002/14 → re-reserved 2023) | §2023:8.9 | 2002 | 2023 | 0901 | active |
| end-receive-as-user-word | END-RECEIVE (same interval as RECEIVE) | §2023:8.9 | 2002 | 2023 | 0901 | active |

## Counts
**Total: 183 constructs · 167 active · 16 pending.**

| Category | Count | | Introduced in | Count |
|---|---|---|---|---|
| Identification & Environment | 21 | | 85 (baseline/removed) | 44 |
| Data Division — items/PIC/USAGE | 34 | | 2002 | 96 |
| Data Division — tables | 2 | | 2014 | 18 |
| Arithmetic & Data Movement | 20 | | 2023 | 25 |
| Control Flow | 10 | | | |
| String Ops | 1 | | | |
| Conditions & Exceptions | 10 | | | |
| File I/O | 23 | | | |
| Interprogram | 14 | | | |
| Object Orientation | 13 | | | |
| Intrinsic Functions | 8 | | | |
| Report Writer | 4 | | | |
| Directives | 7 | | | |
| Reserved-word meta | 16 | | | |

The **16 pending** constructs are catalogued (edition metadata frozen by drift tests) but not yet implemented — they
track the remaining implementation work. See [[kb/Modernization/Tasks]].

## See also
- [[kb/Spec/Lookup/Grammar]] — the grammar surface these constructs belong to.
- [[kb/Spec/Version Targeting]] — how `f(construct, edition)` is computed.
- [[kb/Semantics/Passes]] — the `VersionConformancePass` gate that fires these diagnostics.
- [[kb/Modernization/Tasks]] — the pending constructs as remaining work.

## Backlinks
- [[kb/Spec/Lookup/Index]] · [[kb/Spec/Lookup/Grammar]] · [[kb/Spec/Version Targeting]] — link here.
- Pipeline: [[kb/Compiler/Pipeline-to-ISO-Mapping]] — the Validate (edition-gating) phase.
