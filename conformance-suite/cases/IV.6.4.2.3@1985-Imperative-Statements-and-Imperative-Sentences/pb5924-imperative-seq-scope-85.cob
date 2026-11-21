      *>--- conformance-case
      *> id: pb5924-imperative-seq-scope-85
      *> rules: [1985-SR-IV.6.4.2.3.1-p3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:IV.6.4.2.3.1 (cite.py: OK): "sequence of consecutive imperative statements that must be ended by a period or by any phrase"
      *> The imperative-statement of ON SIZE ERROR / NOT ON SIZE ERROR is every consecutive imperative
      *> statement up to the next phrase of the ADD or the period.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. IMPSEQ.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  X  PIC 9 VALUE 9.
       01  Y  PIC 9 VALUE 1.
       PROCEDURE DIVISION.
       P1.
           ADD 1 TO X ON SIZE ERROR DISPLAY "E1" DISPLAY "E2"
           NOT ON SIZE ERROR DISPLAY "N1" DISPLAY "N2".
           DISPLAY "AFTER1".
           ADD 1 TO Y ON SIZE ERROR DISPLAY "E3" DISPLAY "E4"
           NOT ON SIZE ERROR DISPLAY "N3" DISPLAY "N4".
           DISPLAY "AFTER2".
           ADD 1 TO X ON SIZE ERROR DISPLAY "E5" MOVE 7 TO Y
           DISPLAY "E6".
           DISPLAY "AFTER3 " X " " Y.
           STOP RUN.
