      *>--- conformance-case
      *> id: 1985-SR-VII_2_12_3-2
      *> rules: [1985-SR-VII.2.12.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.2.12.3 SR2 (THE RERUN CLAUSE). A refusal: the rule
      *> forbids line 12 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL6529 there; a compiler that accepts it fails.
      *> Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. RERUNT.
000300 ENVIRONMENT DIVISION.
000400 INPUT-OUTPUT SECTION.
000500 FILE-CONTROL.
000600     SELECT CKPT ASSIGN TO DISK1.
000700     SELECT SEQF ASSIGN TO DISK2.
000800     SELECT RELF ASSIGN TO DISK3
000900         ORGANIZATION IS RELATIVE
001000         ACCESS MODE IS SEQUENTIAL.
001100 I-O-CONTROL.
001200     RERUN ON CKPT EVERY END OF REEL OF RELF.
001300 DATA DIVISION.
001400 FILE SECTION.
001500 FD  CKPT.
001600 01  CKPT-REC PIC X(80).
001700 FD  SEQF.
001800 01  SEQF-REC PIC X(80).
001900 FD  RELF.
002000 01  RELF-REC PIC X(80).
002100 PROCEDURE DIVISION.
002200 MAIN-PARA.
002300     DISPLAY "OK".
002400     STOP RUN.
