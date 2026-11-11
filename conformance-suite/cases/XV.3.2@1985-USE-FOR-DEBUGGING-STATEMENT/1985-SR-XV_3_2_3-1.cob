      *>--- conformance-case
      *> id: 1985-SR-XV_3_2_3-1
      *> rules: [1985-SR-XV.3.2.3-1]
      *> source-format: fixed
      *>---
      *> §1985:XV.3.2.3 SR1: "Debugging section(s), if specified, must
      *> appear together immediately after the DECLARATIVES header."
      *> The debugging section S2 follows S1, a USE AFTER STANDARD
      *> ERROR section, so it is not immediately after the header.
      *> WITH DEBUGGING MODE is specified, so the USE FOR DEBUGGING
      *> statements are compiled (§1985:XV.2.1.3 GR1). A refusal at
      *> 85 (COBOL6948): a compiler that accepts it fails (kb/Work
      *> PB5081).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. N5081S1.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. ANY-COMPUTER WITH DEBUGGING MODE.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "N5081S1".
       DATA DIVISION.
       FILE SECTION.
       FD  F1.
       01  R1 PIC X(10).
       PROCEDURE DIVISION.
       DECLARATIVES.
       S1 SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON F1.
       S1-P.
           EXIT.
       S2 SECTION.
           USE FOR DEBUGGING ON MAIN-PARA.
       S2-P.
           EXIT.
       END DECLARATIVES.
       MAIN-SECT SECTION.
       MAIN-PARA.
           STOP RUN.
