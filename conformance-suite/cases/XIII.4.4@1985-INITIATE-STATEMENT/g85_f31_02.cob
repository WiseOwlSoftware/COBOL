      *>--- conformance-case
      *> id: g85_f31_02
      *> rules: [1985-SR-XIII.4.4.3-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.4.4.3 SR1 - INITIATE names report-names each defined by a
      *> report description entry (RD) in the Report Section; two in one
      *> statement. cite.py --check: OK §1985:XIII.4.4.3 SR1 "Report-name-1 must
      *> be defined by a report description entry in the Report Section of the
      *> Data Division."
      *> §1985:XIII.4.4.4 GR1 (cite.py: OK): "LINE-COUNTER is set to zero." and
      *> PAGE-COUNTER is set to one; GR4 (cite.py: OK): the result is the same
      *> as if a separate INITIATE statement had been written for each
      *> report-name.
      *> DERIVATION. Both counters are moved to PIC 9 items (their own size is
      *> the implementor's) after the one INITIATE R-A R-B: "R-A 0 1" and
      *> "R-B 0 1". Then GENERATE R-A's and R-B's details and read the file
      *> back: [A] [B]. Each leg can fail: a refusal of INITIATE R-A R-B, a
      *> report the statement did not initiate (its counters or its GENERATE
      *> fail), or a wrong report bound to a name.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F3102.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SYM-X0A SYM-X0D ARE 11 14.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "G85F3102.TXT".
           SELECT CHK ASSIGN TO "G85F3102.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORTS ARE R-A R-B.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-LC   PIC 9.
       01  WS-PC   PIC 9.
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-I    PIC 99    VALUE 0.
       01  WS-LINE PIC X(30) VALUE SPACES.
       REPORT SECTION.
       RD  R-A.
       01  DTL-A TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           05  COLUMN NUMBER IS 1 PIC X VALUE "A".
       RD  R-B.
       01  DTL-B TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           05  COLUMN NUMBER IS 1 PIC X VALUE "B".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT PRT.
           INITIATE R-A R-B.
           MOVE LINE-COUNTER OF R-A TO WS-LC.
           MOVE PAGE-COUNTER OF R-A TO WS-PC.
           DISPLAY "R-A " WS-LC " " WS-PC.
           MOVE LINE-COUNTER OF R-B TO WS-LC.
           MOVE PAGE-COUNTER OF R-B TO WS-PC.
           DISPLAY "R-B " WS-LC " " WS-PC.
           GENERATE DTL-A.
           GENERATE DTL-B.
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
