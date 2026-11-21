      *>--- conformance-case
      *> id: pb5453-blank-debugging-line-before-object-computer
      *> rules: [1985-GR-VI.4.3.4-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.7.3 p1: "Any debugging line that consists solely of
      *> spaces from margin A to margin R is considered the same as a
      *> blank line", and §1985:IV.7.2.3: "A blank line can appear
      *> anywhere in the source program." So the two debugging lines of
      *> spaces below, one bare and one padded to margin R, may stand
      *> before the OBJECT-COMPUTER paragraph, where a debugging line
      *> holding text is COBOL7060 (pb5212-debugging-line-before-object-
      *> computer). Before the fix each was refused COBOL7060.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5453B.
      D
      D                                                                 
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. COMPUTER-1 WITH DEBUGGING MODE.
       OBJECT-COMPUTER. COMPUTER-1.
       PROCEDURE DIVISION.
       P1.
           DISPLAY "BLANK DEBUG LINES ACCEPTED".
           STOP RUN.
