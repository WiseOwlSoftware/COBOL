      *>--- conformance-case
      *> id: 1985-GR-III_2-p82
      *> rules: [1985-GR-III.2-p82]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:III.2 p82 ((section preamble)). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the PB5453 reader of
      *> lineage-reads-1985/g-02.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DYNACC.
      * III.2 p82: dynamic access - records obtained nonsequentially and
      * sequentially, and placed, in the scope of the same OPEN.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT DFL ASSIGN TO "G02DYN.DAT"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS RK.
       DATA DIVISION.
       FILE SECTION.
       FD  DFL.
       01  RREC            PIC X(3).
       WORKING-STORAGE SECTION.
       01  RK              PIC 9(4).
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT DFL.
           MOVE 1 TO RK. MOVE "AAA" TO RREC.
           WRITE RREC INVALID KEY DISPLAY "W".
           MOVE 2 TO RK. MOVE "BBB" TO RREC.
           WRITE RREC INVALID KEY DISPLAY "W".
           MOVE 3 TO RK. MOVE "CCC" TO RREC.
           WRITE RREC INVALID KEY DISPLAY "W".
           CLOSE DFL.
           OPEN I-O DFL.
           MOVE 2 TO RK.
           READ DFL INVALID KEY DISPLAY "BAD".
           DISPLAY "RND=" RREC.
           READ DFL NEXT RECORD AT END DISPLAY "END".
           DISPLAY "SEQ=" RREC " RK=" RK.
           MOVE 1 TO RK. MOVE "ZZZ" TO RREC.
           REWRITE RREC INVALID KEY DISPLAY "BAD2".
           MOVE 1 TO RK.
           READ DFL INVALID KEY DISPLAY "BAD3".
           DISPLAY "RND2=" RREC.
           READ DFL NEXT RECORD AT END DISPLAY "END2".
           DISPLAY "SEQ2=" RREC " RK=" RK.
           CLOSE DFL.
           STOP RUN.
