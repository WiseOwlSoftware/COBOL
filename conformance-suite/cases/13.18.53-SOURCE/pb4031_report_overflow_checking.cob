      *>--- conformance-case
      *> id: pb4031_report_overflow_checking
      *> rules: [GR-14.7.5-3, GR-7.3.25.4-6, GR-13.18.53.4-2, GR-13.18.41.4-2, GR-13.18.64.4-3]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> PB4031 (review finding R1-1) - the report compose's arithmetic
      *> under EC-SIZE-OVERFLOW checking alone, enabled at the GENERATE
      *> that presents the group.
      *>
      *> ISO 14.7.5 no-phrase rule 3: "If an arithmetic operation on an
      *> intermediate data item of the form corresponding to the mode of
      *> arithmetic currently in effect would cause the new nonzero
      *> value to be farther from zero or nearer to zero than is allowed
      *> for the intermediate data item, the applicable EC-SIZE
      *> exception condition, either EC-SIZE-OVERFLOW or
      *> EC-SIZE-UNDERFLOW, is set to exist". 14.7.5 names GENERATE
      *> among the statements the size error condition "may occur as a
      *> result of", and 7.3.25.4 GR6 enables checking "for the procedure
      *> division statements and procedure division headers that follow
      *> in the compilation group", so the TURN in force at the GENERATE
      *> decides for every arithmetic expression the presentation
      *> evaluates: a SOURCE expression (13.18.53.4 GR2's implicit
      *> COMPUTE), a PRESENT WHEN condition (13.18.41.4 GR2, evaluated
      *> "before the processing of any LINE clauses for the report
      *> group") and a VARYING BY expression (13.18.64.4 GR3 b), "the
      *> value of arithmetic-expression-2 is added to data-name-1").
      *> The leg fails if the compose ignores the presenting statement's
      *> EC-SIZE-OVERFLOW (the branch before the fix: it honored
      *> EC-SIZE-TRUNCATION only, and no RAISED line appeared), if it
      *> raises while that statement's checking is off, or if it raises
      *> EC-SIZE-TRUNCATION, whose checking the line does not enable.
      *>
      *> EVERY EXPECTED VALUE IS COMPUTED FROM THE RULE. WS-D = 1.0E200,
      *> so WS-D x WS-D is past the binary64 intermediate.
      *>   STEP 1/2 checking off: GENERATE OV-F and OV-P raise nothing
      *>          (14.6.13.1.1: a condition whose checking is not
      *>          enabled is not raised)
      *>   STEP 3 EC-SIZE-OVERFLOW on, GENERATE OK-T: 100 x 100 is an
      *>          intermediate the arithmetic forms; its truncation into
      *>          9(3) is EC-SIZE-TRUNCATION, not enabled, so no raise
      *>          and the low-order digits print (CONFORMANCE.md
      *>          DOC-A.1-70): 000
      *>   STEP 4 GENERATE OV-F, SOURCE IS WS-D * WS-D: EC-SIZE-OVERFLOW
      *>   STEP 5 GENERATE OV-P, PRESENT WHEN WS-D * WS-D > 0:
      *>          EC-SIZE-OVERFLOW
      *>   STEP 6 GENERATE OV-V, the second occurrence adds
      *>          BY WS-D * WS-D: EC-SIZE-OVERFLOW
      *>   STEP 7 TERMINATE both reports, checking off: no footing
      *> Each raise reaches the USE AFTER EXCEPTION CONDITION EC-SIZE
      *> declarative, which RESUMEs at the next statement. R-OK's file
      *> is read back (R-OV's is not: the raises interrupt it).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4031RO.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SYM-X0A SYM-X0D
               ARE 11 14.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRK ASSIGN TO "pb4031ok.txt".
           SELECT PRV ASSIGN TO "pb4031ov.txt".
           SELECT CHK ASSIGN TO "pb4031ok.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRK REPORT IS R-OK.
       FD  PRV REPORT IS R-OV.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-EOF    PIC X     VALUE "N".
       01  WS-A      PIC 999   VALUE 100.
       01  WS-B      PIC 999   VALUE 100.
       01  WS-K      PIC 9     VALUE 1.
       01  WS-D      COMP-2    VALUE 1.0E200.
       01  WS-STEP   PIC 9     VALUE 0.
       01  WS-RAISED PIC 9     VALUE 0.
       01  WS-I      PIC 99    VALUE 0.
       01  WS-LINE   PIC X(40) VALUE SPACES.
       REPORT SECTION.
       RD  R-OK PAGE LIMIT 20 LINES.
       01  OK-T TYPE DE LINE PLUS 1.
           03  COLUMN 1  PIC 9(3) SOURCE IS WS-A * WS-B.
       RD  R-OV PAGE LIMIT 20 LINES.
       01  OV-F TYPE DE LINE PLUS 1.
           03  COLUMN 1  PIC 9(3) SOURCE IS WS-D * WS-D.
       01  OV-P TYPE DE LINE PLUS 1.
           03  COLUMN 1  PIC X(2) VALUE "PW"
                         PRESENT WHEN WS-D * WS-D > 0.
       01  OV-V TYPE DE LINE PLUS 1.
           03  COLUMN 1 5 PIC 9(3) SOURCE IS VC
                         VARYING VC FROM WS-K BY WS-D * WS-D.
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
           OPEN OUTPUT PRK PRV.
           INITIATE R-OK R-OV.
           MOVE 1 TO WS-STEP.
           GENERATE OV-F.
           MOVE 2 TO WS-STEP.
           GENERATE OV-P.
       >>TURN EC-SIZE-OVERFLOW CHECKING ON
           MOVE 3 TO WS-STEP.
           GENERATE OK-T.
           MOVE 4 TO WS-STEP.
           GENERATE OV-F.
           MOVE 5 TO WS-STEP.
           GENERATE OV-P.
           MOVE 6 TO WS-STEP.
           GENERATE OV-V.
       >>TURN EC-SIZE-OVERFLOW CHECKING OFF
           MOVE 7 TO WS-STEP.
           TERMINATE R-OK R-OV.
           CLOSE PRK PRV.
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
