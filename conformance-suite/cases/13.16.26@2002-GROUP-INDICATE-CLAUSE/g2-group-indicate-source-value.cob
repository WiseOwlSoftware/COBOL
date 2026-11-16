      *>--- conformance-case
      *> id: g2-group-indicate-source-value
      *> rules: [2002-SR-13.16.26.2-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.26.2 SR1 - GROUP INDICATE "may be specified only
      *> within a detail report group description, in an elementary
      *> entry that also contains a COLUMN clause and a SOURCE or
      *> VALUE clause". Both granted forms are written in the detail
      *> DET-G: GI-S (COLUMN + SOURCE) and GI-V (COLUMN + VALUE); the
      *> program must be ACCEPTED, and each item then behaves as
      *> §2002:13.16.26.3 GR1 says: present only on the first
      *> GENERATE for the detail after an INITIATE (no CONTROL
      *> clause; PAGE LIMIT 20 is never reached, so no page advance).
      *>   INITIATE, GENERATE x3 (S = AAA BBB CCC, K = 1 2 3):
      *>     [AAA VVV 1]  [        2]  [        3]
      *>   TERMINATE, INITIATE again, GENERATE (S = DDD, K = 4):
      *>     [DDD VVV 4]  - the INITIATE re-arms both items.
      *> The report file is read back; non-empty lines are shown
      *> between brackets. Fails if either granted form is refused,
      *> or if a group-indicated item prints on a later GENERATE.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2GRI01.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "g2gri01.txt".
           SELECT CHK ASSIGN TO "g2gri01.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R-G.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-EOF  PIC X VALUE "N".
       01  WS-S    PIC X(3) VALUE SPACES.
       01  WS-K    PIC 9 VALUE 0.
       01  WS-I    PIC 99 VALUE 0.
       01  WS-LINE PIC X(30) VALUE SPACES.
       REPORT SECTION.
       RD  R-G PAGE LIMIT 20 LINES.
       01  DET-G TYPE DE.
           02  LINE PLUS 1.
               03  GI-S COLUMN 1 PIC X(3) SOURCE WS-S GROUP INDICATE.
               03  GI-V COLUMN 5 PIC X(3) VALUE "VVV" GROUP INDICATE.
               03  COLUMN 9 PIC 9 SOURCE WS-K.
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT PRT
           INITIATE R-G
           MOVE "AAA" TO WS-S MOVE 1 TO WS-K GENERATE DET-G
           MOVE "BBB" TO WS-S MOVE 2 TO WS-K GENERATE DET-G
           MOVE "CCC" TO WS-S MOVE 3 TO WS-K GENERATE DET-G
           TERMINATE R-G
           INITIATE R-G
           MOVE "DDD" TO WS-S MOVE 4 TO WS-K GENERATE DET-G
           TERMINATE R-G
           CLOSE PRT
           OPEN INPUT CHK
           PERFORM UNTIL WS-EOF = "Y"
               READ CHK
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END PERFORM TAKE-BYTE
               END-READ
           END-PERFORM
           CLOSE CHK
           PERFORM SHOW-LINE
           STOP RUN.
       TAKE-BYTE.
           IF CHK-REC = X"0A" OR CHK-REC = X"0C"
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
