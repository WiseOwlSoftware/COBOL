      *>--- conformance-case
      *> id: pb4031_report_source_prohibited_checked
      *> rules: [GR-13.18.53.4-2, GR-14.7.4.3-7, GR-7.3.25.4-6]
      *> accept: [2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> PB4031 - ROUNDED MODE IS PROHIBITED in a report SOURCE clause
      *> under the EC-SIZE-TRUNCATION checking of the statement that
      *> PRINTS the item.
      *>
      *> ISO 13.18.53.4 GR2: the SOURCE expression is "the operand of an
      *> implicit COMPUTE statement that is executed implicitly whenever
      *> the associated item is printed. If the ROUNDED phrase is
      *> specified, the implicit COMPUTE statement has the corresponding
      *> ROUNDED phrase." 14.7.4.3 rule 7: "If the PROHIBITED phrase is
      *> specified, and the arithmetic value cannot be represented
      *> exactly in the resultant identifier, the EC-SIZE-TRUNCATION
      *> exception condition is set to exist, the size error condition
      *> exists, and the content of the resultant identifier is
      *> unchanged". 7.3.25.4 GR6 scopes checking to "the procedure
      *> division statements ... that follow", so the GENERATE's own
      *> TURN state decides. With checking off the item keeps its
      *> unchanged content, spaces, and the run goes on (CONFORMANCE.md
      *> DOC-A.1-70, golden pb3990_report_source_prohibited_keeps_the_
      *> item); with checking on the condition is processed as a fatal
      *> one (14.6.13.1.3) and reaches the USE declarative. The leg
      *> fails if the keep swallows the condition under checking (main
      *> before the fix), if the checking-off GENERATE raises, or if an
      *> exact value raises.
      *>
      *> EVERY EXPECTED VALUE IS COMPUTED FROM THE RULE. Item 9.9.
      *>   STEP 1 checking off, WS-F = 1.55: inexact, kept: spaces
      *>   STEP 2 checking on,  WS-F = 2.50: exact: 2.5
      *>   STEP 3 checking on,  WS-F = 1.55: inexact: raises (in a
      *>          second report, R-PX, whose file is not read back: the
      *>          raise interrupts its presentation)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4031RP.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SYM-X0A SYM-X0D
               ARE 11 14.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "pb4031rp.txt".
           SELECT PRX ASSIGN TO "pb4031rq.txt".
           SELECT CHK ASSIGN TO "pb4031rp.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R-PC.
       FD  PRX REPORT IS R-PX.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-EOF    PIC X     VALUE "N".
       01  WS-F      PIC 9V99  VALUE 1.55.
       01  WS-STEP   PIC 9     VALUE 0.
       01  WS-I      PIC 99    VALUE 0.
       01  WS-LINE   PIC X(40) VALUE SPACES.
       REPORT SECTION.
       RD  R-PC PAGE LIMIT 20 LINES.
       01  DET TYPE DE LINE PLUS 1.
           03  COLUMN 1  PIC X    VALUE "<".
           03  COLUMN 2  PIC 9.9  SOURCE IS WS-F
                   ROUNDED MODE IS PROHIBITED.
           03  COLUMN 5  PIC X    VALUE ">".
       RD  R-PX PAGE LIMIT 20 LINES.
       01  DET-X TYPE DE LINE PLUS 1.
           03  COLUMN 1  PIC 9.9  SOURCE IS WS-F
                   ROUNDED MODE IS PROHIBITED.
       PROCEDURE DIVISION.
       DECLARATIVES.
       SZ SECTION.
           USE AFTER EXCEPTION CONDITION EC-SIZE-TRUNCATION.
       SZ-1.
           DISPLAY "RAISED " FUNCTION EXCEPTION-STATUS
               " AT STEP " WS-STEP.
           RESUME NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       M-1.
           OPEN OUTPUT PRT PRX.
           INITIATE R-PC R-PX.
           MOVE 1 TO WS-STEP.
           GENERATE DET.
       >>TURN EC-SIZE-TRUNCATION CHECKING ON
           MOVE 2.5 TO WS-F.
           MOVE 2 TO WS-STEP.
           GENERATE DET.
           MOVE 1.55 TO WS-F.
           MOVE 3 TO WS-STEP.
           GENERATE DET-X.
       >>TURN EC-SIZE-TRUNCATION CHECKING OFF
           TERMINATE R-PC R-PX.
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
