      *>--- conformance-case
      *> id: g2-value-report-suppress
      *> rules: [2002-GR-13.16.61.3-18]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.61.3 GR18 - a format 4 VALUE is the content of the
      *> printable item
      *> whenever it is printed, except that GROUP INDICATE or PRESENT
      *> WHEN may suppress
      *> it. Three GENERATEs of one detail group on one page with WS-K
      *> = 1, 2, 1:
      *> GGG has GROUP INDICATE, printed only on the first GENERATE
      *> after INITIATE
      *> (§2002:13.16.26.3 GR1); PPP has PRESENT WHEN WS-K = 1, absent
      *> when false
      *> (§2002:13.16.39.3 GR2). The report file is read back and each
      *> line shown
      *> between brackets: [GGG PPP 1], [        2], [    PPP 1]. Fails
      *> if a
      *> suppressed VALUE prints, or a VALUE that should print does
      *> not.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2VAL04.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "g2val04.txt".
           SELECT CHK ASSIGN TO "g2val04.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R-V.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-EOF PIC X VALUE "N".
       01  WS-K   PIC 9 VALUE 0.
       01  WS-I   PIC 99 VALUE 0.
       01  WS-LINE PIC X(30) VALUE SPACES.
       REPORT SECTION.
       RD  R-V PAGE LIMIT 20 LINES.
       01  DET-A TYPE DE.
           02  LINE PLUS 1.
               03  COLUMN 1 PIC X(3) VALUE "GGG" GROUP INDICATE.
               03  COLUMN 5 PIC X(3) VALUE "PPP" PRESENT WHEN WS-K = 1.
               03  COLUMN 9 PIC 9 SOURCE WS-K.
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT PRT.
           INITIATE R-V.
           MOVE 1 TO WS-K GENERATE DET-A.
           MOVE 2 TO WS-K GENERATE DET-A.
           MOVE 1 TO WS-K GENERATE DET-A.
           TERMINATE R-V.
           CLOSE PRT.
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
           IF CHK-REC = X"0A"
               PERFORM SHOW-LINE
           ELSE
               IF CHK-REC NOT = X"0D"
                   ADD 1 TO WS-I
                   MOVE CHK-REC TO WS-LINE (WS-I:1)
               END-IF
           END-IF.
       SHOW-LINE.
           IF WS-I > 0
               DISPLAY "[" WS-LINE (1:WS-I) "]"
               MOVE SPACES TO WS-LINE
               MOVE 0 TO WS-I
           END-IF.
