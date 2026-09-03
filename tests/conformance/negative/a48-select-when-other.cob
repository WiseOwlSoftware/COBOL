*> reject-at: 85 2002 2014 2023
*> ISO 1989:2023 Annex A.4.8 item 2), the OTHER arm - the SECOND alternative of 13.18.51.2's
*> `SELECT WHEN { condition-name-1 | OTHER }`. A two-arm construct needs BOTH arms measured or a fix lands
*> on one and the suite still reads green (feedback_two_arm_dispatch); the OTHER arm reaches a different
*> grammar alternative (a reserved word, not a user-defined word) and so a different code path.
*> 13.18.51.3 rule 6 admits OTHER only in the LAST record description entry associated with a given file,
*> which is why the fixture places it last, after a condition-name-1 entry.
IDENTIFICATION DIVISION.
PROGRAM-ID. A48SWO9AL.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT F-IN ASSIGN TO "a48swo9al.dat"
        ORGANIZATION IS SEQUENTIAL.
DATA DIVISION.
FILE SECTION.
FD F-IN
    RECORD IS VARYING IN SIZE FROM 1 TO 40 CHARACTERS.
01 REC-A SELECT WHEN COND-A.
   05 R-KIND PIC X.
      88 COND-A VALUE "A".
   05 R-REST PIC X(39).
01 REC-B SELECT WHEN OTHER.
   05 R-ALL PIC X(40).
PROCEDURE DIVISION.
MAIN.
    STOP RUN.
