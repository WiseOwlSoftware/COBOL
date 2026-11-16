      *>--- conformance-case
      *> id: g85_f31_01
      *> rules: [1985-SR-XIII.4.3.3-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.4.3.3 SR1 - GENERATE data-name-1 names a TYPE DETAIL report
      *> group, qualified by its report-name (OF and IN): the detail name DTL is
      *> declared in both reports R-A and R-B, so only the qualifier picks one.
      *> cite.py --check: OK §1985:XIII.4.3.3 SR1 "Data-name-1 must name a type
      *> detail report group and may be qualified by a report-name."
      *> §1985:XIII.4.3.4 GR2 (cite.py: OK): "the execution of a GENERATE
      *> data-name-1 statement causes the report writer control system to
      *> present the designated detail report group."
      *> DERIVATION. No PAGE clause, so each LINE PLUS 1 group is one line in
      *> execution order. GENERATE DTL OF R-A presents "A" (R-A's DTL), DTL IN
      *> R-B presents "B", DTL OF R-A again "A". The file is read back byte by
      *> byte (LF ends a line, CR dropped, empty lines not shown): [A] [B] [A].
      *> Each leg can fail: a refusal of the qualified data-name, or a binding
      *> of DTL to the wrong report, changes a line.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F3101.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SYM-X0A SYM-X0D ARE 11 14.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "G85F3101.TXT".
           SELECT CHK ASSIGN TO "G85F3101.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORTS ARE R-A R-B.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-I    PIC 99    VALUE 0.
       01  WS-LINE PIC X(30) VALUE SPACES.
       REPORT SECTION.
       RD  R-A.
       01  DTL TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           05  COLUMN NUMBER IS 1 PIC X VALUE "A".
       RD  R-B.
       01  DTL TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           05  COLUMN NUMBER IS 1 PIC X VALUE "B".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT PRT.
           INITIATE R-A.
           INITIATE R-B.
           GENERATE DTL OF R-A.
           GENERATE DTL IN R-B.
           GENERATE DTL OF R-A.
           TERMINATE R-A.
           TERMINATE R-B.
           CLOSE PRT.
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
