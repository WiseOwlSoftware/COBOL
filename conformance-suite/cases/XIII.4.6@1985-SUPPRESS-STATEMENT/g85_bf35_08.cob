      *>--- conformance-case
      *> id: g85_bf35_08
      *> rules: [1985-FMT-XIII.4.6.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.4.6.2 - the general format SUPPRESS PRINTING,
      *> written in a USE BEFORE REPORTING procedure (§1985:XIII.4.6.3
      *> SR1). The program must translate and run at 85. Behaviour
      *> shown: when WS-N is 2 the USE procedure for the detail
      *> executes SUPPRESS PRINTING, which inhibits only that
      *> presentation (§1985:XIII.4.6.4 GR1) and the adjustment of
      *> LINE-COUNTER (GR3 a, d; §1985:XIII.3.5.6 GR5). An unpaged
      *> report (§1985:XIII.3.8.4 GR3), detail LINE PLUS 1
      *> (§1985:XIII.3.10.8 GR4d: detail 1 on line 1 (LINE-COUNTER
      *> 1), detail 2 suppressed (LINE-COUNTER stays 1), detail 3 on
      *> line 2 (LINE-COUNTER 2). The file is read back as S P<page>
      *> L<line> <text>.
      *>
      *> Fails if SUPPRESS PRINTING is refused at 85, if detail 2 is
      *> presented, or if LINE-COUNTER advances for it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF3508.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SYM-LF SYM-CR SYM-FF
               ARE 11 14 13.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "g85bf3508.txt".
           SELECT CHKS ASSIGN TO "g85bf3508.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R.
       FD  CHKS.
       01  CHKS-REC      PIC X.
       WORKING-STORAGE SECTION.
       01  WS-EOF          PIC X     VALUE "N".
       01  WS-B            PIC X.
       01  WS-I            PIC 99    VALUE 0.
       01  WS-E            PIC 99    VALUE 0.
       01  WS-P            PIC 9     VALUE 1.
       01  WS-L            PIC 99    VALUE 1.
       01  WS-TAG          PIC X     VALUE SPACE.
       01  WS-LINE         PIC X(20) VALUE SPACES.
       01  WS-N            PIC 9     VALUE 0.
       01  WS-LC           PIC 99    VALUE 0.
       REPORT SECTION.
       RD  R.
       01  DTL TYPE DETAIL LINE PLUS 1.
           02  COLUMN 1 PIC X(3) VALUE "DE ".
           02  COLUMN 4 PIC 9 SOURCE WS-N.
       PROCEDURE DIVISION.
       DECLARATIVES.
       U-DTL SECTION.
           USE BEFORE REPORTING DTL.
       U-DTL-P.
           IF WS-N = 2
               SUPPRESS PRINTING
           END-IF.
       END DECLARATIVES.
       MAIN-S SECTION.
       MAIN-P.
           OPEN OUTPUT PRT.
           INITIATE R.
           PERFORM 3 TIMES
               ADD 1 TO WS-N
               GENERATE DTL
               MOVE LINE-COUNTER TO WS-LC
               DISPLAY "GENERATE " WS-N " LINE-COUNTER " WS-LC
           END-PERFORM.
           TERMINATE R.
           CLOSE PRT.
           PERFORM READ-S.
           STOP RUN.
       READ-S.
           MOVE "S" TO WS-TAG.
           PERFORM RESET-RB.
           OPEN INPUT CHKS.
           PERFORM UNTIL WS-EOF = "Y"
               READ CHKS
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END
                       MOVE CHKS-REC TO WS-B
                       PERFORM TAKE-BYTE
               END-READ
           END-PERFORM.
           CLOSE CHKS.
           PERFORM SHOW-LINE.
       RESET-RB.
           MOVE "N" TO WS-EOF.
           MOVE 1 TO WS-P.
           MOVE 1 TO WS-L.
           MOVE 0 TO WS-I.
           MOVE 0 TO WS-E.
           MOVE SPACES TO WS-LINE.
       TAKE-BYTE.
           IF WS-B = SYM-LF
               PERFORM SHOW-LINE
               ADD 1 TO WS-L
           ELSE
               IF WS-B = SYM-FF
                   PERFORM SHOW-LINE
                   ADD 1 TO WS-P
                   MOVE 1 TO WS-L
               ELSE
                   IF WS-B NOT = SYM-CR
                       ADD 1 TO WS-I
                       MOVE WS-B TO WS-LINE (WS-I:1)
                       IF WS-B NOT = SPACE
                           MOVE WS-I TO WS-E
                       END-IF
                   END-IF
               END-IF
           END-IF.
       SHOW-LINE.
           IF WS-LINE NOT = SPACES
               DISPLAY WS-TAG " P" WS-P " L" WS-L " "
                   WS-LINE (1:WS-E)
           END-IF.
           MOVE SPACES TO WS-LINE.
           MOVE 0 TO WS-I.
           MOVE 0 TO WS-E.
