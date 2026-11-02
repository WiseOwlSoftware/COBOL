*>--- conformance-case
*> id: pb4301-misspelled-clause-keyword-is-named
*> rules: [FMT-12.4.5.1]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB4301. ISO 12.4.5.1 prints the clauses of a file control entry, the ASSIGN clause among them
*>   cite.py: OK  12.4.5.1  (General formats) - "ASSIGN"
*> and a closed list admits no other word (ISO 5.2.1, cite.py OK 5.2.1 "words, phrases, clauses"). ASIGN below is
*> no clause: the refusal names ASIGN, the word the unrecognized run opens with, and not the TO that ended the run
*> (a reserved word the file control entry's clauses also take). It fails if the report names 'TO'.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB4301ASIGN.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT F1 ASIGN TO "pb4301.dat".
DATA DIVISION.
FILE SECTION.
FD F1.
01 F1-R PIC X.
PROCEDURE DIVISION.
MAIN-P.
    STOP RUN.
