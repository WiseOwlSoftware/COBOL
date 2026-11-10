      *>--- conformance-case
      *> id: 1985-GR-VII_4_7_4-16
      *> rules: [1985-GR-VII.4.7.4-16]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.4.7.4 GR16 (THE WRITE STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the witness-drafting pass (a rule the
      *> audit never drafted) by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W85C5.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SYM-CR SYM-LF
               ARE 14 11.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT LN-FILE ASSIGN TO "w85c5-l.prt"
               ORGANIZATION IS SEQUENTIAL.
           SELECT PR-FILE ASSIGN TO "w85c5-p.prt"
               ORGANIZATION IS SEQUENTIAL.
           SELECT RD-FILE ASSIGN TO "w85c5-p.prt"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  LN-FILE LINAGE IS 10 LINES.
       01  LN-REC PIC X(3).
       FD  PR-FILE.
       01  PR-REC PIC X(3).
       FD  RD-FILE.
       01  RD-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-TWO PIC 9 VALUE 2.
       01  WS-ZERO PIC 9 VALUE 0.
       01  WS-ONE PIC 9 VALUE 1.
       01  WS-LC PIC 99.
       01  WS-EOF PIC X VALUE "N".
       01  WS-LINE PIC X(6) VALUE SPACES.
       01  WS-PTR PIC 99 VALUE 1.
       01  WS-LINE-NO PIC 99 VALUE 1.
       PROCEDURE DIVISION.
       P-LINAGE.
           OPEN OUTPUT LN-FILE.
           MOVE LINAGE-COUNTER TO WS-LC.
           DISPLAY "OPEN OUTPUT              LC " WS-LC.
           MOVE "AAA" TO LN-REC.
           WRITE LN-REC AFTER ADVANCING 3 LINES.
           MOVE LINAGE-COUNTER TO WS-LC.
           DISPLAY "AFTER ADVANCING 3 LINES  LC " WS-LC.
           MOVE "BBB" TO LN-REC.
           WRITE LN-REC BEFORE ADVANCING 2 LINES.
           MOVE LINAGE-COUNTER TO WS-LC.
           DISPLAY "BEFORE ADVANCING 2 LINES LC " WS-LC.
           MOVE "CCC" TO LN-REC.
           WRITE LN-REC AFTER ADVANCING 0 LINES.
           MOVE LINAGE-COUNTER TO WS-LC.
           DISPLAY "AFTER ADVANCING 0 LINES  LC " WS-LC.
           MOVE "DDD" TO LN-REC.
           WRITE LN-REC BEFORE ADVANCING WS-TWO LINES.
           MOVE LINAGE-COUNTER TO WS-LC.
           DISPLAY "BEFORE ADVANCING WS-TWO  LC " WS-LC.
           MOVE "EEE" TO LN-REC.
           WRITE LN-REC AFTER ADVANCING WS-ZERO LINES.
           MOVE LINAGE-COUNTER TO WS-LC.
           DISPLAY "AFTER ADVANCING WS-ZERO  LC " WS-LC.
           MOVE "FFF" TO LN-REC.
           WRITE LN-REC.
           MOVE LINAGE-COUNTER TO WS-LC.
           DISPLAY "NO ADVANCING PHRASE      LC " WS-LC.
           MOVE "GGG" TO LN-REC.
           WRITE LN-REC AFTER ADVANCING PAGE.
           MOVE LINAGE-COUNTER TO WS-LC.
           DISPLAY "AFTER ADVANCING PAGE     LC " WS-LC.
           MOVE "HHH" TO LN-REC.
           WRITE LN-REC BEFORE ADVANCING 4 LINES.
           MOVE LINAGE-COUNTER TO WS-LC.
           DISPLAY "BEFORE ADVANCING 4 LINES LC " WS-LC.
           MOVE "III" TO LN-REC.
           WRITE LN-REC BEFORE ADVANCING PAGE.
           MOVE LINAGE-COUNTER TO WS-LC.
           DISPLAY "BEFORE ADVANCING PAGE    LC " WS-LC.
           CLOSE LN-FILE.
       P-PRINT.
           OPEN OUTPUT PR-FILE.
           MOVE "AAA" TO PR-REC.
           WRITE PR-REC BEFORE ADVANCING 2 LINES.
           MOVE "BBB" TO PR-REC.
           WRITE PR-REC BEFORE ADVANCING WS-ONE LINE.
           MOVE "CCC" TO PR-REC.
           WRITE PR-REC AFTER ADVANCING 1 LINE.
           MOVE "DDD" TO PR-REC.
           WRITE PR-REC AFTER ADVANCING 3 LINES.
           MOVE "EEE" TO PR-REC.
           WRITE PR-REC.
           CLOSE PR-FILE.
       P-SHOW.
           OPEN INPUT RD-FILE.
           PERFORM P-SHOW-CHAR UNTIL WS-EOF = "Y".
           CLOSE RD-FILE.
           IF WS-PTR > 1
               PERFORM P-SHOW-LINE.
           STOP RUN.
       P-SHOW-CHAR.
           READ RD-FILE
               AT END MOVE "Y" TO WS-EOF
           END-READ.
           IF WS-EOF = "N"
               IF RD-REC = SYM-LF
                   PERFORM P-SHOW-LINE
               ELSE
                   IF RD-REC NOT = SYM-CR
                       STRING RD-REC DELIMITED BY SIZE INTO WS-LINE
                           WITH POINTER WS-PTR.
       P-SHOW-LINE.
           DISPLAY "PRINT LINE " WS-LINE-NO " [" WS-LINE "]".
           ADD 1 TO WS-LINE-NO.
           MOVE SPACES TO WS-LINE.
           MOVE 1 TO WS-PTR.
