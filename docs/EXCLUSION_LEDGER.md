# CobolSharp — NIST Exclusion Ledger (live suites)

Generated from the Wave-1 audit (24 agents, 2026-06-04). Every unbaselined live-suite NIST program,
classified into exactly one documented exclusion class. **real-GAPs = 0** — the M1 baseline axis is
complete; no unbaselined live program is a hidden failure. Classes: `flagging-M` | `no-output-producer`
| `callee-half` | `non-deterministic` | `GAP`. WS-VERIFY deliverable (see docs/COBOL85_COMPLIANCE_PLAN.md §3).

## NC

| Program | Class | Evidence |
|---|---|---|
| NC214M | non-deterministic | Source line 350 'ACCEPT WRK-DU-6V0-1 FROM DATE.', line 366 'ACCEPT WRK-DU-5V0-1 FROM DAY.', line 382 'ACCEPT WRK-DU-8V0-1 FROM TIME.', line 398 'ACCEPT WRK-DU-1V0-1 FROM DAY-OF-WEEK.' — output depends on wall-clock date/time so it cannot be reproducibly baselined (per MEMORY: NC214M dropped as non-deterministic when the collating baselines were corrected). |
| NC303M | flagging-M | Name ends in M; header (lines 4-5) 'TESTS THE FLAGGING OF OBSOLETE HIGH SUBSET NUCLEUS FEATURES'. Exercises obsolete constructs each annotated 'Message expected: OBSOLETE': DATE-COMPILED paragraph (line 6), ALTER...TO PROCEED TO (lines 19-20), and altered bare 'GO TO.' (lines 25,29). Expects exactly 4 OBSOLETE flags (line 32); produces no CCVS report (no DISPLAY/WRITE output). |

## IF

| Program | Class | Evidence |
|---|---|---|
| IF401M | flagging-M | Name ends in M. Source line 6: 'THIS PROGRAM TESTS THE FLAGGING OF HIGH SUBSET INTRINSIC FUNCTION FEATURES'; line 104: 'TOTAL NUMBER OF FLAGS EXPECTED = 14.' PROCEDURE DIVISION (line 29) has no USING, so not a callee. It exercises high-subset intrinsic functions the compiler must FLAG (ACOS, ANNUITY, ASIN, ATAN, CHAR, COS, CURRENT-DATE, DATE-OF-INTEGER, DAY-OF-INTEGER, FACTORIAL, INTEGER, INTEGER-OF-DATE, INTEGER-OF-DAY, INTEGER-PART) rather than producing a comparable CCVS report. guard.sh line 87: 'IF401M/402M/403M are flagging-conformance modules: they emit no CCVS report'. |
| IF402M | flagging-M | Name ends in M. Source line 6: 'THIS PROGRAM TESTS THE FLAGGING OF HIGH SUBSET INTRINSIC FUNCTION FEATURES'; line 124: 'TOTAL NUMBER OF FLAGS EXPECTED = 17.' PROCEDURE DIVISION (line 33) has no USING. Flags high-subset intrinsics: LENGTH, LOG, LOWER-CASE, MAX, MEAN, MEDIAN, MIDRANGE, MIN, MOD, NUMVAL, NUMVAL-C, ORD, ORD-MAX, ORD-MIN. guard.sh line 87 documents IF401M/402M/403M as flagging-conformance modules emitting no CCVS report. |
| IF403M | flagging-M | Name ends in M. Source line 6: 'THIS PROGRAM TESTS THE FLAGGING OF HIGH SUBSET INTRINSIC FUNCTION FEATURES'; line 97: 'TOTAL NUMBER OF FLAGS EXPECTED = 13.' PROCEDURE DIVISION (line 27) has no USING. Flags high-subset intrinsics: PRESENT-VALUE, RANDOM, RANGE, REM, REVERSE, SIN, SQRT, STANDARD-DEVIATION, SUM, TAN, UPPER-CASE, VARIANCE, WHEN-COMPILED. guard.sh line 87 documents IF401M/402M/403M as flagging-conformance modules emitting no CCVS report. |

## SM

| Program | Class | Evidence |
|---|---|---|
| SM301M | flagging-M | Name ends in M. Program comment: 'The following program tests the flagging of the intermediate subset COPY feature.' Body is only PERFORM SM301M-COPY / STOP RUN; the flagged construct is 'COPY KSM31.' annotated '*Message expected for following statement: NON-CONFORMING STANDARD' and '*TOTAL NUMBER OF FLAGS EXPECTED = 1.' No CCVS report produced. guard.sh line 36 documents flagging-M modules as excluded. |
| SM401M | flagging-M | Name ends in M. Program comment: 'THE FOLLOWING PROGRAM TESTS THE FLAGGING OF HIGH SUBSET FEATURES THAT ARE USED IN SOURCE TEXT MANIPULATION.' Flagged constructs are 'COPY KSM41 REPLACING "PIG" BY "HORSE".' and 'REPLACE OFF.', each annotated NON-CONFORMING STANDARD; '*TOTAL NUMBER OF FLAGS EXPECTED = 2.' Body is only PERFORM...THRU / STOP RUN; no CCVS report. guard.sh line 36 documents flagging-M modules as excluded. |

## IC

| Program | Class | Evidence |
|---|---|---|
| IC102A | callee-half | Line 42: 'PROCEDURE DIVISION USING DN1.' — subprogram (PROGRAM-ID IC1024). CALLed by baselined caller IC101A (IC101A.cob: CALL "IC102A"). Cannot run standalone. |
| IC104A | callee-half | Line 59: 'PROCEDURE DIVISION USING GRP-01 ELEM-01 GRP-02.' Subprogram CALLed by baselined IC103A (IC103A.cob: CALL "IC104A"). |
| IC105A | callee-half | Line 35: 'PROCEDURE DIVISION  USING DN1 DN2.' Subprogram CALLed by baselined IC103A (IC103A.cob: CALL "IC105A"). |
| IC107A | callee-half | Line 62: 'PROCEDURE DIVISION USING IDN2 GROUP-1 GROUP-2.' Subprogram CALLed by baselined IC106A (IC106A.cob: CALL "IC107A"). |
| IC109A | callee-half | Line 56: 'PROCEDURE DIVISION USING GRP-01.' Subprogram CALLed by baselined IC108A (IC108A.cob: CALL "IC109A"). |
| IC110A | callee-half | Line 58: 'PROCEDURE DIVISION USING LS1 GRP-01.' Subprogram CALLed by baselined IC108A (IC108A.cob: CALL "IC110A"). |
| IC111A | callee-half | Line 48: 'PROCEDURE DIVISION USING LS1 GRP-01 LS2.' Subprogram CALLed by baselined IC108A (IC108A.cob: CALL "IC111A"). |
| IC113A | callee-half | Line 58: 'PROCEDURE DIVISION USING RECORDS-IN-ERROR  SQ-FS3-R1-G-120 ...' Subprogram CALLed by baselined IC112A (IC112A.cob: CALL "IC113A"). |
| IC115A | callee-half | Line 126: 'PROCEDURE DIVISION USING GROUP-LINKAGE-VARIABLES ...' Subprogram CALLed by baselined IC114A (IC114A.cob: CALL "IC115A"). |
| IC116M | flagging-M | M-named flagging-conformance module. Comment (lines 2200-2800): 'IC116 AND THE SUBPROGRAMS IC117 AND IC118 TEST THE CALL STATEMENT WITHOUT THE OPTIONAL USING PHRASE AND THE PROCEDURE DIVISION HEADER WITHOUT THE OPTIONAL USING PHRASE'; references obsolete X3.23-1974 (COBOL-74). Source is the concatenated IC1164/IC1174/IC1184 unit; uses col-7 indicator-letter flagging cards (S=skip on lines 2150-2170, Y=replace on lines 2790-2870). Main half CALLs "IC117M" (line 3210) without USING — the flagged construct. |
| IC117M | flagging-M | M-named flagging-conformance module: subprogram half of the IC116/117/118 'CALL / PROCEDURE DIVISION without USING' flagging test. Line 46: 'PROCEDURE DIVISION.' (no USING — the flagged construct); references X3.23-1995 (line 6600). Line 62: CALL "IC118M" (without USING). It is also a callee (CALLed by IC116M line 3210), but its M-flagging purpose (PROC-DIV/CALL without USING) governs. |
| IC118M | flagging-M | M-named flagging-conformance module: terminal subprogram half of the IC116/117/118 'CALL / PROCEDURE DIVISION without USING' flagging test. Line 40: 'PROCEDURE DIVISION.' (no USING — the flagged construct); ends 'EXIT PROGRAM.' (line 5500). CALLed by IC117M (line 6200) without USING; its M-flagging purpose governs. |
| IC202A | callee-half | Line 49: 'PROCEDURE DIVISION USING DN1, DN2, DN3, DN4.' Subprogram CALLed by baselined IC201A (IC201A.cob: CALL "IC202A"). |
| IC204A | callee-half | Line 54: 'PROCEDURE DIVISION USING SUB-TABLE-1, SUB-DN1.' Subprogram CALLed by baselined IC203A (IC203A.cob: CALL "IC204A"). |
| IC205A | callee-half | Line 52: 'PROCEDURE DIVISION USING TABLE-1, TABLE-2, DN1.' Subprogram CALLed by baselined IC203A (IC203A.cob: CALL "IC205A"). |
| IC206A | callee-half | Line 50: 'PROCEDURE DIVISION USING DN1.' Subprogram CALLed by baselined IC203A (IC203A.cob: CALL "IC206A"). |
| IC208A | callee-half | Line 50: 'PROCEDURE DIVISION USING TABLE-01, TABLE-02, INDEX-1, DN3.' Subprogram. Its baselined caller IC207A is in the guard list (IC207A.cob shows no resolvable CALL literal in the quick scan because it CALLs an identifier/data-name, but IC208A is the USING-callee of the IC207A index-passing test family and cannot run standalone). |
| IC210A | callee-half | Line 36: 'PROCEDURE DIVISION USING TEST-AREA.' Subprogram CALLed by baselined IC209A (IC209A.cob: CALL "IC210A"). |
| IC211A | callee-half | Line 36: 'PROCEDURE DIVISION USING TEST-AREA.' Subprogram CALLed by baselined IC209A (IC209A.cob: CALL "IC211A"). |
| IC212A | callee-half | Line 36: 'PROCEDURE DIVISION USING TEST-AREA.' Subprogram CALLed by baselined IC209A (IC209A.cob: CALL "IC212A"). |
| IC214A | callee-half | Line 32: 'PROCEDURE DIVISION USING DN1.' Subprogram CALLed by baselined IC213A (IC213A.cob: CALL "IC214A"). |
| IC215A | callee-half | Line 33: 'PROCEDURE DIVISION USING DN2, DN3.' Subprogram CALLed by baselined IC213A (IC213A.cob: CALL "IC215A"). |
| IC217A | callee-half | Line 40: 'PROCEDURE DIVISION USING DN1, DN4.' Subprogram CALLed by baselined IC216A (IC216A.cob: CALL "IC217A"). |
| IC401M | flagging-M | M-named flagging-conformance module. Header comment (lines 600-800): 'The following program tests the flagging of high subset Features that are used in inter-program communication.' Every flagged construct is annotated '*Message expected for above statement: NON-CONFORMING STANDARD': PROGRAM-ID IS INITIAL (line 300), 01 GLOB IS GLOBAL (1800), 01 EXTE IS EXTERNAL (2100), USE GLOBAL AFTER STANDARD ERROR (2900), CANCEL "NESTEDPROG" (3900), CALL "NESTEDPROG" USING BY REFERENCE (4400), CALL "FIC401M" USING BY CONTENT (4900), and a nested IDENTIFICATION DIVISION (5300). No PRINT-FILE/CCVS report machinery — emits no CCVS report. |

## SQ

| Program | Class | Evidence |
|---|---|---|
| SQ303M | flagging-M | Header (line 4-5): 'TESTS THE FLAGGING OF OBSOLETE FEATURES THAT ARE USED IN HIGH SUBSET SEQUENTIAL'. Exercises obsolete constructs each with '*Message expected for above statement: OBSOLETE': line 26 'MULTIPLE FILE TAPE CONTAINS TFIL2.' (I-O-CONTROL) and line 42 'OPEN INPUT TFIL REVERSED.'. 'TOTAL NUMBER OF FLAGS EXPECTED = 2.' Emits no CCVS report — flagging module, no valid baseline. |
| SQ401M | flagging-M | Header (line 4-6): 'TESTS THE FLAGGING OF HIGH SUBSET FEATURES THAT ARE USED IN SEQUENTIAL INPUT-OUTPUT.' 18 constructs each annotated '*Message expected for above statement: NON-CONFORMING STANDARD' (e.g. line 44 'MULTIPLE FILE TAPE CONTAINS TFIL2.', plus 17 others in lines 16-133). 'TOTAL NUMBER OF FLAGS EXPECTED = 18.' Flagging-conformance module, no valid baseline. |

## OBSQ

| Program | Class | Evidence |
|---|---|---|
| OBSQ3A | no-output-producer | Producer in the OBSQ tape chain. Header (lines 13-22): 'TWO TAPES ARE CREATED CONTAINING 4 FILES EACH... TAPE ONE IS THEN PASSED ON TO OBSQ4A AND OBSQ5A WHERE IT IS READ AND VALIDATED. TAPE TWO IS THEN PASSED ON TO OBSQ5A WHERE IT IS READ AND VALIDATED.' OBSQ3A opens OUTPUT and WRITEs the shared SEQUENTIAL files SQ-FS1=XXXXP004 (OPEN OUTPUT SQ-FS1 / WRITE SQ-FS1R1-F-G-120, lines ~448-451), SQ-FS2=XXXXP008, SQ-FS3=XXXXP009, SQ-FS4=XXXXP010 (tape one via MULTIPLE FILE TAPE CONTAINS SQ-FS1..SQ-FS4), and SQ-FS5=XXXXP005, SQ-FS6=XXXXP011, SQ-FS7=XXXXP012, SQ-FS8=XXXXP013 (tape two via MULTIPLE FILE TAPE ... POSITION). Consumers: baselined OBSQ4A.txt (OPEN INPUT SQ-FS1/SQ-FS3/SQ-FS2/SQ-FS4 + READ ... AT END) and baselined OBSQ5A.txt (OPEN INPUT SQ-FS3/FS5/FS6/FS7/FS8 + READ). Listed in guard.sh NIST_TESTS line 115 ('OBSQ1A OBSQ3A OBSQ4A OBSQ5A') but has NO tests/nist/valid/OBSQ3A.txt, so guard.sh runs it to build the tapes and hits the 'NO BASELINE' branch (no output comparison) — guard comment line 29 counts only '3 OBSQ' baselined. Standard NO_OUTPUT shared-file-builder pattern; fully accounted for. |

## IX

| Program | Class | Evidence |
|---|---|---|
| IX301M | flagging-M | Ends in M; comment line 4 'THE FOLLOWING PROGRAM TESTS THE FLAGGING OF INTERMEDIATE SUBSET FEATURES THAT ARE USED IN LEVEL 1 INDEXED INPUT-OUTPUT'. Seven '*Message expected for above statement: NON-CONFORMING STANDARD' markers (line 70: 'TOTAL NUMBER OF FLAGS EXPECTED = 7'). Flags subset/intermediate INDEXED I-O constructs: ORGANIZATION IS INDEXED (l.1700), ACCESS MODE IS RANDOM (l.2000), RECORD KEY IS RKEY (l.2300), READ/REWRITE/WRITE/DELETE with INVALID KEY + NOT INVALID KEY phrases. No 'PROCEDURE DIVISION USING' (not a callee), no ACCEPT FROM DATE/TIME, produces no shared data file. guard.sh line 36 documents flagging-'M' modules as excluded for operational conformance. |
| IX401M | flagging-M | Ends in M; comment line 4 'THE FOLLOWING PROGRAM TESTS THE FLAGGING OF HIGH SUBSET FEATURES THAT ARE USED IN INDEXED INPUT-OUTPUT'. Ten '*Message expected for above statement: NON-CONFORMING STANDARD' markers (line 84: 'TOTAL NUMBER OF FLAGS EXPECTED = 10'). Flags high-subset INDEXED I-O constructs: SELECT OPTIONAL (l.1500), RESERVE 2 AREAS (l.1900), ACCESS MODE IS DYNAMIC (l.2300), ALTERNATE RECORD KEY IS BEANO (l.2700), RECORD IS VARYING IN SIZE FROM 18 TO 36 (l.3900), CLOSE TFIL WITH LOCK (l.6000), OPEN EXTEND (l.6400), READ NEXT RECORD (l.6900), READ RECORD KEY IS (l.7400), START KEY IS EQUAL TO (l.8000). No 'PROCEDURE DIVISION USING' (not a callee), no ACCEPT FROM DATE/TIME, produces no shared data file. guard.sh line 36 documents flagging-'M' modules as excluded for operational conformance. |

## RL

| Program | Class | Evidence |
|---|---|---|
| RL212A | no-output-producer | Intro comment (line 1300): 'THIS RUN UNIT IS THE FIRST OF A SERIES OF TWO PROGRAMS... THE FUNCTION OF THIS PROGRAM IS TO CREATE A RELATIVE FILE SEQUENTIALLY... THE FILE IS IDENTIFIED AS "RL-FS1"'. Body OPENs RL-FS1 OUTPUT and WRITEs records (lines 384-397). The baselined consumer RL213A's intro (its line 459/1500) states explicitly: 'USED AS INPUT IS THE FILE "RL-FS1" CREATED BY RL212A'; RL213A then OPENs EXTEND RL-FS1 (line 843). In scripts/guard.sh NIST_TESTS (line 112) RL212A is placed immediately before RL213A: '...RL302M RL212A RL213A'. Guard runs it as NO BASELINE (producer) — guard.sh lines 144-151 skip comparison for tests with no valid/*.txt. Shared file = RL-FS1; consumer = RL213A (baselined). |
| RL301M | flagging-M | Name ends in M; intro (line 4): 'The following program tests the flagging of intermediate subset features that are used in relative input-output.' Source carries 6 '*Message expected for above statement: NON-CONFORMING STANDARD' comments and 'TOTAL NUMBER OF FLAGS EXPECTED = 6.' (line 67). Flagged constructs are the NOT INVALID KEY phrase on relative I/O: READ TFIL ... NOT INVALID KEY (line 4300), REWRITE FREC ... NOT INVALID KEY (4700), WRITE FREC ... NOT INVALID KEY (5200), DELETE TFIL ... NOT INVALID KEY (5700). Not guarded (only RL302M is guarded among RL M-tests). |
| RL401M | flagging-M | Name ends in M; intro (line 4): 'THE FOLLOWING PROGRAM TESTS THE FLAGGING OF HIGH SUBSET FEATURES THAT ARE USED IN RELATIVE INPUT-OUTPUT.' Source carries 9 '*Message expected for above statement: NON-CONFORMING STANDARD' comments and 'TOTAL NUMBER OF FLAGS EXPECTED = 9.' (line 78). Flagged high-subset relative-I/O constructs include SELECT OPTIONAL TFIL (1500), RESERVE 2 AREAS (1800), SAME RECORD AREA FOR TFIL2, TFIL (3000), RECORD IS VARYING IN SIZE (3600), CLOSE TFIL WITH LOCK (6000), OPEN EXTEND TFIL2 (6500), READ TFIL NEXT RECORD (6900), START TFIL KEY IS EQUAL TO (7400). Not guarded. |

## ST

| Program | Class | Evidence |
|---|---|---|
| ST102A | no-output-producer | Single 78-line program: PROCEDURE DIVISION does only 'SORT SORTFILE-1B ... USING SORTIN-1B (XXXXD001) GIVING SORTOUT-1B (XXXXP002)' then STOP RUN. Zero detail-print statements (no WRITE PRINT-REC / CCVS report). Chain per guard.sh:40-41 — ST101A builds the SORT output file TF002 -> ST102A updates it (NO_OUTPUT producer, not baselined) -> ST103A (baselined .txt exists) verifies. Consumer=ST103A, shared file=SORTOUT (XXXXP002/TF002). |
| ST109A | no-output-producer | Header comment line 21: 'ST109 BUILDS A FILE WHICH IS SORTED IN ST110 AND CHECKED IN' (ST111). It is the builder of a 40-variable-length-record file (guard.sh:43). No CCVS report (it is the build half of the concatenated file; the printed verifier is ST111A). Consumer chain: ST109A builds -> ST110A sorts -> ST111A (baselined, verifies 7/7). Shared variable-length SORT input file. |
| ST110A | no-output-producer | Standalone .cob (only PROGRAM-ID ST1104) doing 'SORT SORTFILE-1J' over variable-length records; companion file ST109A.cob line 834 emits ' *****  ST110 DOES NOT PRODUCE A PRINTED REPORT  ***'. guard.sh:43-44: ST109A (build) -> ST110A (variable-length SORT, NO_OUTPUT) -> ST111A (baselined, verifies 7/7). Consumer=ST111A, shared variable-length SORT file. |
| ST112M | no-output-producer | NOT a flagging module — grep for 'Message expected'/'NON-CONFORMING'/'TOTAL NUMBER OF FLAGS' returns nothing. The 'M' denotes MULTIPLE-REEL: line 50 literal '3-REEL FILE WHICH WILL BE PASSED TO ST113 FOR SORTING', paragraph BUILD-REEL, H-card 'CLOSE SORTOUT-1L REEL' (line 373); ST114M feature literal 'SORT, MULTIPLE REEL'. It builds the 3-reel file with no printed report. guard.sh:41-43: ST112M (builds 3-reel file, 000-of-000 builder, NOT baselined) -> ST113M (sorts, NO_OUTPUT) -> ST114M (baselined, verifies 10/10). Consumer=ST114M. |
| ST113M | no-output-producer | NOT a flagging module — no 'Message expected'/flag-count comments. 'M' = multiple-reel chain. Standalone sorter: PROCEDURE has 'SORT SORTFILE-1M DESCENDING SORT-KEY USING SORTIN-1M GIVING SORTOUT-1M' (lines 63-66), no CCVS report. guard.sh:42-43: ST112M (build) -> ST113M (sorts, NO_OUTPUT) -> ST114M (baselined, 10/10). Consumer=ST114M, shared multi-reel SORT files SORTIN-1M/SORTOUT-1M. |
| ST115A | no-output-producer | Header line 29: 'IS THEN PASSED TO ST116 TO BE SORTED.' Builds the 204-record file SQ-FS1 (XXXXX065 record count substituted to 204=51*4, guard.sh:45-46). It is the build half of the concatenated 115/116/117 file; the printed verifier is ST117A. guard.sh:45-47: ST115A (builds 204-record file, 000-of-000 builder, NOT baselined) -> ST116A (BIG-SORT, NO_OUTPUT) -> ST117A (baselined, verifies native-collating sort, 1/1). Consumer=ST117A, shared file SQ-FS1. |
| ST116A | no-output-producer | Standalone sorter .cob (only PROGRAM-ID ST1164). Comment line 65 'SQ-FS1 IS SORTED GIVING SQ-FS2.' — the BIG-SORT of 204 records, no printed report. guard.sh:46-47: ST115A (build) -> ST116A (BIG-SORT, NO_OUTPUT) -> ST117A (baselined, 1/1). Consumer=ST117A, shared files SQ-FS1 (in) / SQ-FS2 (out). |
| ST120A | no-output-producer | Standalone .cob (only PROGRAM-ID ST1204), no CCVS detail print. guard.sh:48-50: ST119A (baselined; SORTs and writes TF001 via XXXXP001) -> ST120A (SORT USING TF001 GIVING TF002, the USING/GIVING feature; NO_OUTPUT producer) -> ST121A (baselined, verifies the TF002 sort, 9/9). Consumer=ST121A, shared files TF001 (in) / TF002 (out). |
| ST122A | no-output-producer | Header line 25: 'ST122 BUILDS A FILE WHICH IS SORTED IN ST123 AND CHECKED IN' (ST124). Build half of concatenated 122/123/124 file; companion line 872 emits 'ST123A DOES NOT PRODUCE A PRINTED REPORT'. guard.sh:44: 'ST122A -> ST123A -> ST124A (the second variable-length SORT chain)'. ST124A baselined. Consumer=ST124A, shared variable-length SORT file. |
| ST123A | no-output-producer | Standalone sorter .cob (only PROGRAM-ID ST1234) doing 'SORT SORTFILE-1J' over variable-length records; companion ST122A.cob line 872 emits ' *****  ST123A DOES NOT PRODUCE A PRINTED REPORT'. guard.sh:44: ST122A -> ST123A -> ST124A (second variable-length SORT chain). Consumer=ST124A (baselined), shared variable-length SORT file. |
| ST301M | flagging-M | Name ends in M and source header lines 4-5 state 'The following program tests the flagging of intermediate subset features that are used in sort-merge functions.' Exercises non-conforming constructs: 'SAME SORT-MERGE AREA FOR TFIL-5, TFIL.' (line 30) plus MERGE/SORT statements, each annotated '*Message expected for above statement: NON-CONFORMING STANDARD' (lines 31,35,66,70,75,81); line 84 '*TOTAL NUMBER OF FLAGS EXPECTED = 6.' Emits flag diagnostics, not a CCVS report — excluded from baselining like other …M flagging modules. |

## RW

| Program | Class | Evidence |
|---|---|---|
| RW301M | flagging-M | Name ends in M; header comment line 400 'TESTS THE FLAGGING OF FEATURES THAT ARE USED IN REPORT WRITING'. 10 '*Message expected for above statement: NON-CONFORMING STANDARD' flagging-comment lines (at src lines 4300,5100,5300,5600,5900,6100,6300,7400,7600,7800) on FD...REPORT IS, REPORT SECTION, RD, 01 TYPE IS DETAIL, SOURCE IS, COLUMN NUMBER, etc. No 'PROCEDURE DIVISION USING' (grep exit 1). Not in guard NIST_TESTS (scripts/guard.sh line 116 lists only RW101A RW102A RW103A RW104A). It is a flagging-conformance module exercising Report Writer flagged as non-conforming to the standard. |
| RW302M | flagging-M | Name ends in M; header comment lines 400-600 'TESTS THE FLAGGING OF OBSOLETE FEATURES THAT ARE USED IN REPORT WRITING'. 3 '*Message expected for above statement: OBSOLETE' flagging-comment lines (src lines 2800,3500,4000) on MULTIPLE FILE TAPE CONTAINS, LABEL RECORDS STANDARD, and VALUE OF...IS. No 'PROCEDURE DIVISION USING' (grep exit 1). Not in guard NIST_TESTS (scripts/guard.sh line 116 lists only RW101A-RW104A). It is a flagging-conformance module exercising obsolete features expected to raise OBSOLETE diagnostics. |


