      *>--- conformance-case
      *> id: 1985-SR-XV_3_2_3-8
      *> rules: [1985-SR-XV.3.2.3-8]
      *> source-format: fixed
      *>---
      *> §1985:XV.3.2.3 SR8: "When the ALL PROCEDURES phrase is
      *> specified, procedure-name-1 must not be specified in any USE
      *> FOR DEBUGGING statement." D1 specifies ALL PROCEDURES and D2
      *> names the procedure-name P1.
      *> WITH DEBUGGING MODE is specified, so the USE FOR DEBUGGING
      *> statements are compiled (§1985:XV.2.1.3 GR1). A refusal at
      *> 85 (COBOL6947): a compiler that accepts it fails (kb/Work
      *> PB5081).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. N5081S8.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. ANY-COMPUTER WITH DEBUGGING MODE.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "N5081S8".
       DATA DIVISION.
       FILE SECTION.
       FD  F1.
       01  R1 PIC X(10).
       PROCEDURE DIVISION.
       DECLARATIVES.
       D1 SECTION.
           USE FOR DEBUGGING ON ALL PROCEDURES.
       D1-PARA.
           EXIT.
       D2 SECTION.
           USE FOR DEBUGGING ON P1.
       D2-PARA.
           EXIT.
       END DECLARATIVES.
       MAIN-SECT SECTION.
       P1.
           STOP RUN.
