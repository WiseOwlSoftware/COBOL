      *>--- conformance-case
      *> id: 1985-SR-XV_3_2_3-6
      *> rules: [1985-SR-XV.3.2.3-6]
      *> source-format: fixed
      *>---
      *> §1985:XV.3.2.3 SR6: "Any given identifier, cd-name, file-name,
      *> or procedure-name may appear in only one USE FOR DEBUGGING
      *> statement and may appear only once in that statement." The
      *> statement below names the procedure-name P1 twice.
      *> WITH DEBUGGING MODE is specified, so the USE FOR DEBUGGING
      *> statements are compiled (§1985:XV.2.1.3 GR1). A refusal at
      *> 85 (COBOL6335): a compiler that accepts it fails (kb/Work
      *> PB5081).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. N5081S6.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. ANY-COMPUTER WITH DEBUGGING MODE.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "N5081S6".
       DATA DIVISION.
       FILE SECTION.
       FD  F1.
       01  R1 PIC X(10).
       PROCEDURE DIVISION.
       DECLARATIVES.
       D1 SECTION.
           USE FOR DEBUGGING ON P1 P1.
       D1-PARA.
           EXIT.
       END DECLARATIVES.
       MAIN-SECT SECTION.
       P1.
           STOP RUN.
