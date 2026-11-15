      *>--- conformance-case
      *> id: pb5573_control_name_ref_mod_2002
      *> rules: [SR-13.18.16.3-4, SR-13.18.57.3-10]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5573. A reference-modified control name is a 2002 introduction.
      *> "Data-name-1 may be reference modified"
      *>   cite.py: OK  §2023:13.18.16.3 SR4 (ISO/IEC 1989:2002 13.16.15.2 SR4)
      *> "Data-name-1 and data-name-2 may be qualified and reference modified"
      *>   cite.py: OK  §2002:13.16.55.2 SR10 (§2023:13.18.57.3 SR10)
      *> so CONTROL IS WK (1:1), TYPE CH WK (1:1) and TYPE CF WK (1:1) compile
      *> from 2002 (at 85 each is refused COBOL0900: the refusal tests
      *> pb5573-control-clause-ref-mod-85 and pb5573-type-control-ref-mod-85).
      *> DERIVATION. The control data item is WK (1:1), the first character
      *> of WK only.
      *> G1 (WK = "AB"): the first GENERATE presents the control heading,
      *>   "CH=A", then the detail "AB".
      *> G2 (WK = "AC"): WK (1:1) is still "A": no control break, the detail
      *>   "AC" alone. Fails here if the whole of WK is taken as the control.
      *> G3 (WK = "BC"): a control break. The control footing presents with
      *>   the prior control value, "CF=A", then the control heading "CH=B"
      *>   and the detail "BC".
      *> TERMINATE: the control footing "CF=B".
      *> Each non-blank line of the report is displayed in brackets.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5573RM.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "pb5573rm.txt".
           SELECT CHK ASSIGN TO "pb5573rm.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R-1.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WK      PIC X(2) VALUE "AB".
       01  WS-EOF  PIC X    VALUE "N".
       01  WS-I    PIC 99   VALUE 0.
       01  WS-LINE PIC X(8) VALUE SPACES.
       REPORT SECTION.
       RD  R-1 CONTROL IS WK (1:1).
       01  CH-1 TYPE CH WK (1:1) LINE PLUS 1.
           03  COLUMN 1 PIC X(3) VALUE "CH=".
           03  COLUMN 4 PIC X SOURCE WK (1:1).
       01  DE-1 TYPE DE LINE PLUS 1.
           03  COLUMN 1 PIC X(2) SOURCE WK.
       01  CF-1 TYPE CF WK (1:1) LINE PLUS 1.
           03  COLUMN 1 PIC X(3) VALUE "CF=".
           03  COLUMN 4 PIC X SOURCE WK (1:1).
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT PRT.
           INITIATE R-1.
           GENERATE DE-1.
           MOVE "AC" TO WK.
           GENERATE DE-1.
           MOVE "BC" TO WK.
           GENERATE DE-1.
           TERMINATE R-1.
           CLOSE PRT.
           OPEN INPUT CHK.
           PERFORM UNTIL WS-EOF = "Y"
               READ CHK
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END PERFORM TAKE-BYTE
               END-READ
           END-PERFORM.
           CLOSE CHK.
           IF WS-I > 0
               PERFORM SHOW-LINE
           END-IF.
           STOP RUN.
       TAKE-BYTE.
           IF CHK-REC = X"0A"
               PERFORM SHOW-LINE
           ELSE
               IF CHK-REC NOT = X"0D" AND CHK-REC NOT = X"0C"
                   ADD 1 TO WS-I
                   MOVE CHK-REC TO WS-LINE (WS-I:1)
               END-IF
           END-IF.
       SHOW-LINE.
           IF WS-LINE NOT = SPACES
               DISPLAY "[" WS-LINE (1:4) "]"
           END-IF.
           MOVE SPACES TO WS-LINE.
           MOVE 0 TO WS-I.
