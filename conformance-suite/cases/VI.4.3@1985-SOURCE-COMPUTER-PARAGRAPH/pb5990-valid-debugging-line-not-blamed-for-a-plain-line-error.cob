      *>--- conformance-case
      *> id: pb5990-valid-debugging-line-not-blamed-for-a-plain-line-error
      *> rules: [1985-GR-VI.4.3.4-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.7.3: the D line below is valid source. The plain line
      *> after it has literals with no separator (COBOL2633, §1985:IV.4.2.1
      *> SR5); that error is the plain line's own, so only COBOL2633 is
      *> reported and the valid debugging line is not blamed with COBOL7061
      *> (kb/Work PB5990).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5990G.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. COMPUTER-1.
       OBJECT-COMPUTER. COMPUTER-1.
       PROCEDURE DIVISION.
       P1.
           DISPLAY "START".
      D    DISPLAY "OK".
           DISPLAY "A""B"'C'.
           STOP RUN.
