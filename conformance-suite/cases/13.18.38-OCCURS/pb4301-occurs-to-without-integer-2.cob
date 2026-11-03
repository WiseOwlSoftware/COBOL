*>--- conformance-case
*> id: pb4301-occurs-to-without-integer-2
*> rules: [FMT-13.18.38.2]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB4301. ISO 13.18.38.2 prints Format 2 as OCCURS integer-1 TO integer-2 TIMES
*>   cite.py: OK  13.18.38.2  (General formats) - "integer-1 TO integer-2"
*> so the TO requires integer-2 before TIMES. OCCURS 0 TO TIMES below omits it: the OCCURS clause is incomplete, and
*> the refusal stands at the TIMES, naming the operand the position takes. It fails if the report blames the TO as a
*> word that is no clause of the entry (COBOL1941: a word of the clause itself), or is the bare COBOL0001.
*> From 2002 the operand is an integer literal or an integer constant-name (13.10.3 SR2), so the refusal names both;
*> COBOL-85 has no constant-name, and its refusal is pb4435-occurs-bound-omitted-at-85 (kb/Work PB4435).
IDENTIFICATION DIVISION.
PROGRAM-ID. PB4301OCCURS.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 W-E PIC 9(3) VALUE 5.
01 W-G.
    05 W-C PIC 9(3) OCCURS 0 TO TIMES DEPENDING ON W-E.
PROCEDURE DIVISION.
MAIN-P.
    STOP RUN.
