      *>--- conformance-case
      *> id: pb5924-seg-classification-85
      *> rules: [1985-SR-XVI.1.4.1-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:XVI.1.4.1 (cite.py: OK): "each section must be classified as belonging either to the fixed portion or to one of the independent segments"
      *> An unnumbered section is fixed (its ALTER persists across PERFORMs); a section 50 is
      *> independent (each explicit entry from segment 0 returns its GO TO to its initial state).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SEGCLS.
       PROCEDURE DIVISION.
       MAIN SECTION.
       M1.
           PERFORM FIX.
           PERFORM FIX.
           PERFORM IND.
           PERFORM IND.
           STOP RUN.
       FIX SECTION.
       F2.
           GO TO F3.
       F3.
           DISPLAY "F3".
           ALTER F2 TO PROCEED TO F4.
       F4.
           DISPLAY "F4".
       IND SECTION 50.
       I2.
           GO TO I3.
       I3.
           DISPLAY "I3".
           ALTER I2 TO PROCEED TO I4.
       I4.
           DISPLAY "I4".
