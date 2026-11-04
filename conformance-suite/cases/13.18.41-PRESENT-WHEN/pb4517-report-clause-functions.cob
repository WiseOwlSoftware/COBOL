*>--- conformance-case
*> id: pb4517-report-clause-functions
*> rules: [FMT-13.18.41.2, GR-13.18.41.4-2, GR-13.18.53.4-1, GR-13.18.53.4-2,
*>    GR-13.18.54.4-6, GR-13.18.64.4-3, GR-13.18.12.4-3, GR-8.4.3.2.4-1]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4517 - a function-identifier in a report clause
      *> operand is evaluated at EACH evaluation of the clause: in a
      *> PRESENT WHEN condition (an intrinsic and a user function), in
      *> a SOURCE operand and in its subscript, in a SUM operand, in the
      *> SOURCE of an entry a SUM rolls up, in a VARYING FROM / BY, and
      *> in the subscript of the CODE clause's identifier-1.
      *>
      *> condition-1 is any condition (13.18.41.2 Format 1), and
      *> "condition-1 of each PRESENT WHEN clause is evaluated before
      *> the processing of any LINE clauses for the report group"
      *>   cite.py: OK  13.18.41.4 2)  (General rules)
      *> "it specifies the sending operand of an implicit MOVE statement
      *> in which the data item referenced by identifier-1 is moved to
      *> the printable item"
      *>   cite.py: OK  13.18.53.4 1)  (General rules)
      *> "Arithmetic-expression-1 specifies the operand of an implicit
      *> COMPUTE statement that is executed implicitly whenever the
      *> associated item is printed."
      *>   cite.py: OK  13.18.53.4 2)  (General rules)
      *> "the value added is that of the operand of the SOURCE or VALUE
      *> clause"
      *>   cite.py: OK  13.18.54.4 6)  (General rules)
      *> "the value of arithmetic-expression-2 is added to data-name-1"
      *>   cite.py: OK  13.18.64.4 3)  (General rules)
      *> "If identifier-1 is specified, it is evaluated at the start of
      *> the processing for each body group"
      *>   cite.py: OK  13.18.12.4 3)  (General rules)
      *> "A function-identifier references a temporary data item whose
      *> value is determined when the function is referenced at runtime."
      *>   cite.py: OK  8.4.3.2.4 1)  (General rules)
      *>
      *> DERIVATION. PB4517TW returns twice its argument. R-A's detail is
      *> generated for WS-N = 1, 2, 3:
      *>   col 1  WS-N                                 01 02 03
      *>   col 4  WS-T (MOD (WS-N, 2) + 1) of EVNODD   ODD EVN ODD
      *>   col 8  PB4517TW (WS-N)                      02 04 06
      *>   col 11 "ODD" present when MOD (WS-N, 2) = 1 yes no yes
      *>   col 15 "BIG" present when PB4517TW (WS-N) > 4  no no yes
      *>   col 19 DT = PB4517TW (WS-N)                 02 04 06
      *> TERMINATE prints CF FINAL two lines down, with WS-N = 4 (the
      *> PERFORM's last augment): SUM PB4517TW (WS-N) added at each
      *> GENERATE is 2 + 4 + 6 = 012; SUM DT rolls DT's 2 + 4 + 6 = 012;
      *> V FROM PB4517TW (WS-N) = 8 BY PB4517TW (1) = 2 is 08 10 12.
      *> R-B's CODE is WS-C (MOD (WS-N, 2) + 1) of "AB" at each detail:
      *> B, A, B ahead of 01, 02, 03.
      *> WHY EACH LEG FAILS. Before PB4517 PRESENT WHEN FUNCTION was
      *> refused COBOL0899, and the other operands bound outside every
      *> statement, so their steps (the activation, the subscript store)
      *> were hoisted into the FIRST procedure statement and never ran
      *> at a presentation: a SOURCE WS-T (FUNCTION MOD ...) printed
      *> spaces and a SOURCE FUNCTION user-function printed 0 on every
      *> line. A leg that evaluates its function once, or before the
      *> PERFORM, prints a value the derivation above does not give.
IDENTIFICATION DIVISION.
FUNCTION-ID. PB4517TW.
DATA DIVISION.
LINKAGE SECTION.
01 N PIC 99.
01 R PIC 99.
PROCEDURE DIVISION USING N RETURNING R.
P0.
    COMPUTE R = N * 2
    GOBACK.
END FUNCTION PB4517TW.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB4517PW.
ENVIRONMENT DIVISION.
CONFIGURATION SECTION.
REPOSITORY.
    FUNCTION PB4517TW.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT PRA ASSIGN TO "pb4517a.txt".
    SELECT PRB ASSIGN TO "pb4517b.txt".
    SELECT CHA ASSIGN TO "pb4517a.txt".
    SELECT CHB ASSIGN TO "pb4517b.txt".
DATA DIVISION.
FILE SECTION.
FD PRA REPORT IS R-A.
FD PRB REPORT IS R-B.
FD CHA.
01 CHA-REC PIC X.
FD CHB.
01 CHB-REC PIC X.
WORKING-STORAGE SECTION.
01 WS-N    PIC 99 VALUE 0.
01 WS-TT   VALUE "EVNODD".
   05 WS-T PIC X(3) OCCURS 2.
01 WS-CC   VALUE "AB".
   05 WS-C PIC X OCCURS 2.
01 WS-EOF  PIC X VALUE "N".
01 WS-B    PIC X.
01 WS-I    PIC 99 VALUE 0.
01 WS-LN   PIC 99 VALUE 0.
01 WS-LINE PIC X(24) VALUE SPACES.
REPORT SECTION.
RD R-A CONTROL FINAL.
01 DE-A TYPE DE LINE PLUS 1.
   03 COLUMN 1 PIC 99 SOURCE WS-N.
   03 COLUMN 4 PIC X(3) SOURCE WS-T (FUNCTION MOD (WS-N, 2) + 1).
   03 COLUMN 8 PIC 99 SOURCE FUNCTION PB4517TW (WS-N).
   03 COLUMN 11 PIC X(3) VALUE "ODD" PRESENT WHEN FUNCTION MOD (WS-N, 2) = 1.
   03 COLUMN 15 PIC X(3) VALUE "BIG" PRESENT WHEN FUNCTION PB4517TW (WS-N) > 4.
   03 DT COLUMN 19 PIC 99 SOURCE FUNCTION PB4517TW (WS-N).
01 TYPE CF FINAL LINE PLUS 2.
   03 COLUMN 1 PIC X(3) VALUE "TOT".
   03 COLUMN 5 PIC 999 SUM FUNCTION PB4517TW (WS-N).
   03 COLUMN 9 PIC 999 SUM DT.
   03 COLUMN 13 PIC 99 OCCURS 3 STEP 3
      VARYING V FROM FUNCTION PB4517TW (WS-N) BY FUNCTION PB4517TW (1)
      SOURCE V.
RD R-B CODE WS-C (FUNCTION MOD (WS-N, 2) + 1).
01 DE-B TYPE DE LINE PLUS 1.
   03 COLUMN 1 PIC 99 SOURCE WS-N.
PROCEDURE DIVISION.
MAIN-P.
    OPEN OUTPUT PRA PRB
    INITIATE R-A R-B
    PERFORM VARYING WS-N FROM 1 BY 1 UNTIL WS-N > 3
        GENERATE DE-A
        GENERATE DE-B
    END-PERFORM
    TERMINATE R-A R-B
    CLOSE PRA PRB
    DISPLAY "R-A"
    OPEN INPUT CHA
    PERFORM UNTIL WS-EOF = "Y"
        READ CHA
            AT END MOVE "Y" TO WS-EOF
            NOT AT END MOVE CHA-REC TO WS-B PERFORM TAKE-BYTE
        END-READ
    END-PERFORM
    CLOSE CHA
    PERFORM END-FILE
    DISPLAY "R-B"
    MOVE "N" TO WS-EOF
    OPEN INPUT CHB
    PERFORM UNTIL WS-EOF = "Y"
        READ CHB
            AT END MOVE "Y" TO WS-EOF
            NOT AT END MOVE CHB-REC TO WS-B PERFORM TAKE-BYTE
        END-READ
    END-PERFORM
    CLOSE CHB
    PERFORM END-FILE
    STOP RUN.
TAKE-BYTE.
    IF WS-B = X"0A"
        PERFORM SHOW-LINE
    ELSE
        IF WS-B NOT = X"0D"
            ADD 1 TO WS-I
            MOVE WS-B TO WS-LINE (WS-I:1)
        END-IF
    END-IF.
SHOW-LINE.
    ADD 1 TO WS-LN
    IF WS-LINE NOT = SPACES
        DISPLAY "LINE " WS-LN " [" WS-LINE (1:20) "]"
    END-IF
    MOVE SPACES TO WS-LINE
    MOVE 0 TO WS-I.
END-FILE.
    IF WS-I > 0
        PERFORM SHOW-LINE
    END-IF
    MOVE 0 TO WS-LN.
END PROGRAM PB4517PW.
