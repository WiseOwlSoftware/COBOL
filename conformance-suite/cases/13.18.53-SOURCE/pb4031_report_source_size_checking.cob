      *>--- conformance-case
      *> id: pb4031_report_source_size_checking
      *> rules: [GR-13.18.53.4-2, GR-7.3.25.4-6, GR-14.7.5-4, GR-14.7.5-3]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> PB4031 - a report SOURCE expression's implicit COMPUTE under the
      *> EC-SIZE-TRUNCATION checking of the statement that PRINTS it.
      *>
      *> ISO 13.18.53.4 GR2: "Arithmetic-expression-1 specifies the
      *> operand of an implicit COMPUTE statement that is executed
      *> implicitly whenever the associated item is printed." 14.7.5:
      *> "The size error condition may occur as a result of the
      *> execution of an ADD, COMPUTE, DIVIDE, GENERATE, MULTIPLY, and
      *> SUBTRACT statement", and with no SIZE ERROR phrase (rule 4)
      *> "if the result of the arithmetic statement is a value further
      *> from zero than permitted for the associated resultant data
      *> item, the EC-SIZE-TRUNCATION exception condition is set to
      *> exist". 7.3.25.4 GR6: checking "is enabled for the procedure
      *> division statements and procedure division headers that follow
      *> in the compilation group", so the TURN in force at the
      *> GENERATE or TERMINATE that prints the item decides, not one at
      *> the REPORT SECTION entry. The leg fails if the compose ignores
      *> the presenting statement's checking (main before the fix: no
      *> RAISED line, the run went on), if it raises while that
      *> statement's checking is off, or if it raises on a value the
      *> item can hold.
      *>
      *> EVERY EXPECTED VALUE IS COMPUTED FROM THE RULE. WS-B = 100.
      *>   STEP 1 GENERATE SC-N, checking off, 100 x 100 into 9(3):
      *>          the low-order digits (CONFORMANCE.md DOC-A.1-70) 000
      *>   STEP 2 GENERATE SC-E, checking off, the same into ZZ9:   0
      *>   STEP 3/4 checking on, WS-A = 5: 500 fits both items, no
      *>          raise: 500 and 500
      *>   STEP 5 GENERATE RX-N, WS-A = 100: 10000 into 9(3) raises
      *>   STEP 6 GENERATE RX-E: the same into ZZ9 raises
      *>   STEP 7 GENERATE RX-F, WS-D = 1.0E200: WS-D x WS-D is past
      *>          binary64, an intermediate the arithmetic cannot form,
      *>          so the condition is rule 3's EC-SIZE-OVERFLOW, as in a
      *>          COMPUTE statement, not the final transfer's truncation
      *>   STEP 8 TERMINATE R-RX prints its CONTROL FOOTING FINAL,
      *>          10000 into 9(3): raises
      *>   STEP 9 TERMINATE R-SC, checking off: R-SC has no footing
      *> The TURN names the level-2 EC-SIZE (7.3.25.4 GR3: as if every
      *> subordinate name were specified). Each raise reaches the USE
      *> AFTER EXCEPTION CONDITION EC-SIZE declarative, which RESUMEs at
      *> the next
      *> statement; R-SC's file is read back (R-RX's is not: the raises
      *> interrupt its presentation).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4031RS.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SYM-X0A SYM-X0D
               ARE 11 14.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "pb4031rs.txt".
           SELECT PRX ASSIGN TO "pb4031rx.txt".
           SELECT CHK ASSIGN TO "pb4031rs.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R-SC.
       FD  PRX REPORT IS R-RX.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-EOF    PIC X     VALUE "N".
       01  WS-A      PIC 999   VALUE 100.
       01  WS-B      PIC 999   VALUE 100.
       01  WS-D      COMP-2    VALUE 1.0E200.
       01  WS-STEP   PIC 9     VALUE 0.
       01  WS-RAISED PIC 9     VALUE 0.
       01  WS-I      PIC 99    VALUE 0.
       01  WS-LINE   PIC X(40) VALUE SPACES.
       REPORT SECTION.
       RD  R-SC PAGE LIMIT 20 LINES.
       01  SC-N TYPE DE LINE PLUS 1.
           03  COLUMN 1  PIC 9(3) SOURCE IS WS-A * WS-B.
       01  SC-E TYPE DE LINE PLUS 1.
           03  COLUMN 1  PIC ZZ9  SOURCE IS WS-A * WS-B.
       RD  R-RX CONTROL IS FINAL PAGE LIMIT 20 LINES.
       01  RX-N TYPE DE LINE PLUS 1.
           03  COLUMN 1  PIC 9(3) SOURCE IS WS-A * WS-B.
       01  RX-E TYPE DE LINE PLUS 1.
           03  COLUMN 1  PIC ZZ9  SOURCE IS WS-A * WS-B.
       01  RX-F TYPE DE LINE PLUS 1.
           03  COLUMN 1  PIC 9(3) SOURCE IS WS-D * WS-D.
       01  TYPE CF FINAL LINE PLUS 1.
           03  COLUMN 1  PIC 9(3) SOURCE IS WS-A * WS-B.
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
           OPEN OUTPUT PRT PRX.
           INITIATE R-SC R-RX.
           MOVE 1 TO WS-STEP.
           GENERATE SC-N.
           MOVE 2 TO WS-STEP.
           GENERATE SC-E.
       >>TURN EC-SIZE CHECKING ON
           MOVE 5 TO WS-A.
           MOVE 3 TO WS-STEP.
           GENERATE SC-N.
           MOVE 4 TO WS-STEP.
           GENERATE SC-E.
           MOVE 100 TO WS-A.
           MOVE 5 TO WS-STEP.
           GENERATE RX-N.
           MOVE 6 TO WS-STEP.
           GENERATE RX-E.
           MOVE 7 TO WS-STEP.
           GENERATE RX-F.
           MOVE 8 TO WS-STEP.
           TERMINATE R-RX.
       >>TURN EC-SIZE CHECKING OFF
           MOVE 9 TO WS-STEP.
           TERMINATE R-SC.
           CLOSE PRT PRX.
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
