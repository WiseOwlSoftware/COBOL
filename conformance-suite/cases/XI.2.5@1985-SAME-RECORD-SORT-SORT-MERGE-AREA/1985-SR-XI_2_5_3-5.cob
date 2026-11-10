      *>--- conformance-case
      *> id: 1985-SR-XI_2_5_3-5
      *> rules: [1985-SR-XI.2.5.3-5]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XI.2.5.3 SR5 (THE SAME RECORD/SORT/SORT-MERGE AREA
      *> CLAUSE). A refusal: the rule forbids line 12 of the program
      *> below, and WiseOwl refuses it at 85 with COBOL2738 there; a
      *> compiler that accepts it fails. Placed from the audit's draft,
      *> confirmed by scripts/spec/place_witnesses.py (kb/Work PB4995).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. SAMESRT.
000300 ENVIRONMENT DIVISION.
000400 INPUT-OUTPUT SECTION.
000500 FILE-CONTROL.
000600     SELECT SD-A ASSIGN TO SORTWK1.
000700     SELECT SD-B ASSIGN TO SORTWK2.
000800     SELECT SD-C ASSIGN TO SORTWK3.
000900     SELECT SD-D ASSIGN TO SORTWK4.
001000 I-O-CONTROL.
001100     SAME SORT AREA FOR SD-A SD-B
001200     SAME SORT AREA FOR SD-A SD-C.
001300 DATA DIVISION.
001400 FILE SECTION.
001500 SD  SD-A.
001600 01  REC-A PIC X(10).
001700 SD  SD-B.
001800 01  REC-B PIC X(10).
001900 SD  SD-C.
002000 01  REC-C PIC X(10).
002100 SD  SD-D.
002200 01  REC-D PIC X(10).
002300 PROCEDURE DIVISION.
002400 P1.
002500     DISPLAY "OK".
002600     STOP RUN.
