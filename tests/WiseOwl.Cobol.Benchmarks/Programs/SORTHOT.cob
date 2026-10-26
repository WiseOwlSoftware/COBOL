      *> kb/Work PB2777 -- SORT through an INPUT and an OUTPUT PROCEDURE.
      *> RELEASEs N 80-byte records in scrambled key order (key =
      *> MOD(I * 7919, N) for I = 1..N, a permutation of 0..N-1 as 7919
      *> is prime to N = LOOP-COUNT, which is 2**a * 5**b at every
      *> scale), SORTs them on the ascending key, and RETURNs every one,
      *> counting each record whose key is not the next of 0..N-1.
      *> scripts/arch/perf_baseline.py runs it under two memory limits:
      *> the default, where every scale is sorted in memory, and one
      *> several times smaller than the records, where the sort writes
      *> sorted runs to temporary files and merges them.
      *> The same source runs under GnuCOBOL for the external comparison
      *> (scripts/arch/perf_baseline.py), so it is plain COBOL 85.
      *> WITNESS (computed, not observed), N = 100000: RETURN-COUNT = N,
      *> SUM-KEY = N(N-1)/2 = 4999950000, ERR-COUNT = 0 (every key
      *> returned in order).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SORTHOT.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SORT-FILE ASSIGN TO "sorthot.srt".
       DATA DIVISION.
       FILE SECTION.
       SD SORT-FILE.
       01 SORT-REC.
          05 SR-KEY    PIC 9(9).
          05 SR-SEQ    PIC 9(9).
          05 SR-NAME   PIC X(62).
       WORKING-STORAGE SECTION.
       01 LOOP-COUNT   PIC 9(9) COMP VALUE 100000.
       01 I            PIC 9(9) COMP VALUE 0.
       01 PROD         PIC 9(18) COMP VALUE 0.
       01 QUOT         PIC 9(18) COMP VALUE 0.
       01 NEXT-KEY     PIC 9(9) COMP VALUE 0.
       01 EOF-FLAG     PIC X VALUE "N".
       01 RETURN-COUNT PIC 9(18) COMP VALUE 0.
       01 SUM-KEY      PIC 9(18) COMP VALUE 0.
       01 ERR-COUNT    PIC 9(9) COMP VALUE 0.
       01 OUT-COUNT    PIC 9(9).
       01 OUT-SUM      PIC 9(18).
       01 OUT-ERR      PIC 9(9).
       PROCEDURE DIVISION.
       MAIN-PARA.
           SORT SORT-FILE ON ASCENDING KEY SR-KEY
               INPUT PROCEDURE IS RELEASE-PARA
               OUTPUT PROCEDURE IS RETURN-PARA
           MOVE RETURN-COUNT TO OUT-COUNT
           MOVE SUM-KEY TO OUT-SUM
           MOVE ERR-COUNT TO OUT-ERR
           DISPLAY "SORTHOT " OUT-COUNT " " OUT-SUM " " OUT-ERR
           STOP RUN.
       RELEASE-PARA.
           PERFORM VARYING I FROM 1 BY 1 UNTIL I > LOOP-COUNT
               MULTIPLY I BY 7919 GIVING PROD
               DIVIDE PROD BY LOOP-COUNT GIVING QUOT REMAINDER SR-KEY
               MOVE I TO SR-SEQ
               MOVE "SORT RECORD" TO SR-NAME
               RELEASE SORT-REC
           END-PERFORM.
       RETURN-PARA.
           PERFORM UNTIL EOF-FLAG = "Y"
               RETURN SORT-FILE
                   AT END
                       MOVE "Y" TO EOF-FLAG
                   NOT AT END
                       ADD 1 TO RETURN-COUNT
                       ADD SR-KEY TO SUM-KEY
                       IF SR-KEY NOT = NEXT-KEY
                           ADD 1 TO ERR-COUNT
                       END-IF
                       ADD 1 TO NEXT-KEY
               END-RETURN
           END-PERFORM.
