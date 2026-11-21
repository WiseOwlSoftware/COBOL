      *>--- conformance-case
      *> id: pb5924-cfg-container-only-85
      *> rules: [1985-SR-VI.4.2-p3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:VI.4.2 (cite.py: OK): "The Configuration
      *> Section must not be stated in a program which is contained
      *> directly or indirectly within another program"
      *> Only the outermost program states a Configuration Section; the
      *> contained program states none: compiles and runs.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB59242.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. WISEOWL.
       PROCEDURE DIVISION.
       P0.
           CALL "PB5924J".
           DISPLAY "OUTER".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5924J.
       PROCEDURE DIVISION.
       P1.
           DISPLAY "INNER".
           EXIT PROGRAM.
       END PROGRAM PB5924J.
       END PROGRAM PB59242.
