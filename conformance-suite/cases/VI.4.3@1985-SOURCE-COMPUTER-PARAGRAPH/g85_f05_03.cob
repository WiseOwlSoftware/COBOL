      *>--- conformance-case
      *> id: g85_f05_03
      *> rules: [1985-GR-VI.4.3.4-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.4.3.4 GR2 - no SOURCE-COMPUTER paragraph anywhere: the
      *> Configuration Section holds only OBJECT-COMPUTER, and the
      *> contained program G85F05C3 is within a program with no
      *> SOURCE-COMPUTER paragraph; the compiling computer is the source
      *> computer, so both translate and run here. A compiler that
      *> requires the paragraph fails to translate; one that loses the
      *> contained program prints other lines.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F0503.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. G85-OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N PIC 9 VALUE 2.
       PROCEDURE DIVISION.
       P1.
           DISPLAY "NO SOURCE-COMPUTER " N.
           CALL "G85F05C3".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F05C3.
       PROCEDURE DIVISION.
       P1.
           DISPLAY "CONTAINED".
           EXIT PROGRAM.
       END PROGRAM G85F05C3.
       END PROGRAM G85F0503.
