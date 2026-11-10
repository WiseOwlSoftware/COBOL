      *>--- conformance-case
      *> id: 1985-GR-IV_4_2_2_2_3-6
      *> rules: [1985-GR-IV.4.2.2.2.3-6]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.2.2.2.3 GR6 (Literals). An output test: the program
      *> must translate and run at 85 and DISPLAY exactly the lines its
      *> .out holds, the outcome the rule requires. Placed from the
      *> audit's draft, confirmed by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. ALLLIT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  X PIC X(5).
       01  Y PIC X(4).
       PROCEDURE DIVISION.
       P1.
           MOVE ALL "AB" TO X.
           DISPLAY X.
           MOVE ALL "XYZ" TO Y.
           DISPLAY Y.
           STOP RUN.
