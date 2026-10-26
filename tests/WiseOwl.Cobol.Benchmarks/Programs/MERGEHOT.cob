      *> kb/Work PB2777 -- MERGE of four sequential files through an
      *> OUTPUT PROCEDURE. Writes the keys 0..N-1 round-robin to four
      *> ORGANIZATION SEQUENTIAL files of 80-byte records (key K goes to
      *> file MOD(K, 4) + 1, so each file is in ascending key order),
      *> MERGEs them on the ascending key, and RETURNs every record,
      *> counting each one whose key is not the next of 0..N-1.
      *> scripts/arch/perf_baseline.py runs it under two memory limits:
      *> the default, where every scale is merged in memory, and one
      *> several times smaller than the records, where the merge writes
      *> its records to temporary run files and merges those.
      *> The same source runs under GnuCOBOL for the external comparison
      *> (scripts/arch/perf_baseline.py), so it is plain COBOL 85.
      *> WITNESS (computed, not observed), N = 100000: RETURN-COUNT = N,
      *> SUM-KEY = N(N-1)/2 = 4999950000, ERR-COUNT = 0 (no write
      *> failed and every key was returned in order).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MERGEHOT.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IN-1 ASSIGN TO "mergehot1.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS FS.
           SELECT IN-2 ASSIGN TO "mergehot2.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS FS.
           SELECT IN-3 ASSIGN TO "mergehot3.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS FS.
           SELECT IN-4 ASSIGN TO "mergehot4.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS FS.
           SELECT MERGE-FILE ASSIGN TO "mergehot.srt".
       DATA DIVISION.
       FILE SECTION.
       FD IN-1.
       01 IN-1-REC     PIC X(80).
       FD IN-2.
       01 IN-2-REC     PIC X(80).
       FD IN-3.
       01 IN-3-REC     PIC X(80).
       FD IN-4.
       01 IN-4-REC     PIC X(80).
       SD MERGE-FILE.
       01 MERGE-REC.
          05 MR-KEY    PIC 9(9).
          05 MR-NAME   PIC X(71).
       WORKING-STORAGE SECTION.
       01 LOOP-COUNT   PIC 9(9) COMP VALUE 100000.
       01 K            PIC 9(9) COMP VALUE 0.
       01 QUOT         PIC 9(9) COMP VALUE 0.
       01 WHICH        PIC 9 COMP VALUE 0.
       01 WORK-REC.
          05 WR-KEY    PIC 9(9).
          05 WR-NAME   PIC X(71).
       01 FS           PIC XX.
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
           OPEN OUTPUT IN-1 IN-2 IN-3 IN-4
           MOVE "MERGE RECORD" TO WR-NAME
           PERFORM VARYING K FROM 0 BY 1 UNTIL K NOT < LOOP-COUNT
               MOVE K TO WR-KEY
               DIVIDE K BY 4 GIVING QUOT REMAINDER WHICH
               EVALUATE WHICH
                   WHEN 0 WRITE IN-1-REC FROM WORK-REC
                   WHEN 1 WRITE IN-2-REC FROM WORK-REC
                   WHEN 2 WRITE IN-3-REC FROM WORK-REC
                   WHEN 3 WRITE IN-4-REC FROM WORK-REC
               END-EVALUATE
               IF FS NOT = "00"
                   ADD 1 TO ERR-COUNT
               END-IF
           END-PERFORM
           CLOSE IN-1 IN-2 IN-3 IN-4
           MERGE MERGE-FILE ON ASCENDING KEY MR-KEY
               USING IN-1 IN-2 IN-3 IN-4
               OUTPUT PROCEDURE IS RETURN-PARA
           MOVE RETURN-COUNT TO OUT-COUNT
           MOVE SUM-KEY TO OUT-SUM
           MOVE ERR-COUNT TO OUT-ERR
           DISPLAY "MERGEHOT " OUT-COUNT " " OUT-SUM " " OUT-ERR
           STOP RUN.
       RETURN-PARA.
           PERFORM UNTIL EOF-FLAG = "Y"
               RETURN MERGE-FILE
                   AT END
                       MOVE "Y" TO EOF-FLAG
                   NOT AT END
                       ADD 1 TO RETURN-COUNT
                       ADD MR-KEY TO SUM-KEY
                       IF MR-KEY NOT = NEXT-KEY
                           ADD 1 TO ERR-COUNT
                       END-IF
                       ADD 1 TO NEXT-KEY
               END-RETURN
           END-PERFORM.
