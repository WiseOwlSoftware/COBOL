      *>--- conformance-case
      *> id: 1985-SR-XV_3_2_3-7
      *> rules: [1985-SR-XV.3.2.3-7]
      *> source-format: fixed
      *>---
      *> §1985:XV.3.2.3 SR7: "The ALL PROCEDURES phrase can appear only
      *> once in a program." The statement below writes it twice (the
      *> format's ellipsis admits the repetition syntactically).
      *> WITH DEBUGGING MODE is specified, so the USE FOR DEBUGGING
      *> statements are compiled (§1985:XV.2.1.3 GR1). A refusal at
      *> 85 (COBOL6947): a compiler that accepts it fails (kb/Work
      *> PB5081).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. N5081S7.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. ANY-COMPUTER WITH DEBUGGING MODE.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "N5081S7".
       DATA DIVISION.
       FILE SECTION.
       FD  F1.
       01  R1 PIC X(10).
       PROCEDURE DIVISION.
       DECLARATIVES.
       D1 SECTION.
           USE FOR DEBUGGING ON ALL PROCEDURES ALL PROCEDURES.
       D1-PARA.
           EXIT.
       END DECLARATIVES.
       MAIN-SECT SECTION.
       P1.
           STOP RUN.
