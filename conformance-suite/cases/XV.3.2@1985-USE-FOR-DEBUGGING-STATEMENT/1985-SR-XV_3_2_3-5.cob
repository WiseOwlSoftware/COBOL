      *>--- conformance-case
      *> id: 1985-SR-XV_3_2_3-5
      *> rules: [1985-SR-XV.3.2.3-5]
      *> source-format: fixed
      *>---
      *> §1985:XV.3.2.3 SR5: "Procedure-names defined within debugging
      *> sections must not appear within USE FOR DEBUGGING statements."
      *> D2's USE FOR DEBUGGING ON D1-PARA names a paragraph of the
      *> debugging section D1.
      *> WITH DEBUGGING MODE is specified, so the USE FOR DEBUGGING
      *> statements are compiled (§1985:XV.2.1.3 GR1). A refusal at
      *> 85 (COBOL6946): a compiler that accepts it fails (kb/Work
      *> PB5081).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. N5081S5.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. ANY-COMPUTER WITH DEBUGGING MODE.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "N5081S5".
       DATA DIVISION.
       FILE SECTION.
       FD  F1.
       01  R1 PIC X(10).
       PROCEDURE DIVISION.
       DECLARATIVES.
       D1 SECTION.
           USE FOR DEBUGGING ON MAIN-PARA.
       D1-PARA.
           EXIT.
       D2 SECTION.
           USE FOR DEBUGGING ON D1-PARA.
       D2-PARA.
           EXIT.
       END DECLARATIVES.
       MAIN-SECT SECTION.
       MAIN-PARA.
           STOP RUN.
