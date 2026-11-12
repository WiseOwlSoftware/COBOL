      *>--- conformance-case
      *> id: 1985-SR-XV_3_2_3-3
      *> rules: [1985-SR-XV.3.2.3-3]
      *> source-format: fixed
      *>---
      *> §1985:XV.3.2.3 SR3: "Statements appearing outside of the set
      *> of debugging sections must not reference procedure-names
      *> defined within the set of debugging sections." PERFORM
      *> DBG-PARA in MAIN-PARA does; the rule has no PERFORM exception.
      *> WITH DEBUGGING MODE is specified, so the USE FOR DEBUGGING
      *> statements are compiled (§1985:XV.2.1.3 GR1). A refusal at
      *> 85 (COBOL6946): a compiler that accepts it fails (kb/Work
      *> PB5081).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. N5081S3.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. ANY-COMPUTER WITH DEBUGGING MODE.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "N5081S3".
       DATA DIVISION.
       FILE SECTION.
       FD  F1.
       01  R1 PIC X(10).
       PROCEDURE DIVISION.
       DECLARATIVES.
       DBG-SECT SECTION.
           USE FOR DEBUGGING ON MAIN-PARA.
       DBG-PARA.
           EXIT.
       END DECLARATIVES.
       MAIN-SECT SECTION.
       MAIN-PARA.
           PERFORM DBG-PARA.
           STOP RUN.
