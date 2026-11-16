      *>--- conformance-case
      *> id: g85_bf15_05
      *> rules: [1985-GR-VI.6.22.4-4, 1985-GR-VI.6.22.4-5]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.22.4 GR4 - SEARCH ALL ignores the initial index
      *> setting; when all WHEN conditions can be satisfied "the index
      *> indicates an occurrence that allows the conditions to be
      *> satisfied, and control passes to imperative-statement-2, if
      *> specified, or to the next executable sentence if the NEXT
      *> SENTENCE phrase is specified"; when they cannot, control goes
      *> to the AT END imperative, or to the end of the SEARCH without
      *> AT END (the final index is not predictable, so it is never
      *> shown). §1985:VI.6.22.4 GR5 - after imperative-statement-1 or
      *> -2 not ending in GO TO, control passes to the end of the
      *> SEARCH; one ending in GO TO transfers there instead.
      *> Table keys (K1,K2) ascending: 11A 12B 21C 23D 35E.
      *> S1 IX preset 5; K1=2 AND K2=1 -> FOUND C, IX=3, then S1 END
      *> S2 condition-name K1-TWO AND K2 = W + 2 (W=1) -> FOUND D, IX=4
      *> S3 K1=2 AND K2=2 none -> AT END, then S3 END
      *> S4 K1=4 none, no AT END -> only S4 END
      *> S5 K1=1 AND K2=2 NEXT SENTENCE (inside IF) -> the MOVE "X" of
      *>    the same sentence is skipped: FLG=N, IX=2
      *> S6 K1=3 AND K2=5 -> FOUND E then GO TO S6-OUT: the DISPLAY
      *>    after END-SEARCH is not executed, S6 OUT is.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF1505.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  TBL-VALS PIC X(15) VALUE "11A12B21C23D35E".
       01  TBL REDEFINES TBL-VALS.
           05  ENT OCCURS 5 TIMES
                   ASCENDING KEY IS K1 K2
                   INDEXED BY IX.
               10  K1  PIC 9.
                   88  K1-TWO VALUE 2.
               10  K2  PIC 9.
               10  VL  PIC X.
       01  W    PIC 9 VALUE 1.
       01  N    PIC 9 VALUE 0.
       01  FLG  PIC X VALUE SPACE.
       PROCEDURE DIVISION.
       S1-PARA.
           SET IX TO 5.
           SEARCH ALL ENT
               AT END DISPLAY "S1 AT END"
               WHEN K1 (IX) = 2 AND K2 (IX) = 1
                   DISPLAY "S1 FOUND " VL (IX)
           END-SEARCH
           SET N TO IX
           DISPLAY "S1 IX=" N
           DISPLAY "S1 END".
       S2-PARA.
           SET IX TO 1.
           SEARCH ALL ENT
               AT END DISPLAY "S2 AT END"
               WHEN K1-TWO (IX) AND K2 (IX) = W + 2
                   DISPLAY "S2 FOUND " VL (IX)
           END-SEARCH.
           SET N TO IX.
           DISPLAY "S2 IX=" N.
       S3-PARA.
           SEARCH ALL ENT
               AT END DISPLAY "S3 AT END"
               WHEN K1 (IX) = 2 AND K2 (IX) = 2
                   DISPLAY "S3 FOUND " VL (IX)
           END-SEARCH
           DISPLAY "S3 END".
       S4-PARA.
           SEARCH ALL ENT
               WHEN K1 (IX) = 4
                   DISPLAY "S4 FOUND " VL (IX)
           END-SEARCH
           DISPLAY "S4 END".
       S5-PARA.
           MOVE "N" TO FLG.
           IF W = 1
               SEARCH ALL ENT
                   AT END MOVE "A" TO FLG
                   WHEN K1 (IX) = 1 AND K2 (IX) = 2
                       NEXT SENTENCE
           ELSE
               MOVE "E" TO FLG
           END-IF
           MOVE "X" TO FLG.
           SET N TO IX.
           DISPLAY "S5 FLG=" FLG " IX=" N.
       S6-PARA.
           SEARCH ALL ENT
               AT END DISPLAY "S6 AT END"
               WHEN K1 (IX) = 3 AND K2 (IX) = 5
                   DISPLAY "S6 FOUND " VL (IX)
                   GO TO S6-OUT
           END-SEARCH
           DISPLAY "S6 NOT REACHED".
       S6-OUT.
           DISPLAY "S6 OUT".
           STOP RUN.
