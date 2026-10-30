      *>--- conformance-case
      *> id: pb4092_report_compose_per_name
      *> rules: [GR-14.7.5-3, GR-14.7.5-4, GR-7.3.25.4-6, GR-13.18.53.4-2, GR-13.18.41.4-2]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> PB4092 - the report compose takes the PRESENTING statement's
      *> EC-SIZE enablement PER NAME (the sibling of
      *> pb4031_report_overflow_checking, which enables the family).
      *>
      *> ISO 14.6.13.1.1 NOTE 3: "An exception condition is set to
      *> exist only if checking for that condition is enabled".
      *> 14.7.5 names GENERATE among the statements the size error
      *> condition "may occur as a result of", and 7.3.25.4 GR6 enables
      *> checking "for the procedure division statements ... that follow
      *> in the compilation group", so the TURN in force at the GENERATE
      *> decides, name by name, for every arithmetic expression the
      *> presentation evaluates: a SOURCE expression (13.18.53.4 GR2's
      *> implicit COMPUTE) and a PRESENT WHEN condition (13.18.41.4
      *> GR2). A condition whose checking is not enabled takes
      *> 14.6.13.1.3 item 8's not-enabled disposition, which
      *> CONFORMANCE.md DOC-A.1-70 documents: execution continues, a
      *> value the printable item cannot hold prints as its low-order
      *> digits, and a binary64 intermediate past its range is the IEEE
      *> value (DOC-A.1-179).
      *>
      *> EVERY EXPECTED VALUE IS COMPUTED FROM THAT RULE. WS-D = 1.0E200
      *> so WS-D x WS-D is IEEE +Infinity, which is > 0; WS-A x WS-A =
      *> 10000 does not fit the printable item PIC 9(3): 000.
      *>   STEP 1 EC-SIZE-TRUNCATION on, GENERATE PP-P (PRESENT WHEN
      *>          WS-D * WS-D > 0): the overflow is not enabled, so the
      *>          condition is true and the line prints: PW
      *>   STEP 2 EC-SIZE-OVERFLOW on, GENERATE PP-F (SOURCE IS
      *>          WS-A * WS-A): the truncation is not enabled, so the
      *>          line prints the low-order digits: 000
      *>   STEP 3 EC-SIZE-OVERFLOW on, GENERATE PP-Q (PRESENT WHEN
      *>          WS-D * WS-D > 0): EC-SIZE-OVERFLOW is raised
      *>   STEP 4 EC-SIZE-TRUNCATION on, GENERATE PP-G (SOURCE IS
      *>          WS-A * WS-A): EC-SIZE-TRUNCATION is raised
      *> Each raise reaches the USE AFTER EXCEPTION CONDITION EC-SIZE
      *> declarative, which RESUMEs at the next statement. R-OK's file
      *> is read back (R-RZ's is not: the raises interrupt it). The leg
      *> fails if step 1 raises EC-SIZE-OVERFLOW (the family-wide
      *> checked kernels did) or if step 2 raises, or prints anything
      *> but 000.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4092RC.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SYM-X0A SYM-X0D
               ARE 11 14.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRK ASSIGN TO "pb4092ok.txt".
           SELECT PRZ ASSIGN TO "pb4092rz.txt".
           SELECT CHK ASSIGN TO "pb4092ok.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRK REPORT IS R-OK.
       FD  PRZ REPORT IS R-RZ.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-EOF    PIC X     VALUE "N".
       01  WS-A      PIC 999   VALUE 100.
       01  WS-D      COMP-2    VALUE 1.0E200.
       01  WS-STEP   PIC 9     VALUE 0.
       01  WS-RAISED PIC 9     VALUE 0.
       01  WS-I      PIC 99    VALUE 0.
       01  WS-LINE   PIC X(40) VALUE SPACES.
       REPORT SECTION.
       RD  R-OK PAGE LIMIT 20 LINES.
       01  PP-P TYPE DE LINE PLUS 1.
           03  COLUMN 1  PIC X(2) VALUE "PW"
                         PRESENT WHEN WS-D * WS-D > 0.
       01  PP-F TYPE DE LINE PLUS 1.
           03  COLUMN 1  PIC 9(3) SOURCE IS WS-A * WS-A.
       RD  R-RZ PAGE LIMIT 20 LINES.
       01  PP-Q TYPE DE LINE PLUS 1.
           03  COLUMN 1  PIC X(2) VALUE "PW"
                         PRESENT WHEN WS-D * WS-D > 0.
       01  PP-G TYPE DE LINE PLUS 1.
           03  COLUMN 1  PIC 9(3) SOURCE IS WS-A * WS-A.
       PROCEDURE DIVISION.
       DECLARATIVES.
       SZ SECTION.
           USE AFTER EXCEPTION CONDITION EC-SIZE.
       SZ-1.
           ADD 1 TO WS-RAISED.
           DISPLAY "RAISED " FUNCTION EXCEPTION-STATUS
               " AT STEP " WS-STEP.
           RESUME NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       M-1.
           OPEN OUTPUT PRK PRZ.
           INITIATE R-OK R-RZ.
       >>TURN EC-SIZE-TRUNCATION CHECKING ON
           MOVE 1 TO WS-STEP.
           GENERATE PP-P.
       >>TURN EC-SIZE-TRUNCATION CHECKING OFF
       >>TURN EC-SIZE-OVERFLOW CHECKING ON
           MOVE 2 TO WS-STEP.
           GENERATE PP-F.
           MOVE 3 TO WS-STEP.
           GENERATE PP-Q.
       >>TURN EC-SIZE-OVERFLOW CHECKING OFF
       >>TURN EC-SIZE-TRUNCATION CHECKING ON
           MOVE 4 TO WS-STEP.
           GENERATE PP-G.
       >>TURN EC-SIZE-TRUNCATION CHECKING OFF
           TERMINATE R-OK R-RZ.
           CLOSE PRK PRZ.
           OPEN INPUT CHK.
           PERFORM UNTIL WS-EOF = "Y"
               READ CHK
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END PERFORM TAKE-BYTE
               END-READ
           END-PERFORM.
           CLOSE CHK.
           PERFORM SHOW-LINE.
           DISPLAY "RAISED " WS-RAISED " TIMES".
           STOP RUN.
       TAKE-BYTE.
           IF CHK-REC = SYM-X0A
               PERFORM SHOW-LINE
           ELSE
               IF CHK-REC NOT = SYM-X0D
                   ADD 1 TO WS-I
                   MOVE CHK-REC TO WS-LINE(WS-I:1)
               END-IF
           END-IF.
       SHOW-LINE.
           IF WS-I > 0
               DISPLAY "[" WS-LINE(1:WS-I) "]"
               MOVE SPACES TO WS-LINE
               MOVE 0 TO WS-I
           END-IF.
