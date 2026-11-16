      *>--- conformance-case
      *> id: g85_f26_01
      *> rules: [1985-SR-X.3.1.3-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.3.1.3 SR3 - "The optional COMMON clause may be used
      *>   only if the program is contained within another program."
      *>   cite.py: OK  §1985:X.3.1.3 SR3
      *> The permitted form: G85F2601A is CONTAINED in G85F2601 and
      *> says IS COMMON PROGRAM. A compiler that rejects COMMON on a
      *> contained program fails here (rejects legal source).
      *> Supporting rules for the output (not witnessed rows):
      *>   §1985:X.3.1.4 GR2 "A common program is contained within
      *>   another program but may be called from programs other than
      *>   that containing it" (cite.py: OK) - so the sibling
      *>   G85F2601B may CALL G85F2601A;
      *>   §1985:X.5.4.4 GR2 "The program state of the called program
      *>   is not altered" (cite.py: OK) - A is not INITIAL, so its
      *>   counter keeps 1 between calls and shows 2 the second time.
      *> Derivation, by line: 1 OUTER CALLS A; 2 A RUNS 1 (first
      *> call, VALUE 0 + 1); 3 B CALLS A; 4 A RUNS 2 (common program
      *> reached from its sibling, state kept); 5 B BACK; 6 OUTER END.
      *> A compiler that ignores COMMON cannot resolve B's CALL.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F2601.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "OUTER CALLS A".
           CALL "G85F2601A".
           CALL "G85F2601B".
           DISPLAY "OUTER END".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F2601A IS COMMON PROGRAM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A-CNT PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       A-PARA.
           ADD 1 TO A-CNT.
           DISPLAY "A RUNS " A-CNT.
           EXIT PROGRAM.
       END PROGRAM G85F2601A.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F2601B.
       PROCEDURE DIVISION.
       B-PARA.
           DISPLAY "B CALLS A".
           CALL "G85F2601A".
           DISPLAY "B BACK".
           EXIT PROGRAM.
       END PROGRAM G85F2601B.
       END PROGRAM G85F2601.
