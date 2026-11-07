*>--- conformance-case
*> id: pb4463-odo-integer-1-not-less-than-integer-2
*> rules: [SR-13.18.38.3-16]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §2023:13.18.38.3 SR16 (cite.py --check 13.18.38.3 "Integer-1 shall be greater than or equal to zero and
      *> integer-2 shall be greater than integer-1" -> OK 16)): the bounds of OCCURS integer-1 TO integer-2 DEPENDING ON
      *> are ordered. 5 TO 3 puts integer-1 above integer-2, so the clause is refused (COBOL0850, kb/Work PB4463).
      *> The rule has no introduction axis (OCCURS DEPENDING ON is COBOL-85), so it is refused at every edition.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB4463OD.
DATA DIVISION.
WORKING-STORAGE SECTION.
77 N PIC 9 VALUE 3.
01 G.
   05 T PIC X OCCURS 5 TO 3 TIMES DEPENDING ON N.
PROCEDURE DIVISION.
MAIN.
    STOP RUN.
