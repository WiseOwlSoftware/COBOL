*>--- conformance-case
*> id: pb4301-misspelled-occurs-before-an-integer
*> rules: [FMT-13.18.38.2]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB4301. ISO 13.18.38.2 prints Format 1 as OCCURS integer-2 TIMES
*>   cite.py: OK  13.18.38.2  (General formats) - "integer-2 TIMES"
*> and a data description entry takes only the clauses of its general format (ISO 5.2.1, cite.py OK 5.2.1). OCURS
*> below is no clause, and the integer after it is the end of the unrecognized run the data division's form of the
*> error production cannot hold (the next entry's level-number may stand there). The refusal names OCURS, the word
*> the run opens with, once: not the bare COBOL0001 at the 3, and not a second refusal of the TIMES as a clause of a
*> level-3 entry the program does not hold.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB4301MISSPELT.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 W-C PIC 9(3) OCURS 3 TIMES.
PROCEDURE DIVISION.
MAIN-P.
    STOP RUN.
