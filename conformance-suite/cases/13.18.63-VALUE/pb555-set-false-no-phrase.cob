*>--- conformance-case
*> id: pb555-set-false-no-phrase
*> rules: [GR-13.18.63.4-20, SR-14.9.39.3-7]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB555 — arm: ISO 14.9.39.3 SR7. "If the FALSE phrase is specified, the FALSE phrase shall
*> be specified in the VALUE clause of the data description entry for condition-name-1."
*> 14.9.39.4 GR7 places "the literal in the FALSE phrase of the VALUE clause associated with
*> condition-name-1" in the conditional variable; with no such phrase there is no value to place, and
*> the standard states no default — 13.18.63.4 GR20's NOTE 3 says so in terms ("The WHEN SET TO FALSE
*> phrase specifies just one of possibly many false values"), so the processor cannot choose one.
*> COBOL2049.
*> Refused from 2002 on (2002, 2014, 2023), each citing its own SR7. At 85 SET ... TO FALSE is itself a COBOL-2002
*> introduction (row set-condition-false-2002), so the edition gate COBOL0900 is the one refusal and the SR7 screen
*> stands down (kb/Work PB6356; pb6356-set-condition-false-without-phrase-gate-only pins the 85 leg).
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGPB555C.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 CV PIC 9 VALUE 1.
   88 CN VALUE 1.
PROCEDURE DIVISION.
    SET CN TO FALSE.
    STOP RUN.
