      *>--- conformance-case
      *> id: pb5150_global_subordinate_name_unique_beside_other_names
      *> rules: [GR-13.18.27.4-1]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5150 - the LEGAL side of the global-subordinate uniqueness rule. SUB-G is subordinate to the GLOBAL record GLB,
      *>   so it is a global name (§2023:13.18.27.4 GR1, cite.py: OK) and the contained program reads it. No other item of the
      *>   source element is spelled SUB-G; the non-global name N is spelled by two items told apart by qualification
      *>   (§2023:8.4.2.2.1). conformance:negative/pb5150-global-subordinate-name-reused-by-local-record is the refused side.
      *> DERIVATION. The outer program displays N OF L1 and N OF L2 ("A", "B"), then the contained program displays SUB-G ("S").
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5150P.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  GLB GLOBAL.
           02  SUB-G PIC X VALUE "S".
       01  L1.
           02  N PIC X VALUE "A".
       01  L2.
           02  N PIC X VALUE "B".
       PROCEDURE DIVISION.
       P-MAIN.
           DISPLAY N OF L1 N OF L2
           CALL "PB5150Q"
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5150Q.
       PROCEDURE DIVISION.
       Q-MAIN.
           DISPLAY SUB-G
           EXIT PROGRAM.
       END PROGRAM PB5150Q.
       END PROGRAM PB5150P.
