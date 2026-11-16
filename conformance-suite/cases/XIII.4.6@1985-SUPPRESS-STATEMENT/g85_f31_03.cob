      *>--- conformance-case
      *> id: g85_f31_03
      *> rules: [1985-SR-XIII.4.6.3-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.4.6.3 SR1 - SUPPRESS PRINTING written where the rule grants
      *> it: in a USE BEFORE REPORTING procedure (a declarative section).
      *> cite.py --check: OK §1985:XIII.4.6.3 SR1 "The SUPPRESS statement may
      *> only appear in a USE BEFORE REPORTING procedure."
      *> §1985:XIII.4.9.4 GR4 (cite.py: OK): the designated procedures are
      *> executed by the RWCS just before the named report group is produced;
      *> §1985:XIII.4.6.4 GR2 (cite.py: OK): SUPPRESS must be executed each time
      *> presentation is to be inhibited; GR3a (cite.py: OK): it inhibits "The
      *> presentation of the print lines of the report group."
      *> DERIVATION. WS-N 1, 2, 3, 4; the USE procedure counts its calls and
      *> suppresses WS-N 2 and 4. Calls 4; lines presented [D 1] [D 3]. Each
      *> leg can fail: a refusal of the legal SUPPRESS, a SUPPRESS that is not
      *> honoured (a [D 2] or [D 4] line), one that sticks past its own
      *> GENERATE (no [D 3]), or a USE procedure not run before each group.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F3103.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SYM-X0A SYM-X0D ARE 11 14.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "G85F3103.TXT".
           SELECT CHK ASSIGN TO "G85F3103.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS RPT.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-N    PIC 9     VALUE 0.
       01  WS-CALLS PIC 9    VALUE 0.
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-I    PIC 99    VALUE 0.
       01  WS-LINE PIC X(30) VALUE SPACES.
       REPORT SECTION.
       RD  RPT.
       01  DTL TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           05  COLUMN NUMBER IS 1 PIC X VALUE "D".
           05  COLUMN NUMBER IS 3 PIC 9 SOURCE WS-N.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D1 SECTION.
           USE BEFORE REPORTING DTL.
       D1-P.
           ADD 1 TO WS-CALLS.
           IF WS-N = 2 OR WS-N = 4
               SUPPRESS PRINTING.
       END DECLARATIVES.
       M SECTION.
       MAIN-PARA.
           OPEN OUTPUT PRT.
           INITIATE RPT.
           MOVE 1 TO WS-N.
           GENERATE DTL.
           MOVE 2 TO WS-N.
           GENERATE DTL.
           MOVE 3 TO WS-N.
           GENERATE DTL.
           MOVE 4 TO WS-N.
           GENERATE DTL.
           TERMINATE RPT.
           CLOSE PRT.
           DISPLAY "CALLS " WS-CALLS.
           PERFORM READ-BACK.
           STOP RUN.
       READ-BACK.
           MOVE "N" TO WS-EOF.
           OPEN INPUT CHK.
           PERFORM UNTIL WS-EOF = "Y"
               READ CHK
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END PERFORM TAKE-BYTE
               END-READ
           END-PERFORM.
           CLOSE CHK.
           PERFORM SHOW-LINE.
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
